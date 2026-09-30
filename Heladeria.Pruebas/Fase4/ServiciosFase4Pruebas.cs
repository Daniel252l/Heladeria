using Heladeria.Aplicacion.Configuracion;
using Heladeria.Aplicacion.Inventario;
using Heladeria.Aplicacion.Productos;
using Heladeria.Aplicacion.Seguridad.Configuracion;
using Heladeria.Infraestructura.Fase4;
using Heladeria.Infraestructura.Inventario;
using Heladeria.Infraestructura.Persistencia;
using Heladeria.Infraestructura.Persistencia.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;

namespace Heladeria.Pruebas.Fase4;

public sealed class ServiciosFase4Pruebas
{
    private static readonly ContextoConfiguracion Seguridad = new(1, "ADMIN", 1, 1, "127.0.0.1", "xUnit");

    [Fact]
    public async Task Productos_ListadoFiltraPaginaYOrdena()
    {
        await using var escenario = await EscenarioFase4.CrearAsync();
        var pagina = await escenario.Productos.ListarAsync(
            new FiltroProductosSolicitud { Busqueda = "helado", Pagina = 1, TamanioPagina = 1 },
            Seguridad,
            CancellationToken.None);
        Assert.Equal(2, pagina.TotalRegistros);
        Assert.Single(pagina.Elementos);
        Assert.Equal("Helado Chocolate", pagina.Elementos[0].Nombre);
    }

    [Fact]
    public async Task Productos_ObtenerRespetaEmpresaYDevuelve404()
    {
        await using var escenario = await EscenarioFase4.CrearAsync();
        Assert.Equal("HEL-CHO", (await escenario.Productos.ObtenerAsync(1, Seguridad, CancellationToken.None)).SKU);
        await Assert.ThrowsAsync<RecursoFase4NoEncontradoException>(() =>
            escenario.Productos.ObtenerAsync(3, Seguridad, CancellationToken.None));
        await Assert.ThrowsAsync<RecursoFase4NoEncontradoException>(() =>
            escenario.Productos.ObtenerAsync(999, Seguridad, CancellationToken.None));
    }

    [Fact]
    public async Task Productos_ReferenciasSoloDevuelveCatalogosActivosDeLaEmpresa()
    {
        await using var escenario = await EscenarioFase4.CrearAsync();
        var referencias = await escenario.Productos.ObtenerReferenciasAsync(Seguridad, CancellationToken.None);
        Assert.Collection(referencias.TiposSeguimiento,
            item => Assert.Equal("NINGUNO", item.Codigo));
        Assert.Collection(referencias.MetodosValuacion,
            item => Assert.Equal("PROMEDIO", item.Codigo));
    }

    [Fact]
    public async Task Productos_CrearNormalizaYAudita()
    {
        await using var escenario = await EscenarioFase4.CrearAsync();
        var creado = await escenario.Productos.CrearAsync(
            EscenarioFase4.CrearProducto(" nuevo-1 ", "Helado Fresa"),
            Seguridad,
            CancellationToken.None);
        Assert.Equal("NUEVO-1", creado.SKU);
        Assert.Equal(1, creado.EmpresaId);
        Assert.Equal(1, escenario.Auditoria.Cantidad);
    }

    [Fact]
    public async Task Productos_RechazaSkuONombreDuplicado()
    {
        await using var escenario = await EscenarioFase4.CrearAsync();
        await Assert.ThrowsAsync<ConflictoFase4Exception>(() => escenario.Productos.CrearAsync(
            EscenarioFase4.CrearProducto("HEL-CHO", "Otro producto"), Seguridad, CancellationToken.None));
        await Assert.ThrowsAsync<ConflictoFase4Exception>(() => escenario.Productos.CrearAsync(
            EscenarioFase4.CrearProducto("OTRO", "helado chocolate"), Seguridad, CancellationToken.None));
    }

    [Fact]
    public async Task Productos_ActualizarUsaVersionYNoPermiteFkInvalida()
    {
        await using var escenario = await EscenarioFase4.CrearAsync();
        var actual = await escenario.Productos.ObtenerAsync(1, Seguridad, CancellationToken.None);
        var actualizado = await escenario.Productos.ActualizarAsync(
            1,
            EscenarioFase4.ActualizarProducto("HEL-CHO", "Chocolate premium", actual.VersionFila),
            Seguridad,
            CancellationToken.None);
        Assert.Equal("Chocolate premium", actualizado.Nombre);

        var invalida = EscenarioFase4.ActualizarProducto("HEL-CHO", "Chocolate premium", actualizado.VersionFila, categoriaId: 999);
        await Assert.ThrowsAsync<ValidacionFase4Exception>(() =>
            escenario.Productos.ActualizarAsync(1, invalida, Seguridad, CancellationToken.None));
        await Assert.ThrowsAsync<ConflictoFase4Exception>(() => escenario.Productos.ActualizarAsync(
            1,
            EscenarioFase4.ActualizarProducto("HEL-CHO", "Otro", Convert.ToBase64String([99])),
            Seguridad,
            CancellationToken.None));
    }

