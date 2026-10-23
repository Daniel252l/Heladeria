using System.Text.Json;
using Heladeria.Aplicacion.Configuracion;
using Heladeria.Aplicacion.Reportes;
using Heladeria.Infraestructura.Persistencia.Entidades;
using Heladeria.Infraestructura.ReportesOperativos;
using Heladeria.Pruebas.Fase5;
using Heladeria.Pruebas.Fase7;
using Microsoft.EntityFrameworkCore;

namespace Heladeria.Pruebas.Fase10;

public sealed class ReportesServicioPruebas
{
    internal sealed class RelojFijo : TimeProvider
    { public override DateTimeOffset GetUtcNow() => new(2026, 10, 19, 12, 0, 0, TimeSpan.Zero); }
    [Theory]
    [InlineData("ventas")] [InlineData("productos")] [InlineData("inventario")] [InlineData("kardex")]
    [InlineData("compras")] [InlineData("caja")] [InlineData("cxc")] [InlineData("cxp")] [InlineData("auditoria")]
    public async Task SucursalesSinAsignacionYEmpresaAjenaDenegadas(string reporte)
    {
        await using var e = await EscenarioFase5.CrearAsync();
        var r = new ServicioReportes(e.Contexto, EscenarioFase5.Opciones, new RelojFijo());
        async Task Leer(long sucursal, ContextoConfiguracion s)
        {
            switch (reporte)
            {
                case "ventas": await r.VentasAsync(new() { SucursalId = sucursal }, s, default); break;
                case "productos": await r.ProductosAsync(new() { SucursalId = sucursal }, s, default); break;
                case "inventario": await r.InventarioAsync(new() { SucursalId = sucursal }, s, default); break;
                case "kardex": await r.KardexAsync(new() { SucursalId = sucursal }, s, default); break;
                case "compras": await r.ComprasAsync(new() { SucursalId = sucursal }, s, default); break;
                case "caja": await r.CajaAsync(new() { SucursalId = sucursal }, s, default); break;
                case "cxc": await r.CarteraAsync(false, new() { SucursalId = sucursal }, s, default); break;
                case "cxp": await r.CarteraAsync(true, new() { SucursalId = sucursal }, s, default); break;
                default: await r.AuditoriaAsync(new() { SucursalId = sucursal }, s, default); break;
            }
        }
        foreach (var suc in new long[] { 3, 4, 9999 })
            Assert.Equal(403, (await Assert.ThrowsAsync<ExcepcionReporte>(() => Leer(suc, EscenarioFase5.Seguridad))).EstadoHttp);
        Assert.Equal(403, (await Assert.ThrowsAsync<ExcepcionReporte>(() => Leer(1, EscenarioFase5.Seguridad with { EmpresaId = 2 }))).EstadoHttp);
    }
    [Fact]
    public async Task VentasTotalesGlobalesOrdenPaginaCostosYProductos()
    {
        await using var e = await EscenarioFase7.CrearAsync();
        await e.CompletarAsync(await e.ConfirmarAsync()); await e.CompletarAsync(await e.ConfirmarAsync());
        foreach (var v in await e.Contexto.Ventas.ToArrayAsync()) v.FechaVenta = new DateTime(2026, 10, 19, 12, 0, 0, DateTimeKind.Utc);
        await e.Contexto.SaveChangesAsync(); e.Contexto.ChangeTracker.Clear();
        var r = new ServicioReportes(e.Contexto, EscenarioFase5.Opciones, new RelojFijo());
        var primera = await r.VentasAsync(new() { TamanioPagina = 1 }, EscenarioFase5.Seguridad, default);
        var segunda = await r.VentasAsync(new() { TamanioPagina = 1, Pagina = 2 }, EscenarioFase5.Seguridad, default);
        Assert.Equal(2, primera.Ventas.Datos.TotalRegistros); Assert.Single(primera.Ventas.Datos.Elementos);
        Assert.True(primera.Ventas.Datos.Elementos[0].Id > segunda.Ventas.Datos.Elementos[0].Id);
        Assert.Equal(100, Assert.Single(primera.Ventas.Totales).Total); Assert.Equal(primera.Ventas.Totales, segunda.Ventas.Totales);
        Assert.Equal(40, primera.Ventas.Totales[0].Costo); Assert.Equal(60, primera.Ventas.Totales[0].MargenBruto);
        // Ajuste B: completar contado también cobra; los totales/costos conservan todas las expectativas previas.
        Assert.All(primera.Ventas.Datos.Elementos, x => Assert.Equal("COBRADA", x.EstadoCobro));
        var productos = await r.ProductosAsync(new(), EscenarioFase5.Seguridad, default);
        var producto = Assert.Single(productos.Elementos);
        Assert.Equal(4, producto.Cantidad); Assert.Equal(4000, producto.CantidadBase); Assert.Equal(100, producto.Importe);
        Assert.Equal(40, producto.Costo); Assert.Equal(60, producto.MargenBruto);
        Assert.Empty((await r.VentasAsync(new() { Busqueda = "no existe" }, EscenarioFase5.Seguridad, default)).Ventas.Datos.Elementos);
        Assert.Empty((await r.VentasAsync(new() { MonedaId = 999 }, EscenarioFase5.Seguridad, default)).Ventas.Totales);
        Assert.Empty(e.Contexto.ChangeTracker.Entries());
    }
    [Fact]
    public async Task FechaCivilUtilizaZonaYHastaIncluyeTodoElDia()
    {
        await using var e = await EscenarioFase7.CrearAsync(); var venta = await e.CompletarAsync(await e.ConfirmarAsync());
        var entidad = await e.Contexto.Ventas.SingleAsync();
        var r = new ServicioReportes(e.Contexto, EscenarioFase5.Opciones, new RelojFijo());
        var filtro = new FiltroVentasReporte { FechaDesde = new(2026, 10, 19), FechaHasta = new(2026, 10, 19) };
        foreach (var (fecha, cantidad) in new[] { (new DateTime(2026, 10, 19, 5, 59, 59, DateTimeKind.Utc), 0),
            (new DateTime(2026, 10, 19, 6, 0, 0, DateTimeKind.Utc), 1), (new DateTime(2026, 10, 20, 5, 59, 59, DateTimeKind.Utc), 1),
            (new DateTime(2026, 10, 20, 6, 0, 0, DateTimeKind.Utc), 0) })
        {
            entidad.FechaVenta = fecha; await e.Contexto.SaveChangesAsync();
            Assert.Equal(cantidad, (await r.VentasAsync(filtro, EscenarioFase5.Seguridad, default)).Ventas.Datos.TotalRegistros);
        }
        await Assert.ThrowsAsync<ExcepcionReporte>(() => r.VentasAsync(new() { FechaDesde = new(2025, 1, 1), FechaHasta = new(2026, 10, 19) }, EscenarioFase5.Seguridad, default));
        await Assert.ThrowsAsync<ExcepcionReporte>(() => r.VentasAsync(new() { FechaDesde = new(2026, 10, 20), FechaHasta = new(2026, 10, 19) }, EscenarioFase5.Seguridad, default));
    }
    [Fact]
    public async Task InventarioValorizadoFiltraBodegaYNoIncluyeServicios()
    {
        await using var e = await EscenarioFase5.CrearAsync(); var s = EscenarioFase5.Seguridad;
        var r = new ServicioReportes(e.Contexto, EscenarioFase5.Opciones, new RelojFijo());
        var resultado = await r.InventarioAsync(new(), s, default);
        Assert.Equal(50, resultado.ValorInventario); Assert.Equal(5000, Assert.Single(resultado.Datos.Elementos).Existencia);
        Assert.Equal("GTQ", resultado.MonedaBase);
        Assert.Empty((await r.InventarioAsync(new() { BodegaId = 2 }, s, default)).Datos.Elementos);
        Assert.Equal(9, (await r.InventarioAsync(new() { SucursalId = 2 }, s, default)).ValorInventario);
        var tipo = await e.Contexto.TiposProductos.SingleAsync(x => x.Id == 1); tipo.ManejaExistencia = false;
        await e.Contexto.SaveChangesAsync();
        Assert.Empty((await r.InventarioAsync(new(), s, default)).Datos.Elementos);
    }
    [Fact]
    public async Task MonedasNoSeMezclanYNuncaSeInventaMargenHistorico()
    {
        await using var e = await EscenarioFase7.CrearAsync();
        await e.CompletarAsync(await e.ConfirmarAsync()); await e.CompletarAsync(await e.ConfirmarAsync());
        var moneda = new Monedas { EmpresaId = 1, CodigoISO = "USD", NombreMoneda = "Dólar", Simbolo = "$", Decimales = 2, EstadoId = 1 };
        e.Contexto.Monedas.Add(moneda); await e.Contexto.SaveChangesAsync();
        var ventas = await e.Contexto.Ventas.OrderBy(x => x.Id).ToArrayAsync();
        foreach (var v in ventas) v.FechaVenta = new RelojFijo().GetUtcNow().UtcDateTime;
        ventas[1].MonedaId = moneda.Id; ventas[1].TipoCambio = 7.5m;
        await e.Contexto.SaveChangesAsync(); e.Contexto.ChangeTracker.Clear();
        var r = new ServicioReportes(e.Contexto, EscenarioFase5.Opciones, new RelojFijo());
        var resultado = await r.VentasAsync(new(), EscenarioFase5.Seguridad, default);
        Assert.Equal(2, resultado.Ventas.Totales.Count); Assert.All(resultado.Ventas.Totales, x => Assert.Equal(50, x.Total));
        var usd = resultado.Ventas.Totales.Single(x => x.Moneda == "USD"); Assert.Null(usd.Costo); Assert.Null(usd.MargenBruto);
        var linea = await e.Contexto.VentasDetalle.FirstAsync(x => x.VentaId == ventas[0].Id); linea.CostoTotal = null;
        await e.Contexto.SaveChangesAsync(); e.Contexto.ChangeTracker.Clear();
        Assert.Null((await r.VentasAsync(new() { MonedaId = 1 }, EscenarioFase5.Seguridad, default)).Ventas.Totales[0].Costo);
    }
    [Theory, InlineData(1), InlineData(9999)]
    public async Task FechasExtremasControladas(int anio)
    {
        await using var e = await EscenarioFase5.CrearAsync();
        var r = new ServicioReportes(e.Contexto, EscenarioFase5.Opciones, new RelojFijo());
        var error = await Assert.ThrowsAsync<ExcepcionReporte>(() => r.ComprasAsync(new() { FechaHasta = new(anio, 1, 1) }, EscenarioFase5.Seguridad, default));
        Assert.Equal(400, error.EstadoHttp);
    }
    [Fact]
    public async Task AuditoriaFiltrosPaginacionAislamientoYDtoSinPayloads()
    {
        await using var e = await EscenarioFase5.CrearAsync(); var ahora = new RelojFijo().GetUtcNow().UtcDateTime;
        for (var i = 0; i < 4; i++) e.Contexto.Bitacoras.Add(new Bitacoras { EmpresaId = i == 3 ? 2 : 1,
            SucursalId = i == 2 ? 4 : 1, UsuarioId = 1, ModuloId = "SEGURIDAD", Entidad = "Usuarios", EntidadId = "1",
            Accion = "ACTUALIZADO", Fecha = ahora, ValorAnterior = "password hash token PAN CVV",
            ValorNuevo = "refresh-token-secret", Comentario = "confidencial", Equipo = "cookie" });
        await e.Contexto.SaveChangesAsync(); e.Contexto.ChangeTracker.Clear();
        var r = new ServicioReportes(e.Contexto, EscenarioFase5.Opciones, new RelojFijo());
        var f = new FiltroAuditoria { UsuarioId = 1, Modulo = "SEGURIDAD", Accion = "ACTUALIZADO", Entidad = "Usuarios", Busqueda = "Usuarios", TamanioPagina = 1 };
        var resultado = await r.AuditoriaAsync(f, EscenarioFase5.Seguridad, default);
        Assert.Equal(2, resultado.TotalRegistros); Assert.Single(resultado.Elementos);
        Assert.Equal("REGISTRADA", resultado.Elementos[0].Resultado);
        var json = JsonSerializer.Serialize(resultado);
        foreach (var secreto in new[] { "password", "hash", "token", "PAN", "CVV", "confidencial", "cookie", "ValorAnterior", "ValorNuevo", "Comentario" }) Assert.DoesNotContain(secreto, json);
        Assert.Empty(e.Contexto.ChangeTracker.Entries());
    }
}
