using System.ComponentModel.DataAnnotations;
using Heladeria.Aplicacion.Configuracion;

namespace Heladeria.Aplicacion.Produccion;

public sealed class FiltroProductosProduccionSolicitud : FiltroPaginadoBase;

public sealed class FiltroRecetasSolicitud : FiltroPaginadoBase
{
    [StringLength(30)]
    public string? Codigo { get; init; }

    [Range(typeof(long), "1", "9223372036854775807")]
    public long? ProductoTerminadoId { get; init; }

    public bool? SoloVigentes { get; init; }

    [Range(1, int.MaxValue)]
    public int? Version { get; init; }

    [Range(1, int.MaxValue)]
    public int? EstadoId { get; init; }
}

public abstract class GuardarRecetaSolicitudBase
{
    [Range(typeof(long), "1", "9223372036854775807")]
    public long ProductoTerminadoId { get; init; }

    [Required, StringLength(30, MinimumLength = 1)]
    public string Codigo { get; init; } = string.Empty;

    [Required, StringLength(150, MinimumLength = 1)]
    public string Nombre { get; init; } = string.Empty;

    [Range(typeof(decimal), "0.000001", "9999999999999.999999")]
    public decimal Rendimiento { get; init; }

    [Range(typeof(long), "1", "9223372036854775807")]
    public long? ProductoPresentacionRendimientoId { get; init; }

    public DateOnly VigenteDesde { get; init; }

    [Range(1, int.MaxValue)]
    public int EstadoId { get; init; }

    [Required, MinLength(1), MaxLength(100)]
    public RecetaDetalleSolicitud[] Detalles { get; init; } = [];
}

public sealed class CrearRecetaSolicitud : GuardarRecetaSolicitudBase;

public sealed class ActualizarRecetaSolicitud : GuardarRecetaSolicitudBase
{
    [Required]
    public string VersionFila { get; init; } = string.Empty;
}

public sealed class RecetaDetalleSolicitud
{
    [Range(typeof(long), "1", "9223372036854775807")]
    public long ProductoInsumoId { get; init; }

    [Range(typeof(long), "1", "9223372036854775807")]
    public long? ProductoPresentacionId { get; init; }

    [Range(typeof(decimal), "0.000001", "9999999999999.999999")]
    public decimal Cantidad { get; init; }

    [StringLength(300)]
    public string? Observacion { get; init; }
}

public sealed class FiltroProduccionSolicitud : FiltroPaginadoBase
{
    [Range(typeof(long), "1", "9223372036854775807")]
    public long? ProductoId { get; init; }

    [Range(typeof(long), "1", "9223372036854775807")]
    public long? RecetaId { get; init; }

    [Range(1, int.MaxValue)]
    public int? EstadoId { get; init; }

    public DateTime? FechaDesde { get; init; }
    public DateTime? FechaHasta { get; init; }
}

public abstract class GuardarOrdenProduccionSolicitudBase
{
    [Range(typeof(long), "1", "9223372036854775807")]
    public long SucursalId { get; init; }

    [Range(typeof(long), "1", "9223372036854775807")]
    public long BodegaProduccionId { get; init; }

    [Range(typeof(long), "1", "9223372036854775807")]
    public long RecetaId { get; init; }

    [Range(typeof(long), "1", "9223372036854775807")]
    public long? ProductoPresentacionId { get; init; }

    [Range(typeof(decimal), "0.000001", "9999999999999.999999")]
    public decimal CantidadPlanificada { get; init; }

    public DateTime FechaInicio { get; init; }
}

public sealed class CrearOrdenProduccionSolicitud : GuardarOrdenProduccionSolicitudBase;

public sealed class ActualizarOrdenProduccionSolicitud : GuardarOrdenProduccionSolicitudBase
{
    [Required]
    public string VersionFila { get; init; } = string.Empty;
}

public sealed class FinalizarProduccionSolicitud
{
    public Guid ClaveOperacion { get; init; }

    [Range(typeof(long), "1", "9223372036854775807")]
    public long UbicacionId { get; init; }

    [Range(typeof(decimal), "0.000001", "9999999999999.999999")]
    public decimal CantidadProducida { get; init; }

    [Required]
    public string VersionFila { get; init; } = string.Empty;
}

public sealed class CambiarEstadoProduccionSolicitud
{
    [Required]
    public string VersionFila { get; init; } = string.Empty;
}

