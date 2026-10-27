using Heladeria.Aplicacion.Configuracion;
using Heladeria.Aplicacion.Cartera;
using Heladeria.Aplicacion.Caja;
using Heladeria.Infraestructura.Cartera;
using Heladeria.Aplicacion.Inventario;
using Heladeria.Aplicacion.Seguridad.Autorizacion;
using Heladeria.Aplicacion.Ventas;
using Heladeria.Infraestructura.Inventario;
using Heladeria.Infraestructura.Persistencia.Entidades;
using Microsoft.EntityFrameworkCore;
using VentaEntidad = Heladeria.Infraestructura.Persistencia.Entidades.Ventas;

namespace Heladeria.Infraestructura.VentasPos;

public sealed partial class ServicioVentas
{
    public Task<VentaConsultaDto> CrearAsync(CrearVentaSolicitud solicitud, ContextoConfiguracion s, CancellationToken t) =>
        EscribirAsync(s, async () =>
        {
            var lineas = await PrepararAsync(solicitud, s, t);
            var estado = await EstadoAsync("BORRADOR", t);
            var numero = await DocumentoAsync("VT", s, t);
            var venta = new VentaEntidad { EmpresaId = s.EmpresaId, SucursalId = s.SucursalId,
                NumeroFactura = numero.Texto, FechaVenta = Ahora, FlujoEstadoId = estado.FlujoEstadoId,
                EstadoId = estado.Id, UsuarioCreacionId = s.UsuarioId, TipoCambio = 1, EsCredito = false };
            Asignar(venta, solicitud, lineas);
            Contexto.Ventas.Add(venta);
            await Contexto.SaveChangesAsync(t);
            Auditar(s, venta.Id, "VENTA_CREADA", $"Borrador {venta.NumeroFactura}.");
            await Contexto.SaveChangesAsync(t);
            return await ObtenerAsync(venta.Id, s, t);
        }, t);

    public Task<VentaConsultaDto> ActualizarAsync(long id, ActualizarVentaSolicitud solicitud, ContextoConfiguracion s, CancellationToken t) =>
        EscribirAsync(s, async () =>
        {
            Validar(solicitud);
            var venta = await CargarAsync(id, s, t);
            Version(venta.VersionFila, solicitud.VersionFila);
            ExigirEstado(venta, "BORRADOR");
            var nuevas = await PrepararAsync(solicitud, s, t, venta.EsCredito, venta.FechaVencimientoCredito);
            Contexto.VentaDetalleImpuestos.RemoveRange(venta.VentasDetalle.SelectMany(x => x.ImpuestosSnapshot));
            Contexto.VentasDetalle.RemoveRange(venta.VentasDetalle);
            Contexto.VentaImpuestos.RemoveRange(venta.VentaImpuestos);
            await Contexto.SaveChangesAsync(t); // Liberar claves únicas de línea antes de reemplazar el detalle.
            venta.VentasDetalle.Clear();
            venta.VentaImpuestos.Clear();
            Asignar(venta, solicitud, nuevas);
            Modificada(venta, s);
            Auditar(s, id, "VENTA_MODIFICADA", "Se recalcularon precios, conversiones e impuestos en backend.");
            await Contexto.SaveChangesAsync(t);
            return await ObtenerAsync(id, s, t);
        }, t);

    public Task<VentaConsultaDto> ConfirmarAsync(long id, ConfirmarVentaSolicitud solicitud, ContextoConfiguracion s, CancellationToken t) =>
        EscribirAsync(s, async () =>
        {
            Validar(solicitud);
            var venta = await CargarAsync(id, s, t);
            Version(venta.VersionFila, solicitud.VersionFila);
            ExigirEstado(venta, "BORRADOR");
            await ValidarSnapshotAsync(venta, s, t);
            await TransicionarAsync(venta, "CONFIRMADA", PermisosSistema.VentasAdministrar, t);
            Modificada(venta, s);
            Auditar(s, id, "VENTA_CONFIRMADA", "Confirmada sin reservar ni descontar existencias.");
            await Contexto.SaveChangesAsync(t);
            return await ObtenerAsync(id, s, t);
        }, t);

