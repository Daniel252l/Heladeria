using System.ComponentModel.DataAnnotations;
using Heladeria.Aplicacion.Configuracion;

namespace Heladeria.Aplicacion.Productos;

public sealed class FiltroCatalogoProductoSolicitud : FiltroPaginadoBase
{
    [Range(1, int.MaxValue)]
    public int? EstadoId { get; init; }
}

public sealed class FiltroProductosSolicitud : FiltroPaginadoBase
{
    public bool SoloComponentesVenta { get; init; }
    [Range(1, int.MaxValue)]
    public int? CategoriaId { get; init; }

    [Range(1, int.MaxValue)]
    public int? MarcaId { get; init; }

    [Range(1, int.MaxValue)]
    public int? TipoProductoId { get; init; }

    [Range(1, int.MaxValue)]
    public int? EstadoId { get; init; }
}

public abstract class GuardarCategoriaSolicitudBase
{
    [Range(1, int.MaxValue)]
    public int? CategoriaPadreId { get; init; }

    [Required, StringLength(25, MinimumLength = 1)]
    public string Codigo { get; init; } = string.Empty;

    [Required, StringLength(150, MinimumLength = 1)]
    public string Nombre { get; init; } = string.Empty;

    [StringLength(500)]
    public string? Descripcion { get; init; }

    [Range(1, int.MaxValue)]
    public int EstadoId { get; init; }
}

public sealed class CrearCategoriaSolicitud : GuardarCategoriaSolicitudBase;

public sealed class ActualizarCategoriaSolicitud : GuardarCategoriaSolicitudBase
{
    [Required]
    public string VersionFila { get; init; } = string.Empty;
}

public abstract class GuardarMarcaSolicitudBase
{
    [Required, StringLength(25, MinimumLength = 1)]
    public string Codigo { get; init; } = string.Empty;

    [Required, StringLength(150, MinimumLength = 1)]
    public string Nombre { get; init; } = string.Empty;

    [StringLength(500)]
    public string? Descripcion { get; init; }

    [Range(1, int.MaxValue)]
    public int EstadoId { get; init; }
}

public sealed class CrearMarcaSolicitud : GuardarMarcaSolicitudBase;

public sealed class ActualizarMarcaSolicitud : GuardarMarcaSolicitudBase
{
    [Required]
    public string VersionFila { get; init; } = string.Empty;
}

public abstract class GuardarTipoProductoSolicitudBase
{
    [Required, StringLength(25, MinimumLength = 1)]
    public string Codigo { get; init; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 1)]
    public string Nombre { get; init; } = string.Empty;

    public bool ManejaExistencia { get; init; }

    [Range(1, int.MaxValue)]
    public int EstadoId { get; init; }
}

public sealed class CrearTipoProductoSolicitud : GuardarTipoProductoSolicitudBase;

public sealed class ActualizarTipoProductoSolicitud : GuardarTipoProductoSolicitudBase
{
    [Required]
    public string VersionFila { get; init; } = string.Empty;
}

public abstract class GuardarUnidadMedidaSolicitudBase
{
    [Required, StringLength(20, MinimumLength = 1)]
    public string Codigo { get; init; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 1)]
    public string Nombre { get; init; } = string.Empty;

    [Required, StringLength(20, MinimumLength = 1)]
    public string Abreviatura { get; init; } = string.Empty;

    public bool PermiteDecimales { get; init; }

    [Range(0, 6)]
    public byte CantidadDecimales { get; init; }

    [Range(1, int.MaxValue)]
    public int EstadoId { get; init; }
}

public sealed class CrearUnidadMedidaSolicitud : GuardarUnidadMedidaSolicitudBase;

public sealed class ActualizarUnidadMedidaSolicitud : GuardarUnidadMedidaSolicitudBase
{
    [Required]
    public string VersionFila { get; init; } = string.Empty;
}

public abstract class GuardarPresentacionSolicitudBase
{
    [Required, StringLength(25, MinimumLength = 1)]
    public string Codigo { get; init; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 1)]
    public string Nombre { get; init; } = string.Empty;

    [StringLength(300)]
    public string? Descripcion { get; init; }

    [Range(1, int.MaxValue)]
    public int EstadoId { get; init; }
}

public sealed class CrearPresentacionSolicitud : GuardarPresentacionSolicitudBase;

public sealed class ActualizarPresentacionSolicitud : GuardarPresentacionSolicitudBase
{
    [Required]
    public string VersionFila { get; init; } = string.Empty;
}

public class CrearConversionUnidadSolicitud
{
    [Range(1, int.MaxValue)]
    public int UnidadOrigenId { get; init; }

    [Range(1, int.MaxValue)]
    public int UnidadDestinoId { get; init; }

