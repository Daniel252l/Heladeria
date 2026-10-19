using Heladeria.Aplicacion.Configuracion;
using Heladeria.Aplicacion.Reportes;
using Microsoft.EntityFrameworkCore;

namespace Heladeria.Infraestructura.ReportesOperativos;

public sealed partial class ServicioReportes
{
    private sealed class CuentaFila
    {
        public long Id { get; init; }
        public int TerceroId { get; init; }
        public string Tercero { get; init; } = "";
        public string Documento { get; init; } = "";
        public long OrigenId { get; init; }
        public long? CompraId { get; init; }
        public DateOnly Emision { get; init; }
        public DateOnly Vencimiento { get; init; }
        public int MonedaId { get; init; }
        public string Moneda { get; init; } = "";
        public decimal Original { get; init; }
        public decimal Saldo { get; init; }
        public string Estado { get; init; } = "";
    }
    public async Task<ReporteDto<CuentaReporteDto, TotalCarteraReporteDto>> CarteraAsync(bool pagar, FiltroCarteraReporte f, ContextoConfiguracion s, CancellationToken t)
    {
        var suc = await AlcanceAsync(f, f.SucursalId, s, t);
        var p = Periodo(f, s, suc); var hoy = Hoy(s, suc);
        var q = pagar ? db.CuentasPorPagar.AsNoTracking().Where(x => x.EmpresaId == s.EmpresaId
            && (x.SucursalId ?? x.FacturaProveedor.OrdenCompra!.SucursalId) == suc
            && x.Proveedor.EmpresaId == s.EmpresaId && x.FacturaProveedor.EmpresaId == s.EmpresaId)
            .Select(x => new CuentaFila { Id = x.Id, TerceroId = x.ProveedorId, Tercero = x.Proveedor.NombreProveedor,
                Documento = ((x.FacturaProveedor.Serie ?? "") + " " + x.FacturaProveedor.NumeroDocumento).Trim(),
                OrigenId = x.FacturaProveedorId, CompraId = x.FacturaProveedor.OrdenCompraId,
                Emision = x.FechaEmision, Vencimiento = x.FechaVencimiento, MonedaId = x.MonedaId ?? x.FacturaProveedor.MonedaId,
                Moneda = x.FacturaProveedor.Moneda.CodigoISO, Original = x.MontoOriginal, Saldo = x.SaldoPendiente, Estado = x.EstadosFlujo.Codigo })
            : db.CuentasPorCobrar.AsNoTracking().Where(x => x.EmpresaId == s.EmpresaId && (x.SucursalId ?? x.Venta.SucursalId) == suc
                && x.Cliente.EmpresaId == s.EmpresaId && x.Venta.EmpresaId == s.EmpresaId)
            .Select(x => new CuentaFila { Id = x.Id, TerceroId = x.ClienteId, Tercero = x.Cliente.NombreCliente,
                Documento = x.Venta.NumeroFactura, OrigenId = x.VentaId, Emision = x.FechaEmision,
                Vencimiento = x.FechaVencimiento, MonedaId = x.MonedaId ?? x.Venta.MonedaId,
                Moneda = x.Venta.Moneda.CodigoISO, Original = x.MontoOriginal, Saldo = x.SaldoPendiente, Estado = x.EstadosFlujo.Codigo });
        q = q.Where(x => x.Emision >= p.FechaDesde && x.Emision <= p.FechaHasta);
        if (f.TerceroId.HasValue) q = q.Where(x => x.TerceroId == f.TerceroId);
        if (f.MonedaId.HasValue) q = q.Where(x => x.MonedaId == f.MonedaId);
        if (f.Estado is not null) q = q.Where(x => x.Estado == f.Estado);
        if (f.Vencimiento == "PAGADA") q = q.Where(x => x.Estado == "PAGADA" && x.Saldo == 0);
        if (f.Vencimiento == "VENCIDA") q = q.Where(x => x.Estado != "ANULADA" && x.Saldo > 0 && x.Vencimiento < hoy);
        if (f.Vencimiento == "VIGENTE") q = q.Where(x => x.Estado != "ANULADA" && x.Saldo > 0 && x.Vencimiento >= hoy);
        if (Texto(f.Busqueda) is { } b) q = q.Where(x => x.Tercero.Contains(b) || x.Documento.Contains(b));
        var totales = await ResumenAsync(q.GroupBy(x => new { x.MonedaId, x.Moneda }).OrderBy(g => g.Key.MonedaId)
            .Select(g => new TotalCarteraReporteDto(g.Key.MonedaId, g.Key.Moneda, g.Count(),
                g.Count(x => x.Estado != "ANULADA" && x.Saldo > 0),
                g.Sum(x => x.Estado != "ANULADA" && x.Saldo > 0 ? x.Saldo : 0),
                g.Sum(x => x.Estado != "ANULADA" && x.Saldo > 0 && x.Vencimiento >= hoy ? x.Saldo : 0),
                g.Sum(x => x.Estado != "ANULADA" && x.Saldo > 0 && x.Vencimiento < hoy ? x.Saldo : 0))), t);
        var pagina = await PaginaAsync(q.OrderBy(x => x.Vencimiento).ThenBy(x => x.Id).Select(x => new CuentaReporteDto(
            x.Id, x.TerceroId, x.Tercero, x.Documento, x.OrigenId, x.CompraId, x.Emision, x.Vencimiento,
            x.MonedaId, x.Moneda, x.Original, x.Original - x.Saldo, x.Saldo, x.Estado,
            x.Estado == "ANULADA" ? "ANULADA" : x.Estado == "PAGADA" && x.Saldo == 0 ? "PAGADA"
                : x.Saldo <= 0 ? "REQUIERE_REVISION" : x.Vencimiento < hoy ? "VENCIDA" : "VIGENTE")), f, t, totales.Sum(x => x.Cantidad));
        return new(pagina, totales, p);
    }
    public async Task<RespuestaPaginada<AuditoriaDto>> AuditoriaAsync(FiltroAuditoria f, ContextoConfiguracion s, CancellationToken t)
    {
        var suc = await AlcanceAsync(f, f.SucursalId, s, t);
        var (desde, hasta) = Utc(Periodo(f, s, suc));
        // No inferir alcance de registros sin empresa/sucursal, ni revelar payloads/textos libres históricos.
        var q = db.Bitacoras.AsNoTracking().Where(x => x.EmpresaId == s.EmpresaId && x.SucursalId == suc
            && x.Fecha >= desde && x.Fecha < hasta);
        if (f.UsuarioId.HasValue) q = q.Where(x => x.UsuarioId == f.UsuarioId);
        if (Texto(f.Modulo) is { } modulo) q = q.Where(x => x.ModuloId == modulo);
        if (Texto(f.Accion) is { } accion) q = q.Where(x => x.Accion == accion);
        if (Texto(f.Entidad) is { } entidad) q = q.Where(x => x.Entidad == entidad);
        if (Texto(f.Busqueda) is { } b) q = q.Where(x => x.Entidad.Contains(b) || x.Accion.Contains(b)
            || x.EntidadId != null && x.EntidadId.Contains(b));
        return await PaginaAsync(q.OrderByDescending(x => x.Fecha).ThenByDescending(x => x.Id)
            .Select(x => new AuditoriaDto(x.Id, x.Fecha, x.UsuarioId, x.Usuario == null ? null : x.Usuario.UserName,
                x.ModuloId, x.Entidad, x.EntidadId, x.Accion, x.EmpresaId!.Value, x.SucursalId!.Value,
                x.Accion.EndsWith("RECHAZADA") || x.Accion.EndsWith("RECHAZADO") || x.Accion == "ACCESO_DENEGADO"
                    ? "RECHAZADA" : "REGISTRADA")), f, t);
    }
}