    [Fact]
    public async Task Productos_InactivarConservaHistorialYExistencia()
    {
        await using var escenario = await EscenarioFase4.CrearAsync();
        var resultado = await escenario.Productos.CambiarEstadoAsync(1, 2, Seguridad, CancellationToken.None);
        Assert.Equal(2, resultado.EstadoId);
        Assert.Equal(2, escenario.Contexto.Existencias.Count(item => item.ProductoId == 1));
    }

    [Fact]
    public async Task Unidades_CreacionValidaYDuplicado()
    {
        await using var escenario = await EscenarioFase4.CrearAsync();
        var unidad = await escenario.Catalogos.CrearUnidadAsync(
            new CrearUnidadMedidaSolicitud { Codigo = "kg", Nombre = "Kilogramo", Abreviatura = "kg", PermiteDecimales = true, CantidadDecimales = 3, EstadoId = 1 },
            Seguridad,
            CancellationToken.None);
        Assert.Equal("KG", unidad.Codigo);
        await Assert.ThrowsAsync<ConflictoFase4Exception>(() => escenario.Catalogos.CrearUnidadAsync(
            new CrearUnidadMedidaSolicitud { Codigo = "KG", Nombre = "Otro", Abreviatura = "kg", PermiteDecimales = true, CantidadDecimales = 3, EstadoId = 1 },
            Seguridad,
            CancellationToken.None));
    }

    [Fact]
    public async Task Categorias_CreanJerarquiaYRechazanDuplicado()
    {
        await using var escenario = await EscenarioFase4.CrearAsync();
        var creada = await escenario.Catalogos.CrearCategoriaAsync(
            new CrearCategoriaSolicitud { CategoriaPadreId = 1, Codigo = "PREMIUM", Nombre = "Premium", EstadoId = 1 },
            Seguridad,
            CancellationToken.None);
        Assert.Equal(2, creada.Nivel);
        await Assert.ThrowsAsync<ConflictoFase4Exception>(() => escenario.Catalogos.CrearCategoriaAsync(
            new CrearCategoriaSolicitud { Codigo = "premium", Nombre = "Otra", EstadoId = 1 },
            Seguridad,
            CancellationToken.None));
    }

    [Fact]
    public async Task MarcaYTipoProducto_NoSeInactivanConProductosActivos()
    {
        await using var escenario = await EscenarioFase4.CrearAsync();
        await Assert.ThrowsAsync<ConflictoFase4Exception>(() =>
            escenario.Catalogos.CambiarEstadoMarcaAsync(1, 2, Seguridad, CancellationToken.None));
        await Assert.ThrowsAsync<ConflictoFase4Exception>(() =>
            escenario.Catalogos.CambiarEstadoTipoProductoAsync(1, 2, Seguridad, CancellationToken.None));
    }

    [Fact]
    public async Task Unidad_NoPuedeInactivarseSiEsBaseDeProductoActivo()
    {
        await using var escenario = await EscenarioFase4.CrearAsync();
        await Assert.ThrowsAsync<ConflictoFase4Exception>(() =>
            escenario.Catalogos.CambiarEstadoUnidadAsync(1, 2, Seguridad, CancellationToken.None));
    }

    [Fact]
    public async Task PresentacionProducto_ValidaFactorDuplicadoUnidadYProducto()
    {
        await using var escenario = await EscenarioFase4.CrearAsync();
        await Assert.ThrowsAsync<ConflictoFase4Exception>(() => escenario.Productos.CrearPresentacionAsync(
            1,
            new CrearProductoPresentacionSolicitud { PresentacionId = 3, UnidadMedidaId = 2, FactorConversion = 1000, PermiteVenta = true, EstadoId = 1 },
            Seguridad,
            CancellationToken.None));
        await Assert.ThrowsAsync<ValidacionFase4Exception>(() => escenario.Productos.CrearPresentacionAsync(
            1,
            new CrearProductoPresentacionSolicitud { PresentacionId = 3, UnidadMedidaId = 2, FactorConversion = 0, EstadoId = 1 },
            Seguridad,
            CancellationToken.None));
        await Assert.ThrowsAsync<ValidacionFase4Exception>(() => escenario.Productos.CrearPresentacionAsync(
            999,
            new CrearProductoPresentacionSolicitud { PresentacionId = 3, UnidadMedidaId = 1, FactorConversion = 1, EstadoId = 1 },
            Seguridad,
            CancellationToken.None));
        await Assert.ThrowsAsync<ValidacionFase4Exception>(() => escenario.Productos.CrearPresentacionAsync(
            1,
            new CrearProductoPresentacionSolicitud { PresentacionId = 3, UnidadMedidaId = 999, FactorConversion = 1, EstadoId = 1 },
            Seguridad,
            CancellationToken.None));
    }