    [Range(typeof(decimal), "0.00000001", "99999999999.99999999")]
    public decimal Factor { get; init; }

    [Range(1, int.MaxValue)]
    public int EstadoId { get; init; }
}

public sealed class ActualizarConversionUnidadSolicitud : CrearConversionUnidadSolicitud;

public abstract class GuardarProductoSolicitudBase
{
    [Range(1, int.MaxValue)]
    public int CategoriaId { get; init; }

    [Range(1, int.MaxValue)]
    public int? MarcaId { get; init; }

    [Range(1, int.MaxValue)]
    public int TipoProductoId { get; init; }

    [Range(1, int.MaxValue)]
    public int UnidadBaseId { get; init; }

    [Range(typeof(long), "1", "9223372036854775807")]
    public long TipoSeguimientoId { get; init; }

    [Range(typeof(long), "1", "9223372036854775807")]
    public long MetodoValuacionId { get; init; }

    public bool ControlaVencimiento { get; init; }

    [Required, StringLength(50, MinimumLength = 1)]
    public string SKU { get; init; } = string.Empty;

    [Required, StringLength(200, MinimumLength = 1)]
    public string Nombre { get; init; } = string.Empty;

    [StringLength(1000)]
    public string? Descripcion { get; init; }

    [StringLength(100)]
    public string? Modelo { get; init; }

    [StringLength(300)]
    public string? Imagen { get; init; }

    [Range(typeof(decimal), "0", "9999999999999.999999")]
    public decimal? Peso { get; init; }

    public bool EsComprable { get; init; }
    public bool EsVendible { get; init; }
    public bool PermiteDescuento { get; init; }
    public bool EsCombo { get; init; }

    // Ausente conserva la composición al editar mediante clientes anteriores; [] la elimina.
    [MaxLength(100)]
    public ComponenteVentaSolicitud[]? Componentes { get; init; }

    [Range(typeof(decimal), "0", "99999999.99")]
    public decimal? PrecioVentaReferencial { get; init; }

    [Range(1, int.MaxValue)]
    public int EstadoId { get; init; }
}

public sealed class CrearProductoSolicitud : GuardarProductoSolicitudBase;

public sealed class ActualizarProductoSolicitud : GuardarProductoSolicitudBase
{
    [Required]
    public string VersionFila { get; init; } = string.Empty;
}

public abstract class GuardarProductoPresentacionSolicitudBase
{
    [Range(1, int.MaxValue)]
    public int PresentacionId { get; init; }

    [Range(1, int.MaxValue)]
    public int UnidadMedidaId { get; init; }

    [Range(typeof(decimal), "0.000001", "9999999999999.999999")]
    public decimal FactorConversion { get; init; }

    public bool EsPresentacionBase { get; init; }
    public bool PermiteCompra { get; init; }
    public bool PermiteVenta { get; init; }

    [Range(typeof(decimal), "0", "99999999.99")]
    public decimal? PrecioVentaEstandar { get; init; }

    [Range(1, int.MaxValue)]
    public int EstadoId { get; init; }
}

public sealed class CrearProductoPresentacionSolicitud : GuardarProductoPresentacionSolicitudBase;

public sealed class ActualizarProductoPresentacionSolicitud : GuardarProductoPresentacionSolicitudBase
{
    [Required]
    public string VersionFila { get; init; } = string.Empty;
}

public sealed record CategoriaConsultaDto(
    int Id, int EmpresaId, int? CategoriaPadreId, string Codigo, string Nombre,
    string? Descripcion, byte Nivel, int EstadoId, string Estado, string VersionFila);

public sealed record MarcaConsultaDto(
    int Id, int EmpresaId, string Codigo, string Nombre, string? Descripcion,
    int EstadoId, string Estado, string VersionFila);

public sealed record TipoProductoConsultaDto(
    int Id, int EmpresaId, string Codigo, string Nombre, bool ManejaExistencia,
    int EstadoId, string Estado, string VersionFila);

public sealed record UnidadMedidaConsultaDto(
    int Id, int EmpresaId, string Codigo, string Nombre, string Abreviatura,
    bool PermiteDecimales, byte CantidadDecimales, int EstadoId, string Estado,
    string VersionFila);

public sealed record PresentacionConsultaDto(
    int Id, int EmpresaId, string Codigo, string Nombre, string? Descripcion,
    int EstadoId, string Estado, string VersionFila);

public sealed record ConversionUnidadConsultaDto(
    long Id, int EmpresaId, int UnidadOrigenId, string UnidadOrigen,
    int UnidadDestinoId, string UnidadDestino, decimal Factor, int EstadoId,
    string Estado);