    public Task<VentaConsultaDto> CompletarAsync(long id, CompletarVentaSolicitud solicitud, ContextoConfiguracion s, CancellationToken t) =>
        EscribirAsync(s, async () =>
        {
            Validar(solicitud);
            if (solicitud.ClaveOperacion == Guid.Empty) throw new ValidacionVentasException("ClaveOperacion debe ser un UUID no vacío.");
            var venta = await CargarAsync(id, s, t);
            // El reintento exacto se resuelve ANTES de comprobar una versión que ya quedó obsoleta al completar.
            if (venta.EstadosFlujo.Codigo == "COMPLETADA" && venta.ClaveOperacion == solicitud.ClaveOperacion)
            {
                if ((venta.EsCredito || venta.TotalVenta == 0) && solicitud.Cobro is not null)
                    throw new ConflictoVentasException("La operación aplicada no incluyó un cobro de contado.");
                if (!venta.EsCredito && venta.TotalVenta > 0)
                {
                    if (solicitud.Cobro is null || !await Contexto.Pagos.AnyAsync(x => x.VentaId == id
                        && x.ClaveOperacion == solicitud.ClaveOperacion, t))
                        throw new ConflictoVentasException("El reintento debe corresponder al mismo cobro aplicado.");
                    await CobrarCierreAsync(venta, solicitud, s, t);
                }
                return await ObtenerAsync(id, s, t);
            }
            ExigirEstado(venta, "CONFIRMADA");
            Version(venta.VersionFila, solicitud.VersionFila);
            if (await Contexto.Ventas.AnyAsync(x => x.EmpresaId == s.EmpresaId && x.Id != id
                && x.ClaveOperacion == solicitud.ClaveOperacion, t))
                throw new ConflictoVentasException("La clave de operación ya pertenece a otra venta.");
            await ValidarSnapshotAsync(venta, s, t);
            await ValidarConceptoAsync("VENTA-SALIDA", false, s, t);
            var consumos = await PrepararConsumosAsync(venta, s, t);
            if (venta.EsCredito && solicitud.Cobro is not null)
                throw new ValidacionVentasException("La venta a crédito genera CxC; no admite un cobro de contado al completar.");
            if (venta.TotalVenta == 0 && solicitud.Cobro is not null)
                throw new ValidacionVentasException("La venta no tiene importe a cobrar; no admite un pago ni movimiento de Caja.");
            if (!venta.EsCredito && venta.TotalVenta > 0 && solicitud.Cobro is null)
                throw new ExcepcionCaja("Seleccione una sesión abierta y registre las formas de pago para completar la venta de contado.", "COBRO_REQUERIDO");
            if (solicitud.Cobro is { } cobro)
            {
                Validar(cobro);
                await _caja.AbiertaAsync(cobro.SesionCajaId, cobro.VersionCaja, s, t);
            }
            // Toda validación de stock/costo terminó antes del primer movimiento.
            foreach (var consumo in consumos)
            {
                var linea = consumo.Linea;
                var doc = await DocumentoAsync("VS", s, t);
                var movimiento = await Inventario.AplicarMovimientoAsync(new AjusteInventarioSolicitud {
                    SucursalId = s.SucursalId, BodegaId = venta.BodegaId!.Value, UbicacionId = venta.UbicacionId!.Value,
                    ProductoId = consumo.ProductoId, ProductoPresentacionId = consumo.PresentacionId,
                    Cantidad = consumo.Cantidad, Tipo = "SALIDA", Referencia = venta.NumeroFactura,
                    Observaciones = $"Venta {id}, línea {linea.NumeroLinea}, producto vendido {linea.ProductoId}, componente {consumo.ProductoId}; operación {solicitud.ClaveOperacion}."
                }, s, UnidadCostoInventario.UnidadBase, t, new(id, doc.Serie, doc.Numero, "VENTAS",
                    "VENTA-SALIDA", "VENTA_INVENTARIO_DESCONTADO", "VENTA", linea.Id, consumo.UnidadBaseId));
                if (movimiento.CantidadBase != consumo.CantidadBase || movimiento.CostoUnitario != consumo.CostoUnitario)
                    throw new ConflictoVentasException("Cambió la conversión o el costo durante el cierre. Operación revertida.");
            }
            await TransicionarAsync(venta, "COMPLETADA", PermisosSistema.VentasVender, t);
            venta.ClaveOperacion = solicitud.ClaveOperacion;
            venta.FechaCompletada = Ahora;
            venta.CompletadoPorUsuarioId = s.UsuarioId;
            await ReglasCartera.GenerarDesdeVentaAsync(Contexto, venta, s, ConfiguracionTiempo, Ahora, t);
            Modificada(venta, s);
            Auditar(s, id, "VENTA_COMPLETADA", $"Cierre atómico; operación {solicitud.ClaveOperacion}.");
            await Contexto.SaveChangesAsync(t);
            if (!venta.EsCredito && venta.TotalVenta > 0) await CobrarCierreAsync(venta, solicitud, s, t);
            return await ObtenerAsync(id, s, t);
        }, t);