    [Fact]
    public async Task PresentacionCatalogo_NoPuedeInactivarseConDependenciaActiva()
    {
        await using var escenario = await EscenarioFase4.CrearAsync();
        await Assert.ThrowsAsync<ConflictoFase4Exception>(() =>
            escenario.Catalogos.CambiarEstadoPresentacionAsync(1, 2, Seguridad, CancellationToken.None));
    }

    [Fact]
    public async Task Conversiones_RechazanFactorNoPositivoUnidadAjenaYCicloIncoherente()
    {
        await using var escenario = await EscenarioFase4.CrearAsync();
        await Assert.ThrowsAsync<ValidacionFase4Exception>(() => escenario.Catalogos.CrearConversionAsync(
            new CrearConversionUnidadSolicitud { UnidadOrigenId = 1, UnidadDestinoId = 2, Factor = 0, EstadoId = 1 }, Seguridad, CancellationToken.None));
        await Assert.ThrowsAsync<ValidacionFase4Exception>(() => escenario.Catalogos.CrearConversionAsync(
            new CrearConversionUnidadSolicitud { UnidadOrigenId = 1, UnidadDestinoId = 4, Factor = 1, EstadoId = 1 }, Seguridad, CancellationToken.None));

        await escenario.Catalogos.CrearConversionAsync(
            new CrearConversionUnidadSolicitud { UnidadOrigenId = 1, UnidadDestinoId = 2, Factor = 0.001m, EstadoId = 1 }, Seguridad, CancellationToken.None);
        await Assert.ThrowsAsync<ConflictoFase4Exception>(() => escenario.Catalogos.CrearConversionAsync(
            new CrearConversionUnidadSolicitud { UnidadOrigenId = 2, UnidadDestinoId = 1, Factor = 999, EstadoId = 1 }, Seguridad, CancellationToken.None));
        var inversa = await escenario.Catalogos.CrearConversionAsync(
            new CrearConversionUnidadSolicitud { UnidadOrigenId = 2, UnidadDestinoId = 1, Factor = 1000, EstadoId = 1 }, Seguridad, CancellationToken.None);
        Assert.Equal(1000, inversa.Factor);
    }

    [Fact]
    public async Task Existencias_ConsultaPorSucursalMantieneAislamiento()
    {
        await using var escenario = await EscenarioFase4.CrearAsync();
        var central = await escenario.Inventario.ListarExistenciasAsync(
            new FiltroExistenciasSolicitud { SucursalId = 1 }, Seguridad, CancellationToken.None);
        var norte = await escenario.Inventario.ListarExistenciasAsync(
            new FiltroExistenciasSolicitud { SucursalId = 2 }, Seguridad, CancellationToken.None);
        Assert.Single(central.Elementos);
        Assert.Equal(5000, central.Elementos[0].Cantidad);
        Assert.Single(norte.Elementos);
        Assert.Equal(900, norte.Elementos[0].Cantidad);
    }

    [Fact]
    public async Task Inventario_AlmacenesSoloIncluyeSucursalesAsignadasAlUsuario()
    {
        await using var escenario = await EscenarioFase4.CrearAsync();
        var almacenes = await escenario.Inventario.ListarAlmacenesAsync(Seguridad, CancellationToken.None);
        Assert.Equal(2, almacenes.Count);
        Assert.Contains(almacenes, item => item.SucursalId == 1 && item.BodegaId == 1 && item.UbicacionId == 1);
        Assert.Contains(almacenes, item => item.SucursalId == 2 && item.BodegaId == 2 && item.UbicacionId == 2);
        Assert.DoesNotContain(almacenes, item => item.SucursalId == 4);
    }

    [Fact]
    public async Task Existencias_IDORDeSucursalDevuelve404()
    {
        await using var escenario = await EscenarioFase4.CrearAsync();
        await Assert.ThrowsAsync<RecursoFase4NoEncontradoException>(() =>
            escenario.Inventario.ListarExistenciasAsync(
                new FiltroExistenciasSolicitud { SucursalId = 4 }, Seguridad, CancellationToken.None));
    }

    [Fact]
    public async Task Existencias_FiltroBajoMinimoUsaParametrosStockReales()
    {
        await using var escenario = await EscenarioFase4.CrearAsync();
        var resultado = await escenario.Inventario.ListarExistenciasAsync(
            new FiltroExistenciasSolicitud { SucursalId = 1, SoloBajoMinimo = true },
            Seguridad,
            CancellationToken.None);
        Assert.Single(resultado.Elementos);
        Assert.True(resultado.Elementos[0].BajoMinimo);
        Assert.Equal(6000, resultado.Elementos[0].StockMinimo);
    }

