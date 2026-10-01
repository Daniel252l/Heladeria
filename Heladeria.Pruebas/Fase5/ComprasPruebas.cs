using Heladeria.Aplicacion.Compras;
using Heladeria.Aplicacion.Productos;
using Heladeria.Infraestructura.Persistencia.Entidades;
using Microsoft.EntityFrameworkCore;
using static Heladeria.Pruebas.Fase5.EscenarioFase5;

namespace Heladeria.Pruebas.Fase5;

public sealed class ComprasPruebas
{
    private static Heladeria.Aplicacion.Configuracion.ContextoConfiguracion Seguridad => EscenarioFase5.Seguridad;
    [Fact]
    public async Task CatalogoCompraDevuelvePresentacionesValidasConConversionCompleta()
    {
        await using var e = await CrearAsync();
        var catalogo = await e.Compras.ListarPresentacionesAsync(1, Seguridad, default);

        Assert.Equal("Helado Chocolate", catalogo.Producto);
        Assert.Equal("ML", catalogo.UnidadBaseCodigo);
        Assert.Equal("ml", catalogo.UnidadBaseAbreviatura);
        Assert.Collection(catalogo.Presentaciones,
            litro =>
            {
                Assert.Equal(1, litro.ProductoPresentacionId);
                Assert.Equal("LITRO", litro.PresentacionCodigo);
                Assert.Equal("L", litro.UnidadCodigo);
                Assert.Equal(1000m, litro.FactorConversion);
                Assert.True(litro.Activo);
            },
            vaso =>
            {
                Assert.Equal(2, vaso.ProductoPresentacionId);
                Assert.Equal("VASO", vaso.PresentacionCodigo);
                Assert.Equal("VASO", vaso.UnidadCodigo);
                Assert.Equal(250m, vaso.FactorConversion);
                Assert.True(vaso.Activo);
            });
    }

    [Fact]
    public async Task CatalogoCompraExcluyePresentacionInactiva()
    {
        await using var e = await CrearAsync();
        (await e.Contexto.ProductoPresentaciones.SingleAsync(item => item.Id == 2)).EstadoId = 2;
        await e.Contexto.SaveChangesAsync();
        e.Contexto.ChangeTracker.Clear();

        var catalogo = await e.Compras.ListarPresentacionesAsync(1, Seguridad, default);

        var litro = Assert.Single(catalogo.Presentaciones);
        Assert.Equal("LITRO", litro.PresentacionCodigo);
    }

    [Fact]
    public async Task CatalogoCompraOcultaProductoInexistenteODeOtraEmpresa()
    {
        await using var e = await CrearAsync();

        await Assert.ThrowsAsync<RecursoComprasNoEncontradoException>(() =>
            e.Compras.ListarPresentacionesAsync(999, Seguridad, default));
        await Assert.ThrowsAsync<RecursoComprasNoEncontradoException>(() =>
            e.Compras.ListarPresentacionesAsync(3, Seguridad, default));
    }