    private async Task CobrarCierreAsync(VentaEntidad venta, CompletarVentaSolicitud solicitud, ContextoConfiguracion s, CancellationToken t)
    {
        var cobro = solicitud.Cobro!;
        Validar(cobro);
        // La fila ya se guardó, pero NO se ha hecho COMMIT. SQL puede haber generado una nueva rowversion.
        var version = await Contexto.Ventas.AsNoTracking().Where(x => x.Id == venta.Id).Select(x => x.VersionFila).SingleAsync(t);
        await _caja.CobrarEnTransaccionAsync(cobro.SesionCajaId, new()
        {
            VentaId = venta.Id, VersionVenta = Convert.ToBase64String(version), VersionFila = cobro.VersionCaja,
            ClaveOperacion = solicitud.ClaveOperacion, Pagos = cobro.Pagos
        }, s, t);
    }

    public Task<VentaConsultaDto> AnularAsync(long id, AnularVentaSolicitud solicitud, ContextoConfiguracion s, CancellationToken t) =>
        EscribirAsync(s, async () =>
        {
            Validar(solicitud);
            var motivo = solicitud.Motivo.Trim();
            if (motivo.Length < 3) throw new ValidacionVentasException("Indique un motivo de anulación válido.");
            var venta = await CargarAsync(id, s, t);
            Version(venta.VersionFila, solicitud.VersionFila);
            if (venta.EstadosFlujo.Codigo == "ANULADA") throw new ConflictoVentasException("La venta ya fue anulada.");
            if (await Contexto.CuentasPorCobrar.AnyAsync(x => x.VentaId == id, t))
                throw new ExcepcionCartera("La venta tiene una cuenta financiera. Su anulación requiere conciliación y reversión fuera de esta fase.", "OPERACION_FINANCIERA_EXISTENTE");
            if (await Contexto.Pagos.AnyAsync(x => x.VentaId == id, t))
                throw new ConflictoVentasException("La venta está cobrada. No puede anularse sin una reversión financiera controlada, fuera del alcance de esta fase.");
            if (venta.EstadosFlujo.Codigo == "COMPLETADA")
            {
                await ValidarAlmacenAsync(venta.BodegaId!.Value, venta.UbicacionId, s, t);
                if (await Contexto.DevolucionesVentas.AnyAsync(x => x.VentaId == id, t)
                    || await Contexto.NotasCredito.AnyAsync(x => x.VentaId == id, t)
                    || await Contexto.NotasDebito.AnyAsync(x => x.VentaId == id, t)
                    || await Contexto.CuentasPorCobrar.AnyAsync(x => x.VentaId == id, t))
                    throw new ConflictoVentasException("La venta posee devoluciones, notas o cuentas por cobrar; requiere conciliación antes de anular.");
                await ValidarConceptoAsync("VENTA-ANULACION-ENTRADA", true, s, t);
                await RevertirCompuestosAsync(venta, motivo, s, t);
                foreach (var linea in venta.VentasDetalle.Where(x => x.ManejaExistencia == true && x.EsCompuesto != true)
                    .OrderBy(x => x.ProductoId).ThenBy(x => x.NumeroLinea))
                {
                    await BloquearProductoAsync(linea.ProductoId, s, t);
                    if (!linea.CostoUnitario.HasValue || !await Contexto.Productos.AnyAsync(x => x.Id == linea.ProductoId
                        && x.EmpresaId == s.EmpresaId && x.UnidadBaseId == linea.UnidadBaseId, t))
                        throw new ConflictoVentasException("La unidad base histórica o el costo ya no permite una reversión segura.");
                    var doc = await DocumentoAsync("VA", s, t);
                    var observacion = $"Anulación venta {id}, línea {linea.NumeroLinea}: {motivo}";
                    await Inventario.AplicarMovimientoAsync(new AjusteInventarioSolicitud {
                        SucursalId = s.SucursalId, BodegaId = venta.BodegaId.Value, UbicacionId = venta.UbicacionId!.Value,
                        ProductoId = linea.ProductoId, Cantidad = linea.CantidadBase!.Value,
                        CostoUnitario = linea.CostoUnitario.Value, Tipo = "ENTRADA", Referencia = venta.NumeroFactura,
                        Observaciones = observacion[..Math.Min(500, observacion.Length)]
                    }, s, UnidadCostoInventario.UnidadBase, t, new(id, doc.Serie, doc.Numero, "VENTAS",
                        "VENTA-ANULACION-ENTRADA", "VENTA_INVENTARIO_REVERTIDO", "ANULACION_VENTA"));
                }
            }
            await TransicionarAsync(venta, "ANULADA", PermisosSistema.VentasAnular, t);
            venta.FechaAnulacion = Ahora;
            venta.AnuladoPorUsuarioId = s.UsuarioId;
            venta.MotivoAnulacion = motivo;
            Modificada(venta, s);
            Auditar(s, id, "VENTA_ANULADA", motivo);
            await Contexto.SaveChangesAsync(t);
            return await ObtenerAsync(id, s, t);
        }, t);