    [Fact]
    public async Task AjusteEntrada_ConvierteCantidadYCostoDeLitrosAUnidadBase()
    {
        await using var escenario = await EscenarioFase4.CrearAsync();
        var resultado = await escenario.Inventario.AjustarAsync(
            EscenarioFase4.Ajuste("ENTRADA", 3, presentacionId: 1, costo: 20m),
            Seguridad,
            CancellationToken.None);
        Assert.Equal(1000, resultado.FactorConversion);
        Assert.Equal(3000, resultado.CantidadBase);
        Assert.Equal(8000, resultado.SaldoNuevo);
        Assert.Equal(0.02m, resultado.CostoUnitario);
        Assert.Single(escenario.Contexto.Movimientos, item => item.Id == resultado.MovimientoId);
        var detalle = Assert.Single(escenario.Contexto.MovimientoDetalles,
            item => item.Id == resultado.MovimientoDetalleId);
        Assert.Equal(0.02m, detalle.CostoUnitario);
        Assert.Equal(60m, detalle.CantidadBase * detalle.CostoUnitario);
        var kardex = Assert.Single(escenario.Contexto.Kardex,
            item => item.MovimientoDetalleId == resultado.MovimientoDetalleId);
        Assert.Equal(0.02m, kardex.CostoUnitario);
        Assert.Equal(60m, kardex.Cantidad * kardex.CostoUnitario);
        Assert.Equal(0.01375m, (await escenario.Contexto.Existencias.SingleAsync(item => item.Id == 1)).CostoPromedio);
        Assert.Equal(0.01375m, Assert.Single(escenario.Contexto.CostosProductos, item => item.ProductoId == 1).CostoActual);
        Assert.Equal(0.01375m, Assert.Single(escenario.Contexto.HistoricoCostosProductos, item => item.ProductoId == 1).CostoNuevo);
        Assert.Equal(1, escenario.Auditoria.Cantidad);
    }

    [Fact]
    public async Task AjusteEntrada_SinPresentacionConservaCostoDeUnidadBase()
    {
        await using var escenario = await EscenarioFase4.CrearAsync();
        var resultado = await escenario.Inventario.AjustarAsync(
            EscenarioFase4.Ajuste("ENTRADA", 100, costo: 0.02m),
            Seguridad,
            CancellationToken.None);

        Assert.Equal(1m, resultado.FactorConversion);
        Assert.Equal(0.02m, resultado.CostoUnitario);
        Assert.Equal(0.02m, (await escenario.Contexto.MovimientoDetalles.SingleAsync()).CostoUnitario);
        Assert.Equal(2m, resultado.CantidadBase * resultado.CostoUnitario);
    }

    [Fact]
    public async Task AjusteEntrada_PersisteCostoBaseCoherenteEnKardexCostosEHistorico()
    {
        await using var escenario = await EscenarioFase4.CrearAsync();
        var resultado = await escenario.Inventario.AjustarAsync(
            EscenarioFase4.Ajuste("ENTRADA", 3, presentacionId: 1, costo: 20m),
            Seguridad,
            CancellationToken.None);

        var kardex = await escenario.Contexto.Kardex.SingleAsync(
            item => item.MovimientoDetalleId == resultado.MovimientoDetalleId);
        Assert.Equal(0.02m, kardex.CostoUnitario);
        Assert.Equal(110m, kardex.SaldoCosto);
        Assert.Equal(0.01375m, (await escenario.Contexto.CostosProductos.SingleAsync(
            item => item.ProductoId == 1)).CostoActual);
        var historico = await escenario.Contexto.HistoricoCostosProductos.SingleAsync(
            item => item.ProductoId == 1);
        Assert.Equal("AJUSTE_INVENTARIO", historico.OrigenCambio);
        Assert.Equal(0.01375m, historico.CostoNuevo);
    }

    [Fact]
    public async Task AjusteSalida_ConvierteVasosYDescuentaUnidadBase()
    {
        await using var escenario = await EscenarioFase4.CrearAsync();
        var resultado = await escenario.Inventario.AjustarAsync(
            EscenarioFase4.Ajuste("SALIDA", 2, presentacionId: 2),
            Seguridad,
            CancellationToken.None);
        Assert.Equal(250, resultado.FactorConversion);
        Assert.Equal(500, resultado.CantidadBase);
        Assert.Equal(4500, resultado.SaldoNuevo);
    }

    [Fact]
    public async Task AjusteSalida_RechazaSaldoInsuficienteSinInventarioNegativo()
    {
        await using var escenario = await EscenarioFase4.CrearAsync();
        await Assert.ThrowsAsync<ExistenciaInsuficienteException>(() => escenario.Inventario.AjustarAsync(
            EscenarioFase4.Ajuste("SALIDA", 21, presentacionId: 2), Seguridad, CancellationToken.None));
        Assert.Equal(5000, (await escenario.Contexto.Existencias.SingleAsync(item => item.Id == 1)).Cantidad);
        Assert.Empty(escenario.Contexto.Movimientos);
    }

