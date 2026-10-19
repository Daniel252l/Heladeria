using System.Net;
using System.Text.Json;
using Heladeria.Infraestructura.Persistencia;
using Heladeria.Pruebas.Integracion.Persistencia;
using Heladeria.Pruebas.Seguridad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Heladeria.Pruebas.Fase10;

public sealed class SaludHttpPruebas
{
    [Fact]
    public async Task RutasHttpExistenYSwaggerPublicaLiveYReadyComoAnonimas()
    {
        await using var fabrica = new FabricaCookieRefresh();
        using var cliente = fabrica.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false
        });
        foreach (var ruta in new[] { "/health/live", "/health/ready", "/salud" })
            Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync(ruta)).StatusCode);

        using var swagger = JsonDocument.Parse(await cliente.GetStringAsync("/swagger/v1/swagger.json"));
        var paths = swagger.RootElement.GetProperty("paths");
        foreach (var ruta in new[] { "/health/live", "/health/ready" })
        {
            Assert.True(paths.TryGetProperty(ruta, out var endpoint), $"La ruta HTTP {ruta} existe, pero falta en Swagger.");
            var get = endpoint.GetProperty("get");
            Assert.True(!get.TryGetProperty("security", out var seguridad) || seguridad.GetArrayLength() == 0);
            Assert.True(get.GetProperty("responses").TryGetProperty("200", out _));
        }
        Assert.True(paths.GetProperty("/health/ready").GetProperty("get").GetProperty("responses").TryGetProperty("503", out _));
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    public async Task LiveEsAnonimoYNoConstruyeNiEjecutaLaComprobacionSql(string ambiente)
    {
        var construcciones = 0;
        await using var fabrica = new FabricaSalud(ambiente, configurar: servicios =>
        {
            ExigirAutenticacionPorDefecto(servicios);
            servicios.Configure<HealthCheckServiceOptions>(o =>
                o.Registrations.Single(x => x.Name == "sql-server").Factory = _ =>
                {
                    Interlocked.Increment(ref construcciones);
                    throw new InvalidOperationException("Live no debe construir la dependencia SQL.");
                });
        });
        using var cliente = fabrica.CrearCliente();
        Assert.Null(cliente.DefaultRequestHeaders.Authorization);
        using var respuesta = await cliente.GetAsync("/health/live");
        await VerificarRespuestaAsync(respuesta, saludable: true, incluyeSql: false);
        Assert.Equal(0, construcciones);
    }

    [Theory]
    [InlineData("Development", true)]
    [InlineData("Development", false)]
    [InlineData("Production", true)]
    [InlineData("Production", false)]
    public async Task ReadyYSaludSonAnonimosSanitizadosYConservanElContrato(string ambiente, bool disponible)
    {
        var comprobacion = new ComprobacionContada(disponible);
        await using var fabrica = new FabricaSalud(ambiente, configurar: servicios =>
        {
            ExigirAutenticacionPorDefecto(servicios);
            servicios.Configure<HealthCheckServiceOptions>(o =>
                o.Registrations.Single(x => x.Name == "sql-server").Factory = _ => comprobacion);
        });
        using var cliente = fabrica.CrearCliente();
        Assert.Null(cliente.DefaultRequestHeaders.Authorization);
        using var ready = await cliente.GetAsync("/health/ready");
        await VerificarRespuestaAsync(ready, disponible, incluyeSql: true);
        Assert.Equal(1, comprobacion.Ejecuciones);
        using var salud = await cliente.GetAsync("/salud");
        await VerificarRespuestaAsync(salud, disponible, incluyeSql: true);
        Assert.Equal(2, comprobacion.Ejecuciones);
        Assert.Equal(await ready.Content.ReadAsStringAsync(), await salud.Content.ReadAsStringAsync());
        using var live = await cliente.GetAsync("/health/live");
        await VerificarRespuestaAsync(live, saludable: true, incluyeSql: false);
        Assert.Equal(2, comprobacion.Ejecuciones);
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    public async Task ReadySeleccionaSoloLasDependenciasEtiquetadasReady(string ambiente)
    {
        var sql = new ComprobacionContada(true);
        var otra = new ComprobacionContada(false);
        await using var fabrica = new FabricaSalud(ambiente, configurar: servicios =>
            servicios.Configure<HealthCheckServiceOptions>(o =>
            {
                var registro = o.Registrations.Single(x => x.Name == "sql-server");
                Assert.Contains("ready", registro.Tags);
                registro.Factory = _ => sql;
                o.Registrations.Add(new HealthCheckRegistration("ajena-a-ready", otra, null, ["otra"]));
            }));
        using var cliente = fabrica.CrearCliente();
        using var ready = await cliente.GetAsync("/health/ready");
        await VerificarRespuestaAsync(ready, saludable: true, incluyeSql: true);
        Assert.Equal(1, sql.Ejecuciones);
        Assert.Equal(0, otra.Ejecuciones);
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    public async Task SqlServerRealAisladoDeterminaReadySinAfectarLive(string ambiente)
    {
        await using var aislada = await BaseSqlServerAislada.CrearAsync();
        // Misma instancia privada; una base existente y otra inexistente. Nunca usa la conexión de la aplicación real.
        foreach (var disponible in new[] { true, false })
        {
            var cadena = new SqlConnectionStringBuilder(aislada.CadenaConexion) { ConnectTimeout = 2 };
            if (!disponible) cadena.InitialCatalog = "Inexistente_" + Guid.NewGuid().ToString("N");
            await using var fabrica = new FabricaSalud(ambiente, cadena.ConnectionString);
            using var cliente = fabrica.CrearCliente();
            using var scope = fabrica.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<HeladeriaDbContext>();
            Assert.Equal("Microsoft.EntityFrameworkCore.SqlServer", db.Database.ProviderName);
            Assert.True(string.Equals(cadena.ConnectionString, db.Database.GetConnectionString(), StringComparison.Ordinal),
                "La comprobación debe usar exclusivamente la conexión de la instancia privada del fixture.");
            using var live = await cliente.GetAsync("/health/live");
            await VerificarRespuestaAsync(live, saludable: true, incluyeSql: false);
            using var ready = await cliente.GetAsync("/health/ready");
            await VerificarRespuestaAsync(ready, disponible, incluyeSql: true);
            using var salud = await cliente.GetAsync("/salud");
            await VerificarRespuestaAsync(salud, disponible, incluyeSql: true);
            if (ambiente == "Production")
                Assert.Equal(HttpStatusCode.NotFound, (await cliente.GetAsync("/swagger/v1/swagger.json")).StatusCode);
        }
    }

    [Fact]
    public async Task SaludNoSeBloqueaPorUnaContrasenaTemporal()
    {
        await using var fabrica = new FabricaApiSeguridad(false);
        using var cliente = fabrica.CrearClienteAutenticado(requiereCambioContrasena: true);
        foreach (var ruta in new[] { "/health/live", "/health/ready", "/salud" })
            Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync(ruta)).StatusCode);
    }

    private static void ExigirAutenticacionPorDefecto(IServiceCollection servicios) =>
        servicios.AddAuthorization(o => o.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

    private static async Task VerificarRespuestaAsync(HttpResponseMessage respuesta, bool saludable, bool incluyeSql)
    {
        Assert.Equal(saludable ? HttpStatusCode.OK : HttpStatusCode.ServiceUnavailable, respuesta.StatusCode);
        Assert.Equal("application/json", respuesta.Content.Headers.ContentType?.MediaType);
        var cuerpo = await respuesta.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(cuerpo);
        Assert.Equal(["comprobaciones", "estado"], json.RootElement.EnumerateObject().Select(x => x.Name).Order().ToArray());
        Assert.Equal(saludable ? "saludable" : "no_saludable", json.RootElement.GetProperty("estado").GetString());
        var comprobaciones = json.RootElement.GetProperty("comprobaciones").EnumerateArray().ToArray();
        if (incluyeSql)
        {
            var sql = Assert.Single(comprobaciones);
            Assert.Equal(["estado", "nombre"], sql.EnumerateObject().Select(x => x.Name).Order().ToArray());
            Assert.Equal("sql-server", sql.GetProperty("nombre").GetString());
            Assert.Equal(saludable ? "healthy" : "unhealthy", sql.GetProperty("estado").GetString());
        }
        else Assert.Empty(comprobaciones);
        foreach (var sensible in new[] { "Password", "servidor-interno", "SQL_INTERNO", "ConnectionString", "SqlException", "stackTrace", "HeladeriaF7Xunit", "Inexistente_" })
            Assert.DoesNotContain(sensible, cuerpo, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class ComprobacionContada(bool disponible) : IHealthCheck
    {
        public int Ejecuciones { get; private set; }
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            Ejecuciones++;
            return Task.FromResult(disponible ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy(
                "Server=servidor-interno;Password=secreto;", new InvalidOperationException("SQL_INTERNO"),
                new Dictionary<string, object> { ["ConnectionString"] = "Password=secreto" }));
        }
    }

    private sealed class FabricaSalud(string ambiente, string? cadena = null, Action<IServiceCollection>? configurar = null)
        : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            var seguridad = EscenarioSeguridad.CrearOpciones();
            builder.UseEnvironment(ambiente);
            // Program valida la configuración durante el registro: proporcionarla al host antes de ejecutar Program.
            // No depender de secretos de desarrollo ni permitir que se capture una conexión de appsettings.
            var configuracion = new Dictionary<string, string?>
            {
                ["ConnectionStrings:Heladeria"] = cadena ?? "Server=127.0.0.1,1;Database=SaludPruebas;Integrated Security=true;TrustServerCertificate=true;Connect Timeout=1",
                ["Seguridad:Jwt:Emisor"] = seguridad.Jwt.Emisor,
                ["Seguridad:Jwt:Audiencia"] = seguridad.Jwt.Audiencia,
                ["Seguridad:Jwt:ClaveFirma"] = seguridad.Jwt.ClaveFirma,
                ["Seguridad:CookieRefresh:SameSite"] = "Strict"
            };
            foreach (var (clave, valor) in configuracion) builder.UseSetting(clave, valor);
            builder.ConfigureAppConfiguration((_, c) => c.AddInMemoryCollection(configuracion));
            builder.ConfigureTestServices(s => configurar?.Invoke(s));
        }

        public HttpClient CrearCliente() => CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false
        });
    }
}
