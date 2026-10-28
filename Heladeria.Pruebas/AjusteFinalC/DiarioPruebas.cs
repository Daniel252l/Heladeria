using System.Net;
using Heladeria.Aplicacion.Reportes;
using Heladeria.Aplicacion.Seguridad.Configuracion;
using Heladeria.Infraestructura.ReportesOperativos;
using Heladeria.Pruebas.Fase5;
using Heladeria.Pruebas.Fase7;
using Heladeria.Pruebas.Fase10;
using Heladeria.Pruebas.Seguridad;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Heladeria.Pruebas.AjusteFinalC;

public sealed class DiarioPruebas
{
    [Fact]
    public async Task DiaVacioDefaultLocalYAislamientoDeSucursal()
    {
        await using var e = await EscenarioFase7.CrearAsync(); var s = EscenarioFase7.Seguridad;
        var r = new ServicioReportes(e.Contexto, EscenarioFase5.Opciones, new Reloj(new(2026, 10, 20, 3, 0, 0, TimeSpan.Zero)));
        var d = await r.DiarioAsync(new(), s, default);
        Assert.Equal(new DateOnly(2026, 10, 19), d.Periodo.FechaDesde);
        Assert.Equal(new DateTime(2026, 10, 19, 6, 0, 0, DateTimeKind.Utc), d.DesdeUtc);
        Assert.Empty(d.VentasTotales); Assert.Empty(d.CobrosPorForma); Assert.Empty(d.CreditosGenerados);
        await e.CompletarAsync(await e.ConfirmarAsync());
        Assert.Empty((await r.DiarioAsync(new() { SucursalId = 2 }, s, default)).Ventas.Elementos);
        foreach (var sucursal in new long[] { 3, 999 }) Assert.Equal(403, (await Assert.ThrowsAsync<ExcepcionReporte>(() => r.DiarioAsync(new() { SucursalId = sucursal }, s, default))).EstadoHttp);
        Assert.Equal(403, (await Assert.ThrowsAsync<ExcepcionReporte>(() => r.DiarioAsync(new(), s with { EmpresaId = 2 }, default))).EstadoHttp);
        Assert.Equal(403, (await Assert.ThrowsAsync<ExcepcionReporte>(() => r.DiarioAsync(new() { CajaId = 999 }, s, default))).EstadoHttp);
        Assert.Equal(403, (await Assert.ThrowsAsync<ExcepcionReporte>(() => r.DiarioAsync(new() { FormaPagoId = 999 }, s, default))).EstadoHttp);
    }
    [Fact]
    public async Task LimitesDeCierreNoFechaBorradorYTotalesSinDuplicacionPaginados()
    {
        await using var e = await EscenarioFase7.CrearAsync(); var s = EscenarioFase7.Seguridad;
        await e.CompletarAsync(await e.ConfirmarAsync()); await e.CompletarAsync(await e.ConfirmarAsync());
        var filas = await e.Contexto.Ventas.OrderBy(x => x.Id).ToArrayAsync();
        var inicio = new DateTime(2026, 10, 19, 6, 0, 0, DateTimeKind.Utc);
        foreach (var v in filas) { v.FechaVenta = inicio.AddDays(-20); v.FechaCompletada = inicio; }
        foreach (var p in await e.Contexto.Pagos.ToArrayAsync()) p.Fecha = inicio;
        foreach (var m in await e.Contexto.MovimientosCaja.ToArrayAsync()) m.Fecha = inicio;
        await e.Contexto.SaveChangesAsync(); e.Contexto.ChangeTracker.Clear();
        var r = new ServicioReportes(e.Contexto, EscenarioFase5.Opciones, new ReportesServicioPruebas.RelojFijo());
        var primera = await r.DiarioAsync(new() { TamanioPagina = 1 }, s, default);
        var segunda = await r.DiarioAsync(new() { TamanioPagina = 1, Pagina = 2, PaginaOperaciones = 2 }, s, default);
        Assert.Equal(2, primera.Ventas.TotalRegistros); Assert.Single(primera.Ventas.Elementos);
        Assert.NotEqual(primera.Ventas.Elementos[0].Id, segunda.Ventas.Elementos[0].Id);
        Assert.NotEqual(primera.Operaciones.Elementos[0].Id, segunda.Operaciones.Elementos[0].Id);
        Assert.Equal(100, Assert.Single(primera.VentasTotales).Total); Assert.Equal(100, primera.CobrosPorForma.Sum(x => x.Aplicado));
        Assert.Equal(100, Assert.Single(primera.Finanzas).Ingresos); Assert.Equal(primera.VentasTotales, segunda.VentasTotales);
        Assert.Equal(40, primera.VentasTotales[0].Costo); Assert.Equal(60, primera.VentasTotales[0].MargenBruto);
        foreach (var (fecha, cantidad) in new[] { (inicio.AddTicks(-1), 0), (inicio, 2), (inicio.AddDays(1).AddTicks(-1), 2), (inicio.AddDays(1), 0) }) {
            foreach (var v in await e.Contexto.Ventas.ToArrayAsync()) v.FechaCompletada = fecha;
            await e.Contexto.SaveChangesAsync(); e.Contexto.ChangeTracker.Clear();
            Assert.Equal(cantidad, (await r.DiarioAsync(new(), s, default)).Ventas.TotalRegistros);
        }
        Assert.Empty(e.Contexto.ChangeTracker.Entries());
    }
    [Fact]
    public async Task AnuladaSeparadaYFiltroModalidad()
    {
        await using var e = await EscenarioFase7.CrearAsync(); var s = EscenarioFase7.Seguridad;
        var venta = await e.HistoricaSinCobroAsync(await e.ConfirmarAsync());
        await e.Ventas.AnularAsync(venta.Encabezado.Id, new() { VersionFila = venta.Encabezado.VersionFila, Motivo = "Prueba de lectura" }, s, default);
        var r = new ServicioReportes(e.Contexto, EscenarioFase5.Opciones, TimeProvider.System);
        var d = await r.DiarioAsync(new(), s, default); Assert.Equal("ANULADA", Assert.Single(d.VentasTotales).Estado);
        Assert.Null(d.VentasTotales[0].Costo); Assert.True(Assert.Single(d.Ventas.Elementos).TieneComprobante);
        Assert.Empty((await r.DiarioAsync(new() { EsCredito = true }, s, default)).Ventas.Elementos);
    }
    [Theory, InlineData("America/Guatemala", 2026, 10, 19, 24), InlineData("America/New_York", 2026, 3, 8, 23), InlineData("America/New_York", 2026, 11, 1, 25)]
    public async Task RangoUsaZonaEfectivaSinRestarHorasFijas(string zona, int anio, int mes, int dia, int horas)
    {
        await using var e = await EscenarioFase7.CrearAsync();
        var opciones = Options.Create(new OpcionesSeguridad { ZonasHorariasSucursales = new() { [1] = zona } });
        var r = new ServicioReportes(e.Contexto, opciones, TimeProvider.System);
        var d = await r.DiarioAsync(new() { Fecha = new(anio, mes, dia) }, EscenarioFase7.Seguridad, default);
        Assert.Equal(zona, d.Periodo.ZonaHoraria); Assert.Equal(horas, (d.HastaUtcExclusivo - d.DesdeUtc).TotalHours);
    }
    [Theory, InlineData("/api/reportes/diario"), InlineData("/api/ventas/1")]
    public async Task LecturasExigenSesionPermisoYNoStore(string ruta)
    {
        await using var f = new FabricaApiSeguridad(false); using var anonimo = f.CrearCliente(); using var sinPermiso = f.CrearClienteAutenticado();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.GetAsync(ruta)).StatusCode);
        var denegada = await sinPermiso.GetAsync(ruta); Assert.Equal(HttpStatusCode.Forbidden, denegada.StatusCode);
        Assert.Equal("no-store", denegada.Headers.CacheControl?.ToString());
    }
    [Theory, InlineData("fecha=0001-01-01"), InlineData("fecha=9999-01-01"), InlineData("fecha=mal"), InlineData("pagina=0"), InlineData("tamanioPagina=101"), InlineData("paginaOperaciones=2147483647&tamanioPagina=100"), InlineData("paginaCajas=0"), InlineData("formaPagoId=-1"), InlineData("cajaId=0")]
    public async Task EntradaInvalida400AntesDeSql(string query)
    {
        await using var f = new FabricaApiSeguridad(true); using var c = f.CrearClienteAutenticado();
        Assert.Equal(HttpStatusCode.BadRequest, (await c.GetAsync("/api/reportes/diario?" + query)).StatusCode);
    }
    private sealed class Reloj(DateTimeOffset ahora) : TimeProvider { public override DateTimeOffset GetUtcNow() => ahora; }
}