    [Fact]
    public async Task CrearOrdenCalculaTotalesConversionCorrelativoYSinInventario()
    {
        await using var e = await CrearAsync();
        var orden = await e.Compras.GuardarAsync(null, Orden(10, 100, descuento: 10), Seguridad, default);
        Assert.Equal("BORRADOR", orden.Encabezado.Estado); Assert.Equal("OC-0000000001", orden.Encabezado.NumeroOrden);
        Assert.Equal(900m, orden.Encabezado.TotalEstimado); Assert.Equal(10000m, orden.Detalles[0].CantidadBase);
        Assert.Empty(e.Contexto.Movimientos); Assert.Empty(e.Contexto.RecepcionesCompra);
        Assert.Equal(5000m, (await e.Contexto.Existencias.AsNoTracking().SingleAsync(item => item.Id == 1)).Cantidad);
        Assert.Single(e.Contexto.Bitacoras.Where(item => item.Accion == "COMPRA_CREADA"));
    }
    [Theory]
    [InlineData(999, 1, 1)]
    [InlineData(2, 1, 1)]
    [InlineData(1, 999, 1)]
    [InlineData(1, 3, 1)]
    [InlineData(1, 1, 999)]
    [InlineData(1, 2, 1)]
    public async Task ProveedorProductoPresentacionInvalidos(int proveedor, long producto, long presentacion)
    {
        await using var e = await CrearAsync();
        await Assert.ThrowsAsync<ValidacionComprasException>(() => e.Compras.GuardarAsync(null,
            Orden(proveedor: proveedor, producto: producto, presentacion: presentacion), Seguridad, default));
        Assert.Empty(e.Contexto.OrdenesCompra);
        Assert.All(e.Contexto.CorrelativosDocumentos, item => Assert.Equal(0, item.UltimoNumero));
    }
    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]
    [InlineData(1, -1)]
    [InlineData(1, 0.001)]
    [InlineData(0.0001, 1)]
    public async Task CantidadPrecioPrecisionInvalidos(decimal cantidad, decimal precio)
    {
        await using var e = await CrearAsync();
        await Assert.ThrowsAsync<ValidacionComprasException>(() => e.Compras.GuardarAsync(null, Orden(cantidad, precio), Seguridad, default));
        Assert.Empty(e.Contexto.OrdenesCompra);
    }
    [Fact]
    public async Task LineasDuplicadasRechazadas()
    {
        await using var e = await CrearAsync(); var normal = Orden();
        var solicitud = new GuardarCompraSolicitud { SucursalId = 1, ProveedorId = 1, MonedaId = 1, FechaEmision = normal.FechaEmision,
            Detalles = [normal.Detalles[0], normal.Detalles[0]] };
        await Assert.ThrowsAsync<ValidacionComprasException>(() => e.Compras.GuardarAsync(null, solicitud, Seguridad, default));
    }
    [Fact]
    public async Task ImpuestosConfiguradosVigentesNoPorcentajeHardcodeado()
    {
        await using var e = await CrearAsync();
        e.Contexto.AddRange(new Impuestos { Id = 1, EmpresaId = 1, Codigo = "CONFIG", Nombre = "Configurado", Porcentaje = 7.5m,
            FechaVigenciaDesde = new(2026, 1, 1), EstadoId = 1 },
            new ProductoImpuestos { Id = 1, ProductoId = 1, ImpuestoId = 1, EstadoId = 1 });
        await e.Contexto.SaveChangesAsync();
        var orden = await e.Compras.GuardarAsync(null, Orden(2, 100, descuento: 10), Seguridad, default);
        Assert.Equal(180m, orden.Encabezado.SubTotal); Assert.Equal(13.5m, orden.Encabezado.Impuestos);
        Assert.Equal(193.5m, orden.Encabezado.TotalEstimado);
    }
    [Fact]
    public async Task ImpuestoIncluidoNoSeInterpretaSinPoliticaFiscal()
    {
        await using var e = await CrearAsync();
        e.Contexto.AddRange(new Impuestos { Id = 1, EmpresaId = 1, Codigo = "INC", Nombre = "Incluido", Porcentaje = 7,
            IncluidoEnPrecio = true, FechaVigenciaDesde = new(2026, 1, 1), EstadoId = 1 },
            new ProductoImpuestos { Id = 1, ProductoId = 1, ImpuestoId = 1, EstadoId = 1 });
        await e.Contexto.SaveChangesAsync();
        await Assert.ThrowsAsync<ConflictoComprasException>(() => e.Compras.GuardarAsync(null, Orden(), Seguridad, default));
    }
    [Fact]
    public async Task ListadoPaginadoFiltraYDetalleExponePendiente()
    {
        await using var e = await CrearAsync(); var orden = await e.ConfirmarAsync();
        var pagina = await e.Compras.ListarAsync(new() { ProveedorId = 1, SucursalId = 1, EstadoId = orden.Encabezado.EstadoId,
            FechaDesde = new(2026, 9, 1), FechaHasta = new(2026, 9, 30), Documento = "OC-", Busqueda = "Proveedor", TamanioPagina = 1 }, Seguridad, default);
        Assert.Single(pagina.Elementos); Assert.Equal(1, pagina.TotalRegistros);
        Assert.Equal(10m, orden.Detalles[0].CantidadPendiente); Assert.Empty(orden.Recepciones);
    }
    [Fact]
    public async Task ActualizacionSoloBorradorAnulacionSoloSinRecepciones()
    {
        await using var e = await CrearAsync(); var orden = await e.Compras.GuardarAsync(null, Orden(), Seguridad, default);
        var actual = await e.Compras.GuardarAsync(orden.Encabezado.Id, Orden(8, 120), Seguridad, default);
        Assert.Equal(960m, actual.Encabezado.TotalEstimado); Assert.Single(actual.Detalles);
        await e.Compras.CambiarEstadoAsync(orden.Encabezado.Id, "CONFIRMADA", Seguridad, default);
        await Assert.ThrowsAsync<ConflictoComprasException>(() => e.Compras.GuardarAsync(orden.Encabezado.Id, Orden(), Seguridad, default));
        var anulada = await e.Compras.CambiarEstadoAsync(orden.Encabezado.Id, "ANULADA", Seguridad, default);
        Assert.Equal("ANULADA", anulada.Encabezado.Estado); Assert.Empty(e.Contexto.Movimientos);
        await Assert.ThrowsAsync<ConflictoComprasException>(() => e.Compras.RecibirAsync(orden.Encabezado.Id, Recepcion(actual.Detalles[0].Id, 1), Seguridad, default));
    }
    [Fact]
    public async Task IdorSucursalInclusoAsignadaYEmpresaDevuelve404()
    {
        await using var e = await CrearAsync(); var orden = await e.ConfirmarAsync();
        var otraSucursal = Seguridad with { SucursalId = 2 };
        await Assert.ThrowsAsync<RecursoComprasNoEncontradoException>(() => e.Compras.ObtenerAsync(orden.Encabezado.Id, otraSucursal, default));
        await Assert.ThrowsAsync<RecursoComprasNoEncontradoException>(() => e.Compras.RecibirAsync(orden.Encabezado.Id, Recepcion(orden.Detalles[0].Id, 1), otraSucursal, default));
        await Assert.ThrowsAsync<RecursoComprasNoEncontradoException>(() => e.Compras.GuardarAsync(null, Orden(sucursal: 2), Seguridad, default));
        await Assert.ThrowsAsync<RecursoComprasNoEncontradoException>(() => e.Compras.ListarAsync(new() { SucursalId = 2 }, Seguridad, default));
        await Assert.ThrowsAsync<RecursoComprasNoEncontradoException>(() => e.Compras.ObtenerAsync(999, Seguridad, default));
    }
    [Fact]
    public async Task MonedaAjenaOBaseSinConfigurarNoPermiteOrden()
    {
        await using var e = await CrearAsync();
        await Assert.ThrowsAsync<ValidacionComprasException>(() => e.Compras.GuardarAsync(null, Orden(moneda: 2), Seguridad, default));
        (await e.Contexto.Monedas.SingleAsync(item => item.Id == 1)).EsMonedaBase = false;
        await e.Contexto.SaveChangesAsync();
        await Assert.ThrowsAsync<ConflictoComprasException>(() => e.Compras.GuardarAsync(null, Orden(), Seguridad, default));
    }
    [Fact]
    public async Task MetodoNoSoportadoNoSeValoraSilenciosamente()
    {
        await using var e = await CrearAsync();
        (await e.Contexto.Productos.SingleAsync(item => item.Id == 1)).ControlaVencimiento = true; await e.Contexto.SaveChangesAsync();
        await Assert.ThrowsAsync<ConflictoComprasException>(() => e.Compras.GuardarAsync(null, Orden(), Seguridad, default));
    }
    [Fact]
    public async Task LineaNulaYMonedaConPrecisionNoRepresentableSonErroresControlados()
    {
        await using var e = await CrearAsync();
        await Assert.ThrowsAsync<ValidacionComprasException>(() => e.Compras.GuardarAsync(null,
            new() { SucursalId = 1, ProveedorId = 1, MonedaId = 1, FechaEmision = new(2026, 9, 17), Detalles = [null!] }, Seguridad, default));
        (await e.Contexto.Monedas.SingleAsync(item => item.Id == 1)).Decimales = 3; await e.Contexto.SaveChangesAsync();
        await Assert.ThrowsAsync<ConflictoComprasException>(() => e.Compras.GuardarAsync(null, Orden(), Seguridad, default));
        Assert.Empty(e.Contexto.OrdenesCompra);
    }
    [Fact]
    public async Task PresentacionUtilizadaNoPuedeCambiarFactor()
    {
        await using var e = await CrearAsync(); await e.ConfirmarAsync();
        var actual = (await e.Fase4.Productos.ListarPresentacionesAsync(1, Seguridad, default)).Single(item => item.Id == 1);
        await Assert.ThrowsAsync<ConflictoFase4Exception>(() => e.Fase4.Productos.ActualizarPresentacionAsync(1, 1,
            new ActualizarProductoPresentacionSolicitud { PresentacionId = 1, UnidadMedidaId = 2, FactorConversion = 500,
                PermiteCompra = true, PermiteVenta = true, EstadoId = 1, VersionFila = actual.VersionFila }, Seguridad, default));
        Assert.Equal(1000m, (await e.Contexto.ProductoPresentaciones.AsNoTracking().SingleAsync(item => item.Id == 1)).FactorConversion);
    }
}
