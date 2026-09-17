using System.Net;
using System.Net.Http.Json;
using Heladeria.Pruebas.Seguridad;

namespace Heladeria.Pruebas.Fase5;

public sealed class ComprasHttpPruebas
{
    [Theory]
    [InlineData("/api/compras")]
    [InlineData("/api/compras/1")]
    [InlineData("/api/compras/recepciones/1")]
    [InlineData("/api/compras/proveedores")]
    [InlineData("/api/compras/proveedores/1/productos")]
    public async Task LecturaSinToken401(string ruta)
    {
        await using var fabrica = new FabricaApiSeguridad(true); using var cliente = fabrica.CrearCliente();
        Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.GetAsync(ruta)).StatusCode);
    }
    [Theory]
    [InlineData("/api/compras")]
    [InlineData("/api/compras/1")]
    [InlineData("/api/compras/recepciones/1")]
    [InlineData("/api/compras/proveedores")]
    [InlineData("/api/compras/proveedores/1/productos")]
    public async Task LecturaSinPermiso403(string ruta)
    {
        await using var fabrica = new FabricaApiSeguridad(false); using var cliente = fabrica.CrearClienteAutenticado();
        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.GetAsync(ruta)).StatusCode);
        Assert.Equal(1, fabrica.Auditoria.CantidadAccesosDenegados);
    }
    [Theory]
    [InlineData("/api/compras")]
    [InlineData("/api/compras/proveedores")]
    [InlineData("/api/compras/1/confirmar")]
    [InlineData("/api/compras/1/recepciones")]
    public async Task EscrituraSinPermiso403(string ruta)
    {
        await using var fabrica = new FabricaApiSeguridad(false); using var cliente = fabrica.CrearClienteAutenticado();
        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.PostAsJsonAsync(ruta, new { })).StatusCode);
    }
    [Theory]
    [InlineData("/api/compras")]
    [InlineData("/api/compras/proveedores")]
    [InlineData("/api/compras/1/recepciones")]
    public async Task BodyInvalido400SinMetadataDeRecord(string ruta)
    {
        await using var fabrica = new FabricaApiSeguridad(true); using var cliente = fabrica.CrearClienteAutenticado();
        Assert.Equal(HttpStatusCode.BadRequest, (await cliente.PostAsJsonAsync(ruta, new { })).StatusCode);
    }
    [Theory]
    [InlineData("/api/compras/999")]
    [InlineData("/api/compras/proveedores/999")]
    [InlineData("/api/compras/recepciones/999")]
    public async Task ContextoFueraDeAlcance404No500(string ruta)
    {
        await using var fabrica = new FabricaApiSeguridad(true); using var cliente = fabrica.CrearClienteAutenticado();
        Assert.Equal(HttpStatusCode.NotFound, (await cliente.GetAsync(ruta)).StatusCode);
    }
}
