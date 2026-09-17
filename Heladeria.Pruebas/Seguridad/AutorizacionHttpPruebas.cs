using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Heladeria.Aplicacion.Seguridad.Autorizacion;
using Heladeria.Aplicacion.Seguridad.Configuracion;
using Heladeria.Infraestructura.Persistencia;
using Heladeria.Infraestructura.Persistencia.Entidades;
using Heladeria.Infraestructura.Seguridad.Autenticacion;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;

namespace Heladeria.Pruebas.Seguridad;

public sealed class AutorizacionHttpPruebas
{
    [Fact]
    public async Task ReferenciasOrganizacionSinPermisoConfiguracion_Responde403()
    {
        await using var fabrica = new FabricaApiSeguridad(tienePermiso: false);
        using var cliente = fabrica.CrearClienteAutenticado();
        var respuesta = await cliente.GetAsync("/api/configuracion/referencias-organizacion?tipo=PROVEEDORES&empresaId=1");
        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }
    [Fact]
    public async Task LoginConModeloInvalido_Responde400SinErrorDeMetadata()
    {
        await using var fabrica = new FabricaApiSeguridad(tienePermiso: false);
        using var cliente = fabrica.CrearCliente();

        var respuesta = await cliente.PostAsJsonAsync(
            "/api/autenticacion/iniciar-sesion",
            new
            {
                identificador = string.Empty,
                contrasena = string.Empty,
                sucursalId = (long?)null
            });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task CambioContrasenaConModeloInvalido_Responde400SinErrorDeMetadata()
    {
        await using var fabrica = new FabricaApiSeguridad(tienePermiso: false);
        using var cliente = fabrica.CrearClienteAutenticado();

        var respuesta = await cliente.PostAsJsonAsync(
            "/api/autenticacion/cambiar-contrasena",
            new
            {
                contrasenaActual = string.Empty,
                nuevaContrasena = string.Empty,
                confirmacion = string.Empty
            });

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task EndpointProtegidoSinToken_Responde401()
    {
        await using var fabrica = new FabricaApiSeguridad(tienePermiso: false);
        using var cliente = fabrica.CrearCliente();

        var respuesta = await cliente.GetAsync("/api/autenticacion/sesion");

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task UsuarioAutenticadoSinPermiso_Responde403YAudita()
    {
        await using var fabrica = new FabricaApiSeguridad(tienePermiso: false);
        using var cliente = fabrica.CrearClienteAutenticado();

        var respuesta = await cliente.GetAsync("/api/seguridad/permisos");

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        Assert.Equal(1, fabrica.Auditoria.CantidadAccesosDenegados);
    }

    [Fact]
    public async Task UsuarioConPermiso_ObtieneAccesoCorrecto()
    {
        await using var fabrica = new FabricaApiSeguridad(tienePermiso: true);
        using var cliente = fabrica.CrearClienteAutenticado();

        var respuesta = await cliente.GetAsync("/api/seguridad/permisos");
        var permisos = await respuesta.Content.ReadFromJsonAsync<string[]>();

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains(PermisosSistema.SeguridadAdministrar, permisos!);
    }

    [Fact]
    public async Task ContrasenaTemporal_BloqueaModulosNormales()
    {
        await using var fabrica = new FabricaApiSeguridad(tienePermiso: true);
        using var cliente = fabrica.CrearClienteAutenticado(requiereCambioContrasena: true);

        var respuesta = await cliente.GetAsync("/api/seguridad/permisos");

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        Assert.Contains(
            "CAMBIO_CONTRASENA_REQUERIDO",
            await respuesta.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task MantenimientoSinToken_Responde401()
    {
        await using var fabrica = new FabricaApiSeguridad(tienePermiso: false);
        using var cliente = fabrica.CrearCliente();
        Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.GetAsync("/api/configuracion/modulos")).StatusCode);
    }

    [Fact]
    public async Task EscrituraMantenimientoSinPermiso_Responde403()
    {
        await using var fabrica = new FabricaApiSeguridad(tienePermiso: false);
        using var cliente = fabrica.CrearClienteAutenticado();
        var respuesta = await cliente.PostAsJsonAsync("/api/configuracion/modulos", new
        {
            id = "VENTAS",
            nombre = "Ventas",
            icono = "cart",
            activo = true
        });
        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }

    [Fact]
    public async Task ActualizarEmpresaSinPermisoConfiguracion_Responde403()
    {
        await using var fabrica = new FabricaApiSeguridad(tienePermiso: false);
        using var cliente = fabrica.CrearClienteAutenticado();
        var respuesta = await cliente.PutAsJsonAsync("/api/configuracion/empresas/4", new
        {
            razonSocial = "Heladeria Inicial",
            nombreComercial = "Heladeria",
            eslogan = "Configuración inicial",
            direccion = "Pendiente de configurar",
            nit = "CF",
            telefono = "Pendiente",
            email = "empresa@heladeria.local",
            imagen = "sin-imagen",
            icono = "sin-icono",
            imagenReportes = "sin-imagen",
            estadoId = 1
        });

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }

    [Fact]
    public async Task MantenimientoConBodyInvalido_Responde400()
    {
        await using var fabrica = new FabricaApiSeguridad(tienePermiso: true);
        using var cliente = fabrica.CrearClienteAutenticado();
        var respuesta = await cliente.PostAsJsonAsync("/api/configuracion/modulos", new { });
        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task LecturaMantenimientoConUsuarioAutenticado_Responde200()
    {
        await using var fabrica = new FabricaApiSeguridad(tienePermiso: true);
        using var cliente = fabrica.CrearClienteAutenticado();
        var respuesta = await cliente.GetAsync("/api/configuracion/modulos");
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    [Fact]
    public async Task ProductosSinToken_Responde401()
    {
        await using var fabrica = new FabricaApiSeguridad(tienePermiso: true);
        using var cliente = fabrica.CrearCliente();
        Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.GetAsync("/api/productos")).StatusCode);
    }

    [Fact]
    public async Task ProductosSinPermisoConsultar_Responde403()
    {
        await using var fabrica = new FabricaApiSeguridad(tienePermiso: false);
        using var cliente = fabrica.CrearClienteAutenticado();
        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.GetAsync("/api/productos")).StatusCode);
    }

    [Fact]
    public async Task ProductosConPermisoConsultar_Responde200()
    {
        await using var fabrica = new FabricaApiSeguridad(tienePermiso: true);
        using var cliente = fabrica.CrearClienteAutenticado();
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync("/api/productos")).StatusCode);
    }

    [Fact]
    public async Task AjusteInventarioSinPermisoAdministrar_Responde403()
    {
        await using var fabrica = new FabricaApiSeguridad(tienePermiso: false);
        using var cliente = fabrica.CrearClienteAutenticado();
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await cliente.PostAsJsonAsync("/api/inventario/ajustes", new { })).StatusCode);
    }
}

