using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Heladeria.Aplicacion.Cartera;
using Heladeria.Aplicacion.Configuracion;
using Heladeria.Infraestructura.Caja;
using Heladeria.Infraestructura.Persistencia.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Heladeria.Infraestructura.Cartera;

public sealed partial class ServicioCartera
{
    private static byte[] Huella(object valor) => SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(valor));
    private static void Uuid(Guid clave)
    { if (clave == Guid.Empty) throw new ExcepcionCartera("ClaveOperacion debe ser una UUID no vacía.", "VALIDACION_CARTERA", 400); }
    private static void VersionCuenta(byte[] actual, string recibida, bool pagar)
    {
        byte[] bytes;
        try { bytes = Convert.FromBase64String(recibida); }
        catch (FormatException) { throw new ExcepcionCartera("VersionFila debe ser Base64.", "VALIDACION_CARTERA", 400); }
        if (actual.Length != 8 || !actual.AsSpan().SequenceEqual(bytes))
            throw new ExcepcionCartera("La cuenta cambió; recargue su versión.", pagar ? "CONCURRENCIA_CXP" : "CONCURRENCIA_CXC");
    }
    public Task<ResultadoPagoCarteraDto> AplicarAsync(bool porPagar, long id, AplicarPagoCarteraSolicitud solicitud,
        ContextoConfiguracion s, CancellationToken t) => EscribirAsync(s, async () =>
    {
        Validar(solicitud); Uuid(solicitud.ClaveOperacion); Importe(solicitud.Monto, "Monto aplicado");
        ServicioCaja.ReferenciaSegura(solicitud.Referencia);
        var huella = Huella(new { porPagar, id, s.EmpresaId, s.SucursalId, s.UsuarioId, solicitud.SesionCajaId,
            solicitud.FormaPagoId, Monto = solicitud.Monto.ToString("G29", CultureInfo.InvariantCulture),
            Referencia = Opcional(solicitud.Referencia), Observacion = Opcional(solicitud.Observacion), solicitud.VersionFila, solicitud.VersionCaja });
        var actual = await LeerAsync(porPagar, id, s, t);
        var previo = await Contexto.Pagos.AsNoTracking().SingleOrDefaultAsync(x => x.EmpresaId == s.EmpresaId && x.ClaveOperacion == solicitud.ClaveOperacion, t);
        if (previo is not null)
        {
            if (previo.HuellaSolicitud is null || !CryptographicOperations.FixedTimeEquals(previo.HuellaSolicitud, huella)
                || !actual.Historial.Any(x => x.PagoId == previo.Id))
                throw new ExcepcionCartera("La UUID ya pertenece a otra solicitud financiera.", "CLAVE_OPERACION_CONFLICTIVA");
            return new ResultadoPagoCarteraDto(previo.Id, actual, true);
        }
        var c = actual.Encabezado;
        if (!c.Operable) throw new ExcepcionCartera("Cuenta histórica: solo consulta hasta conciliación explícita.");
        if (c.Estado == "PAGADA" || c.SaldoPendiente == 0) throw new ExcepcionCartera("La cuenta ya está pagada.", "CUENTA_YA_PAGADA");
        if (c.Estado is not ("PENDIENTE" or "PARCIAL")) throw new ExcepcionCartera("La cuenta no admite aplicaciones.");
        VersionCuenta(Convert.FromBase64String(c.VersionFila), solicitud.VersionFila, porPagar);
        if (solicitud.Monto > c.SaldoPendiente)
            throw new ExcepcionCartera("El importe excede el saldo pendiente.", porPagar ? "PAGO_SUPERA_SALDO" : "ABONO_SUPERA_SALDO");
        var turno = await caja.AbiertaAsync(solicitud.SesionCajaId, solicitud.VersionCaja, s, t);
        var libro = await caja.LeerSesionAsync(turno.Id, s, t);
        if (turno.MonedaId != c.MonedaId) throw new ExcepcionCartera("La moneda de la cuenta y Caja no coinciden.");
        if (porPagar && solicitud.Monto > libro.EfectivoEsperado)
            throw new ExcepcionCartera("Caja no dispone de efectivo suficiente.", "SALDO_INSUFICIENTE");
        var forma = (await FormasAsync(s, t)).SingleOrDefault(x => x.Id == solicitud.FormaPagoId
            && (x.Codigo == "EFECTIVO" || !porPagar && x.Codigo == "TARJETA"))
            ?? throw new ExcepcionCartera("Forma de pago no admitida para esta operación.", "FORMA_PAGO_INVALIDA", 400);
        if (forma.RequiereReferencia && Opcional(solicitud.Referencia) is null)
            throw new ExcepcionCartera("La forma de pago requiere una referencia segura.", "REFERENCIA_REQUERIDA", 400);
        // Verificar el origen y el tercero de nuevo dentro de la reserva compartida con Ventas/Compras/Caja.
        var origenValido = porPagar
            ? await Contexto.FacturasProveedores.AnyAsync(x => x.Id == c.DocumentoOrigenId && x.EmpresaId == s.EmpresaId
                && x.ProveedorId == c.TerceroId && x.Proveedor.EmpresaId == s.EmpresaId && x.Proveedor.Estado.Activo
                && Operativos.Contains(x.Proveedor.Estado.Nombre) && x.Estado.Activo && Operativos.Contains(x.Estado.Nombre)
                && x.OrdenCompra != null && x.OrdenCompra.EmpresaId == s.EmpresaId && x.OrdenCompra.EsCredito == true
                && x.OrdenCompra.EstadosFlujo.FlujoEstado.Codigo == "ORDENES_COMPRA" && x.OrdenCompra.EstadosFlujo.Codigo == "RECIBIDA"
                && x.OrdenCompra.SucursalId == s.SucursalId && x.Total == c.ImporteOriginal && x.MonedaId == c.MonedaId && x.TipoCambio == 1, t)
            : await Contexto.Ventas.AnyAsync(x => x.Id == c.DocumentoOrigenId && x.EmpresaId == s.EmpresaId && x.SucursalId == s.SucursalId
                && x.EsCredito && x.ClienteId == c.TerceroId && x.Cliente!.EmpresaId == s.EmpresaId && x.Cliente.Estado.Activo
                && Operativos.Contains(x.Cliente.Estado.Nombre) && x.EstadosFlujo.FlujoEstado.Codigo == "VENTA_POS"
                && x.EstadosFlujo.Codigo == "COMPLETADA" && x.FechaAnulacion == null && x.FechaCompletada != null
                && x.TotalVenta == c.ImporteOriginal && x.MonedaId == c.MonedaId && x.TipoCambio == 1, t);
        if (!origenValido) throw new ExcepcionCartera("El documento origen o tercero dejó de ser válido; concilie antes de aplicar pagos.");
        var vigente = await ReglasCartera.EstadoAsync(Contexto, porPagar, c.Estado, Operativos, t);
        var saldo = c.ImporteOriginal - c.TotalAplicado - solicitud.Monto;
        var siguiente = await ReglasCartera.EstadoAsync(Contexto, porPagar, saldo == 0 ? "PAGADA" : "PARCIAL", Operativos, t);
        var concepto = porPagar ? ReglasCartera.Pago : ReglasCartera.Cobro;
        var pago = new Pagos { EmpresaId = s.EmpresaId, SucursalId = s.SucursalId, ClienteId = porPagar ? null : c.TerceroId,
            ProveedorId = porPagar ? c.TerceroId : null, Fecha = Ahora, MonedaId = c.MonedaId, TipoCambio = 1,
            MontoTotal = solicitud.Monto, TipoPagoOperacionId = await TipoAsync("OPERACIONES_PAGO_CAJA", concepto, t),
            TurnoCajaId = turno.Id, UsuarioId = s.UsuarioId, EstadoId = await ActivoAsync(t),
            ClaveOperacion = solicitud.ClaveOperacion, HuellaSolicitud = huella, Observacion = Opcional(solicitud.Observacion) };
        var detalle = new PagoDetalles { FormaPagoId = forma.Id, Monto = solicitud.Monto, MontoRecibido = solicitud.Monto,
            EsEfectivo = forma.Codigo == "EFECTIVO", Referencia = Opcional(solicitud.Referencia) };
        pago.PagoDetalles.Add(detalle); Contexto.Pagos.Add(pago);
        if (porPagar)
        {
            var cuenta = await Contexto.CuentasPorPagar.SingleAsync(x => x.Id == id, t);
            if (cuenta.EstadoId != vigente.Id || cuenta.FlujoEstadoId != vigente.FlujoEstadoId) throw new ExcepcionCartera("Flujo financiero incompatible.");
            cuenta.SaldoPendiente = saldo; cuenta.EstadoId = siguiente.Id; cuenta.FechaModificacion = Ahora; cuenta.UsuarioModificacionId = s.UsuarioId;
            Contexto.AplicacionesCuentasPorPagar.Add(new() { CuentaPorPagarId = id, Pago = pago,
                FechaAplicacion = Ahora, MontoAplicado = solicitud.Monto, UsuarioId = s.UsuarioId });
        }
        else
        {
            var cuenta = await Contexto.CuentasPorCobrar.SingleAsync(x => x.Id == id, t);
            if (cuenta.EstadoId != vigente.Id || cuenta.FlujoEstadoId != vigente.FlujoEstadoId) throw new ExcepcionCartera("Flujo financiero incompatible.");
            cuenta.SaldoPendiente = saldo; cuenta.EstadoId = siguiente.Id; cuenta.FechaModificacion = Ahora; cuenta.UsuarioModificacionId = s.UsuarioId;
            Contexto.AplicacionesCuentasPorCobrar.Add(new() { CuentaPorCobrarId = id, Pago = pago,
                FechaAplicacion = Ahora, MontoAplicado = solicitud.Monto, UsuarioId = s.UsuarioId });
        }
        await caja.AgregarMovimientoAsync(turno, concepto, solicitud.Monto, forma.Id, detalle.EsEfectivo.Value,
            $"Cuenta {id}; {(porPagar ? "proveedor" : "cliente")} {c.TerceroId}; origen {c.DocumentoOrigenId}.", c.NumeroDocumentoOrigen,
            detalle, s, t);
        caja.Tocar(turno);
        await Contexto.SaveChangesAsync(t);
        ReglasCartera.Auditar(Contexto, porPagar, id, porPagar ? "PAGO_APLICADO" : "ABONO_APLICADO",
            $"Pago {pago.Id}; aplicado {solicitud.Monto}; saldo {saldo}; {c.Estado} → {siguiente.Codigo}.", s, Ahora);
        await Contexto.SaveChangesAsync(t);
        await caja.LeerSesionAsync(turno.Id, s, t); // Validación contable del libro antes del commit.
        return new ResultadoPagoCarteraDto(pago.Id, await LeerAsync(porPagar, id, s, t), false);
    }, t, "OPERACION_FINANCIERA_RECHAZADA");

    public Task<ResultadoFacturaCreditoDto> RegistrarFacturaAsync(RegistrarFacturaCreditoSolicitud solicitud,
        ContextoConfiguracion s, CancellationToken t) => EscribirAsync(s, async () =>
    {
        Validar(solicitud); Uuid(solicitud.ClaveOperacion);
        var serie = Opcional(solicitud.Serie); var numero = solicitud.NumeroDocumento.Trim();
        if (numero.Length == 0 || solicitud.FechaDocumento == default || solicitud.FechaDocumento > Hoy(s)
            || solicitud.FechaVencimiento < solicitud.FechaDocumento)
            throw new ExcepcionCartera("Revise número, emisión y vencimiento de la factura real.", "VALIDACION_CXP", 400);
        var huella = Huella(new { solicitud.OrdenCompraId, s.EmpresaId, s.SucursalId, s.UsuarioId, serie, numero,
            solicitud.FechaDocumento, solicitud.FechaVencimiento, solicitud.VersionCompra });
        var orden = await Contexto.OrdenesCompra.Include(x => x.EstadosFlujo).ThenInclude(x => x.FlujoEstado)
            .Include(x => x.EstadosFlujo).ThenInclude(x => x.Estado)
            .Include(x => x.Proveedor).ThenInclude(x => x.Estado)
            .SingleOrDefaultAsync(x => x.Id == solicitud.OrdenCompraId && x.EmpresaId == s.EmpresaId && x.SucursalId == s.SucursalId, t)
            ?? throw new ExcepcionCartera("Compra no encontrada en la sucursal autorizada.", "COMPRA_NO_ENCONTRADA", 404);
        var previo = await Contexto.FacturasProveedores.AsNoTracking().Include(x => x.CuentasPorPagar)
            .SingleOrDefaultAsync(x => x.EmpresaId == s.EmpresaId && x.ClaveOperacion == solicitud.ClaveOperacion, t);
        if (previo is not null)
        {
            if (previo.HuellaSolicitud is null || !CryptographicOperations.FixedTimeEquals(previo.HuellaSolicitud, huella) || previo.CuentasPorPagar is null)
                throw new ExcepcionCartera("La UUID ya corresponde a otra factura.", "CLAVE_OPERACION_CONFLICTIVA");
            return new ResultadoFacturaCreditoDto(previo.Id, await LeerAsync(true, previo.CuentasPorPagar.Id, s, t), true);
        }
        VersionCuenta(orden.VersionFila, solicitud.VersionCompra, true);
        if (orden.EsCredito != true || orden.EstadosFlujo.FlujoEstado.Codigo != "ORDENES_COMPRA" || orden.EstadosFlujo.Codigo != "RECIBIDA"
            || !orden.EstadosFlujo.Estado.Activo || !Operativos.Contains(orden.EstadosFlujo.Estado.Nombre))
            throw new ExcepcionCartera("Solo una compra explícitamente a crédito y totalmente recibida admite esta factura.");
        if (orden.Proveedor.EmpresaId != s.EmpresaId || !orden.Proveedor.Estado.Activo || !Operativos.Contains(orden.Proveedor.Estado.Nombre))
            throw new ExcepcionCartera("Proveedor no operativo o ajeno a la empresa.");
        if (await Contexto.FacturasProveedores.AnyAsync(x => x.OrdenCompraId == orden.Id
            || x.EmpresaId == s.EmpresaId && x.ProveedorId == orden.ProveedorId && x.Serie == serie && x.NumeroDocumento == numero, t))
            throw new ExcepcionCartera("Ya existe una factura para la compra o el documento indicado.", "DOCUMENTO_FINANCIERO_DUPLICADO");
        Importe(orden.TotalEstimado, "Importe factura");
        if (orden.TotalEstimado != orden.SubTotal + orden.Impuestos || orden.TipoCambio != 1 || orden.MonedaId != (await MonedaAsync(s, t)).Id)
            throw new ExcepcionCartera("Importes o moneda de la compra incompatibles con esta fase.");
        var cantidades = await Contexto.OrdenesCompraDetalle.Where(x => x.OrdenCompraId == orden.Id)
            .Select(x => new { x.Cantidad, Recibidas = Contexto.RecepcionCompraDetalles.Where(r => r.OrdenCompraDetalleId == x.Id && r.RecepcionCompra.EmpresaId == s.EmpresaId
                && r.RecepcionCompra.SucursalId == s.SucursalId && r.RecepcionCompra.OrdenCompraId == orden.Id
                && r.RecepcionCompra.EstadosFlujo.FlujoEstado.Codigo == "RECEPCIONES_COMPRA" && r.RecepcionCompra.EstadosFlujo.Codigo == "APLICADA")
                .Select(r => r.CantidadRecibida).ToArray() }).ToArrayAsync(t);
        if (cantidades.Length == 0 || cantidades.Any(x => x.Cantidad != x.Recibidas.Sum()))
            throw new ExcepcionCartera("Las recepciones no concilian con el total de la compra.");
        var estado = await ReglasCartera.EstadoAsync(Contexto, true, "PENDIENTE", Operativos, t);
        var factura = new FacturasProveedores { EmpresaId = s.EmpresaId, ProveedorId = orden.ProveedorId, OrdenCompraId = orden.Id,
            Serie = serie, NumeroDocumento = numero, FechaDocumento = solicitud.FechaDocumento, FechaVencimiento = solicitud.FechaVencimiento,
            MonedaId = orden.MonedaId, TipoCambio = orden.TipoCambio, SubTotal = orden.SubTotal, Impuestos = orden.Impuestos,
            Total = orden.TotalEstimado, EstadoId = await ActivoAsync(t), ClaveOperacion = solicitud.ClaveOperacion, HuellaSolicitud = huella };
        var cuenta = new CuentasPorPagar { EmpresaId = s.EmpresaId, SucursalId = s.SucursalId, ProveedorId = orden.ProveedorId,
            FacturaProveedor = factura, MonedaId = orden.MonedaId, FechaEmision = solicitud.FechaDocumento,
            FechaVencimiento = solicitud.FechaVencimiento, MontoOriginal = factura.Total, SaldoPendiente = factura.Total,
            FlujoEstadoId = estado.FlujoEstadoId, EstadoId = estado.Id, UsuarioCreacionId = s.UsuarioId, FechaCreacion = Ahora };
        Contexto.CuentasPorPagar.Add(cuenta); await Contexto.SaveChangesAsync(t);
        ReglasCartera.Auditar(Contexto, true, cuenta.Id, "CUENTA_GENERADA", $"Factura {factura.Id}; compra {orden.Id}; saldo {factura.Total}.", s, Ahora);
        await Contexto.SaveChangesAsync(t);
        return new ResultadoFacturaCreditoDto(factura.Id, await LeerAsync(true, cuenta.Id, s, t), false);
    }, t, "FACTURA_CREDITO_RECHAZADA");
}
