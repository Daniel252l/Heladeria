using Heladeria.Aplicacion.Configuracion;
using Heladeria.Aplicacion.Reportes;
using Heladeria.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Heladeria.Infraestructura.ReportesOperativos;

public sealed partial class ServicioReportes
{
    public Task<ReporteDiarioDto> DiarioAsync(FiltroDiario f, ContextoConfiguracion s, CancellationToken t) =>
        LecturaConsistente.EjecutarAsync(db, () => LeerDiarioAsync(f, s, t), t);

    private async Task<ReporteDiarioDto> LeerDiarioAsync(FiltroDiario f, ContextoConfiguracion s, CancellationToken t)
    {
        var suc = await AlcanceAsync(f, f.SucursalId, s, t);
        var fecha = f.Fecha ?? Hoy(s, suc);
        var periodo = Periodo(new FiltroReporte { FechaDesde = fecha, FechaHasta = fecha }, s, suc);
        var (desde, hasta) = Utc(periodo);
        var generado = reloj.GetUtcNow().UtcDateTime;
        if (f.CajaId is { } caja && !await db.Cajas.AsNoTracking().AnyAsync(x => x.Id == caja && x.EmpresaId == s.EmpresaId && x.SucursalId == suc, t)
            || f.FormaPagoId is { } forma && !await db.FormasPago.AsNoTracking().AnyAsync(x => x.Id == forma && x.EmpresaId == s.EmpresaId, t))
            throw new ExcepcionReporte("Caja o forma de pago fuera del alcance autorizado.", "ALCANCE_REPORTE_DENEGADO", 403);

        // Actividad comercial por cierre/anulación, NO por fecha de creación del borrador.
        var ventas = db.Ventas.AsNoTracking().Where(x => x.EmpresaId == s.EmpresaId && x.SucursalId == suc
            && x.EstadosFlujo.FlujoEstado.Codigo == "VENTA_POS"
            && (x.EstadosFlujo.Codigo == "COMPLETADA" && x.FechaCompletada >= desde && x.FechaCompletada < hasta
                || x.EstadosFlujo.Codigo == "ANULADA" && x.FechaAnulacion >= desde && x.FechaAnulacion < hasta));
        if (f.EsCredito.HasValue) ventas = ventas.Where(x => x.EsCredito == f.EsCredito);
        var pagos = db.Pagos.AsNoTracking().Where(x => x.EmpresaId == s.EmpresaId && x.SucursalId == suc
            && x.Estado.Activo && activos.Contains(x.Estado.Nombre));
        if (f.CajaId.HasValue) pagos = pagos.Where(x => x.TurnoCaja != null && x.TurnoCaja.CajaId == f.CajaId);
        if (f.FormaPagoId.HasValue) pagos = pagos.Where(x => x.PagoDetalles.Any(d => d.FormaPagoId == f.FormaPagoId));
        if (f.CajaId.HasValue || f.FormaPagoId.HasValue)
            ventas = ventas.Where(x => pagos.Any(p => p.VentaId == x.Id)); // Crédito sin cobro no tiene caja/forma atribuible.
        if (Texto(f.Busqueda) is { } texto) ventas = ventas.Where(x => x.NumeroFactura.Contains(texto) || x.Cliente != null && x.Cliente.NombreCliente.Contains(texto));
        var totalesVentas = await TotalesVentasAsync(ventas, t);
        var paginaVentas = await PaginaAsync(ventas.OrderByDescending(x => x.EstadosFlujo.Codigo == "ANULADA" ? x.FechaAnulacion : x.FechaCompletada)
            .ThenByDescending(x => x.Id).Select(x => new VentaDiarioDto(x.Id, x.NumeroFactura,
                (x.EstadosFlujo.Codigo == "ANULADA" ? x.FechaAnulacion : x.FechaCompletada)!.Value,
                x.Cliente == null ? null : x.Cliente.NombreCliente, x.EsCredito, x.EstadosFlujo.Codigo,
                x.TotalVenta, x.Moneda.CodigoISO, x.UsuarioCreacion.UserName,
                x.FechaCompletada != null && x.VentasDetalle.Any() && !x.VentasDetalle.Any(l => l.TotalLinea == null || l.SubtotalNeto == null),
                Array.Empty<string>())), f, t, totalesVentas.Sum(x => x.Cantidad));
        var idsVentas = paginaVentas.Elementos.Select(x => x.Id).ToArray();
        var formasVentas = await db.PagoDetalles.AsNoTracking().Where(x => x.Pago.VentaId.HasValue && idsVentas.Contains(x.Pago.VentaId.Value)
            && x.Pago.EmpresaId == s.EmpresaId && x.Pago.SucursalId == suc)
            .Select(x => new { Venta = x.Pago.VentaId!.Value, Forma = x.FormaPago.Nombre }).Distinct().ToArrayAsync(t);
        paginaVentas = new(paginaVentas.Elementos.Select(x => x with { FormasPago = formasVentas.Where(p => p.Venta == x.Id).Select(p => p.Forma).Order().ToArray() }).ToArray(),
            f.Pagina, f.TamanioPagina, paginaVentas.TotalRegistros);

        // Cobros por fecha del PAGO, incluso de ventas o CxC de días anteriores. No incluye egresos CxP.
        var cobros = pagos.Where(x => x.Fecha >= desde && x.Fecha < hasta
            && (x.VentaId != null || x.AplicacionesCuentasPorCobrar.Any()));
        if (f.EsCredito == true) cobros = cobros.Where(x => x.VentaId == null && x.AplicacionesCuentasPorCobrar.Any());
        if (f.EsCredito == false) cobros = cobros.Where(x => x.Venta != null && !x.Venta.EsCredito);
        var detallesCobros = db.PagoDetalles.AsNoTracking().Where(x => cobros.Select(p => p.Id).Contains(x.PagoId));
        if (f.FormaPagoId.HasValue) detallesCobros = detallesCobros.Where(x => x.FormaPagoId == f.FormaPagoId);
        var porForma = await ResumenAsync(detallesCobros.GroupBy(x => new { x.Pago.MonedaId, Moneda = x.Pago.Moneda.CodigoISO, x.FormaPagoId, Forma = x.FormaPago.Nombre })
            .OrderBy(g => g.Key.MonedaId).ThenBy(g => g.Key.FormaPagoId)
            .Select(g => new CobrosFormaReporteDto(g.Key.MonedaId, g.Key.Moneda, g.Key.FormaPagoId, g.Key.Forma, g.Sum(x => x.Monto))), t);

        var movimientos = db.MovimientosCaja.AsNoTracking().Where(x => x.TurnoCaja.Caja.EmpresaId == s.EmpresaId
            && x.TurnoCaja.Caja.SucursalId == suc && x.Fecha >= desde && x.Fecha < hasta);
        if (f.CajaId.HasValue) movimientos = movimientos.Where(x => x.TurnoCaja.CajaId == f.CajaId);
        if (f.FormaPagoId.HasValue) movimientos = movimientos.Where(x => x.FormaPagoId == f.FormaPagoId);
        if (f.EsCredito.HasValue) movimientos = f.EsCredito.Value
            ? movimientos.Where(x => x.CodigoConcepto == "COBRO_CUENTA_POR_COBRAR")
            : movimientos.Where(x => x.CodigoConcepto == "COBRO_VENTA");
        var finanzas = await ResumenAsync(movimientos.GroupBy(x => new { x.TurnoCaja.MonedaId, Moneda = x.TurnoCaja.Moneda == null ? null : x.TurnoCaja.Moneda.CodigoISO })
            .OrderBy(g => g.Key.MonedaId).Select(g => new FinanzasDiarioDto(g.Key.MonedaId, g.Key.Moneda,
                g.Sum(x => x.CodigoConcepto == "APERTURA_CAJA" ? x.Monto : 0),
                g.Sum(x => ingresos.Contains(x.CodigoConcepto!) ? x.Monto : 0), g.Sum(x => egresos.Contains(x.CodigoConcepto!) ? x.Monto : 0),
                g.Sum(x => x.CodigoConcepto == "COBRO_VENTA" ? x.Monto : 0), g.Sum(x => x.CodigoConcepto == "COBRO_CUENTA_POR_COBRAR" ? x.Monto : 0),
                g.Sum(x => x.CodigoConcepto == "PAGO_CUENTA_POR_PAGAR" ? x.Monto : 0),
                g.Count(x => x.CodigoConcepto == null || !conceptosCaja.Contains(x.CodigoConcepto) || x.EsEfectivo == null))), t);
        var operaciones = await PaginaAsync(movimientos.OrderByDescending(x => x.Fecha).ThenByDescending(x => x.Id)
            .Select(x => new OperacionDiarioDto(x.Id, x.Fecha, x.CodigoConcepto ?? "SIN_CLASIFICAR", x.NumeroDocumento, x.Referencia,
                x.FormaPago == null ? null : x.FormaPago.Nombre, x.TurnoCaja.Moneda == null ? null : x.TurnoCaja.Moneda.CodigoISO,
                ingresos.Contains(x.CodigoConcepto!) ? x.Monto : conceptosCaja.Contains(x.CodigoConcepto!) ? 0 : null,
                egresos.Contains(x.CodigoConcepto!) ? x.Monto : conceptosCaja.Contains(x.CodigoConcepto!) ? 0 : null,
                x.CodigoConcepto == "APERTURA_CAJA" ? x.Monto : 0, x.Usuario.UserName)),
            new FiltroDiario { Pagina = f.PaginaOperaciones, TamanioPagina = f.TamanioPagina }, t);
        var creditos = db.CuentasPorCobrar.AsNoTracking().Where(x => x.EmpresaId == s.EmpresaId && x.Venta.EmpresaId == s.EmpresaId
            && (x.SucursalId ?? x.Venta.SucursalId) == suc && x.FechaEmision == fecha && x.EstadosFlujo.Codigo != "ANULADA");
        if (f.EsCredito == false || f.CajaId.HasValue || f.FormaPagoId.HasValue) creditos = creditos.Where(x => false);
        var generados = await ResumenAsync(creditos.GroupBy(x => new { MonedaId = x.MonedaId ?? x.Venta.MonedaId, Moneda = x.Venta.Moneda.CodigoISO })
            .OrderBy(g => g.Key.MonedaId).Select(g => new CreditoDiarioDto(g.Key.MonedaId, g.Key.Moneda, g.Count(), g.Sum(x => x.MontoOriginal))), t);
        return new(periodo, desde, hasta, generado, await db.Sucursales.AsNoTracking().Where(x => x.Id == suc).Select(x => x.Nombre).SingleAsync(t),
            totalesVentas, porForma, finanzas, generados, paginaVentas, operaciones, await CajasDelDiaAsync(f, s, suc, desde, hasta, t));
    }

