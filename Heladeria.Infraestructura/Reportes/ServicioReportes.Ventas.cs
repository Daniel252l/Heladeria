using Heladeria.Aplicacion.Configuracion;
using Heladeria.Aplicacion.Reportes;
using Heladeria.Infraestructura.Persistencia.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Heladeria.Infraestructura.ReportesOperativos;

public sealed partial class ServicioReportes
{
    private async Task<TotalVentasDto[]> TotalesVentasAsync(IQueryable<Ventas> q, CancellationToken t)
    {
        var totales = await ResumenAsync(q.GroupBy(x => new { x.MonedaId, Moneda = x.Moneda.CodigoISO, Estado = x.EstadosFlujo.Codigo })
            .OrderBy(g => g.Key.MonedaId).ThenBy(g => g.Key.Estado)
            .Select(g => new TotalVentasDto(g.Key.MonedaId, g.Key.Moneda, g.Key.Estado, g.Count(),
                g.Sum(x => x.SubTotal), g.Sum(x => x.Descuento), g.Sum(x => x.Impuestos), g.Sum(x => x.TotalVenta), null, null)), t);
        // Agregar sobre detalles, no SUM de subconsultas correlacionadas (SQL Server no lo admite).
        var costos = await ResumenAsync(db.VentasDetalle.AsNoTracking().Where(x => q.Select(v => v.Id).Contains(x.VentaId))
            .GroupBy(x => new { x.Venta.MonedaId, Estado = x.Venta.EstadosFlujo.Codigo })
            .OrderBy(g => g.Key.MonedaId).ThenBy(g => g.Key.Estado)
            .Select(g => new { g.Key.MonedaId, g.Key.Estado, Costo = g.Sum(x => x.CostoTotal),
                Desconocidos = g.Count(x => x.CostoTotal == null || x.Venta.TipoCambio != 1 || !x.Venta.Moneda.EsMonedaBase),
                Ventas = g.Select(x => x.VentaId).Distinct().Count() }), t);
        totales = totales.Select(x => {
            var c = costos.SingleOrDefault(c => c.MonedaId == x.MonedaId && c.Estado == x.Estado);
            var costo = x.Estado == "COMPLETADA" && c is { Desconocidos: 0 } && c.Ventas == x.Cantidad ? c.Costo : null;
            return x with { Costo = costo, MargenBruto = x.Subtotal - x.Descuentos - costo };
        }).ToArray();
        return totales;
    }
    private IQueryable<Ventas> VentasFiltradas(FiltroVentasReporte f, ContextoConfiguracion s, PeriodoReporte p)
    {
        var (desde, hasta) = Utc(p);
        var estado = f.Estado ?? "COMPLETADA";
        var q = db.Ventas.AsNoTracking().Where(x => x.EmpresaId == s.EmpresaId && x.SucursalId == p.SucursalId
            && x.EstadosFlujo.FlujoEstado.Codigo == "VENTA_POS" && x.EstadosFlujo.Codigo == estado
            && x.FechaVenta >= desde && x.FechaVenta < hasta);
        if (f.ClienteId.HasValue) q = q.Where(x => x.ClienteId == f.ClienteId);
        if (f.MonedaId.HasValue) q = q.Where(x => x.MonedaId == f.MonedaId);
        if (Texto(f.Busqueda) is { } b) q = q.Where(x => x.NumeroFactura.Contains(b) || x.Cliente != null && x.Cliente.NombreCliente.Contains(b));
        return q;
    }
    public async Task<ReporteVentasDto> VentasAsync(FiltroVentasReporte f, ContextoConfiguracion s, CancellationToken t)
    {
        var suc = await AlcanceAsync(f, f.SucursalId, s, t);
        var periodo = Periodo(f, s, suc);
        var q = VentasFiltradas(f, s, periodo);
        var totales = await TotalesVentasAsync(q, t);
        var proyectada = q.Select(v => new {
            v.Id, Numero = v.NumeroFactura, Fecha = v.FechaVenta, Estado = v.EstadosFlujo.Codigo,
            v.ClienteId, Cliente = v.Cliente == null ? null : v.Cliente.NombreCliente, v.EsCredito,
            v.MonedaId, Moneda = v.Moneda.CodigoISO, v.SubTotal, v.Descuento, v.Impuestos, v.TotalVenta,
            Costo = v.EstadosFlujo.Codigo == "COMPLETADA" && v.TipoCambio == 1 && v.Moneda.EsMonedaBase
                && v.VentasDetalle.Any() && !v.VentasDetalle.Any(l => l.CostoTotal == null)
                ? v.VentasDetalle.Sum(l => l.CostoTotal) : null,
            Cobrado = (db.Pagos.Where(p => p.EmpresaId == s.EmpresaId && p.SucursalId == suc && p.VentaId == v.Id
                && p.Estado.Activo && activos.Contains(p.Estado.Nombre)).Sum(p => (decimal?)p.MontoTotal) ?? 0)
                + (db.AplicacionesCuentasPorCobrar.Where(a => a.CuentaPorCobrar.VentaId == v.Id
                    && a.Pago.EmpresaId == s.EmpresaId && a.Pago.SucursalId == suc && a.Pago.Estado.Activo
                    && activos.Contains(a.Pago.Estado.Nombre)).Sum(a => (decimal?)a.MontoAplicado) ?? 0)
        });
        var pagina = await PaginaAsync(proyectada.OrderByDescending(x => x.Fecha).ThenByDescending(x => x.Id), f, t, totales.Sum(x => x.Cantidad));
        var filas = pagina.Elementos.Select(x => new VentaReporteDto(x.Id, x.Numero, x.Fecha, x.Estado, x.ClienteId,
            x.Cliente, x.EsCredito, x.MonedaId, x.Moneda, x.SubTotal, x.Descuento, x.Impuestos, x.TotalVenta,
            x.Costo, x.SubTotal - x.Descuento - x.Costo, x.Cobrado, x.Estado != "COMPLETADA" ? "NO_APLICA"
                : x.TotalVenta == 0 ? "SIN_IMPORTE" : x.Cobrado >= x.TotalVenta ? "COBRADA" : x.Cobrado > 0 ? "PARCIAL" : "PENDIENTE")).ToArray();
        var formas = await ResumenAsync(db.PagoDetalles.AsNoTracking().Where(d => d.Pago.EmpresaId == s.EmpresaId
            && d.Pago.SucursalId == suc && d.Pago.Estado.Activo && activos.Contains(d.Pago.Estado.Nombre)
            && (d.Pago.VentaId != null && q.Select(v => v.Id).Contains(d.Pago.VentaId.Value)
                || d.Pago.VentaId == null && d.Pago.AplicacionesCuentasPorCobrar.Count == 1
                && d.Pago.AplicacionesCuentasPorCobrar.Any(a => a.MontoAplicado == d.Pago.MontoTotal && q.Select(v => v.Id).Contains(a.CuentaPorCobrar.VentaId))))
            .GroupBy(d => new { d.Pago.MonedaId, Moneda = d.Pago.Moneda.CodigoISO, d.FormaPagoId, Forma = d.FormaPago.Nombre })
            .OrderBy(g => g.Key.MonedaId).ThenBy(g => g.Key.FormaPagoId)
            .Select(g => new CobrosFormaReporteDto(g.Key.MonedaId, g.Key.Moneda, g.Key.FormaPagoId, g.Key.Forma, g.Sum(x => x.Monto))), t);
        return new(new(new(filas, f.Pagina, f.TamanioPagina, pagina.TotalRegistros), totales, periodo), formas);
    }
    public async Task<RespuestaPaginada<ProductoVendidoDto>> ProductosAsync(FiltroProductosVendidos f, ContextoConfiguracion s, CancellationToken t)
    {
        if (f.Estado is not (null or "COMPLETADA")) throw new ExcepcionReporte("Productos vendidos solo incluye ventas COMPLETADAS.");
        var suc = await AlcanceAsync(f, f.SucursalId, s, t);
        var ventas = VentasFiltradas(f, s, Periodo(f, s, suc));
        var q = db.VentasDetalle.AsNoTracking().Where(x => ventas.Select(v => v.Id).Contains(x.VentaId));
        if (f.ProductoId.HasValue) q = q.Where(x => x.ProductoId == f.ProductoId);
        var agrupada = q.GroupBy(x => new { x.ProductoId, x.Producto.SKU, Producto = x.Producto.Nombre,
            x.ProductoPresentacionId, Presentacion = x.ProductoPresentacion == null ? null : x.ProductoPresentacion.Presentaciones.Nombre,
            Unidad = x.UnidadMedida == null ? null : x.UnidadMedida.Abreviatura,
            UnidadBase = x.UnidadBase == null ? null : x.UnidadBase.Abreviatura, x.Venta.MonedaId, Moneda = x.Venta.Moneda.CodigoISO })
            .Select(g => new { g.Key, Cantidad = g.Sum(x => x.Cantidad),
                CantidadBase = g.Count(x => x.CantidadBase == null) == 0 ? g.Sum(x => x.CantidadBase) : null,
                Importe = g.Count(x => x.TotalLinea == null) == 0 ? g.Sum(x => x.TotalLinea) : null,
                Costo = g.Count(x => x.CostoTotal == null || x.Venta.TipoCambio != 1 || !x.Venta.Moneda.EsMonedaBase) == 0 ? g.Sum(x => x.CostoTotal) : null,
                Neto = g.Count(x => x.SubtotalNeto == null) == 0 ? g.Sum(x => x.SubtotalNeto - x.MontoDescuento) : null });
        return await PaginaAsync(agrupada.OrderBy(x => x.Key.ProductoId).ThenBy(x => x.Key.ProductoPresentacionId)
            .ThenBy(x => x.Key.Unidad).ThenBy(x => x.Key.UnidadBase).ThenBy(x => x.Key.MonedaId)
            .Select(x => new ProductoVendidoDto(x.Key.ProductoId, x.Key.SKU, x.Key.Producto, x.Key.ProductoPresentacionId,
                x.Key.Presentacion, x.Key.Unidad, x.Key.UnidadBase, x.Key.MonedaId, x.Key.Moneda, x.Cantidad,
                x.CantidadBase, x.Importe, x.Costo, x.Neto - x.Costo)), f, t);
    }
}
