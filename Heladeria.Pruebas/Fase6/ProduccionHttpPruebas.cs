using System.Net;
using System.Net.Http.Json;
using Heladeria.Pruebas.Seguridad;

namespace Heladeria.Pruebas.Fase6;

public sealed class ProduccionHttpPruebas
{
    [Theory]
    [InlineData("/api/recetas")]
    [InlineData("/api/recetas/1")]
    [InlineData("/api/produccion")]
    [InlineData("/api/produccion/1")]
    [InlineData("/api/produccion/referencias")]
    [InlineData("/api/recetas/referencias")]
    [InlineData("/api/produccion/productos")]
    [InlineData("/api/produccion/recetas")]
    [InlineData("/api/produccion/recetas/1")]
    [InlineData("/api/produccion/productos/1/presentaciones")]
    public async Task LecturaSinTokenResponde401(string ruta)
    {
        await using var fabrica = new FabricaApiSeguridad(true);
        using var cliente = fabrica.CrearCliente();
        Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.GetAsync(ruta)).StatusCode);
    }

    [Theory]
    [InlineData("/api/recetas")]
    [InlineData("/api/recetas/1")]
    [InlineData("/api/produccion")]
    [InlineData("/api/produccion/1")]
    [InlineData("/api/produccion/referencias")]
    [InlineData("/api/recetas/referencias")]
    [InlineData("/api/produccion/productos")]
    [InlineData("/api/produccion/recetas")]
    [InlineData("/api/produccion/recetas/1")]
    [InlineData("/api/produccion/productos/1/presentaciones")]
    public async Task LecturaSinPermisoResponde403(string ruta)
    {
        await using var fabrica = new FabricaApiSeguridad(false);
        using var cliente = fabrica.CrearClienteAutenticado();
        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.GetAsync(ruta)).StatusCode);
    }

    [Theory]
    [InlineData("/api/recetas")]
    [InlineData("/api/produccion")]
    [InlineData("/api/produccion/1/confirmar")]
    [InlineData("/api/produccion/1/finalizar")]
    [InlineData("/api/produccion/1/anular")]
    public async Task EscrituraSinPermisoResponde403(string ruta)
    {
        await using var fabrica = new FabricaApiSeguridad(false);
        using var cliente = fabrica.CrearClienteAutenticado();
        Assert.Equal(HttpStatusCode.Forbidden,
            (await cliente.PostAsJsonAsync(ruta, new { })).StatusCode);
    }

    [Theory]
    [InlineData("/api/recetas")]
    [InlineData("/api/produccion")]
    [InlineData("/api/produccion/1/finalizar")]
    public async Task BodyInvalidoResponde400SinErrorDeMetadata(string ruta)
    {
        await using var fabrica = new FabricaApiSeguridad(true);
        using var cliente = fabrica.CrearClienteAutenticado();
        Assert.Equal(HttpStatusCode.BadRequest,
            (await cliente.PostAsJsonAsync(ruta, new { })).StatusCode);
    }
}
