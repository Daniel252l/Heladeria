using System.Net;
using System.Net.Http.Json;
using Heladeria.Aplicacion.Compras;
using Heladeria.Aplicacion.Seguridad.Autorizacion;
using Heladeria.Infraestructura.Inventario;
using Heladeria.Infraestructura.Persistencia;
using Heladeria.Infraestructura.Seguridad.Autorizacion;
using Heladeria.Pruebas.Fase4;
using Heladeria.Pruebas.Seguridad;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using static Heladeria.Pruebas.Fase5.EscenarioFase5;

namespace Heladeria.Pruebas.Fase5;

public sealed class ComprasIntegracionHttpPruebas
{
    [Fact]
    public async Task CrearConfirmarRecibirConsultarPorHttpUsaPermisosEfectivosYDto()
    {
        await using var fabrica = await CrearApiAsync(); using var cliente = fabrica.CrearClienteAutenticado();
        var referencias = await cliente.GetFromJsonAsync<ReferenciasComprasDto>("/api/compras/referencias");
        Assert.Single(referencias!.Monedas); Assert.Equal(6, referencias.Estados.Count); Assert.Equal(3, referencias.Series.Count);
        var presentaciones = await cliente.GetFromJsonAsync<ProductoPresentacionesCompraDto>("/api/compras/productos/1/presentaciones");
        Assert.Equal(2, presentaciones!.Presentaciones.Count);
        Assert.Contains(presentaciones.Presentaciones, item => item.PresentacionCodigo == "LITRO" && item.FactorConversion == 1000m);
        Assert.Contains(presentaciones.Presentaciones, item => item.PresentacionCodigo == "VASO" && item.FactorConversion == 250m);
        var creada = await cliente.PostAsJsonAsync("/api/compras", Orden(3, 25000)); Assert.Equal(HttpStatusCode.Created, creada.StatusCode);
        var orden = (await creada.Content.ReadFromJsonAsync<CompraConsultaDto>())!;
        Assert.NotNull(creada.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, (await cliente.PostAsync($"/api/compras/{orden.Encabezado.Id}/confirmar", null)).StatusCode);
        var solicitud = Recepcion(orden.Detalles[0].Id, 3);
        var recibida = await cliente.PostAsJsonAsync($"/api/compras/{orden.Encabezado.Id}/recepciones", solicitud);
        Assert.Equal(HttpStatusCode.Created, recibida.StatusCode); Assert.NotNull(recibida.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync(recibida.Headers.Location)).StatusCode);
        var final = await cliente.GetFromJsonAsync<CompraConsultaDto>($"/api/compras/{orden.Encabezado.Id}");
        Assert.Equal("RECIBIDA", final!.Encabezado.Estado); Assert.Equal(0m, final.Detalles[0].CantidadPendiente);
        Assert.Equal(HttpStatusCode.Conflict, (await cliente.PostAsJsonAsync($"/api/compras/{orden.Encabezado.Id}/recepciones", solicitud)).StatusCode);
    }
    [Fact]
    public async Task AdministrarNoOtorgaRecibirYRolAdminNoTieneBypass()
    {
        await using var fabrica = await CrearApiAsync(); using var cliente = fabrica.CrearClienteAutenticado();
        var creada = await cliente.PostAsJsonAsync("/api/compras", Orden()); var orden = (await creada.Content.ReadFromJsonAsync<CompraConsultaDto>())!;
        await cliente.PostAsync($"/api/compras/{orden.Encabezado.Id}/confirmar", null);
        using (var scope = fabrica.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HeladeriaDbContext>();
            var permiso = await db.RolesPermisos.SingleAsync(item => item.Permiso.Codigo == PermisosSistema.ComprasRecibir);
            permiso.Permitido = false; await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.PostAsJsonAsync($"/api/compras/{orden.Encabezado.Id}/recepciones", Recepcion(orden.Detalles[0].Id, 1))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync($"/api/compras/{orden.Encabezado.Id}")).StatusCode);
    }
    [Fact]
    public async Task ProveedoresCrudEstadoEIdorPorHttp()
    {
        await using var fabrica = await CrearApiAsync(); using var cliente = fabrica.CrearClienteAutenticado();
        var creada = await cliente.PostAsJsonAsync("/api/compras/proveedores", Proveedor()); Assert.Equal(HttpStatusCode.Created, creada.StatusCode);
        var proveedor = (await creada.Content.ReadFromJsonAsync<ProveedorConsultaDto>())!;
        Assert.Equal(HttpStatusCode.Conflict, (await cliente.PostAsJsonAsync("/api/compras/proveedores", Proveedor())).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await cliente.PutAsJsonAsync($"/api/compras/proveedores/{proveedor.Id}", Proveedor(nombre: "Actualizado"))).StatusCode);
        var estado = await cliente.PutAsJsonAsync($"/api/compras/proveedores/{proveedor.Id}/estado", new { estadoId = 2 });
        Assert.Equal(HttpStatusCode.OK, estado.StatusCode); Assert.Equal(2, (await estado.Content.ReadFromJsonAsync<ProveedorConsultaDto>())!.EstadoId);
        Assert.Equal(HttpStatusCode.NotFound, (await cliente.GetAsync("/api/compras/proveedores/2")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await cliente.PutAsJsonAsync("/api/compras/proveedores/2", Proveedor())).StatusCode);
    }
    [Fact]
    public async Task CompraOtraSucursalAsignada404PorHttpSinModificar()
    {
        await using var fabrica = await CrearApiAsync(); using var cliente = fabrica.CrearClienteAutenticado(); long id;
        using (var scope = fabrica.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HeladeriaDbContext>();
            var coordinador = scope.ServiceProvider.GetRequiredService<CoordinadorAjustesInventario>();
            var orden = await scope.ServiceProvider.GetRequiredService<IServicioCompras>().GuardarAsync(null, Orden(sucursal: 2),
                EscenarioFase5.Seguridad with { SucursalId = 2 }, default); id = orden.Encabezado.Id;
            Assert.False(db.ChangeTracker.HasChanges());
            Assert.NotNull(coordinador);
        }
        Assert.Equal(HttpStatusCode.NotFound, (await cliente.GetAsync($"/api/compras/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await cliente.PutAsJsonAsync($"/api/compras/{id}", Orden())).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await cliente.PostAsync($"/api/compras/{id}/confirmar", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await cliente.PostAsJsonAsync($"/api/compras/{id}/recepciones", Recepcion(1, 1))).StatusCode);
    }

    [Fact]
    public async Task CatalogoPresentacionesRequiereComprasConsultar()
    {
        await using var fabrica = new FabricaApiSeguridad(tienePermiso: false);
        using var cliente = fabrica.CrearClienteAutenticado();

        Assert.Equal(HttpStatusCode.Forbidden,
            (await cliente.GetAsync("/api/compras/productos/1/presentaciones")).StatusCode);
    }

    private static async Task<FabricaApiSeguridad> CrearApiAsync()
    {
        var fabrica = new FabricaApiSeguridad(true, servicios =>
        {
            servicios.RemoveAll<IServicioPermisos>(); servicios.AddScoped<IServicioPermisos, ServicioPermisos>();
        });
        using var scope = fabrica.Services.CreateScope();
        var contexto = scope.ServiceProvider.GetRequiredService<HeladeriaDbContext>();
        var coordinador = scope.ServiceProvider.GetRequiredService<CoordinadorAjustesInventario>();
        await using var escenario = new EscenarioFase5(await EscenarioFase4.CrearConContextoAsync(contexto, coordinador), coordinador);
        await escenario.SemillarAsync();
        return fabrica;
    }
}