    private async Task BloquearProductoAsync(long producto, ContextoConfiguracion s, CancellationToken t)
    {
        if (Contexto.Database.IsSqlServer())
            await Contexto.Database.ExecuteSqlInterpolatedAsync($"""
                DECLARE @r int;
                EXEC @r = sys.sp_getapplock @Resource = {"Heladeria.Inventario.Producto." + s.EmpresaId + "." + producto},
                    @LockMode=N'Exclusive', @LockOwner=N'Transaction', @LockTimeout=10000;
                IF @r < 0 THROW 51002, 'Producto ocupado.', 1;
                """, t);
    }

    private async Task ValidarConceptoAsync(string codigo, bool afectaCosto, ContextoConfiguracion s, CancellationToken t)
    {
        if (!await Contexto.ConceptosMovimientos.AsNoTracking().AnyAsync(x => x.EmpresaId == s.EmpresaId
            && x.Codigo == codigo && x.AfectaCosto == afectaCosto && x.EsAutomatico && x.RequiereReferencia
            && x.TipoOperacion == (afectaCosto ? "ENTRADA" : "SALIDA") && x.Estado.Activo && Operativos.Contains(x.Estado.Nombre), t))
            throw new ConflictoVentasException("El concepto de venta no está preparado o tiene una política de costos incompatible.");
    }