    private async Task<RespuestaPaginada<SesionCajaReporteDto>> CajasDelDiaAsync(FiltroDiario f, ContextoConfiguracion s, long suc,
        DateTime desde, DateTime hasta, CancellationToken t)
    {
        // Turnos que cruzan el día. Saldos completos al corte, no un saldo ficticio filtrado por forma/modalidad.
        var q = db.TurnosCaja.AsNoTracking().Where(x => x.Caja.EmpresaId == s.EmpresaId && x.Caja.SucursalId == suc
            && x.FechaApertura < hasta && (x.FechaCierre == null || x.FechaCierre >= desde));
        if (f.CajaId.HasValue) q = q.Where(x => x.CajaId == f.CajaId);
        var pagina = await PaginaAsync(q.OrderByDescending(x => x.FechaApertura).ThenByDescending(x => x.Id).Select(x => new {
            x.Id, Caja = x.Caja.Nombre, x.FechaApertura, Cierre = x.FechaCierre < hasta ? x.FechaCierre : null,
            Usuario = x.Usuario.UserName, x.MonedaId, Moneda = x.Moneda == null ? null : x.Moneda.CodigoISO, x.MontoInicial,
            Contado = x.FechaCierre < hasta ? x.MontoFisico : null,
            Conciliable = x.NumeroApertura != null && x.MonedaId != null && x.EstadosFlujo.FlujoEstado.Codigo == "CAJA_SESION"
                && !x.MovimientosCaja.Any(m => m.Fecha < hasta && (m.EsEfectivo == null || m.CodigoConcepto == null || !conceptosCaja.Contains(m.CodigoConcepto))),
            Ingresos = x.MovimientosCaja.Where(m => m.Fecha < hasta && ingresos.Contains(m.CodigoConcepto!)).Sum(m => (decimal?)m.Monto) ?? 0,
            Egresos = x.MovimientosCaja.Where(m => m.Fecha < hasta && egresos.Contains(m.CodigoConcepto!)).Sum(m => (decimal?)m.Monto) ?? 0,
            EfectivoIngresos = x.MovimientosCaja.Where(m => m.Fecha < hasta && m.EsEfectivo == true && ingresos.Contains(m.CodigoConcepto!)).Sum(m => (decimal?)m.Monto) ?? 0,
            EfectivoEgresos = x.MovimientosCaja.Where(m => m.Fecha < hasta && m.EsEfectivo == true && egresos.Contains(m.CodigoConcepto!)).Sum(m => (decimal?)m.Monto) ?? 0
        }), new FiltroDiario { Pagina = f.PaginaCajas, TamanioPagina = f.TamanioPagina }, t);
        return new(pagina.Elementos.Select(x => new SesionCajaReporteDto(x.Id, x.Caja, x.FechaApertura, x.Cierre, x.Usuario,
            x.Cierre != null ? "CERRADA" : "ABIERTA", x.MonedaId, x.Moneda, x.MontoInicial, x.Conciliable ? x.Ingresos : null,
            x.Conciliable ? x.Egresos : null, x.Conciliable ? x.MontoInicial + x.Ingresos - x.Egresos : null,
            x.Conciliable ? x.MontoInicial + x.EfectivoIngresos - x.EfectivoEgresos : null, x.Contado,
            x.Conciliable ? x.Contado - (x.MontoInicial + x.EfectivoIngresos - x.EfectivoEgresos) : null, x.Conciliable)).ToArray(),
            f.PaginaCajas, f.TamanioPagina, pagina.TotalRegistros);
    }
}
