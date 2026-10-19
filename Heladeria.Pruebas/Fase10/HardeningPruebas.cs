using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Text;
using Heladeria.Api.ManejoErrores;
using Heladeria.Aplicacion.Seguridad.Configuracion;
using Heladeria.Pruebas.Seguridad;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Heladeria.Pruebas.Fase10;

public sealed class HardeningPruebas
{
    [Fact]
    public async Task ExcepcionYLogsNoRevelanSecretosNiSql()
    {
        var logger = new LoggerCaptura(); var handler = new ManejadorGlobalExcepciones(logger);
        var c = new DefaultHttpContext(); c.Response.Body = new MemoryStream(); c.TraceIdentifier = "traza-controlada";
        var secreto = "Password=SECRETO; JWT=eyTokenCompleto; refresh=secreto-refresh; SELECT * FROM Usuarios";
        await handler.TryHandleAsync(c, new InvalidOperationException(secreto), default);
        c.Response.Body.Position = 0; var cuerpo = await new StreamReader(c.Response.Body).ReadToEndAsync();
        Assert.Equal(500, c.Response.StatusCode); Assert.Contains("traza-controlada", cuerpo); Assert.Contains("ERROR_INTERNO", cuerpo);
        foreach (var texto in new[] { cuerpo, string.Join("\n", logger.Mensajes) })
            foreach (var sensible in new[] { "SECRETO", "eyTokenCompleto", "secreto-refresh", "SELECT", "StackTrace" }) Assert.DoesNotContain(sensible, texto);
        Assert.All(logger.Excepciones, Assert.Null);
    }
    [Fact]
    public async Task AlgoritmoJwtDistintoAlEmisorEsRechazado()
    {
        await using var f = new FabricaApiSeguridad(true); using var c = f.CrearCliente();
        var o = f.Services.GetRequiredService<IOptions<OpcionesSeguridad>>().Value;
        // Misma clave válida de pruebas: solo cambia el algoritmo, para aislar la restricción HS256.
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(o.Jwt.ClaveFirma));
        var jwt = new JwtSecurityToken(o.Jwt.Emisor, o.Jwt.Audiencia, expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha384));
        c.DefaultRequestHeaders.Authorization = new("Bearer", new JwtSecurityTokenHandler().WriteToken(jwt));
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync("/api/reportes/ventas")).StatusCode);
        var options = f.Services.GetRequiredService<IOptionsMonitor<Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions>>().Get("Bearer");
        Assert.Equal([SecurityAlgorithms.HmacSha256], options.TokenValidationParameters.ValidAlgorithms);
    }
    [Fact]
    public async Task ReadyFallaSanitizadoPeroLiveSigueVivo()
    {
        await using var f = new FabricaApiSeguridad(true, servicios => servicios.Configure<HealthCheckServiceOptions>(o => {
            o.Registrations.Single(x => x.Name == "sql-server").Factory = _ => new DependenciaCaida();
        }));
        using var c = f.CrearCliente();
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync("/health/live")).StatusCode);
        var ready = await c.GetAsync("/health/ready"); Assert.Equal(HttpStatusCode.ServiceUnavailable, ready.StatusCode);
        Assert.DoesNotContain("Password", await ready.Content.ReadAsStringAsync());
    }
    [Fact]
    public async Task SwaggerMismoOrigenNoEsRechazadoPorProteccionCookie()
    {
        await using var f = new FabricaApiSeguridad(true); using var c = f.CrearCliente();
        c.DefaultRequestHeaders.Add("Origin", "https://localhost");
        var r = await c.PostAsync("/api/autenticacion/renovar", null);
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode); // Sin cookie; no 403 por Origin.
    }
    [Theory, InlineData(1, 20, 0), InlineData(2, 20, 20), InlineData(int.MaxValue, 100, int.MaxValue)]
    public void DesplazamientoOperativoNoDesborda(int pagina, int tamanio, int esperado)
    {
        var f = new Heladeria.Aplicacion.Configuracion.FiltroActivosSolicitud { Pagina = pagina, TamanioPagina = tamanio };
        Assert.Equal(esperado, f.CalcularDesplazamiento());
    }
    private sealed class DependenciaCaida : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
            Task.FromResult(HealthCheckResult.Unhealthy("Password=no-publicar", new Exception("No publicar conexión")));
    }
    private sealed class LoggerCaptura : ILogger<ManejadorGlobalExcepciones>
    {
        public List<string> Mensajes { get; } = [];
        public List<Exception?> Excepciones { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        { Mensajes.Add(formatter(state, exception)); Excepciones.Add(exception); }
    }
}