    private static void AsignarCosto(VentasDetalle l, decimal costo)
    {
        Precision(costo, "Costo unitario base", false);
        decimal total;
        try { total = decimal.Round(l.CantidadBase!.Value * costo, 6, MidpointRounding.AwayFromZero); }
        catch (OverflowException) { throw new ValidacionVentasException("Costo total fuera de rango."); }
        Precision(total, "Costo total", false);
        l.CostoUnitario = costo;
        l.CostoTotal = total;
    }

    private void Modificada(VentaEntidad v, ContextoConfiguracion s)
    { v.FechaModificacion = Ahora; v.ModificadoPorUsuarioId = s.UsuarioId; }

    private static void ExigirEstado(VentaEntidad v, string codigo)
    {
        if (v.EstadosFlujo.Codigo != codigo) throw new ConflictoVentasException($"La venta debe estar en {codigo}.");
    }

    private async Task<VentaEntidad> CargarAsync(long id, ContextoConfiguracion s, CancellationToken t)
    {
        var v = await VentasVisibles(s).Include(x => x.EstadosFlujo).ThenInclude(x => x.FlujoEstado)
            .Include(x => x.VentaImpuestos).Include(x => x.VentasDetalle).ThenInclude(x => x.ImpuestosSnapshot)
            .SingleOrDefaultAsync(x => x.Id == id, t) ?? throw new VentaNoEncontradaException("Venta no encontrada en la sucursal activa.");
        if (v.TurnoCajaId.HasValue || v.PedidoVentaId.HasValue)
            throw new ConflictoVentasException("La venta pertenece a crédito, Caja o pedidos; no puede operarse mediante este POS.");
        if (v.EstadosFlujo.FlujoEstado.Codigo != FlujoVenta || !v.BodegaId.HasValue || !v.UbicacionId.HasValue
            || v.VentasDetalle.Count == 0 || v.VentasDetalle.Any(x => !x.FactorConversion.HasValue || !x.CantidadBase.HasValue
                || !x.UnidadMedidaId.HasValue || !x.UnidadBaseId.HasValue || !x.ManejaExistencia.HasValue || !x.TotalLinea.HasValue))
            throw new ConflictoVentasException("Venta histórica sin snapshots Fase 7: solo consulta; no se infieren conversiones ni costos.");
        return v;
    }