internal sealed class FabricaApiSeguridad(bool tienePermiso, Action<IServiceCollection>? configurarServicios = null) : WebApplicationFactory<Program>
{
    private readonly OpcionesSeguridad _opciones = EscenarioSeguridad.CrearOpciones();
    private readonly string _nombreBasePruebas = $"http-seguridad-{Guid.NewGuid():N}";

    public AuditoriaSeguridadFalsa Auditoria { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuracion) =>
        {
            configuracion.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Heladeria"] =
                    "Server=localhost;Database=HeladeriaPruebas;Trusted_Connection=True;TrustServerCertificate=True;",
                ["Seguridad:Jwt:Emisor"] = _opciones.Jwt.Emisor,
                ["Seguridad:Jwt:Audiencia"] = _opciones.Jwt.Audiencia,
                ["Seguridad:Jwt:ClaveFirma"] = _opciones.Jwt.ClaveFirma,
                ["Seguridad:Jwt:MinutosAccessToken"] = "10",
                ["Seguridad:Jwt:DiasRefreshToken"] = "7",
                ["Seguridad:EstadosActivos:0"] = "Activo"
            });
        });
        builder.ConfigureTestServices(servicios =>
        {
            servicios.RemoveAll<HeladeriaDbContext>();
            servicios.RemoveAll<DbContextOptions<HeladeriaDbContext>>();
            servicios.AddDbContext<HeladeriaDbContext>(opciones =>
                opciones.UseInMemoryDatabase(_nombreBasePruebas));
            servicios.RemoveAll<IServicioValidacionSesion>();
            servicios.RemoveAll<IServicioPermisos>();
            servicios.RemoveAll<IServicioAuditoriaSeguridad>();
            servicios.AddScoped<IServicioValidacionSesion>(_ => new ValidadorSesionFalso());
            servicios.AddScoped<IServicioPermisos>(_ => new ServicioPermisosFalso(tienePermiso));
            servicios.AddSingleton<IServicioAuditoriaSeguridad>(Auditoria);
            configurarServicios?.Invoke(servicios);
        });
    }

    public HttpClient CrearCliente()
    {
        var cliente = CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });
        return cliente;
    }

    public HttpClient CrearClienteAutenticado(bool requiereCambioContrasena = false)
    {
        var cliente = CrearCliente();
        var generador = new GeneradorTokensJwt(Options.Create(_opciones), TimeProvider.System);
        var token = generador.CrearAccessToken(
            new Usuarios
            {
                Id = 1,
                UserName = "admin",
                RoleId = "ADMIN"
            },
            new Sucursales
            {
                Id = 1,
                EmpresaId = 1
            },
            sesionId: 1,
            requiereCambioContrasena);
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            token.Valor);
        return cliente;
    }
}

internal sealed class ValidadorSesionFalso : IServicioValidacionSesion
{
    public Task<bool> EsValidaAsync(
        int usuarioId,
        long sesionId,
        long sucursalId,
        CancellationToken cancellationToken) => Task.FromResult(true);
}

internal sealed class ServicioPermisosFalso(bool tienePermiso) : IServicioPermisos
{
    public Task<bool> TienePermisoAsync(
        int usuarioId,
        string rolId,
        string codigoPermiso,
        CancellationToken cancellationToken) => Task.FromResult(tienePermiso);

    public Task<IReadOnlyList<string>> ObtenerPermisosAsync(
        int usuarioId,
        CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<string>>(
        tienePermiso ? [PermisosSistema.SeguridadAdministrar] : []);
}

internal sealed class AuditoriaSeguridadFalsa : IServicioAuditoriaSeguridad
{
    public int CantidadAccesosDenegados { get; private set; }

    public Task RegistrarAccesoDenegadoAsync(
        int usuarioId,
        long? sucursalId,
        string recurso,
        string? direccionIp,
        CancellationToken cancellationToken)
    {
        CantidadAccesosDenegados++;
        return Task.CompletedTask;
    }
}
