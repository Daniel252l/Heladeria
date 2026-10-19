using Heladeria.Aplicacion.Configuracion;
using Heladeria.Aplicacion.Reportes;
using Microsoft.EntityFrameworkCore;

namespace Heladeria.Infraestructura.ReportesOperativos;

public sealed partial class ServicioReportes
{
    public async Task<ReporteInventarioDto> InventarioAsync(FiltroInventarioReporte f, ContextoConfiguracion s, CancellationToken t)
    {
        var suc = await AlcanceAsync(f, f.SucursalId, s, t);
        var q = db.Existencias.AsNoTracking().Where(x => x.EmpresaId == s.EmpresaId && x.Bodegas.SucursalId == suc
            && x.Productos.EmpresaId == s.EmpresaId && x.Bodegas.EmpresaId == s.EmpresaId
            && x.Ubicaciones.EmpresaId == s.EmpresaId && x.Productos.TiposProductos.ManejaExistencia);
        if (f.ProductoId.HasValue) q = q.Where(x => x.ProductoId == f.ProductoId);
        if (f.BodegaId.HasValue) q = q.Where(x => x.BodegaId == f.BodegaId);
        if (f.UbicacionId.HasValue) q = q.Where(x => x.UbicacionId == f.UbicacionId);
        if (Texto(f.Busqueda) is { } b) q = q.Where(x => x.Productos.Nombre.Contains(b) || x.Productos.SKU.Contains(b));
        var valor = await q.SumAsync(x => (decimal?)(x.Cantidad * x.CostoPromedio), t) ?? 0;
        var monedas = await db.Monedas.AsNoTracking().Where(x => x.EmpresaId == s.EmpresaId && x.EsMonedaBase
            && x.Estado.Activo && activos.Contains(x.Estado.Nombre)).Select(x => x.CodigoISO).Take(2).ToArrayAsync(t);
        var pagina = await PaginaAsync(q.OrderBy(x => x.ProductoId).ThenBy(x => x.BodegaId).ThenBy(x => x.UbicacionId).ThenBy(x => x.Id)
            .Select(x => new InventarioReporteDto(x.Id, x.ProductoId, x.Productos.SKU, x.Productos.Nombre,
                x.BodegaId, x.Bodegas.Nombre, x.UbicacionId, x.Ubicaciones.Nombre, x.Productos.UnidadesMedida.Abreviatura,
                x.Cantidad, x.CantidadReservada, x.Cantidad - x.CantidadReservada, x.CostoPromedio, x.Cantidad * x.CostoPromedio)), f, t);
        return new(pagina, valor, monedas.Length == 1 ? monedas[0] : null, suc);
    }
    public async Task<RespuestaPaginada<KardexReporteDto>> KardexAsync(FiltroKardexReporte f, ContextoConfiguracion s, CancellationToken t)
    {
        var suc = await AlcanceAsync(f, f.SucursalId, s, t);
        var (desde, hasta) = Utc(Periodo(f, s, suc));
        var q = db.Kardex.AsNoTracking().Where(x => x.EmpresaId == s.EmpresaId && x.Bodegas.SucursalId == suc
            && x.MovimientoDetalles.Movimientos.EmpresaId == s.EmpresaId && x.Productos.EmpresaId == s.EmpresaId
            && x.FechaMovimiento >= desde && x.FechaMovimiento < hasta);
        if (f.ProductoId.HasValue) q = q.Where(x => x.ProductoId == f.ProductoId);
        if (f.BodegaId.HasValue) q = q.Where(x => x.BodegaId == f.BodegaId);
        if (f.Tipo is not null) q = q.Where(x => x.TipoAfectacion == f.Tipo);
        if (Texto(f.Concepto) is { } c) q = q.Where(x => x.MovimientoDetalles.Movimientos.ConceptosMovimientos.Codigo == c);
        if (Texto(f.Busqueda) is { } b) q = q.Where(x => x.Productos.SKU.Contains(b) || x.Productos.Nombre.Contains(b));
        return await PaginaAsync(q.OrderByDescending(x => x.FechaMovimiento).ThenByDescending(x => x.MovimientoDetalles.MovimientoId)
            .ThenByDescending(x => x.Id).Select(x => new KardexReporteDto(x.Id, x.FechaMovimiento,
                x.MovimientoDetalles.Movimientos.ConceptosMovimientos.Codigo, x.TipoAfectacion, x.ProductoId,
                x.Productos.SKU, x.Productos.Nombre, x.Productos.UnidadesMedida.Abreviatura, x.BodegaId, x.Bodegas.Nombre,
                x.Ubicaciones.Nombre, x.Cantidad, x.CostoUnitario, x.Cantidad * x.CostoUnitario, x.SaldoCantidad, x.SaldoCosto,
                x.MovimientoDetalles.MovimientoId, x.MovimientoDetalles.Movimientos.ModuloOrigen,
                x.MovimientoDetalles.Movimientos.DocumentoOrigenId,
                x.MovimientoDetalles.Movimientos.SerieDocumento + "-" + x.MovimientoDetalles.Movimientos.NumeroDocumento,
                x.MovimientoDetalles.Movimientos.CreadoPorUsuario.UserName)), f, t);
    }
    public async Task<ReporteDto<CompraReporteDto, TotalComprasDto>> ComprasAsync(FiltroComprasReporte f, ContextoConfiguracion s, CancellationToken t)
    {
        var suc = await AlcanceAsync(f, f.SucursalId, s, t);
        var p = Periodo(f, s, suc);
        // FechaEmision de la orden es civil, no UTC.
        var desde = p.FechaDesde.ToDateTime(TimeOnly.MinValue); var hasta = p.FechaHasta.AddDays(1).ToDateTime(TimeOnly.MinValue);
        var q = db.OrdenesCompra.AsNoTracking().Where(x => x.EmpresaId == s.EmpresaId && x.SucursalId == suc
            && x.FechaEmision >= desde && x.FechaEmision < hasta);
        if (f.ProveedorId.HasValue) q = q.Where(x => x.ProveedorId == f.ProveedorId);
        if (f.MonedaId.HasValue) q = q.Where(x => x.MonedaId == f.MonedaId);
        if (f.EsCredito.HasValue) q = q.Where(x => x.EsCredito == f.EsCredito);
        if (f.Estado is not null) q = q.Where(x => x.EstadosFlujo.Codigo == f.Estado);
        if (Texto(f.Busqueda) is { } b) q = q.Where(x => x.NumeroOrden.Contains(b) || x.Proveedor.NombreProveedor.Contains(b));
        var totales = await ResumenAsync(q.GroupBy(x => new { x.MonedaId, Moneda = x.Moneda.CodigoISO, Estado = x.EstadosFlujo.Codigo })
            .OrderBy(g => g.Key.MonedaId).ThenBy(g => g.Key.Estado)
            .Select(g => new TotalComprasDto(g.Key.MonedaId, g.Key.Moneda, g.Key.Estado, g.Count(), g.Sum(x => x.TotalEstimado))), t);
        var pagina = await PaginaAsync(q.OrderByDescending(x => x.FechaEmision).ThenByDescending(x => x.Id)
            .Select(x => new CompraReporteDto(x.Id, x.NumeroOrden, DateOnly.FromDateTime(x.FechaEmision), x.ProveedorId,
                x.Proveedor.NombreProveedor, x.EsCredito, x.EstadosFlujo.Codigo, x.MonedaId, x.Moneda.CodigoISO, x.TotalEstimado,
                x.OrdenesCompraDetalle.Count, x.OrdenesCompraDetalle.Count(d => d.RecepcionCompraDetalles.Sum(r => (decimal?)r.CantidadRecibida) >= d.Cantidad),
                x.RecepcionesCompra.Count, x.FacturasProveedores.Count(fp => fp.CuentasPorPagar != null) == 1
                    ? x.FacturasProveedores.Where(fp => fp.CuentasPorPagar != null).OrderBy(fp => fp.Id)
                        .Select(fp => (long?)fp.CuentasPorPagar!.Id).FirstOrDefault() : null,
                x.FacturasProveedores.Count(fp => fp.CuentasPorPagar != null) == 1
                    ? x.FacturasProveedores.Where(fp => fp.CuentasPorPagar != null && fp.MonedaId == x.MonedaId).OrderBy(fp => fp.Id)
                        .Select(fp => (decimal?)fp.CuentasPorPagar!.SaldoPendiente).FirstOrDefault() : null)), f, t, totales.Sum(x => x.Cantidad));
        return new(pagina, totales, p);
    }
}