public sealed record ProductoConsultaDto(
    long Id, int EmpresaId, string SKU, string Nombre, string? Descripcion,
    int CategoriaId, string Categoria, int? MarcaId, string? Marca,
    int TipoProductoId, string TipoProducto, bool ManejaExistencia,
    int UnidadBaseId, string UnidadBase, string AbreviaturaUnidadBase,
    long TipoSeguimientoId, string TipoSeguimiento, long MetodoValuacionId,
    string MetodoValuacion, bool ControlaVencimiento, string? Modelo,
    string? Imagen, decimal? Peso, bool EsComprable, bool EsVendible,
    bool PermiteDescuento, bool EsCombo, decimal? PrecioVentaReferencial,
    int EstadoId, string Estado, string VersionFila)
{
    public IReadOnlyList<ComponenteVentaConsultaDto> Componentes { get; init; } = [];
}

public sealed class ComponenteVentaSolicitud
{
    [Range(typeof(long), "1", "9223372036854775807")]
    public long ProductoId { get; init; }
    [Range(typeof(long), "1", "9223372036854775807")]
    public long? ProductoPresentacionId { get; init; }
    [Range(typeof(decimal), "0.000001", "9999999999999.999999")]
    public decimal Cantidad { get; init; }
}

public sealed record ComponenteVentaConsultaDto(long ProductoId, string SKU, string Producto,
    long? ProductoPresentacionId, string? Presentacion, decimal Cantidad, decimal FactorConversion,
    decimal CantidadBase, string Unidad, string AbreviaturaUnidad, int UnidadBaseId,
    string UnidadBase, string AbreviaturaUnidadBase);

public sealed record ProductoPresentacionConsultaDto(
    long Id, int EmpresaId, long ProductoId, int PresentacionId,
    string Presentacion, int UnidadMedidaId, string UnidadMedida,
    decimal FactorConversion, bool EsPresentacionBase, bool PermiteCompra,
    bool PermiteVenta, decimal? PrecioVentaEstandar, int EstadoId,
    string Estado, string VersionFila)
{
    public string AbreviaturaUnidadMedida { get; init; } = string.Empty;
}

public sealed record OpcionCatalogoProductoDto(long Id, string Codigo, string Nombre);

public sealed record ReferenciasProductoDto(
    IReadOnlyList<OpcionCatalogoProductoDto> TiposSeguimiento,
    IReadOnlyList<OpcionCatalogoProductoDto> MetodosValuacion);

public interface IServicioCatalogosProductos
{
    Task<RespuestaPaginada<CategoriaConsultaDto>> ListarCategoriasAsync(FiltroCatalogoProductoSolicitud filtro, ContextoConfiguracion contexto, CancellationToken token);
    Task<CategoriaConsultaDto> ObtenerCategoriaAsync(int id, ContextoConfiguracion contexto, CancellationToken token);
    Task<CategoriaConsultaDto> CrearCategoriaAsync(CrearCategoriaSolicitud solicitud, ContextoConfiguracion contexto, CancellationToken token);
    Task<CategoriaConsultaDto> ActualizarCategoriaAsync(int id, ActualizarCategoriaSolicitud solicitud, ContextoConfiguracion contexto, CancellationToken token);
    Task<CategoriaConsultaDto> CambiarEstadoCategoriaAsync(int id, int estadoId, ContextoConfiguracion contexto, CancellationToken token);

    Task<RespuestaPaginada<MarcaConsultaDto>> ListarMarcasAsync(FiltroCatalogoProductoSolicitud filtro, ContextoConfiguracion contexto, CancellationToken token);
    Task<MarcaConsultaDto> ObtenerMarcaAsync(int id, ContextoConfiguracion contexto, CancellationToken token);
    Task<MarcaConsultaDto> CrearMarcaAsync(CrearMarcaSolicitud solicitud, ContextoConfiguracion contexto, CancellationToken token);
    Task<MarcaConsultaDto> ActualizarMarcaAsync(int id, ActualizarMarcaSolicitud solicitud, ContextoConfiguracion contexto, CancellationToken token);
    Task<MarcaConsultaDto> CambiarEstadoMarcaAsync(int id, int estadoId, ContextoConfiguracion contexto, CancellationToken token);

    Task<RespuestaPaginada<TipoProductoConsultaDto>> ListarTiposProductosAsync(FiltroCatalogoProductoSolicitud filtro, ContextoConfiguracion contexto, CancellationToken token);
    Task<TipoProductoConsultaDto> ObtenerTipoProductoAsync(int id, ContextoConfiguracion contexto, CancellationToken token);
    Task<TipoProductoConsultaDto> CrearTipoProductoAsync(CrearTipoProductoSolicitud solicitud, ContextoConfiguracion contexto, CancellationToken token);
    Task<TipoProductoConsultaDto> ActualizarTipoProductoAsync(int id, ActualizarTipoProductoSolicitud solicitud, ContextoConfiguracion contexto, CancellationToken token);
    Task<TipoProductoConsultaDto> CambiarEstadoTipoProductoAsync(int id, int estadoId, ContextoConfiguracion contexto, CancellationToken token);

