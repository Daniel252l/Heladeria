using System.Net;
using System.Net.Http.Json;
using Heladeria.Aplicacion.Seguridad.Autenticacion;
using Heladeria.Aplicacion.Seguridad.Configuracion;
using Heladeria.Aplicacion.Seguridad.Excepciones;
using Heladeria.Infraestructura.Persistencia;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Heladeria.Pruebas.Seguridad;

public sealed class CookieRefreshHttpPruebas
{
    [Fact]
    public void ConfiguracionMantieneStrictEnBaseYUsaNoneSoloEnDesarrollo()
    {
        var baseConfiguracion = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json")
            .Build();
        var desarrollo = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.Development.json")
            .Build();

        Assert.Equal("Strict", baseConfiguracion["Seguridad:CookieRefresh:SameSite"]);
        Assert.Equal("None", desarrollo["Seguridad:CookieRefresh:SameSite"]);
    }

    [Fact]
    public async Task Desarrollo_EmiteCookieSeguraYLaEnviaAlEndpointRenovar()
    {
        await using var fabrica = new FabricaCookieRefresh();
        using var cliente = fabrica.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        var login = await cliente.PostAsJsonAsync(
            "/api/autenticacion/iniciar-sesion",
            new { identificador = "admin", contrasena = "PruebaSegura1!" });
        login.EnsureSuccessStatusCode();
        var cookie = Assert.Single(login.Headers.GetValues("Set-Cookie")).ToLowerInvariant();

        Assert.Contains("heladeria.refresh=refresh-prueba", cookie);
        Assert.Contains("path=/api/autenticacion", cookie);
        Assert.Contains("samesite=none", cookie);
        Assert.Contains("secure", cookie);
        Assert.Contains("httponly", cookie);
        Assert.DoesNotContain("domain=", cookie);

        var renovacion = await cliente.PostAsync("/api/autenticacion/renovar", null);

        renovacion.EnsureSuccessStatusCode();
        Assert.Equal("refresh-prueba", fabrica.Servicio.TokenRecibidoEnRenovacion);
    }

    [Fact]
    public async Task SesionActiva_SeExponeComoConflicto409SinEmitirOtraCookie()
    {
        await using var fabrica = new FabricaCookieRefresh();
        using var cliente = fabrica.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false,
            HandleCookies = true
        });
        var credenciales = new { identificador = "admin", contrasena = "PruebaSegura1!" };
        (await cliente.PostAsJsonAsync("/api/autenticacion/iniciar-sesion", credenciales))
            .EnsureSuccessStatusCode();

        var segundoLogin = await cliente.PostAsJsonAsync(
            "/api/autenticacion/iniciar-sesion",
            credenciales);
        var cuerpo = await segundoLogin.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Conflict, segundoLogin.StatusCode);
        Assert.Contains("SESION_ACTIVA", cuerpo);
        Assert.False(segundoLogin.Headers.Contains("Set-Cookie"));
    }
}

internal sealed class FabricaCookieRefresh : WebApplicationFactory<Program>
{
    private readonly OpcionesSeguridad _opciones = EscenarioSeguridad.CrearOpciones();

    public ServicioAutenticacionCookieFalso Servicio { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuracion) =>
        {
            configuracion.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Heladeria"] =
                    "Server=localhost;Database=HeladeriaPruebas;Trusted_Connection=True;TrustServerCertificate=True;",
                ["Seguridad:Jwt:Emisor"] = _opciones.Jwt.Emisor,
                ["Seguridad:Jwt:Audiencia"] = _opciones.Jwt.Audiencia,
                ["Seguridad:Jwt:ClaveFirma"] = _opciones.Jwt.ClaveFirma
            });
        });
        builder.ConfigureServices(servicios =>
        {
            servicios.RemoveAll<HeladeriaDbContext>();
            servicios.RemoveAll<DbContextOptions<HeladeriaDbContext>>();
            servicios.AddDbContext<HeladeriaDbContext>(opciones =>
                opciones.UseInMemoryDatabase($"cookie-refresh-{Guid.NewGuid():N}"));
            servicios.RemoveAll<IServicioAutenticacion>();
            servicios.AddSingleton<IServicioAutenticacion>(Servicio);
        });
    }
}

internal sealed class ServicioAutenticacionCookieFalso : IServicioAutenticacion
{
    private int _cantidadLogins;

    public string? TokenRecibidoEnRenovacion { get; private set; }

    public Task<ResultadoAutenticacion> IniciarSesionAsync(
        IniciarSesionSolicitud solicitud,
        ContextoAcceso contextoAcceso,
        CancellationToken cancellationToken) =>
        Interlocked.Increment(ref _cantidadLogins) == 1
            ? Task.FromResult(CrearResultado())
            : Task.FromException<ResultadoAutenticacion>(new SesionActivaException());

    public Task<ResultadoAutenticacion> RenovarAsync(
        string refreshToken,
        ContextoAcceso contextoAcceso,
        CancellationToken cancellationToken)
    {
        TokenRecibidoEnRenovacion = refreshToken;
        return Task.FromResult(CrearResultado());
    }

    public Task<ResultadoAutenticacion> SeleccionarSucursalAsync(
        int usuarioId,
        long sesionId,
        long sucursalActualId,
        string refreshToken,
        SeleccionarSucursalSolicitud solicitud,
        ContextoAcceso contextoAcceso,
        CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task CerrarSesionAsync(
        int usuarioId,
        long sesionId,
        long sucursalId,
        ContextoAcceso contextoAcceso,
        CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task CambiarContrasenaAsync(
        int usuarioId,
        long sucursalId,
        CambiarContrasenaSolicitud solicitud,
        ContextoAcceso contextoAcceso,
        CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<SesionActualRespuesta> ObtenerSesionAsync(
        int usuarioId,
        long sucursalId,
        CancellationToken cancellationToken) => throw new NotSupportedException();

    private static ResultadoAutenticacion CrearResultado()
    {
        var sucursal = new SucursalUsuarioRespuesta(
            4,
            "Sucursal principal",
            4,
            "Heladería",
            true);
        var usuario = new UsuarioAutenticadoRespuesta(
            1,
            "admin",
            "admin@heladeria.local",
            "ADMIN",
            "Administrador",
            sucursal,
            [sucursal]);
        var expiracionAccess = DateTimeOffset.UtcNow.AddMinutes(10);
        var expiracionRefresh = DateTimeOffset.UtcNow.AddDays(7);
        return new ResultadoAutenticacion(
            new IniciarSesionRespuesta(
                true,
                "access-token-prueba",
                expiracionAccess,
                usuario,
                false),
            "refresh-prueba",
            expiracionRefresh);
    }
}