    private async Task<List<VentasDetalle>> PrepararAsync(GuardarVentaSolicitud solicitud, ContextoConfiguracion s, CancellationToken t,
        bool creditoActual = false, DateOnly? vencimientoActual = null)
    {
        Validar(solicitud);
        var credito = solicitud.EsCredito ?? creditoActual;
        var vencimiento = solicitud.FechaVencimientoCredito ?? vencimientoActual;
        if (credito && !solicitud.ClienteId.HasValue)
            throw new ExcepcionCartera("El crédito requiere un cliente identificado.", "CLIENTE_REQUERIDO_PARA_CREDITO", 400);
        if (credito && (vencimiento is null || vencimiento < ReglasCartera.Hoy(Ahora, ConfiguracionTiempo, s))
            || !credito && solicitud.FechaVencimientoCredito is not null)
            throw new ExcepcionCartera("Revise la modalidad y la fecha de vencimiento del crédito.", "VALIDACION_CXC", 400);
        if (solicitud.SucursalId != s.SucursalId) throw new VentaNoEncontradaException("Sucursal fuera del contexto activo.");
        await ValidarAlmacenAsync(solicitud.BodegaId, solicitud.UbicacionId, s, t);
        var moneda = await MonedaBaseAsync(s, t);
        if (solicitud.MonedaId != moneda.Id) throw new ValidacionVentasException("Ventas POS solo admite la moneda base operativa, con tipo de cambio 1.");
        if (solicitud.ClienteId.HasValue && !await Contexto.Clientes.AnyAsync(x => x.Id == solicitud.ClienteId
            && x.EmpresaId == s.EmpresaId && x.Estado.Activo && Operativos.Contains(x.Estado.Nombre), t))
            throw new VentaNoEncontradaException("Cliente no disponible en esta empresa.");
        if (solicitud.Detalles.Any(x => x is null)) throw new ValidacionVentasException("Detalle de venta inválido.");
        if (solicitud.Detalles.GroupBy(x => (x.ProductoId, x.ProductoPresentacionId)).Any(x => x.Count() > 1))
            throw new ValidacionVentasException("No repita el mismo producto y presentación; combine la cantidad en una línea.");
        var resultado = new List<VentasDetalle>();
        // Orden determinista de lecturas de productos, sin cambiar la numeración elegida por el cliente.
        foreach (var (linea, indice) in solicitud.Detalles.Select((x, i) => (x, i)).OrderBy(x => x.x.ProductoId).ThenBy(x => x.i))
        {
            Validar(linea);
            Precision(linea.Cantidad, "Cantidad");
            var p = await ProductosVendibles(s).SingleOrDefaultAsync(x => x.Id == linea.ProductoId, t)
                ?? throw new ValidacionVentasException("Producto no vendible, inactivo, ajeno o no soportado (combo sin composición/lote/serie/valoración).");
            var dto = await ProductoDtoAsync(p, moneda.CodigoISO, null, null, s, t);
            var presentacion = dto.Presentaciones.SingleOrDefault(x => x.ProductoPresentacionId == linea.ProductoPresentacionId)
                ?? throw new ValidacionVentasException("Presentación inactiva, ajena o no permitida para venta.");
            if (!presentacion.PrecioValido) throw new ValidacionVentasException("Configure un precio de venta positivo para la unidad/presentación elegida.");
            var conversion = await Inventario.ConvertirCantidadAsync(p.Id, linea.ProductoPresentacionId, linea.Cantidad, s.EmpresaId, t);
            var calculo = CalculadoraVenta.Calcular(linea.Cantidad, presentacion.Precio!.Value,
                linea.PorcentajeDescuento, p.PermiteDescuento, dto.Impuestos);
            resultado.Add(new VentasDetalle { NumeroLinea = indice + 1, ProductoId = p.Id,
                ProductoPresentacionId = linea.ProductoPresentacionId, Cantidad = linea.Cantidad,
                FactorConversion = conversion.Factor, CantidadBase = conversion.CantidadBase,
                UnidadMedidaId = conversion.UnidadEntradaId, UnidadBaseId = p.UnidadBaseId,
                ManejaExistencia = dto.ManejaExistencia, PrecioUnitario = presentacion.Precio.Value,
                EsCompuesto = await Contexto.CombosDetalle.AnyAsync(x => x.EmpresaId == s.EmpresaId && x.ComboProductoId == p.Id, t),
                FuentePrecio = presentacion.FuentePrecio, PorcentajeDescuento = linea.PorcentajeDescuento,
                SubtotalNeto = calculo.SubtotalNeto, MontoDescuento = calculo.DescuentoNeto,
                DescuentoPrecioCatalogo = calculo.DescuentoPrecioCatalogo, Impuesto = calculo.Impuesto,
                TotalLinea = calculo.Total, ImpuestosSnapshot = calculo.Impuestos.Select(x => new VentaDetalleImpuestos {
                    ImpuestoId = x.ImpuestoId, Codigo = x.Codigo, Nombre = x.Nombre, Porcentaje = x.Porcentaje,
                    IncluidoEnPrecio = x.IncluidoEnPrecio, BaseImponible = x.BaseImponible, Monto = x.Monto }).ToList() });
        }
        return resultado;
    }