    [Fact]
    public async Task AjusteEntrada_RechazaValorizacionQueExcedeDecimalSql()
    {
        await using var escenario = await EscenarioFase4.CrearAsync();
        await Assert.ThrowsAsync<ValidacionFase4Exception>(() => escenario.Inventario.AjustarAsync(
            EscenarioFase4.Ajuste("ENTRADA", 1_000_000, costo: 100_000_000),
            Seguridad,
            CancellationToken.None));
        Assert.Empty(escenario.Contexto.Movimientos);
    }

    [Fact]
    public async Task AjustesConcurrentes_SerializanYSoloUnoPuedeConsumirStockLimitado()
    {
        var raiz = EscenarioFase4.RaizCompartida;
        var nombre = $"fase4-concurrencia-{Guid.NewGuid():N}";
        await using (var semilla = await EscenarioFase4.CrearAsync(nombre, raiz, semillar: true))
        {
            (await semilla.Contexto.Existencias.SingleAsync(item => item.Id == 1)).Cantidad = 10;
            await semilla.Contexto.SaveChangesAsync();
        }
        var coordinador = new CoordinadorAjustesInventario();
        await using var escenarioA = await EscenarioFase4.CrearAsync(nombre, raiz, semillar: false, coordinador);
        await using var escenarioB = await EscenarioFase4.CrearAsync(nombre, raiz, semillar: false, coordinador);
        var resultados = await Task.WhenAll(
            IntentarAsync(escenarioA.Inventario, EscenarioFase4.Ajuste("SALIDA", 7)),
            IntentarAsync(escenarioB.Inventario, EscenarioFase4.Ajuste("SALIDA", 6)));
        Assert.Single(resultados, item => item);
        Assert.Single(resultados, item => !item);
        await using var verificacion = await EscenarioFase4.CrearAsync(nombre, raiz, semillar: false, coordinador);
        var saldo = (await verificacion.Contexto.Existencias.AsNoTracking().SingleAsync(item => item.Id == 1)).Cantidad;
        Assert.True(saldo is 3 or 4);
    }

    private static async Task<bool> IntentarAsync(IServicioInventario servicio, AjusteInventarioSolicitud solicitud)
    {
        try
        {
            await servicio.AjustarAsync(solicitud, Seguridad, CancellationToken.None);
            return true;
        }
        catch (ExistenciaInsuficienteException)
        {
            return false;
        }
    }
}

internal sealed class EscenarioFase4 : IAsyncDisposable
{
    private static readonly IOptions<OpcionesSeguridad> Opciones = Options.Create(new OpcionesSeguridad());
    internal static InMemoryDatabaseRoot RaizCompartida { get; } = new();

    private EscenarioFase4(HeladeriaDbContext contexto, CoordinadorAjustesInventario coordinador)
    {
        Contexto = contexto;
        Auditoria = new AuditoriaFase4Falsa();
        Catalogos = new ServicioCatalogosProductos(contexto, Auditoria, Opciones, TimeProvider.System);
        Productos = new ServicioProductos(contexto, Auditoria, Opciones, TimeProvider.System, coordinador);
        Inventario = new ServicioInventario(contexto, Auditoria, Opciones, TimeProvider.System, coordinador);
    }

    public HeladeriaDbContext Contexto { get; }
    public AuditoriaFase4Falsa Auditoria { get; }
    public ServicioCatalogosProductos Catalogos { get; }
    public ServicioProductos Productos { get; }
    public ServicioInventario Inventario { get; }

    public static Task<EscenarioFase4> CrearAsync() =>
        CrearAsync($"fase4-{Guid.NewGuid():N}", RaizCompartida, true);

    internal static async Task<EscenarioFase4> CrearConContextoAsync(HeladeriaDbContext contexto, CoordinadorAjustesInventario coordinador)
    {
        var escenario = new EscenarioFase4(contexto, coordinador);
        await escenario.SemillarAsync();
        return escenario;
    }

    public static async Task<EscenarioFase4> CrearAsync(
        string nombre,
        InMemoryDatabaseRoot raiz,
        bool semillar,
        CoordinadorAjustesInventario? coordinador = null)
    {
        var opciones = new DbContextOptionsBuilder<HeladeriaDbContext>()
            .UseInMemoryDatabase(nombre, raiz)
            .Options;
        var escenario = new EscenarioFase4(new HeladeriaDbContext(opciones), coordinador ?? new CoordinadorAjustesInventario());
        if (semillar)
            await escenario.SemillarAsync();
        return escenario;
    }

    public ValueTask DisposeAsync() => Contexto.DisposeAsync();

