using Heladeria.Aplicacion.Configuracion;
using Heladeria.Aplicacion.Reportes;
using Microsoft.EntityFrameworkCore;

namespace Heladeria.Infraestructura.ReportesOperativos;

public sealed partial class ServicioReportes
{
    private static readonly string[] ingresos = ["INGRESO_CAJA", "COBRO_VENTA", "COBRO_CUENTA_POR_COBRAR"];
    private static readonly string[] egresos = ["EGRESO_CAJA", "PAGO_CUENTA_POR_PAGAR"];
    private static readonly string[] conceptosCaja = ["APERTURA_CAJA", ..ingresos, ..egresos];
    public async Task<ReporteDto<SesionCajaReporteDto, FormaCajaReporteDto>> CajaAsync(FiltroCajaReporte f, ContextoConfiguracion s, CancellationToken t)
    {
        var suc = await AlcanceAsync(f, f.SucursalId, s, t);
        var p = Periodo(f, s, suc); var (desde, hasta) = Utc(p);
        var q = db.TurnosCaja.AsNoTracking().Where(x => x.Caja.EmpresaId == s.EmpresaId && x.Caja.SucursalId == suc
            && x.FechaApertura >= desde && x.FechaApertura < hasta);
        if (f.CajaId.HasValue) q = q.Where(x => x.CajaId == f.CajaId);
        if (f.UsuarioId.HasValue) q = q.Where(x => x.UsuarioId == f.UsuarioId);
        if (f.Estado is not null) q = q.Where(x => x.EstadosFlujo.Codigo == f.Estado);
        if (Texto(f.Busqueda) is { } b) q = q.Where(x => x.Caja.Nombre.Contains(b) || x.NumeroApertura != null && x.NumeroApertura.Contains(b));
        var pagina = await PaginaAsync(q.OrderByDescending(x => x.FechaApertura).ThenByDescending(x => x.Id).Select(x => new {
            x.Id, Caja = x.Caja.Nombre, x.FechaApertura, x.FechaCierre, Usuario = x.Usuario.UserName,
            Estado = x.EstadosFlujo.Codigo, x.MonedaId, Moneda = x.Moneda == null ? null : x.Moneda.CodigoISO,
            x.MontoInicial, x.MontoFisico, x.MontoEsperado,
            Conciliable = x.NumeroApertura != null && x.MonedaId != null && x.EstadosFlujo.FlujoEstado.Codigo == "CAJA_SESION"
                && !x.MovimientosCaja.Any(m => m.EsEfectivo == null || m.CodigoConcepto == null || !conceptosCaja.Contains(m.CodigoConcepto)),
            Ingresos = x.MovimientosCaja.Where(m => ingresos.Contains(m.CodigoConcepto!)).Sum(m => (decimal?)m.Monto) ?? 0,
            Egresos = x.MovimientosCaja.Where(m => egresos.Contains(m.CodigoConcepto!)).Sum(m => (decimal?)m.Monto) ?? 0,
            IngresosEfectivo = x.MovimientosCaja.Where(m => m.EsEfectivo == true && ingresos.Contains(m.CodigoConcepto!)).Sum(m => (decimal?)m.Monto) ?? 0,
            EgresosEfectivo = x.MovimientosCaja.Where(m => m.EsEfectivo == true && egresos.Contains(m.CodigoConcepto!)).Sum(m => (decimal?)m.Monto) ?? 0
        }), f, t);
        var filas = pagina.Elementos.Select(x => new SesionCajaReporteDto(x.Id, x.Caja, x.FechaApertura, x.FechaCierre,
            x.Usuario, x.Estado, x.MonedaId, x.Moneda, x.MontoInicial, x.Conciliable ? x.Ingresos : null,
            x.Conciliable ? x.Egresos : null, x.Conciliable ? x.MontoInicial + x.Ingresos - x.Egresos : null,
            x.Conciliable ? x.MontoInicial + x.IngresosEfectivo - x.EgresosEfectivo : null, x.MontoFisico,
            x.Conciliable ? x.MontoFisico - (x.MontoInicial + x.IngresosEfectivo - x.EgresosEfectivo) : null, x.Conciliable)).ToArray();
        var sesionesCalculables = q.Where(x => x.NumeroApertura != null && x.MonedaId != null
            && x.EstadosFlujo.FlujoEstado.Codigo == "CAJA_SESION"
            && !x.MovimientosCaja.Any(m => m.EsEfectivo == null || m.CodigoConcepto == null || !conceptosCaja.Contains(m.CodigoConcepto)));
        var formas = await ResumenAsync(db.MovimientosCaja.AsNoTracking().Where(x => sesionesCalculables.Select(v => v.Id).Contains(x.TurnoCajaId)
            && x.CodigoConcepto != null && conceptosCaja.Contains(x.CodigoConcepto))
            .GroupBy(x => new { x.TurnoCaja.MonedaId, Moneda = x.TurnoCaja.Moneda == null ? null : x.TurnoCaja.Moneda.CodigoISO,
                x.FormaPagoId, Forma = x.FormaPago == null ? null : x.FormaPago.Nombre, x.EsEfectivo })
            .OrderBy(g => g.Key.MonedaId).ThenBy(g => g.Key.FormaPagoId).ThenBy(g => g.Key.EsEfectivo)
            .Select(g => new FormaCajaReporteDto(g.Key.MonedaId, g.Key.Moneda, g.Key.FormaPagoId, g.Key.Forma, g.Key.EsEfectivo,
                g.Sum(x => x.CodigoConcepto == "APERTURA_CAJA" ? x.Monto : 0),
                g.Sum(x => ingresos.Contains(x.CodigoConcepto!) ? x.Monto : 0),
                g.Sum(x => egresos.Contains(x.CodigoConcepto!) ? x.Monto : 0))), t);
        return new(new(filas, f.Pagina, f.TamanioPagina, pagina.TotalRegistros), formas, p);
    }
}