    private static void Asignar(VentaEntidad venta, GuardarVentaSolicitud solicitud, List<VentasDetalle> lineas)
    {
        venta.BodegaId = solicitud.BodegaId; venta.UbicacionId = solicitud.UbicacionId;
        venta.EsCredito = solicitud.EsCredito ?? venta.EsCredito;
        venta.FechaVencimientoCredito = venta.EsCredito ? solicitud.FechaVencimientoCredito ?? venta.FechaVencimientoCredito : null;
        venta.MonedaId = solicitud.MonedaId; venta.ClienteId = solicitud.ClienteId;
        venta.VentasDetalle = lineas;
        venta.SubTotal = CalculadoraVenta.Moneda(lineas.Sum(x => x.SubtotalNeto!.Value));
        venta.Descuento = CalculadoraVenta.Moneda(lineas.Sum(x => x.MontoDescuento));
        venta.Impuestos = CalculadoraVenta.Moneda(lineas.Sum(x => x.Impuesto));
        venta.TotalVenta = CalculadoraVenta.Moneda(lineas.Sum(x => x.TotalLinea!.Value));
        venta.VentaImpuestos = lineas.SelectMany(x => x.ImpuestosSnapshot).GroupBy(x => x.ImpuestoId).Select(g => new VentaImpuestos {
            ImpuestoId = g.Key, Porcentaje = g.First().Porcentaje,
            BaseImponible = CalculadoraVenta.Moneda(g.Sum(x => x.BaseImponible)), Monto = CalculadoraVenta.Moneda(g.Sum(x => x.Monto)) }).ToList();
    }

    private async Task ValidarSnapshotAsync(VentaEntidad v, ContextoConfiguracion s, CancellationToken t)
    {
        var actuales = await PrepararAsync(new CrearVentaSolicitud { SucursalId = v.SucursalId,
            EsCredito = v.EsCredito, FechaVencimientoCredito = v.FechaVencimientoCredito,
            BodegaId = v.BodegaId!.Value, UbicacionId = v.UbicacionId!.Value, MonedaId = v.MonedaId, ClienteId = v.ClienteId,
            Detalles = v.VentasDetalle.OrderBy(x => x.NumeroLinea).Select(x => new LineaVentaSolicitud {
                ProductoId = x.ProductoId, ProductoPresentacionId = x.ProductoPresentacionId, Cantidad = x.Cantidad,
                PorcentajeDescuento = x.PorcentajeDescuento }).ToArray() }, s, t);
        foreach (var actual in actuales)
        {
            var anterior = v.VentasDetalle.Single(x => x.NumeroLinea == actual.NumeroLinea);
            if (anterior.FactorConversion != actual.FactorConversion || anterior.CantidadBase != actual.CantidadBase
                || (anterior.EsCompuesto ?? false) != actual.EsCompuesto
                || anterior.UnidadBaseId != actual.UnidadBaseId || anterior.UnidadMedidaId != actual.UnidadMedidaId
                || anterior.ManejaExistencia != actual.ManejaExistencia || anterior.PrecioUnitario != actual.PrecioUnitario
                || anterior.FuentePrecio != actual.FuentePrecio || anterior.SubtotalNeto != actual.SubtotalNeto
                || anterior.MontoDescuento != actual.MontoDescuento || anterior.TotalLinea != actual.TotalLinea
                || !anterior.ImpuestosSnapshot.OrderBy(x => x.ImpuestoId).Select(ImpuestoDto)
                    .SequenceEqual(actual.ImpuestosSnapshot.OrderBy(x => x.ImpuestoId).Select(ImpuestoDto)))
                throw new ConflictoVentasException("Cambió la configuración comercial desde el borrador. Actualice el borrador o anule la venta confirmada y cree otra.");
        }
        if (v.TipoCambio != 1 || v.TurnoCajaId.HasValue || v.PedidoVentaId.HasValue
            || v.SubTotal != actuales.Sum(x => x.SubtotalNeto) || v.Descuento != actuales.Sum(x => x.MontoDescuento)
            || v.Impuestos != actuales.Sum(x => x.Impuesto) || v.TotalVenta != actuales.Sum(x => x.TotalLinea))
            throw new ConflictoVentasException("Encabezado incompatible con los cálculos o el alcance POS de Fase 7.");
    }
}