    Task<RespuestaPaginada<UnidadMedidaConsultaDto>> ListarUnidadesAsync(FiltroCatalogoProductoSolicitud filtro, ContextoConfiguracion contexto, CancellationToken token);
    Task<UnidadMedidaConsultaDto> ObtenerUnidadAsync(int id, ContextoConfiguracion contexto, CancellationToken token);
    Task<UnidadMedidaConsultaDto> CrearUnidadAsync(CrearUnidadMedidaSolicitud solicitud, ContextoConfiguracion contexto, CancellationToken token);
    Task<UnidadMedidaConsultaDto> ActualizarUnidadAsync(int id, ActualizarUnidadMedidaSolicitud solicitud, ContextoConfiguracion contexto, CancellationToken token);
    Task<UnidadMedidaConsultaDto> CambiarEstadoUnidadAsync(int id, int estadoId, ContextoConfiguracion contexto, CancellationToken token);

    Task<RespuestaPaginada<PresentacionConsultaDto>> ListarPresentacionesAsync(FiltroCatalogoProductoSolicitud filtro, ContextoConfiguracion contexto, CancellationToken token);
    Task<PresentacionConsultaDto> ObtenerPresentacionAsync(int id, ContextoConfiguracion contexto, CancellationToken token);
    Task<PresentacionConsultaDto> CrearPresentacionAsync(CrearPresentacionSolicitud solicitud, ContextoConfiguracion contexto, CancellationToken token);
    Task<PresentacionConsultaDto> ActualizarPresentacionAsync(int id, ActualizarPresentacionSolicitud solicitud, ContextoConfiguracion contexto, CancellationToken token);
    Task<PresentacionConsultaDto> CambiarEstadoPresentacionAsync(int id, int estadoId, ContextoConfiguracion contexto, CancellationToken token);

    Task<RespuestaPaginada<ConversionUnidadConsultaDto>> ListarConversionesAsync(FiltroCatalogoProductoSolicitud filtro, ContextoConfiguracion contexto, CancellationToken token);
    Task<ConversionUnidadConsultaDto> CrearConversionAsync(CrearConversionUnidadSolicitud solicitud, ContextoConfiguracion contexto, CancellationToken token);
    Task<ConversionUnidadConsultaDto> ActualizarConversionAsync(long id, ActualizarConversionUnidadSolicitud solicitud, ContextoConfiguracion contexto, CancellationToken token);
    Task<ConversionUnidadConsultaDto> CambiarEstadoConversionAsync(long id, int estadoId, ContextoConfiguracion contexto, CancellationToken token);
}

public interface IServicioProductos
{
    Task<ReferenciasProductoDto> ObtenerReferenciasAsync(ContextoConfiguracion contexto, CancellationToken token);
    Task<RespuestaPaginada<ProductoConsultaDto>> ListarAsync(FiltroProductosSolicitud filtro, ContextoConfiguracion contexto, CancellationToken token);
    Task<ProductoConsultaDto> ObtenerAsync(long id, ContextoConfiguracion contexto, CancellationToken token);
    Task<ProductoConsultaDto> CrearAsync(CrearProductoSolicitud solicitud, ContextoConfiguracion contexto, CancellationToken token);
    Task<ProductoConsultaDto> ActualizarAsync(long id, ActualizarProductoSolicitud solicitud, ContextoConfiguracion contexto, CancellationToken token);
    Task<ProductoConsultaDto> CambiarEstadoAsync(long id, int estadoId, ContextoConfiguracion contexto, CancellationToken token);
    Task<IReadOnlyList<ProductoPresentacionConsultaDto>> ListarPresentacionesAsync(long productoId, ContextoConfiguracion contexto, CancellationToken token);
    Task<ProductoPresentacionConsultaDto> CrearPresentacionAsync(long productoId, CrearProductoPresentacionSolicitud solicitud, ContextoConfiguracion contexto, CancellationToken token);
    Task<ProductoPresentacionConsultaDto> ActualizarPresentacionAsync(long productoId, long id, ActualizarProductoPresentacionSolicitud solicitud, ContextoConfiguracion contexto, CancellationToken token);
    Task<ProductoPresentacionConsultaDto> CambiarEstadoPresentacionAsync(long productoId, long id, int estadoId, ContextoConfiguracion contexto, CancellationToken token);
}

public interface IAuditoriaFase4
{
    void Registrar(ContextoConfiguracion contexto, string entidad, string? entidadId, string accion, string comentario);
}