    public static CrearProductoSolicitud CrearProducto(string sku, string nombre) => new()
    {
        CategoriaId = 1,
        MarcaId = 1,
        TipoProductoId = 1,
        UnidadBaseId = 1,
        TipoSeguimientoId = 1,
        MetodoValuacionId = 2,
        SKU = sku,
        Nombre = nombre,
        EsComprable = true,
        EsVendible = true,
        PermiteDescuento = true,
        PrecioVentaReferencial = 10,
        EstadoId = 1
    };

    public static ActualizarProductoSolicitud ActualizarProducto(string sku, string nombre, string version, int categoriaId = 1) => new()
    {
        CategoriaId = categoriaId,
        MarcaId = 1,
        TipoProductoId = 1,
        UnidadBaseId = 1,
        TipoSeguimientoId = 1,
        MetodoValuacionId = 2,
        SKU = sku,
        Nombre = nombre,
        EsComprable = true,
        EsVendible = true,
        PermiteDescuento = true,
        PrecioVentaReferencial = 10,
        EstadoId = 1,
        VersionFila = version
    };

    public static AjusteInventarioSolicitud Ajuste(string tipo, decimal cantidad, long? presentacionId = null, decimal? costo = null) => new()
    {
        SucursalId = 1,
        BodegaId = 1,
        UbicacionId = 1,
        ProductoId = 1,
        ProductoPresentacionId = presentacionId,
        Tipo = tipo,
        Cantidad = cantidad,
        CostoUnitario = costo,
        Referencia = "PRUEBA"
    };

