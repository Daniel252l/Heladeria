using System.Net;
using System.Net.Http.Json;
using Heladeria.Aplicacion.Cartera;
using Heladeria.Aplicacion.Seguridad.Autorizacion;
using Heladeria.Infraestructura.Persistencia;
using Heladeria.Pruebas.Fase5;
using Heladeria.Pruebas.Seguridad;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Heladeria.Pruebas.Fase9;

public sealed class CarteraHttpPruebas
{
    [Theory]
    [InlineData("GET", "/api/cuentas-por-cobrar")] [InlineData("GET", "/api/cuentas-por-cobrar/1")]
    [InlineData("GET", "/api/cuentas-por-cobrar/resumen")] [InlineData("GET", "/api/cuentas-por-cobrar/referencias")]
    [InlineData("POST", "/api/cuentas-por-cobrar/1/abonos")]
    [InlineData("GET", "/api/cuentas-por-pagar")] [InlineData("GET", "/api/cuentas-por-pagar/1")]
    [InlineData("GET", "/api/cuentas-por-pagar/resumen")] [InlineData("GET", "/api/cuentas-por-pagar/referencias")]
    [InlineData("POST", "/api/cuentas-por-pagar/1/pagos")] [InlineData("POST", "/api/cuentas-por-pagar/desde-compra")]
    public async Task AutenticacionYPermisosObligatorios(string metodo, string ruta)
    {
        await using var fabrica = new FabricaApiSeguridad(false);
        using var anonimo = fabrica.CrearCliente(); using var cliente = fabrica.CrearClienteAutenticado();
        using var a = new HttpRequestMessage(new HttpMethod(metodo), ruta) { Content = JsonContent.Create(new { }) };
        using var b = new HttpRequestMessage(new HttpMethod(metodo), ruta) { Content = JsonContent.Create(new { }) };
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.SendAsync(a)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.SendAsync(b)).StatusCode);
    }
    [Theory]
    [InlineData("/api/cuentas-por-cobrar/1/abonos")] [InlineData("/api/cuentas-por-pagar/1/pagos")]
    [InlineData("/api/cuentas-por-pagar/desde-compra")]
    public async Task ValidacionMvc400No500(string ruta)
    {
        await using var fabrica = new FabricaApiSeguridad(true); using var cliente = fabrica.CrearClienteAutenticado();
        Assert.Equal(HttpStatusCode.BadRequest, (await cliente.PostAsJsonAsync(ruta, new { })).StatusCode);
    }
    [Theory]
    [InlineData(false, PermisosSistema.CajaOperar)]
    [InlineData(false, PermisosSistema.CajaCobrar)]
    [InlineData(false, PermisosSistema.CxcCobrar)]
    [InlineData(true, PermisosSistema.CajaOperar)]
    [InlineData(true, PermisosSistema.CxpPagar)]
    public async Task CadaPermisoSeExigeIndependientemente(bool pagar, string denegado)
    {
        await using var fabrica = new FabricaApiSeguridad(true, servicios => {
            servicios.RemoveAll<IServicioPermisos>(); servicios.AddSingleton<IServicioPermisos>(new PermisosSin(denegado));
        });
        using var cliente = fabrica.CrearClienteAutenticado();
        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.PostAsJsonAsync(
            pagar ? "/api/cuentas-por-pagar/1/pagos" : "/api/cuentas-por-cobrar/1/abonos", new { })).StatusCode);
    }
    [Theory, InlineData(false), InlineData(true)]
    public async Task PagoHttpRetorna200Conflictos409Detalle404YTimestampsZ(bool pagar)
    {
        await using var e = await EscenarioCartera.CrearAsync();
        var cuenta = await e.CuentaAsync(pagar); var sesion = await e.Caja.AbrirAsync();
        await using var fabrica = new FabricaApiSeguridad(true, servicios => {
            servicios.RemoveAll<HeladeriaDbContext>(); servicios.RemoveAll<DbContextOptions<HeladeriaDbContext>>();
            servicios.AddScoped<HeladeriaDbContext>(_ => new ContextoSqlitePruebas(new DbContextOptionsBuilder<HeladeriaDbContext>()
                .UseSqlite(e.Db.Database.GetDbConnection()).Options));
        });
        using var cliente = fabrica.CrearClienteAutenticado();
        var ruta = "/api/cuentas-por-" + (pagar ? "pagar/" : "cobrar/") + cuenta.Encabezado.Id;
        var op = ruta + (pagar ? "/pagos" : "/abonos");
        var respuesta = await cliente.PostAsJsonAsync(op, await e.SolicitudAsync(cuenta, sesion));
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var dto = (await respuesta.Content.ReadFromJsonAsync<ResultadoPagoCarteraDto>())!;
        Assert.Equal(0, dto.Cuenta.Encabezado.SaldoPendiente);
        using var json = System.Text.Json.JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync());
        Assert.EndsWith("Z", json.RootElement.GetProperty("cuenta").GetProperty("historial")[0].GetProperty("fecha").GetString());
        respuesta = await cliente.PostAsJsonAsync(op, await e.SolicitudAsync(dto.Cuenta, sesion));
        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Contains("CUENTA_YA_PAGADA", await respuesta.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.NotFound, (await cliente.GetAsync("/api/cuentas-por-" + (pagar ? "pagar/" : "cobrar/") + "999999")).StatusCode);
    }
    private sealed class PermisosSin(string denegado) : IServicioPermisos
    {
        public Task<bool> TienePermisoAsync(int usuarioId, string rolId, string codigoPermiso, CancellationToken t) => Task.FromResult(codigoPermiso != denegado);
        public Task<IReadOnlyList<string>> ObtenerPermisosAsync(int usuarioId, CancellationToken t) => Task.FromResult<IReadOnlyList<string>>([]);
    }
}
