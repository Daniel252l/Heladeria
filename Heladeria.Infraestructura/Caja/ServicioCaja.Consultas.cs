using Heladeria.Aplicacion.Caja;
using Heladeria.Aplicacion.Configuracion;
using Heladeria.Aplicacion.Seguridad.Configuracion;
using Heladeria.Infraestructura.Inventario;
using Heladeria.Infraestructura.Persistencia;
using Heladeria.Infraestructura.Persistencia.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Heladeria.Infraestructura.Caja;

public sealed partial class ServicioCaja(HeladeriaDbContext contexto, IOptions<OpcionesSeguridad> opciones,
    TimeProvider reloj, CoordinadorAjustesInventario coordinador)
    : ServicioCajaBase(contexto, opciones, reloj, coordinador), IServicioCaja
{
    private IQueryable<TurnosCaja> Sesiones(ContextoConfiguracion s) => Contexto.TurnosCaja
        .Include(x => x.Caja).ThenInclude(x => x.Estado).Include(x => x.Usuario).Include(x => x.Moneda)
        .Include(x => x.EstadosFlujo).ThenInclude(x => x.FlujoEstado)
        .Where(x => x.Caja.EmpresaId == s.EmpresaId && x.Caja.SucursalId == s.SucursalId);
    private async Task<TurnosCaja> CargarAsync(long id, ContextoConfiguracion s, CancellationToken t) =>
        await Sesiones(s).SingleOrDefaultAsync(x => x.Id == id, t)
        ?? throw new CajaNoEncontradaException("Sesión no encontrada en la sucursal activa.");
    private static bool EsFase8(TurnosCaja x) => x.MonedaId.HasValue && x.NumeroApertura is not null
        && x.EstadosFlujo.FlujoEstado.Codigo == FlujoCaja;
    private static SesionCajaResumenDto Resumen(TurnosCaja x) => new(x.Id, x.CajaId, x.Caja.Nombre,
        x.Caja.EmpresaId, x.Caja.SucursalId, x.UsuarioId, x.Usuario.UserName, x.FechaApertura, x.MontoInicial,
        x.EstadosFlujo.Codigo, x.MonedaId, x.Moneda?.CodigoISO, x.NumeroApertura, x.NumeroCierre,
        x.FechaCierre, x.UsuarioCierreId, x.MontoEsperado, x.MontoFisico,
        x.MontoFisico - x.MontoEsperado, x.TotalIngresos, x.TotalEgresos, x.ArqueoCierreId,
        Convert.ToBase64String(x.VersionFila), EsFase8(x));

    public async Task<RespuestaPaginada<CajaDto>> CajasAsync(FiltroPaginadoBase filtro, ContextoConfiguracion s, CancellationToken t)
    {
        Validar(filtro); await ValidarSucursalAsync(s, t);
        var q = Contexto.Cajas.AsNoTracking().Where(x => x.EmpresaId == s.EmpresaId && x.SucursalId == s.SucursalId);
        if (Opcional(filtro.Busqueda) is { } b) q = q.Where(x => x.Codigo.Contains(b) || x.Nombre.Contains(b));
        var total = await q.CountAsync(t);
        var filas = await q.OrderBy(x => x.Codigo).ThenBy(x => x.Id).Skip(filtro.CalcularDesplazamiento())
            .Take(filtro.TamanioPagina).Select(x => new CajaDto(x.Id, x.EmpresaId, x.SucursalId, x.Codigo, x.Nombre,
                x.EstadoId, x.Estado.Nombre, x.TurnosCaja.Where(y => y.FechaCierre == null).Select(y => (long?)y.Id).FirstOrDefault())).ToArrayAsync(t);
        return new(filas, filtro.Pagina, filtro.TamanioPagina, total);
    }
    public async Task<RespuestaPaginada<SesionCajaResumenDto>> SesionesAsync(FiltroSesionesCaja filtro, ContextoConfiguracion s, CancellationToken t)
    {
        Validar(filtro); await ValidarSucursalAsync(s, t);
        var q = Sesiones(s).AsNoTracking();
        if (filtro.CajaId is { } caja) q = q.Where(x => x.CajaId == caja);
        if (filtro.Abierta is { } abierta) q = q.Where(x => (x.FechaCierre == null) == abierta);
        if (Opcional(filtro.Busqueda) is { } b) q = q.Where(x => x.Caja.Nombre.Contains(b) || x.NumeroApertura != null && x.NumeroApertura.Contains(b));
        var total = await q.CountAsync(t);
        var filas = await q.OrderByDescending(x => x.Id).Skip(filtro.CalcularDesplazamiento()).Take(filtro.TamanioPagina).ToArrayAsync(t);
        return new(filas.Select(Resumen).ToArray(), filtro.Pagina, filtro.TamanioPagina, total);
    }
    public async Task<ReferenciasCajaDto> ReferenciasAsync(ContextoConfiguracion s, CancellationToken t, long? cajaId = null)
    {
        await ValidarSucursalAsync(s, t);
        var moneda = await MonedaAsync(s, t);
        return new(s.EmpresaId, s.SucursalId, moneda.Id, moneda.CodigoISO,
            (await FormasAsync(s, t)).Select(x => new FormaPagoCajaDto(x.Id, x.Codigo, x.Nombre,
                x.Codigo == "EFECTIVO", x.RequiereReferencia, x.PermiteCambio)).ToArray())
            { SaldoUltimoCierre = cajaId is { } id ? await UltimoCierreAsync(id, moneda.Id, s, t) : null };
    }

    private async Task<SaldoUltimoCierreDto?> UltimoCierreAsync(long cajaId, int monedaId, ContextoConfiguracion s, CancellationToken t)
    {
        if (!await Contexto.Cajas.AsNoTracking().AnyAsync(x => x.Id == cajaId && x.EmpresaId == s.EmpresaId
            && x.SucursalId == s.SucursalId, t))
            throw new CajaNoEncontradaException("Caja no encontrada en la sucursal activa.");
        // Primero elegir el último cerrado; nunca sustituir un cierre incompleto por otro más antiguo.
        var ultimo = await Sesiones(s).AsNoTracking().Where(x => x.CajaId == cajaId && x.FechaCierre != null)
            .OrderByDescending(x => x.FechaCierre).ThenByDescending(x => x.Id).FirstOrDefaultAsync(t);
        if (ultimo is null || !EsFase8(ultimo) || ultimo.EstadosFlujo.Codigo != "CERRADA"
            || ultimo.MonedaId != monedaId || ultimo.NumeroCierre is null || ultimo.UsuarioCierreId is null
            || ultimo.MontoFisico is not { } contado || contado < 0 || contado > 9999999999.99m
            || decimal.Round(contado, 2) != contado || ultimo.MontoEsperado is null or < 0
            || ultimo.TotalIngresos is null or < 0 || ultimo.TotalEgresos is null or < 0
            || ultimo.FechaCierre < ultimo.FechaApertura
            || !await Contexto.ArqueosCaja.AsNoTracking().AnyAsync(x => x.Id == ultimo.ArqueoCierreId
                && x.TurnoCajaId == ultimo.Id && x.MontoContado == contado && x.MontoEsperado == ultimo.MontoEsperado, t))
            return null;
        return new(ultimo.Id, cajaId, ultimo.NumeroCierre, ultimo.FechaCierre!.Value, contado);
    }
    private IQueryable<Persistencia.Entidades.Ventas> VentasPendientes(ContextoConfiguracion s) =>
        Contexto.Ventas.AsNoTracking().Where(x => x.EmpresaId == s.EmpresaId && x.SucursalId == s.SucursalId
            && x.EstadosFlujo.FlujoEstado.Codigo == "VENTA_POS" && x.EstadosFlujo.Codigo == "COMPLETADA"
            && x.FechaCompletada != null && x.BodegaId != null && x.ClaveOperacion != null && !x.EsCredito
            && x.FechaAnulacion == null && x.TurnoCajaId == null && x.PedidoVentaId == null && x.TotalVenta > 0
            && !Contexto.Pagos.Any(p => p.VentaId == x.Id) && !Contexto.CuentasPorCobrar.Any(p => p.VentaId == x.Id)
            && !Contexto.DevolucionesVentas.Any(p => p.VentaId == x.Id)
            && !Contexto.NotasCredito.Any(p => p.VentaId == x.Id) && !Contexto.NotasDebito.Any(p => p.VentaId == x.Id));
    public async Task<RespuestaPaginada<VentaPendienteCajaDto>> VentasPendientesAsync(FiltroPaginadoBase filtro, ContextoConfiguracion s, CancellationToken t)
    {
        Validar(filtro); await ValidarSucursalAsync(s, t);
        var q = VentasPendientes(s);
        if (Opcional(filtro.Busqueda) is { } b) q = q.Where(x => x.NumeroFactura.Contains(b));
        var total = await q.CountAsync(t);
        var filas = await q.Include(x => x.Cliente).Include(x => x.Moneda).OrderBy(x => x.Id)
            .Skip(filtro.CalcularDesplazamiento()).Take(filtro.TamanioPagina).ToArrayAsync(t);
        return new(filas.Select(x => new VentaPendienteCajaDto(x.Id, x.NumeroFactura, x.FechaVenta,
            x.ClienteId, x.Cliente?.NombreCliente, x.TotalVenta, x.TotalVenta, x.MonedaId, x.Moneda.CodigoISO,
            Convert.ToBase64String(x.VersionFila))).ToArray(), filtro.Pagina, filtro.TamanioPagina, total);
    }
    // El detalle reúne varias consultas; usa la misma reserva para no mezclar encabezado, pagos y movimientos de instantes distintos.
    // No inserta ni modifica datos. Las operaciones ya transaccionales llaman directamente a LeerSesionAsync.
    public Task<SesionCajaDto> ObtenerAsync(long id, ContextoConfiguracion s, CancellationToken t) =>
        EscribirAsync(s, () => LeerSesionAsync(id, s, t), t);
    internal async Task<SesionCajaDto> LeerSesionAsync(long id, ContextoConfiguracion s, CancellationToken t)
    {
        await ValidarSucursalAsync(s, t);
        var turno = await Sesiones(s).AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, t)
            ?? throw new CajaNoEncontradaException("Sesión no encontrada en la sucursal activa.");
        var movimientos = await Contexto.MovimientosCaja.AsNoTracking().Include(x => x.FormaPago).Include(x => x.TipoMovimiento)
            .Include(x => x.Usuario).Include(x => x.PagoDetalle!).ThenInclude(x => x.Pago).ThenInclude(x => x.Venta)
            .Where(x => x.TurnoCajaId == id).OrderBy(x => x.Id).ToArrayAsync(t);
        var pagos = await Contexto.Pagos.AsNoTracking().Include(x => x.Moneda).Include(x => x.Venta)
            .Include(x => x.PagoDetalles).ThenInclude(x => x.FormaPago)
            .Include(x => x.AplicacionesCuentasPorCobrar).ThenInclude(x => x.CuentaPorCobrar)
            .Include(x => x.AplicacionesCuentasPorPagar).ThenInclude(x => x.CuentaPorPagar)
            .Where(x => x.TurnoCajaId == id).OrderBy(x => x.Id).ToArrayAsync(t);
        var arqueos = await Contexto.ArqueosCaja.AsNoTracking().Include(x => x.Usuario)
            .Where(x => x.TurnoCajaId == id).OrderBy(x => x.Id).ToArrayAsync(t);
        var fase8 = EsFase8(turno);
        if (fase8) Consistencia(turno, movimientos, pagos);
        var ingresos = movimientos.Where(x => x.CodigoConcepto is "INGRESO_CAJA" or "COBRO_VENTA" or "COBRO_CUENTA_POR_COBRAR").Sum(x => x.Monto);
        var egresos = movimientos.Where(x => x.CodigoConcepto is "EGRESO_CAJA" or "PAGO_CUENTA_POR_PAGAR").Sum(x => x.Monto);
        var efectivo = turno.MontoInicial + movimientos.Where(x => x.EsEfectivo == true && x.CodigoConcepto is "INGRESO_CAJA" or "COBRO_VENTA" or "COBRO_CUENTA_POR_COBRAR").Sum(x => x.Monto)
            - movimientos.Where(x => x.EsEfectivo == true && x.CodigoConcepto is "EGRESO_CAJA" or "PAGO_CUENTA_POR_PAGAR").Sum(x => x.Monto);
        if (fase8)
        {
            Importe(ingresos, "Ingresos acumulados", true); Importe(egresos, "Egresos acumulados", true);
            Importe(efectivo, "Efectivo esperado", true);
            if (turno.FechaCierre is not null && (turno.MontoEsperado != efectivo || turno.TotalIngresos != ingresos
                || turno.TotalEgresos != egresos || !arqueos.Any(x => x.Id == turno.ArqueoCierreId
                    && x.MontoEsperado == efectivo && x.MontoContado == turno.MontoFisico)))
                throw new ExcepcionCaja("El cierre no coincide con el libro y su arqueo; requiere revisión.");
        }
        return new(Resumen(turno), movimientos.Select(x => new MovimientoCajaDto(x.Id, x.NumeroDocumento,
            x.CodigoConcepto ?? x.TipoMovimiento.Nombre, x.Fecha, x.Monto, x.FormaPagoId, x.FormaPago?.Nombre,
            x.EsEfectivo, x.Referencia, x.Descripcion, x.UsuarioId, x.Usuario.UserName,
            x.PagoDetalle?.PagoId, x.PagoDetalle?.Pago.VentaId, x.PagoDetalle?.Pago.Venta?.NumeroFactura)).ToArray(),
            movimientos.GroupBy(x => x.CodigoConcepto ?? x.TipoMovimiento.Nombre).Select(g => new TotalTipoCajaDto(g.Key, g.Sum(x => x.Monto))).ToArray(),
            movimientos.Where(x => x.EsEfectivo.HasValue).GroupBy(x => new { x.FormaPagoId, Nombre = x.FormaPago?.Nombre ?? "Sin forma", Efectivo = x.EsEfectivo == true })
                .Select(g => new TotalFormaCajaDto(g.Key.FormaPagoId, g.Key.Nombre, g.Key.Efectivo,
                    g.Where(x => x.CodigoConcepto == "APERTURA_CAJA").Sum(x => x.Monto),
                    g.Where(x => x.CodigoConcepto is "INGRESO_CAJA" or "COBRO_VENTA" or "COBRO_CUENTA_POR_COBRAR").Sum(x => x.Monto),
                    g.Where(x => x.CodigoConcepto is "EGRESO_CAJA" or "PAGO_CUENTA_POR_PAGAR").Sum(x => x.Monto),
                    g.Sum(x => x.CodigoConcepto is "EGRESO_CAJA" or "PAGO_CUENTA_POR_PAGAR" ? -x.Monto : x.Monto))).ToArray(),
            fase8 ? turno.MontoInicial + ingresos - egresos : null, fase8 ? efectivo : null,
            pagos.Where(x => x.VentaId != null && x.ClaveOperacion != null && x.PagoDetalles.All(d => d.EsEfectivo.HasValue && d.MontoRecibido.HasValue))
                .Select(Cobro).ToArray(), arqueos.Select(x => new ArqueoCajaDto(x.Id, x.Fecha, x.MontoEsperado,
                x.MontoContado, x.MontoContado - x.MontoEsperado, x.UsuarioId, x.Usuario.UserName, x.Observaciones,
                turno.FechaCierre is null ? x.VersionTurno is not null && x.VersionTurno.AsSpan().SequenceEqual(turno.VersionFila)
                    : x.Id == turno.ArqueoCierreId)).ToArray())
        {
            UsuarioCierre = turno.UsuarioCierreId.HasValue
                ? await Contexto.Usuarios.AsNoTracking().Where(x => x.Id == turno.UsuarioCierreId)
                    .Select(x => x.UserName).SingleOrDefaultAsync(t) : null
        };
    }
    private static CobroCajaDto Cobro(Pagos x) => new(x.Id, x.VentaId!.Value, x.Venta!.NumeroFactura, x.TurnoCajaId!.Value,
        x.Fecha, x.UsuarioId, x.MontoTotal, x.MonedaId, x.Moneda.CodigoISO, x.ClaveOperacion!.Value,
        x.PagoDetalles.OrderBy(d => d.Id).Select(d => new CobroDetalleDto(d.Id, d.FormaPagoId, d.FormaPago.Nombre,
            d.EsEfectivo == true, d.Monto, d.MontoRecibido!.Value, d.MontoRecibido.Value - d.Monto, d.Referencia)).ToArray());
    private static void Consistencia(TurnosCaja turno, MovimientosCaja[] movimientos, Pagos[] pagos)
    {
        static void Error() => throw new ExcepcionCaja("Los registros de la sesión no son consistentes. Requieren revisión; no se corregirán automáticamente.");
        if (turno.Moneda is null || turno.Moneda.EmpresaId != turno.Caja.EmpresaId
            || (turno.FechaCierre is null ? turno.EstadosFlujo.Codigo != "ABIERTA" : turno.EstadosFlujo.Codigo != "CERRADA")) Error();
        if (movimientos.Any(x => x.EsEfectivo is null || x.NumeroDocumento is null || x.Monto <= 0 || x.FormaPagoId is null
            || x.FormaPago?.EmpresaId != turno.Caja.EmpresaId
            || x.CodigoConcepto is not ("APERTURA_CAJA" or "INGRESO_CAJA" or "EGRESO_CAJA" or "COBRO_VENTA" or "COBRO_CUENTA_POR_COBRAR" or "PAGO_CUENTA_POR_PAGAR"))) Error();
        var aperturas = movimientos.Where(x => x.CodigoConcepto == "APERTURA_CAJA").ToArray();
        if (aperturas.Length != (turno.MontoInicial > 0 ? 1 : 0) || aperturas.Sum(x => x.Monto) != turno.MontoInicial) Error();
        if (movimientos.Any(x => x.CodigoConcepto is not ("COBRO_VENTA" or "COBRO_CUENTA_POR_COBRAR" or "PAGO_CUENTA_POR_PAGAR") && (x.EsEfectivo != true || x.PagoDetalleId is not null))) Error();
        foreach (var pago in pagos)
        {
            if (pago.HuellaSolicitud is not null)
            {
                if (pago.AplicacionesCuentasPorCobrar.Count > 1 || pago.AplicacionesCuentasPorPagar.Count > 1) Error();
                var cobrar = pago.AplicacionesCuentasPorCobrar.SingleOrDefault();
                var pagar = pago.AplicacionesCuentasPorPagar.SingleOrDefault();
                var concepto = cobrar is not null ? "COBRO_CUENTA_POR_COBRAR" : "PAGO_CUENTA_POR_PAGAR";
                if ((cobrar is null) == (pagar is null) || pago.VentaId is not null || pago.ClaveOperacion is null
                    || pago.MonedaId != turno.MonedaId || pago.EmpresaId != turno.Caja.EmpresaId || pago.SucursalId != turno.Caja.SucursalId
                    || pago.TipoCambio != 1 || pago.PagoDetalles.Count != 1 || pago.HuellaSolicitud.Length != 32
                    || cobrar is not null && (cobrar.MontoAplicado != pago.MontoTotal || cobrar.CuentaPorCobrar.ClienteId != pago.ClienteId
                        || pago.ProveedorId is not null || cobrar.CuentaPorCobrar.EmpresaId != pago.EmpresaId || cobrar.CuentaPorCobrar.SucursalId != pago.SucursalId)
                    || pagar is not null && (pagar.MontoAplicado != pago.MontoTotal || pagar.CuentaPorPagar.ProveedorId != pago.ProveedorId
                        || pago.ClienteId is not null || pagar.CuentaPorPagar.EmpresaId != pago.EmpresaId || pagar.CuentaPorPagar.SucursalId != pago.SucursalId)) Error();
                foreach (var d in pago.PagoDetalles)
                    if (d.Monto != pago.MontoTotal || d.MontoRecibido != d.Monto || d.EsEfectivo is null
                        || pagar is not null && d.EsEfectivo != true
                        || movimientos.Count(m => m.PagoDetalleId == d.Id && m.CodigoConcepto == concepto
                            && m.Monto == d.Monto && m.EsEfectivo == d.EsEfectivo && m.FormaPagoId == d.FormaPagoId) != 1) Error();
                continue;
            }
            if (pago.MonedaId != turno.MonedaId || pago.ClaveOperacion is null || pago.Venta is null
                || pago.EmpresaId != turno.Caja.EmpresaId || pago.SucursalId != turno.Caja.SucursalId
                || pago.Venta.EmpresaId != pago.EmpresaId || pago.Venta.SucursalId != pago.SucursalId
                || pago.Venta.TotalVenta != pago.MontoTotal || pago.ProveedorId is not null || pago.TipoCambio != 1
                || pago.PagoDetalles.Count == 0 || pago.PagoDetalles.Sum(x => x.Monto) != pago.MontoTotal) Error();
            foreach (var d in pago.PagoDetalles)
                if (d.EsEfectivo is null || d.MontoRecibido < d.Monto || d.MontoRecibido is null
                    || d.EsEfectivo == false && d.MontoRecibido != d.Monto
                    || movimientos.Count(m => m.PagoDetalleId == d.Id && m.CodigoConcepto == "COBRO_VENTA"
                        && m.Monto == d.Monto && m.EsEfectivo == d.EsEfectivo && m.FormaPagoId == d.FormaPagoId) != 1) Error();
        }
        if (movimientos.Where(x => x.CodigoConcepto is "COBRO_VENTA" or "COBRO_CUENTA_POR_COBRAR" or "PAGO_CUENTA_POR_PAGAR").Any(m => !pagos.SelectMany(p => p.PagoDetalles).Any(d => d.Id == m.PagoDetalleId))) Error();
    }
}