    private async Task SemillarAsync()
    {
        Contexto.AddRange(
            new ClaseEstados { Id = 1, Nombre = "General", Activo = true },
            new Estados { Id = 1, Nombre = "Activo", Activo = true, ClaseEstadoId = 1 },
            new Estados { Id = 2, Nombre = "Inactivo", Activo = true, ClaseEstadoId = 1 },
            new NivelesDeAccesos { Id = 1, Nombre = "Administrador" },
            new ClaseTipos { Id = 1, Nombre = "Seguridad", EstadoId = 1 },
            new Tipos { Id = 1, Nombre = "Local", EstadoId = 1, ClaseTipoId = 1 },
            new Roles { Id = "ADMIN", Nombre = "Administrador", NivelDeAccesoId = 1, CambioDeContrasenia = 12, TipoId = 1 },
            CrearEmpresa(1, "Empresa 1"),
            CrearEmpresa(2, "Empresa 2"),
            new Proveedores { Id = 1, EmpresaId = 1, CodigoProveedor = "P1", NombreProveedor = "Proveedor 1", DiasCredito = 0, EstadoId = 1 },
            new Proveedores { Id = 2, EmpresaId = 2, CodigoProveedor = "P2", NombreProveedor = "Proveedor 2", DiasCredito = 0, EstadoId = 1 },
            CrearSucursal(1, 1, 1, "Central"),
            CrearSucursal(2, 1, 1, "Norte"),
            CrearSucursal(3, 2, 2, "Otra empresa"),
            CrearSucursal(4, 1, 1, "Sin acceso"),
            CrearUsuario(1, "admin"),
            CrearUsuario(2, "otro"),
            new UsuariosSucursales { Id = 1, UsuarioId = 1, SucursalId = 1, EsPrincipal = true, EstadoId = 1 },
            new UsuariosSucursales { Id = 2, UsuarioId = 1, SucursalId = 2, EsPrincipal = false, EstadoId = 1 },
            new UsuariosSucursales { Id = 3, UsuarioId = 2, SucursalId = 4, EsPrincipal = true, EstadoId = 1 },
            new ClasesCatalogos { Id = 1, Codigo = "TIPO_SEGUIMIENTO_PRODUCTO", Nombre = "Seguimiento", EstadoId = 1, FechaCreacion = DateTime.UtcNow, CreadoPorUsuarioId = 1 },
            new ClasesCatalogos { Id = 2, Codigo = "METODO_VALUACION_INVENTARIO", Nombre = "Valuación", EstadoId = 1, FechaCreacion = DateTime.UtcNow, CreadoPorUsuarioId = 1 },
            new ClasesCatalogos { Id = 3, Codigo = "TIPO_UBICACION", Nombre = "Ubicación", EstadoId = 1, FechaCreacion = DateTime.UtcNow, CreadoPorUsuarioId = 1 },
            new Catalogos { Id = 1, ClaseCatalogoId = 1, EmpresaId = 1, Codigo = "NINGUNO", Nombre = "Sin seguimiento", EstadoId = 1, FechaCreacion = DateTime.UtcNow, CreadoPorUsuarioId = 1 },
            new Catalogos { Id = 2, ClaseCatalogoId = 2, EmpresaId = 1, Codigo = "PROMEDIO", Nombre = "Promedio", EstadoId = 1, FechaCreacion = DateTime.UtcNow, CreadoPorUsuarioId = 1 },
            new Catalogos { Id = 3, ClaseCatalogoId = 3, EmpresaId = 1, Codigo = "GENERAL", Nombre = "General", EstadoId = 1, FechaCreacion = DateTime.UtcNow, CreadoPorUsuarioId = 1 },
            new Catalogos { Id = 4, ClaseCatalogoId = 1, EmpresaId = 2, Codigo = "NINGUNO", Nombre = "Sin seguimiento", EstadoId = 1, FechaCreacion = DateTime.UtcNow, CreadoPorUsuarioId = 2 },
            CrearCategoria(1, 1, "HELADOS", "Helados"),
            CrearCategoria(2, 2, "OTRA", "Otra"),
            CrearMarca(1, 1, "CASA", "De la casa"),
            CrearMarca(2, 2, "OTRA", "Otra"),
            CrearTipoProducto(1, 1, "INVENTARIO", "Inventariable"),
            CrearTipoProducto(2, 2, "INVENTARIO", "Inventariable"),
            CrearUnidad(1, 1, "ML", "Mililitro", "ml", true, 2),
            CrearUnidad(2, 1, "L", "Litro", "L", true, 3),
            CrearUnidad(3, 1, "VASO", "Vaso", "vaso", false, 0),
            CrearUnidad(4, 2, "U", "Unidad ajena", "u", false, 0),
            CrearPresentacion(1, 1, "LITRO", "Litro"),
            CrearPresentacion(2, 1, "VASO", "Vaso"),
            CrearPresentacion(3, 1, "BASE", "Base"),
            CrearProductoEntidad(1, 1, "HEL-CHO", "Helado Chocolate"),
            CrearProductoEntidad(2, 1, "HEL-VAI", "Helado Vainilla"),
            CrearProductoEntidad(3, 2, "AJENO", "Producto ajeno"),
            CrearProductoPresentacion(1, 1, 1, 2, 1000),
            CrearProductoPresentacion(2, 1, 2, 3, 250),
            CrearBodega(1, 1, 1, "CENTRAL"),
            CrearBodega(2, 1, 2, "NORTE"),
            CrearBodega(3, 1, 4, "SIN-ACCESO"),
            CrearUbicacion(1, 1, 1),
            CrearUbicacion(2, 1, 2),
            CrearUbicacion(3, 1, 3),
            new FlujosEstados { Id = 1, Codigo = "MOVIMIENTOS_INVENTARIO", Nombre = "Movimientos", EstadoId = 1, FechaCreacion = DateTime.UtcNow, CreadoPorUsuarioId = 1 },
            new EstadosFlujo { Id = 1, FlujoEstadoId = 1, Codigo = "APLICADO", Nombre = "Aplicado", Orden = 1, EsInicial = true, EsFinal = true, EsSistema = true, EstadoId = 1, FechaCreacion = DateTime.UtcNow, CreadoPorUsuarioId = 1, VersionFila = [1] },
            CrearConcepto(1, "AJUSTE-ENTRADA", "ENTRADA", true),
            CrearConcepto(2, "AJUSTE-SALIDA", "SALIDA", false),
            new ParametrosStockProductos { Id = 1, EmpresaId = 1, ProductoId = 1, BodegaId = 1, UbicacionPredeterminadaId = 1, StockMinimo = 6000, PuntoReorden = 6000, EstadoId = 1, FechaCreacion = DateTime.UtcNow, CreadoPorUsuarioId = 1, VersionFila = [1] },
            new Existencias { Id = 1, EmpresaId = 1, ProductoId = 1, BodegaId = 1, UbicacionId = 1, Cantidad = 5000, CantidadReservada = 0, CostoPromedio = 0.01m, VersionFila = [1] },
            new Existencias { Id = 2, EmpresaId = 1, ProductoId = 1, BodegaId = 2, UbicacionId = 2, Cantidad = 900, CantidadReservada = 0, CostoPromedio = 0.01m, VersionFila = [1] });
        await Contexto.SaveChangesAsync();
        Contexto.ChangeTracker.Clear();
    }