public sealed record PresentacionProduccionDto(
    long ProductoPresentacionId, int PresentacionId, string PresentacionCodigo,
    string PresentacionNombre, int UnidadMedidaId, string UnidadCodigo,
    string UnidadNombre, string UnidadAbreviatura, decimal FactorConversion,
    bool EsPresentacionBase);

public sealed record ProductoPresentacionesProduccionDto(
    long ProductoId, string Producto, int UnidadBaseId, string UnidadBaseCodigo,
    string UnidadBaseNombre, string UnidadBaseAbreviatura,
    IReadOnlyList<PresentacionProduccionDto> Presentaciones);

public sealed record RecetaResumenDto(
    long Id, int EmpresaId, long ProductoTerminadoId, string ProductoTerminado,
    string Codigo, string Nombre, decimal Rendimiento,
    long? ProductoPresentacionRendimientoId, int UnidadRendimientoId,
    string UnidadRendimiento, string AbreviaturaRendimiento,
    decimal FactorConversion, decimal RendimientoBase, int Version,
    DateOnly VigenteDesde, DateOnly? VigenteHasta, bool EsVigente,
    int EstadoId, string Estado, string VersionFila);

public sealed record RecetaDetalleDto(
    long Id, long ProductoInsumoId, string ProductoInsumo,
    long? ProductoPresentacionId, string? Presentacion,
    int UnidadMedidaId, string UnidadMedida, string AbreviaturaUnidad,
    decimal Cantidad, decimal FactorConversion, decimal CantidadBase,
    int UnidadBaseId, string UnidadBase, string AbreviaturaUnidadBase,
    string? Observacion);

public sealed record RecetaConsultaDto(
    RecetaResumenDto Encabezado, IReadOnlyList<RecetaDetalleDto> Detalles)
{
    public bool FueUtilizada { get; init; }
    public string UnidadBase { get; init; } = string.Empty;
    public string AbreviaturaUnidadBase { get; init; } = string.Empty;
    public string? PresentacionRendimiento { get; init; }
}

public sealed record OpcionRecetaDto(long Id, string Codigo, string Nombre);
public sealed record ReferenciasRecetasDto(
    IReadOnlyList<OpcionRecetaDto> Productos, IReadOnlyList<OpcionRecetaDto> Estados);

public sealed record OrdenProduccionResumenDto(
    long Id, int EmpresaId, long SucursalId, string Sucursal,
    long BodegaProduccionId, string Bodega, long ProductoId, string Producto,
    long RecetaId, string Receta, int RecetaVersion, string NumeroOrden,
    decimal CantidadPlanificada, decimal CantidadPlanificadaBase,
    decimal? CantidadProducida, long? ProductoPresentacionId,
    string UnidadPlanificada, string AbreviaturaUnidadPlanificada,
    decimal FactorConversion, DateTime FechaInicio, DateTime? FechaFin,
    int EstadoId, string Estado, int ResponsableUsuarioId, string Responsable,
    string VersionFila)
{
    public string UnidadBase { get; init; } = string.Empty;
    public string AbreviaturaUnidadBase { get; init; } = string.Empty;
    public string? Presentacion { get; init; }
}

public sealed record OrdenProduccionDetalleDto(
    long Id, long ProductoInsumoId, string ProductoInsumo,
    long? ProductoPresentacionId, string? Presentacion,
    int UnidadMedidaId, string UnidadMedida, string AbreviaturaUnidad,
    decimal CantidadPlanificada, decimal FactorConversion,
    decimal CantidadPlanificadaBase, decimal? CostoPlanificado)
{
    public string AbreviaturaUnidadBase { get; init; } = string.Empty;
}

public sealed record ConsumoProduccionDto(
    long Id, long ProductoId, string Producto, int UnidadMedidaId,
    string UnidadMedida, decimal CantidadConsumida, decimal CostoUnitario,
    decimal CostoTotal, DateTime FechaConsumo);

public sealed record ProduccionTerminadaDto(
    long Id, long ProductoId, string Producto, decimal CantidadProducida,
    decimal CostoMateriaPrima, decimal CostoManoObra, decimal CostoIndirecto,
    decimal CostoTotal, decimal CostoUnitarioBase, DateTime FechaFinalizacion);

