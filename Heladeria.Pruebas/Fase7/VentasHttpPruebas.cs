using System.Net;
using System.Net.Http.Json;
using Heladeria.Pruebas.Seguridad;
using Heladeria.Aplicacion.Seguridad.Autorizacion;
using Heladeria.Aplicacion.Ventas;
using Heladeria.Infraestructura.Persistencia;
using Heladeria.Pruebas.Fase4;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Heladeria.Pruebas.Fase7;

public sealed class VentasHttpPruebas
{
    [Fact]
    public async Task ConsultarPuedeLeerEstadosDelFlujoSinPermisoPosNiAccesoAlCatalogo()
    {
        var nombre = "http-estados-ventas-" + Guid.NewGuid().ToString("N");
        await using var e = await EscenarioFase7.CrearAsync(nombre);
        await using var fabrica = new FabricaApiSeguridad(true, servicios => {
            servicios.RemoveAll<HeladeriaDbContext>();
            servicios.RemoveAll<DbContextOptions<HeladeriaDbContext>>();
            servicios.AddDbContext<HeladeriaDbContext>(o => o.UseInMemoryDatabase(nombre, EscenarioFase4.RaizCompartida));
            servicios.RemoveAll<IServicioPermisos>();
            servicios.AddSingleton<IServicioPermisos>(new PermisosLimitados(PermisosSistema.VentasConsultar));
        });
        using var cliente = fabrica.CrearClienteAutenticado();
        var estados = (await cliente.GetFromJsonAsync<ReferenciaVentaDto[]>("/api/ventas/estados"))!;
        Assert.Equal(new[] { "ANULADA", "BORRADOR", "COMPLETADA", "CONFIRMADA" }, estados.Select(x => x.Codigo).Order().ToArray());
        Assert.All(estados, x => Assert.True(x.Id > 0));
        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.GetAsync("/api/ventas/referencias")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.GetAsync("/api/ventas/productos")).StatusCode);
        Assert.Empty(await e.Contexto.Ventas.ToArrayAsync());
    }

    [Theory]
    [InlineData(PermisosSistema.VentasVender)]
    [InlineData(PermisosSistema.PosOperar)]
    public async Task CompletarExigeVenderYPosOperarNoSoloUno(string permiso)
    {
        await using var fabrica = new FabricaApiSeguridad(true, servicios => {
            servicios.RemoveAll<IServicioPermisos>();
            servicios.AddSingleton<IServicioPermisos>(new PermisosLimitados(permiso));
        });
        using var cliente = fabrica.CrearClienteAutenticado();
        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.PostAsJsonAsync("/api/ventas/1/completar",
            new { versionFila = "AQ==", claveOperacion = Guid.NewGuid() })).StatusCode);
    }

    [Fact]
    public async Task CicloHttpReal201200409404YContratoPosConSucursalActual()
    {
        var nombre = "http-ventas-" + Guid.NewGuid().ToString("N");
        await using var e = await EscenarioFase7.CrearAsync(nombre);
        await using var fabrica = new FabricaApiSeguridad(true, servicios => {
            servicios.RemoveAll<HeladeriaDbContext>();
            servicios.RemoveAll<DbContextOptions<HeladeriaDbContext>>();
            servicios.AddDbContext<HeladeriaDbContext>(o => o.UseInMemoryDatabase(nombre, EscenarioFase4.RaizCompartida));
        });
        using var cliente = fabrica.CrearClienteAutenticado();
        var respuesta = await cliente.PostAsJsonAsync("/api/ventas", EscenarioFase7.Solicitud());
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var v = (await respuesta.Content.ReadFromJsonAsync<VentaConsultaDto>())!;
        var entidad = await e.Contexto.Ventas.SingleAsync(x => x.Id == v.Encabezado.Id);
        entidad.VersionFila = [1]; await e.Contexto.SaveChangesAsync(); e.Contexto.ChangeTracker.Clear();
        respuesta = await cliente.PostAsJsonAsync($"/api/ventas/{v.Encabezado.Id}/confirmar", new { versionFila = "AQ==" });
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        v = (await respuesta.Content.ReadFromJsonAsync<VentaConsultaDto>())!;
        respuesta = await cliente.PostAsJsonAsync($"/api/ventas/{v.Encabezado.Id}/completar", new { versionFila = v.Encabezado.VersionFila, claveOperacion = Guid.NewGuid(), cobro = await e.CobroAsync(v) });
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        v = (await respuesta.Content.ReadFromJsonAsync<VentaConsultaDto>())!;
        Assert.Equal("COMPLETADA", v.Encabezado.Estado);
        respuesta = await cliente.PostAsJsonAsync($"/api/ventas/{v.Encabezado.Id}/completar", new { versionFila = v.Encabezado.VersionFila, claveOperacion = Guid.NewGuid() });
        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Contains("CONFLICTO_VENTAS", await respuesta.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.NotFound, (await cliente.GetAsync("/api/ventas/9999999")).StatusCode);
        var p = await cliente.GetFromJsonAsync<ProductoPosDto>("/api/ventas/productos/1/presentaciones");
        Assert.Equal(1000, p!.Presentaciones.Single(x => x.ProductoPresentacionId == 1).FactorConversion);
    }

    [Theory]
    [InlineData("GET", "/api/ventas")]
    [InlineData("GET", "/api/ventas/estados")]
    [InlineData("GET", "/api/ventas/1")]
    [InlineData("GET", "/api/ventas/referencias")]
    [InlineData("GET", "/api/ventas/productos")]
    [InlineData("GET", "/api/ventas/productos/1/presentaciones")]
    [InlineData("POST", "/api/ventas")]
    [InlineData("PUT", "/api/ventas/1")]
    [InlineData("POST", "/api/ventas/1/confirmar")]
    [InlineData("POST", "/api/ventas/1/completar")]
    [InlineData("POST", "/api/ventas/1/anular")]
    public async Task TodosLosEndpointsExigenTokenYPermiso(string metodo, string ruta)
    {
        await using var fabrica = new FabricaApiSeguridad(false);
        using var anonimo = fabrica.CrearCliente();
        using var autenticado = fabrica.CrearClienteAutenticado();
        using var solicitud1 = new HttpRequestMessage(new HttpMethod(metodo), ruta) { Content = JsonContent.Create(new { }) };
        using var solicitud2 = new HttpRequestMessage(new HttpMethod(metodo), ruta) { Content = JsonContent.Create(new { }) };
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.SendAsync(solicitud1)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await autenticado.SendAsync(solicitud2)).StatusCode);
    }
    [Theory]
    [InlineData("POST", "/api/ventas")]
    [InlineData("PUT", "/api/ventas/1")]
    [InlineData("POST", "/api/ventas/1/confirmar")]
    [InlineData("POST", "/api/ventas/1/completar")]
    [InlineData("POST", "/api/ventas/1/anular")]
    public async Task RequestsClaseSinMetadataRecordYValidacion400(string metodo, string ruta)
    {
        await using var fabrica = new FabricaApiSeguridad(true);
        using var cliente = fabrica.CrearClienteAutenticado();
        using var solicitud = new HttpRequestMessage(new HttpMethod(metodo), ruta) { Content = JsonContent.Create(new { }) };
        Assert.Equal(HttpStatusCode.BadRequest, (await cliente.SendAsync(solicitud)).StatusCode);
    }

    private sealed class PermisosLimitados(string permiso) : IServicioPermisos
    {
        public Task<bool> TienePermisoAsync(int usuarioId, string rolId, string codigoPermiso, CancellationToken t) => Task.FromResult(codigoPermiso == permiso);
        public Task<IReadOnlyList<string>> ObtenerPermisosAsync(int usuarioId, CancellationToken t) => Task.FromResult<IReadOnlyList<string>>([permiso]);
    }
}