    private static Empresas CrearEmpresa(int id, string nombre) => new() { Id = id, RazonSocial = nombre, NombreComercial = nombre, Eslogan = "E", Direccion = "D", Nit = $"NIT-{id}", Telefono = "1", Email = $"e{id}@p.local", Imagen = "i", Icono = "i", ImagenReportes = "i", EstadoId = 1, RoleId = "ADMIN" };
    private static Sucursales CrearSucursal(long id, int empresaId, int proveedorId, string nombre) => new() { Id = id, Nombre = nombre, Direccion = "D", DireccionCorta = "D", Telefono = "1", Email = $"s{id}@p.local", EmpresaId = empresaId, EsCentral = id == 1, EstadoId = 1, ProveedorId = proveedorId, Codigo = $"S{id}", CodigoNombre = $"S{id} {nombre}" };
    private static Usuarios CrearUsuario(int id, string nombre) => new() { Id = id, Email = $"{nombre}@p.local", UserName = nombre, IdentityId = nombre, PasswordHash = "hash", RoleId = "ADMIN", HorarioInicio = new(), HorarioFinal = new(), Imagen = "i", Soundex = "S", EstadoId = 1, TipoInicioSesionId = 1 };
    private static Categorias CrearCategoria(int id, int empresaId, string codigo, string nombre) => new() { Id = id, EmpresaId = empresaId, Codigo = codigo, Nombre = nombre, Nivel = 1, EstadoId = 1, FechaCreacion = DateTime.UtcNow, CreadoPorUsuarioId = empresaId, VersionFila = [1] };
    private static Marcas CrearMarca(int id, int empresaId, string codigo, string nombre) => new() { Id = id, EmpresaId = empresaId, Codigo = codigo, Nombre = nombre, EstadoId = 1, FechaCreacion = DateTime.UtcNow, CreadoPorUsuarioId = empresaId, VersionFila = [1] };
    private static TiposProductos CrearTipoProducto(int id, int empresaId, string codigo, string nombre) => new() { Id = id, EmpresaId = empresaId, Codigo = codigo, Nombre = nombre, ManejaExistencia = true, EstadoId = 1, FechaCreacion = DateTime.UtcNow, CreadoPorUsuarioId = empresaId, VersionFila = [1] };
    private static UnidadesMedida CrearUnidad(int id, int empresaId, string codigo, string nombre, string abreviatura, bool decimales, byte cantidadDecimales) => new() { Id = id, EmpresaId = empresaId, Codigo = codigo, Nombre = nombre, Abreviatura = abreviatura, PermiteDecimales = decimales, CantidadDecimales = cantidadDecimales, EstadoId = 1, FechaCreacion = DateTime.UtcNow, CreadoPorUsuarioId = empresaId, VersionFila = [1] };
    private static Presentaciones CrearPresentacion(int id, int empresaId, string codigo, string nombre) => new() { Id = id, EmpresaId = empresaId, Codigo = codigo, Nombre = nombre, EstadoId = 1, FechaCreacion = DateTime.UtcNow, CreadoPorUsuarioId = empresaId, VersionFila = [1] };
    private static Productos CrearProductoEntidad(long id, int empresaId, string sku, string nombre) => new() { Id = id, EmpresaId = empresaId, CategoriaId = empresaId, MarcaId = empresaId, TipoProductoId = empresaId, UnidadBaseId = empresaId == 1 ? 1 : 4, TipoSeguimientoId = empresaId == 1 ? 1 : 4, MetodoValuacionId = empresaId == 1 ? 2 : 4, SKU = sku, Nombre = nombre, EsComprable = true, EsVendible = true, PermiteDescuento = true, PrecioVentaReferencial = 10, EstadoId = 1, FechaCreacion = DateTime.UtcNow, CreadoPorUsuarioId = empresaId, VersionFila = [1] };
    private static ProductoPresentaciones CrearProductoPresentacion(long id, long productoId, int presentacionId, int unidadId, decimal factor) => new() { Id = id, EmpresaId = 1, ProductoId = productoId, PresentacionId = presentacionId, UnidadMedidaId = unidadId, FactorConversion = factor, PermiteCompra = true, PermiteVenta = true, EstadoId = 1, FechaCreacion = DateTime.UtcNow, CreadoPorUsuarioId = 1, VersionFila = [1] };
    private static Bodegas CrearBodega(long id, int empresaId, long sucursalId, string codigo) => new() { Id = id, EmpresaId = empresaId, SucursalId = sucursalId, Codigo = codigo, Nombre = codigo, EsPrincipal = true, EstadoId = 1, FechaCreacion = DateTime.UtcNow, CreadoPorUsuarioId = 1, VersionFila = [1] };
    private static Ubicaciones CrearUbicacion(long id, int empresaId, long bodegaId) => new() { Id = id, EmpresaId = empresaId, BodegaId = bodegaId, Codigo = "GENERAL", Nombre = "General", TipoUbicacionId = 3, EsRecepcion = true, EsDespacho = true, EstadoId = 1, FechaCreacion = DateTime.UtcNow, CreadoPorUsuarioId = 1, VersionFila = [1] };
    private static ConceptosMovimientos CrearConcepto(int id, string codigo, string tipo, bool afectaCosto) => new() { Id = id, EmpresaId = 1, Codigo = codigo, Nombre = codigo, TipoOperacion = tipo, AfectaCosto = afectaCosto, PermiteCostoCero = true, EstadoId = 1, FechaCreacion = DateTime.UtcNow, CreadoPorUsuarioId = 1, VersionFila = [1] };
}

internal sealed class AuditoriaFase4Falsa : IAuditoriaFase4
{
    public int Cantidad { get; private set; }
    public void Registrar(ContextoConfiguracion contexto, string entidad, string? entidadId, string accion, string comentario) => Cantidad++;
}
