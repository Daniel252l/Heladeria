using System.Net;
using System.Net.Http.Json;
using Heladeria.Aplicacion.Caja;
using Heladeria.Aplicacion.Seguridad.Autorizacion;
using Heladeria.Infraestructura.Persistencia;
using Heladeria.Pruebas.Fase5;
using Heladeria.Pruebas.Seguridad;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Heladeria.Pruebas.Fase8;

public sealed class CajaHttpPruebas
{
    [Theory]
    [InlineData("GET", "/api/caja/cajas")] [InlineData("POST", "/api/caja/cajas")]
    [InlineData("GET", "/api/caja/sesiones")] [InlineData("GET", "/api/caja/sesiones/1")]
    [InlineData("GET", "/api/caja/referencias")] [InlineData("GET", "/api/caja/ventas-pendientes")]
    [InlineData("POST", "/api/caja/sesiones/abrir")] [InlineData("POST", "/api/caja/sesiones/1/movimientos")]
    [InlineData("POST", "/api/caja/sesiones/1/cobrar-venta")] [InlineData("POST", "/api/caja/sesiones/1/arqueo")]
    [InlineData("POST", "/api/caja/sesiones/1/cerrar")]
    public async Task EndpointsExigenAutenticacionYPermiso(string metodo, string ruta)
    {
        await using var fabrica = new FabricaApiSeguridad(false);
        using var anonimo = fabrica.CrearCliente(); using var autenticado = fabrica.CrearClienteAutenticado();
        using var a = new HttpRequestMessage(new HttpMethod(metodo), ruta) { Content = JsonContent.Create(new { }) };
        using var b = new HttpRequestMessage(new HttpMethod(metodo), ruta) { Content = JsonContent.Create(new { }) };
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.SendAsync(a)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await autenticado.SendAsync(b)).StatusCode);
    }
    [Theory]
    [InlineData("/api/caja/cajas")] [InlineData("/api/caja/sesiones/abrir")]
    [InlineData("/api/caja/sesiones/1/movimientos")] [InlineData("/api/caja/sesiones/1/cobrar-venta")]
    [InlineData("/api/caja/sesiones/1/arqueo")] [InlineData("/api/caja/sesiones/1/cerrar")]
    public async Task RequestsSelladosConValidacionMvcDevuelven400No500(string ruta)
    {
        await using var fabrica = new FabricaApiSeguridad(true); using var cliente = fabrica.CrearClienteAutenticado();
        Assert.Equal(HttpStatusCode.BadRequest, (await cliente.PostAsJsonAsync(ruta, new { })).StatusCode);
    }
    [Theory]
    [InlineData("cobrar-venta", PermisosSistema.CajaOperar)]
    [InlineData("cobrar-venta", PermisosSistema.CajaCobrar)]
    [InlineData("movimientos", PermisosSistema.CajaOperar)]
    [InlineData("movimientos", PermisosSistema.CajaMovimientoManual)]
    public async Task EscribirExigeAmbosPermisos(string operacion, string permiso)
    {
        await using var fabrica = new FabricaApiSeguridad(true, servicios => {
            servicios.RemoveAll<IServicioPermisos>(); servicios.AddSingleton<IServicioPermisos>(new PermisosLimitados(permiso));
        });
        using var cliente = fabrica.CrearClienteAutenticado();
        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.PostAsJsonAsync($"/api/caja/sesiones/1/{operacion}", new { })).StatusCode);
    }
    [Fact]
    public async Task FlujoHttpAbrirCobrarArqueoCerrarYConflictosControlados()
    {
        await using var e = await EscenarioCaja.CrearAsync();
        var venta = await e.VentaAsync();
        await using var fabrica = new FabricaApiSeguridad(true, servicios => {
            servicios.RemoveAll<HeladeriaDbContext>(); servicios.RemoveAll<DbContextOptions<HeladeriaDbContext>>();
            servicios.AddScoped<HeladeriaDbContext>(_ => new ContextoSqlitePruebas(new DbContextOptionsBuilder<HeladeriaDbContext>()
                .UseSqlite(e.Contexto.Database.GetDbConnection()).Options));
        });
        using var cliente = fabrica.CrearClienteAutenticado();
        var respuesta = await cliente.PostAsJsonAsync("/api/caja/cajas", new { codigo = "HTTP", nombre = "Caja HTTP" });
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var caja = (await respuesta.Content.ReadFromJsonAsync<CajaDto>())!;
        respuesta = await cliente.PostAsJsonAsync("/api/caja/sesiones/abrir", new { cajaId = caja.Id, monedaId = 1, saldoInicial = 100 });
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var sesion = (await respuesta.Content.ReadFromJsonAsync<SesionCajaDto>())!;
        var ruta = $"/api/caja/sesiones/{sesion.Encabezado.Id}";
        var solicitud = await e.SolicitudCobroAsync(sesion, venta);
        respuesta = await cliente.PostAsJsonAsync(ruta + "/cobrar-venta", solicitud);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        sesion = (await respuesta.Content.ReadFromJsonAsync<ResultadoCobroCajaDto>())!.Sesion;
        respuesta = await cliente.PostAsJsonAsync(ruta + "/arqueo", new { versionFila = sesion.Encabezado.VersionFila, efectivoContado = 149 });
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode); sesion = (await respuesta.Content.ReadFromJsonAsync<SesionCajaDto>())!;
        var cierre = new { versionFila = sesion.Encabezado.VersionFila, arqueoId = sesion.Arqueos.Single().Id };
        respuesta = await cliente.PostAsJsonAsync(ruta + "/cerrar", cierre);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode); sesion = (await respuesta.Content.ReadFromJsonAsync<SesionCajaDto>())!;
        Assert.Equal(-1, sesion.Encabezado.DiferenciaCierre);
        respuesta = await cliente.PostAsJsonAsync(ruta + "/cerrar", cierre);
        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode); Assert.Contains("CAJA_NO_ABIERTA", await respuesta.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.NotFound, (await cliente.GetAsync("/api/caja/sesiones/999999")).StatusCode);
    }
    private sealed class PermisosLimitados(string permiso) : IServicioPermisos
    {
        public Task<bool> TienePermisoAsync(int usuarioId, string rolId, string codigoPermiso, CancellationToken t) => Task.FromResult(permiso == codigoPermiso);
        public Task<IReadOnlyList<string>> ObtenerPermisosAsync(int usuarioId, CancellationToken t) => Task.FromResult<IReadOnlyList<string>>([permiso]);
    }
}
