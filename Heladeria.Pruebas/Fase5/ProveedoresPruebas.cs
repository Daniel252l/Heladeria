using Heladeria.Aplicacion.Compras;
using Heladeria.Aplicacion.Configuracion;
using Microsoft.EntityFrameworkCore;
using static Heladeria.Pruebas.Fase5.EscenarioFase5;

namespace Heladeria.Pruebas.Fase5;

public sealed class ProveedoresPruebas
{
    private static ContextoConfiguracion Seguridad => EscenarioFase5.Seguridad;
    [Fact]
    public async Task ListadoFiltraPaginaSinOtraEmpresa()
    {
        await using var e = await CrearAsync();
        await e.Proveedores.GuardarAsync(null, Proveedor(nit: "N-1", correo: "nuevo@p.local"), Seguridad, default);
        var listado = await e.Proveedores.ListarAsync(new() { Busqueda = "proveedor", TamanioPagina = 1 }, Seguridad, default);
        Assert.Equal(2, listado.TotalRegistros); Assert.Single(listado.Elementos);
        Assert.All(listado.Elementos, item => Assert.Equal(1, item.EmpresaId));
        var filtrado = await e.Proveedores.ListarAsync(new() { Codigo = "P-NUEVO", Nombre = "Nuevo", Nit = "N-1", Correo = "nuevo", EstadoId = 1 }, Seguridad, default);
        Assert.Single(filtrado.Elementos);
    }
    [Fact]
    public async Task DetalleY404AntiIdor()
    {
        await using var e = await CrearAsync();
        Assert.Equal("P1", (await e.Proveedores.ObtenerAsync(1, Seguridad, default)).CodigoProveedor);
        await Assert.ThrowsAsync<RecursoComprasNoEncontradoException>(() => e.Proveedores.ObtenerAsync(2, Seguridad, default));
        await Assert.ThrowsAsync<RecursoComprasNoEncontradoException>(() => e.Proveedores.ObtenerAsync(999, Seguridad, default));
    }
    [Fact]
    public async Task CrearNormalizaYAudita()
    {
        await using var e = await CrearAsync();
        var proveedor = await e.Proveedores.GuardarAsync(null, Proveedor(" p-nuevo ", nit: " nit ", correo: "Nuevo@p.local"), Seguridad, default);
        Assert.Equal("P-NUEVO", proveedor.CodigoProveedor); Assert.Equal("NIT", proveedor.Nit);
        Assert.Equal("nuevo@p.local", proveedor.Correo);
        Assert.Single(e.Contexto.Bitacoras.Where(item => item.Accion == "PROVEEDOR_CREADO"));
    }
    [Theory]
    [InlineData("P1", null, null)]
    [InlineData("OTRO", "NIT", null)]
    [InlineData("OTRO", null, "nuevo@p.local")]
    public async Task DuplicadosRechazados(string codigo, string? nit, string? correo)
    {
        await using var e = await CrearAsync();
        await e.Proveedores.GuardarAsync(null, Proveedor(nit: "NIT", correo: "nuevo@p.local"), Seguridad, default);
        await Assert.ThrowsAsync<ConflictoComprasException>(() => e.Proveedores.GuardarAsync(null, Proveedor(codigo, nit: nit, correo: correo), Seguridad, default));
    }
    [Fact]
    public async Task ActualizarEInactivarSoloModificaProveedorSolicitado()
    {
        await using var e = await CrearAsync();
        await e.Proveedores.GuardarAsync(1, Proveedor("P1", "Nombre actualizado"), Seguridad, default);
        var resultado = await e.Proveedores.CambiarEstadoAsync(1, 2, Seguridad, default);
        Assert.Equal("Nombre actualizado", resultado.NombreProveedor); Assert.Equal(2, resultado.EstadoId);
        Assert.Equal(1, (await e.Contexto.Proveedores.AsNoTracking().SingleAsync(item => item.Id == 2)).EstadoId);
    }
    [Fact]
    public async Task EscriturasOtraEmpresaNoModificanDatos()
    {
        await using var e = await CrearAsync();
        await Assert.ThrowsAsync<RecursoComprasNoEncontradoException>(() => e.Proveedores.GuardarAsync(2, Proveedor(), Seguridad, default));
        await Assert.ThrowsAsync<RecursoComprasNoEncontradoException>(() => e.Proveedores.CambiarEstadoAsync(2, 2, Seguridad, default));
        Assert.Equal("P2", (await e.Contexto.Proveedores.AsNoTracking().SingleAsync(item => item.Id == 2)).CodigoProveedor);
    }
    [Fact]
    public async Task CorreoCreditoEstadoMonedaInvalidos()
    {
        await using var e = await CrearAsync();
        foreach (var solicitud in new GuardarProveedorSolicitud[]
        {
            Proveedor(correo: "invalido"), Proveedor(estado: 999),
            new() { CodigoProveedor = "P", NombreProveedor = "P", EstadoId = 1, DiasCredito = -1 },
            new() { CodigoProveedor = "P", NombreProveedor = "P", EstadoId = 1, MonedaId = 2 },
            new() { CodigoProveedor = "P", NombreProveedor = "P", EstadoId = 1, LimiteCredito = 0.001m }
        }) await Assert.ThrowsAsync<ValidacionComprasException>(() => e.Proveedores.GuardarAsync(null, solicitud, Seguridad, default));
    }
    [Fact]
    public async Task AsociacionUsaSoloColumnasRealesYNoDuplica()
    {
        await using var e = await CrearAsync();
        var solicitud = new GuardarProveedorProductoSolicitud { ProductoId = 1, CodigoProveedor = "CH", PrecioUltimaCompra = 20, EsPreferido = true, EstadoId = 1 };
        var relacion = await e.Proveedores.GuardarProductoAsync(1, null, solicitud, Seguridad, default);
        Assert.Single((await e.Proveedores.ListarProductosAsync(1, new FiltroConEstadoSolicitud(), Seguridad, default)).Elementos);
        await Assert.ThrowsAsync<ConflictoComprasException>(() => e.Proveedores.GuardarProductoAsync(1, null, solicitud, Seguridad, default));
        await e.Proveedores.CambiarEstadoProductoAsync(1, relacion.Id, 2, Seguridad, default);
        var actual = await e.Proveedores.GuardarProductoAsync(1, relacion.Id, solicitud, Seguridad, default);
        Assert.Equal(relacion.Id, actual.Id); Assert.Equal(1, actual.EstadoId);
        await Assert.ThrowsAsync<ValidacionComprasException>(() => e.Proveedores.GuardarProductoAsync(1, null,
            new() { ProductoId = 3, EstadoId = 1 }, Seguridad, default));
        await Assert.ThrowsAsync<RecursoComprasNoEncontradoException>(() => e.Proveedores.CambiarEstadoProductoAsync(2, relacion.Id, 1, Seguridad, default));
    }
    [Fact]
    public async Task ProveedorConOrdenPendienteNoPuedeInactivarsePorNingunCamino()
    {
        await using var e = await CrearAsync(); await e.ConfirmarAsync();
        await Assert.ThrowsAsync<ConflictoComprasException>(() => e.Proveedores.CambiarEstadoAsync(1, 2, Seguridad, default));
        await Assert.ThrowsAsync<ConflictoComprasException>(() => e.Proveedores.GuardarAsync(1, Proveedor("P1", estado: 2), Seguridad, default));
    }
}
