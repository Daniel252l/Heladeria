using System.Net;
using System.Net.Http.Json;
using Heladeria.Aplicacion.Productos;
using Heladeria.Aplicacion.Seguridad.Autorizacion;
using Heladeria.Infraestructura.Persistencia;
using Heladeria.Pruebas.Fase4;
using Heladeria.Pruebas.Fase7;
using Heladeria.Pruebas.Seguridad;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Heladeria.Pruebas.AjusteFinalD;

public sealed class ComposicionHttpPruebas
{
    [Theory, InlineData("POST", "/api/productos"), InlineData("PUT", "/api/productos/1")]
    public async Task ComposicionExigeAdministrarProductos(string metodo, string ruta)
    {
        await using var fabrica = new FabricaApiSeguridad(true, services => {
            services.RemoveAll<IServicioPermisos>(); services.AddSingleton<IServicioPermisos>(new SoloConsulta());
        });
        using var cliente = fabrica.CrearClienteAutenticado();
        using var request = new HttpRequestMessage(new(metodo), ruta) { Content = JsonContent.Create(new { componentes = new[] { new { productoId = 1, cantidad = 2 } } }) };
        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.SendAsync(request)).StatusCode);
    }

    [Fact]
    public async Task ContratoHttpCreaYConsultaComposicionConBindingYValidaCantidad()
    {
        var nombre = "http-ajuste-d-" + Guid.NewGuid().ToString("N");
        await using var e = await EscenarioFase7.CrearAsync(nombre);
        await using var fabrica = new FabricaApiSeguridad(true, servicios => {
            servicios.RemoveAll<HeladeriaDbContext>(); servicios.RemoveAll<DbContextOptions<HeladeriaDbContext>>();
            servicios.AddDbContext<HeladeriaDbContext>(o => o.UseInMemoryDatabase(nombre, EscenarioFase4.RaizCompartida));
        });
        using var cliente = fabrica.CrearClienteAutenticado();
        object Solicitud(decimal cantidad, string sku) => new { sku, nombre = sku, categoriaId = 1, tipoProductoId = 1, unidadBaseId = 1,
            tipoSeguimientoId = 1, metodoValuacionId = 2, estadoId = 1, esVendible = true, precioVentaReferencial = 25,
            componentes = new[] { new { productoId = 1, productoPresentacionId = 1, cantidad } } };
        var respuesta = await cliente.PostAsJsonAsync("/api/productos", Solicitud(2, "COMPUESTO-HTTP"));
        Assert.True(respuesta.StatusCode == HttpStatusCode.Created, await respuesta.Content.ReadAsStringAsync());
        var p = (await respuesta.Content.ReadFromJsonAsync<ProductoConsultaDto>())!;
        Assert.Equal(2000, Assert.Single(p.Componentes).CantidadBase);
        var lectura = (await cliente.GetFromJsonAsync<ProductoConsultaDto>($"/api/productos/{p.Id}"))!;
        Assert.Equal(p.Componentes, lectura.Componentes);
        Assert.Equal(HttpStatusCode.BadRequest, (await cliente.PostAsJsonAsync("/api/productos", Solicitud(0, "INVALIDO"))).StatusCode);
        Assert.False(await e.Contexto.Productos.AnyAsync(x => x.SKU == "INVALIDO"));
    }
    private sealed class SoloConsulta : IServicioPermisos
    {
        public Task<bool> TienePermisoAsync(int usuarioId, string rolId, string codigoPermiso, CancellationToken t) => Task.FromResult(codigoPermiso == PermisosSistema.ProductosConsultar);
        public Task<IReadOnlyList<string>> ObtenerPermisosAsync(int usuarioId, CancellationToken t) => Task.FromResult<IReadOnlyList<string>>([PermisosSistema.ProductosConsultar]);
    }
}
