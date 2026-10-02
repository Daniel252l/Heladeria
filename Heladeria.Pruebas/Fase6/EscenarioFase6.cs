using Heladeria.Aplicacion.Configuracion;
using Heladeria.Aplicacion.Produccion;
using Heladeria.Infraestructura.Persistencia;
using Heladeria.Infraestructura.Persistencia.Entidades;
using Heladeria.Infraestructura.Produccion;
using Heladeria.Infraestructura.Seguridad.Bootstrap;
using Heladeria.Infraestructura.Inventario;
using Heladeria.Pruebas.Fase4;
using Heladeria.Pruebas.Fase5;
using Microsoft.EntityFrameworkCore;

namespace Heladeria.Pruebas.Fase6;

internal sealed class EscenarioFase6 : IAsyncDisposable
{
    private readonly EscenarioFase5 _fase5;

    private EscenarioFase6(EscenarioFase5 fase5)
    {
        _fase5 = fase5;
        Contexto = fase5.Contexto;
        Recetas = new ServicioRecetas(Contexto, EscenarioFase5.Opciones,
            TimeProvider.System, fase5.Coordinador, fase5.Fase4.Inventario);
        Produccion = new ServicioProduccion(Contexto, EscenarioFase5.Opciones,
            TimeProvider.System, fase5.Coordinador, fase5.Fase4.Inventario);
        Preparador = new PreparadorFase6(Contexto, EscenarioFase5.Opciones,
            TimeProvider.System, fase5.Coordinador, fase5.Fase4.Inventario);
    }

    internal static ContextoConfiguracion Seguridad => EscenarioFase5.Seguridad;
    internal HeladeriaDbContext Contexto { get; }
    internal ServicioRecetas Recetas { get; }
    internal ServicioProduccion Produccion { get; }
    internal PreparadorFase6 Preparador { get; }

    internal static async Task<EscenarioFase6> CrearAsync()
    {
        return await CrearAsync($"fase6-{Guid.NewGuid():N}", true,
            new CoordinadorAjustesInventario());
    }

    internal static async Task<EscenarioFase6> CrearAsync(
        string nombre, bool semillar, CoordinadorAjustesInventario coordinador)
    {
        var fase4 = await EscenarioFase4.CrearAsync(
            nombre, EscenarioFase4.RaizCompartida, semillar, coordinador);
        var fase5 = new EscenarioFase5(fase4, coordinador);
        var escenario = new EscenarioFase6(fase5);
        if (!semillar)
            return escenario;
        await fase5.SemillarAsync();
        escenario.Contexto.Existencias.Add(new Existencias
        {
            Id = 10,
            EmpresaId = 1,
            ProductoId = 2,
            BodegaId = 1,
            UbicacionId = 1,
            Cantidad = 100m,
            CantidadReservada = 0m,
            CostoPromedio = 0.02m,
            VersionFila = [1]
        });
        await escenario.Contexto.SaveChangesAsync();
        escenario.Contexto.ChangeTracker.Clear();
        await escenario.Preparador.EjecutarAsync(CancellationToken.None);
        return escenario;
    }

    internal static CrearRecetaSolicitud SolicitudReceta(
        long terminado = 2, long ingrediente = 1, decimal cantidadIngrediente = 0.5m,
        long? presentacionIngrediente = 1, string codigo = "REC-VAI",
        DateOnly? vigenteDesde = null) => new()
    {
        ProductoTerminadoId = terminado,
        Codigo = codigo,
        Nombre = "Receta de prueba",
        Rendimiento = 1000m,
        ProductoPresentacionRendimientoId = null,
        VigenteDesde = vigenteDesde ?? DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-1)),
        EstadoId = 1,
        Detalles =
        [
            new RecetaDetalleSolicitud
            {
                ProductoInsumoId = ingrediente,
                ProductoPresentacionId = presentacionIngrediente,
                Cantidad = cantidadIngrediente,
                Observacion = "Snapshot multimedida"
            }
        ]
    };

    internal static CrearOrdenProduccionSolicitud SolicitudOrden(
        long recetaId, decimal cantidad = 1000m, long bodega = 1,
        long sucursal = 1, long? presentacion = null) => new()
    {
        SucursalId = sucursal,
        BodegaProduccionId = bodega,
        RecetaId = recetaId,
        ProductoPresentacionId = presentacion,
        CantidadPlanificada = cantidad,
        FechaInicio = DateTime.UtcNow
    };

    internal async Task<RecetaConsultaDto> CrearRecetaAsync(CrearRecetaSolicitud? solicitud = null)
    {
        var resultado = await Recetas.CrearAsync(solicitud ?? SolicitudReceta(), Seguridad, default);
        await FijarVersionRecetaAsync(resultado.Encabezado.Id, 11);
        return await Recetas.ObtenerAsync(resultado.Encabezado.Id, Seguridad, default);
    }

    internal async Task<OrdenProduccionConsultaDto> CrearOrdenAsync(long recetaId)
    {
        var resultado = await Produccion.CrearAsync(SolicitudOrden(recetaId), Seguridad, default);
        await FijarVersionOrdenAsync(resultado.Encabezado.Id, 21);
        return await Produccion.ObtenerAsync(resultado.Encabezado.Id, Seguridad, default);
    }

    internal async Task<OrdenProduccionConsultaDto> ConfirmarAsync(long ordenId)
    {
        var orden = await Produccion.ObtenerAsync(ordenId, Seguridad, default);
        var resultado = await Produccion.ConfirmarAsync(ordenId,
            new CambiarEstadoProduccionSolicitud { VersionFila = orden.Encabezado.VersionFila },
            Seguridad, default);
        await FijarVersionOrdenAsync(ordenId, 22);
        return await Produccion.ObtenerAsync(ordenId, Seguridad, default);
    }

    internal async Task FijarVersionRecetaAsync(long id, byte valor)
    {
        var entidad = await Contexto.Recetas.SingleAsync(item => item.Id == id);
        entidad.VersionFila = [valor];
        await Contexto.SaveChangesAsync();
        Contexto.ChangeTracker.Clear();
    }

    internal async Task FijarVersionOrdenAsync(long id, byte valor)
    {
        var entidad = await Contexto.OrdenesProduccion.SingleAsync(item => item.Id == id);
        entidad.VersionFila = [valor];
        await Contexto.SaveChangesAsync();
        Contexto.ChangeTracker.Clear();
    }

    public ValueTask DisposeAsync() => _fase5.DisposeAsync();
}