public sealed record OrdenProduccionConsultaDto(
    OrdenProduccionResumenDto Encabezado,
    IReadOnlyList<OrdenProduccionDetalleDto> Detalles,
    IReadOnlyList<ConsumoProduccionDto> Consumos,
    ProduccionTerminadaDto? ProduccionTerminada,
    IReadOnlyList<long> MovimientoIds);

public sealed record EstadoProduccionDto(
    int Id, string Codigo, string Nombre, bool PermiteEdicion, bool EsFinal);

public sealed record RecetaVigenteDto(
    long Id, long ProductoId, string Producto, string Codigo, string Nombre,
    int Version, decimal RendimientoBase, string UnidadBase);

public sealed record AlmacenProduccionDto(
    long BodegaId, string Bodega, long UbicacionId, string Ubicacion);

public sealed record ReferenciasProduccionDto(
    IReadOnlyList<EstadoProduccionDto> Estados,
    IReadOnlyList<RecetaVigenteDto> RecetasVigentes,
    IReadOnlyList<AlmacenProduccionDto> Almacenes);

public interface IServicioRecetas
{
    Task<ReferenciasRecetasDto> ObtenerReferenciasAsync(ContextoConfiguracion contexto, CancellationToken token);
    Task<RespuestaPaginada<OpcionRecetaDto>> ListarProductosAsync(FiltroProductosProduccionSolicitud filtro, ContextoConfiguracion contexto, CancellationToken token);
    Task<RespuestaPaginada<RecetaResumenDto>> ListarAsync(
        FiltroRecetasSolicitud filtro, ContextoConfiguracion contexto, CancellationToken token);
    Task<RecetaConsultaDto> ObtenerAsync(
        long id, ContextoConfiguracion contexto, CancellationToken token);
    Task<ProductoPresentacionesProduccionDto> ListarPresentacionesAsync(
        long productoId, ContextoConfiguracion contexto, CancellationToken token);
    Task<RecetaConsultaDto> CrearAsync(
        CrearRecetaSolicitud solicitud, ContextoConfiguracion contexto, CancellationToken token);
    Task<RecetaConsultaDto> ActualizarAsync(
        long id, ActualizarRecetaSolicitud solicitud, ContextoConfiguracion contexto, CancellationToken token);
}

public interface IServicioProduccion
{
    Task<ReferenciasProduccionDto> ObtenerReferenciasAsync(
        ContextoConfiguracion contexto, CancellationToken token);
    Task<RespuestaPaginada<OrdenProduccionResumenDto>> ListarAsync(
        FiltroProduccionSolicitud filtro, ContextoConfiguracion contexto, CancellationToken token);
    Task<OrdenProduccionConsultaDto> ObtenerAsync(
        long id, ContextoConfiguracion contexto, CancellationToken token);
    Task<OrdenProduccionConsultaDto> CrearAsync(
        CrearOrdenProduccionSolicitud solicitud, ContextoConfiguracion contexto, CancellationToken token);
    Task<OrdenProduccionConsultaDto> ActualizarAsync(
        long id, ActualizarOrdenProduccionSolicitud solicitud, ContextoConfiguracion contexto, CancellationToken token);
    Task<OrdenProduccionConsultaDto> ConfirmarAsync(
        long id, CambiarEstadoProduccionSolicitud solicitud, ContextoConfiguracion contexto, CancellationToken token);
    Task<OrdenProduccionConsultaDto> FinalizarAsync(
        long id, FinalizarProduccionSolicitud solicitud, ContextoConfiguracion contexto, CancellationToken token);
    Task<OrdenProduccionConsultaDto> AnularAsync(
        long id, CambiarEstadoProduccionSolicitud solicitud, ContextoConfiguracion contexto, CancellationToken token);
}

public abstract class ExcepcionProduccion(string mensaje, string codigo) : Exception(mensaje)
{
    public string Codigo { get; } = codigo;
}

public sealed class ValidacionProduccionException(string mensaje)
    : ExcepcionProduccion(mensaje, "VALIDACION_PRODUCCION");

public sealed class ConflictoProduccionException(string mensaje)
    : ExcepcionProduccion(mensaje, "CONFLICTO_PRODUCCION");

public sealed class ExistenciaProduccionInsuficienteException(string mensaje)
    : ExcepcionProduccion(mensaje, "EXISTENCIA_INSUFICIENTE");

public sealed class RecursoProduccionNoEncontradoException(string mensaje)
    : ExcepcionProduccion(mensaje, "RECURSO_NO_ENCONTRADO");
