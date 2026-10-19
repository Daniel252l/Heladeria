using System.Net;
using Heladeria.Pruebas.Seguridad;

namespace Heladeria.Pruebas.Fase10;

public sealed class ReportesHttpPruebas
{
    public static TheoryData<string> Rutas => new() { "/api/reportes/ventas", "/api/reportes/productos-vendidos",
        "/api/reportes/inventario", "/api/reportes/kardex", "/api/reportes/compras", "/api/reportes/caja",
        "/api/reportes/cuentas-por-cobrar", "/api/reportes/cuentas-por-pagar", "/api/auditoria" };
    [Theory, MemberData(nameof(Rutas))]
    public async Task ExigeAutenticacionYPermiso(string ruta)
    {
        await using var f = new FabricaApiSeguridad(false);
        using var a = f.CrearCliente(); using var b = f.CrearClienteAutenticado();
        Assert.Equal(HttpStatusCode.Unauthorized, (await a.GetAsync(ruta)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await b.GetAsync(ruta)).StatusCode);
    }
    [Theory, MemberData(nameof(Rutas))]
    public async Task RechazaPaginacionAbusivaSinConsultarSql(string ruta)
    {
        await using var f = new FabricaApiSeguridad(true); using var c = f.CrearClienteAutenticado();
        foreach (var filtro in new[] { "pagina=0", "tamanioPagina=101", "pagina=2147483647&tamanioPagina=100", "sucursalId=-1", "fechaDesde=no-es-fecha" })
        {
            if (ruta.EndsWith("/inventario", StringComparison.Ordinal) && filtro.StartsWith("fechaDesde", StringComparison.Ordinal)) continue;
            var r = await c.GetAsync(ruta + "?" + filtro); Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
            Assert.Contains("traceId", await r.Content.ReadAsStringAsync());
        }
    }
    [Theory]
    [InlineData("/api/reportes/ventas?estado=ESTADO_INVALIDO")]
    [InlineData("/api/reportes/caja?usuarioId=-1")]
    [InlineData("/api/reportes/compras?proveedorId=0")]
    [InlineData("/api/reportes/cuentas-por-cobrar?vencimiento=NO_EXISTE")]
    [InlineData("/api/reportes/kardex?tipo=NO_EXISTE")]
    public async Task InputsInvalidos400(string ruta)
    {
        await using var f = new FabricaApiSeguridad(true); using var c = f.CrearClienteAutenticado();
        var r = await c.GetAsync(ruta); Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.DoesNotContain("NO_EXISTE", await r.Content.ReadAsStringAsync());
    }
    [Fact]
    public async Task LiveNoDependeSqlYHeadersDefensivos()
    {
        await using var f = new FabricaApiSeguridad(true); using var c = f.CrearCliente();
        var r = await c.GetAsync("/health/live"); Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.DoesNotContain("sql-server", await r.Content.ReadAsStringAsync());
        r = await c.GetAsync("/health/ready"); Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Contains("sql-server", await r.Content.ReadAsStringAsync());
        r = await c.GetAsync("/api/reportes/ventas");
        Assert.Equal("no-store", r.Headers.CacheControl?.ToString());
        Assert.Equal("nosniff", Assert.Single(r.Headers.GetValues("X-Content-Type-Options")));
    }
    [Theory, InlineData("/api/autenticacion/renovar"), InlineData("/api/autenticacion/iniciar-sesion")]
    public async Task OrigenAjenoRechazadoAntesDeAutenticacion(string ruta)
    {
        await using var f = new FabricaApiSeguridad(true); using var c = f.CrearCliente();
        c.DefaultRequestHeaders.Add("Origin", "https://ajeno.example");
        var r = await c.PostAsync(ruta, null); Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
        Assert.Contains("ORIGEN_NO_PERMITIDO", await r.Content.ReadAsStringAsync());
        Assert.False(r.Headers.Contains("Set-Cookie"));
    }
}
