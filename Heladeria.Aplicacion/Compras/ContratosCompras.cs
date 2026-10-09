using System.ComponentModel.DataAnnotations;
using Heladeria.Aplicacion.Configuracion;

namespace Heladeria.Aplicacion.Compras;

public sealed class FiltroProveedoresSolicitud : FiltroPaginadoBase
{
    [StringLength(20)] public string? Codigo { get; init; }
    [StringLength(150)] public string? Nombre { get; init; }
    [StringLength(20)] public string? Nit { get; init; }
    [StringLength(100)] public string? Correo { get; init; }
    [Range(1, int.MaxValue)] public int? EstadoId { get; init; }
}

public sealed class GuardarProveedorSolicitud
{
    [Required, StringLength(20)] public string CodigoProveedor { get; init; } = string.Empty;
    [Required, StringLength(150)] public string NombreProveedor { get; init; } = string.Empty;
    [StringLength(20)] public string? Nit { get; init; }
    [StringLength(20)] public string? Telefono { get; init; }
    [EmailAddress, StringLength(100)] public string? Correo { get; init; }
    [StringLength(250)] public string? Direccion { get; init; }
    [Range(0, short.MaxValue)] public short DiasCredito { get; init; }
    [Range(typeof(decimal), "0", "9999999999.99")] public decimal? LimiteCredito { get; init; }
    [Range(1, int.MaxValue)] public int? MonedaId { get; init; }
    [Range(1, int.MaxValue)] public int EstadoId { get; init; }
}

public sealed class GuardarProveedorProductoSolicitud
{
    [Range(typeof(long), "1", "9223372036854775807")] public long ProductoId { get; init; }
    [StringLength(50)] public string? CodigoProveedor { get; init; }
    [Range(typeof(decimal), "0", "9999999999.99")] public decimal? PrecioUltimaCompra { get; init; }
    public bool EsPreferido { get; init; }
    [Range(1, int.MaxValue)] public int EstadoId { get; init; }
}

public sealed class FiltroComprasSolicitud : FiltroPaginadoBase
{
    [Range(1, int.MaxValue)] public int? ProveedorId { get; init; }
    [Range(typeof(long), "1", "9223372036854775807")] public long? SucursalId { get; init; }
    [Range(1, int.MaxValue)] public int? EstadoId { get; init; }
    public DateTime? FechaDesde { get; init; }
    public DateTime? FechaHasta { get; init; }
    [StringLength(30)] public string? Documento { get; init; }
}

public sealed class GuardarCompraSolicitud
{
    public bool? EsCredito { get; init; }
    [Range(typeof(long), "1", "9223372036854775807")] public long SucursalId { get; init; }
    [Range(1, int.MaxValue)] public int ProveedorId { get; init; }
    public DateTime FechaEmision { get; init; }
    public DateTime? FechaEntregaEsperada { get; init; }
    [Range(1, int.MaxValue)] public int MonedaId { get; init; }
    [Range(typeof(decimal), "0.00000001", "99999999999.99999999")] public decimal TipoCambio { get; init; } = 1m;
    [Required, MinLength(1), MaxLength(100)] public CompraLineaSolicitud[] Detalles { get; init; } = [];
}

public sealed class CompraLineaSolicitud
{
    [Range(typeof(long), "1", "9223372036854775807")] public long ProductoId { get; init; }
    [Range(typeof(long), "1", "9223372036854775807")] public long? ProductoPresentacionId { get; init; }
    [Range(typeof(decimal), "0.000001", "9999999999999.999999")] public decimal Cantidad { get; init; }
    [Range(typeof(decimal), "0", "9999999999.99")] public decimal PrecioUnitario { get; init; }
    [Range(typeof(decimal), "0", "100")] public decimal PorcentajeDescuento { get; init; }
}

public sealed class CambiarEstadoCompraSolicitud
{
    [Required, StringLength(30)] public string Estado { get; init; } = string.Empty;
}

public sealed class CrearRecepcionSolicitud
{
    public Guid ClaveOperacion { get; init; }
    [Range(typeof(long), "1", "9223372036854775807")] public long BodegaId { get; init; }
    [Range(typeof(long), "1", "9223372036854775807")] public long UbicacionId { get; init; }
    [StringLength(500)] public string? Observaciones { get; init; }
    [Required, MinLength(1), MaxLength(100)] public RecepcionLineaSolicitud[] Detalles { get; init; } = [];
}

public sealed class RecepcionLineaSolicitud
{
    [Range(typeof(long), "1", "9223372036854775807")] public long OrdenCompraDetalleId { get; init; }
    [Range(typeof(decimal), "0.000001", "9999999999999.999999")] public decimal Cantidad { get; init; }
}

public sealed record ProveedorConsultaDto(int Id, int EmpresaId, string CodigoProveedor, string NombreProveedor,
    string? Nit, string? Telefono, string? Correo, string? Direccion, short DiasCredito,
    decimal? LimiteCredito, int? MonedaId, int EstadoId, string Estado);
public sealed record ProveedorProductoDto(long Id, int ProveedorId, long ProductoId, string Producto,
    string? CodigoProveedor, decimal? PrecioUltimaCompra, bool EsPreferido, int EstadoId, string Estado);
public sealed record CompraResumenDto(long Id, int EmpresaId, long SucursalId, string Sucursal,
    int ProveedorId, string Proveedor, string NumeroOrden, DateTime FechaEmision, int MonedaId,
    decimal TipoCambio, decimal SubTotal, decimal Impuestos, decimal TotalEstimado, int EstadoId, string Estado)
{
    public bool? EsCredito { get; init; }
    public string VersionFila { get; init; } = string.Empty;
}
public sealed record CompraDetalleDto(long Id, int NumeroLinea, long ProductoId, string Producto,
    long? ProductoPresentacionId, string? Presentacion, int UnidadBaseId, string UnidadBase,
    decimal Cantidad, decimal FactorConversion, decimal CantidadBase, decimal PrecioUnitario,
    decimal Descuento, decimal Impuesto, decimal TotalLinea, decimal CantidadRecibida, decimal CantidadPendiente);
public sealed record CompraConsultaDto(CompraResumenDto Encabezado, DateTime? FechaEntregaEsperada,
    IReadOnlyList<CompraDetalleDto> Detalles, IReadOnlyList<RecepcionCompraDto> Recepciones);
public sealed record RecepcionDetalleDto(long Id, long OrdenCompraDetalleId, long ProductoId,
    decimal CantidadRecibida, decimal CostoUnitarioBase);
public sealed record RecepcionCompraDto(long Id, long OrdenCompraId, string NumeroRecepcion,
    DateTime FechaRecepcion, string Estado, string? Observaciones, IReadOnlyList<RecepcionDetalleDto> Detalles,
    IReadOnlyList<long> MovimientoIds);
public sealed record MonedaCompraDto(int Id, string CodigoISO, string Nombre, bool EsMonedaBase, byte Decimales);
public sealed record EstadoCompraDto(int Id, string Flujo, string Codigo, bool PermiteEdicion, bool EsFinal);
public sealed record SerieCompraDto(long Id, string Serie, string? Prefijo, byte Longitud);
public sealed record ReferenciasComprasDto(IReadOnlyList<MonedaCompraDto> Monedas, IReadOnlyList<EstadoCompraDto> Estados,
    IReadOnlyList<SerieCompraDto> Series);
public sealed record PresentacionCompraDto(long ProductoPresentacionId, int PresentacionId,
    string PresentacionCodigo, string PresentacionNombre, int UnidadMedidaId, string UnidadCodigo,
    string UnidadNombre, string UnidadAbreviatura, decimal FactorConversion, bool EsPresentacionBase, bool Activo);
public sealed record ProductoPresentacionesCompraDto(long ProductoId, string Producto, int UnidadBaseId,
    string UnidadBaseCodigo, string UnidadBaseNombre, string UnidadBaseAbreviatura,
    IReadOnlyList<PresentacionCompraDto> Presentaciones);

public interface IServicioProveedores
{
    Task<RespuestaPaginada<ProveedorConsultaDto>> ListarAsync(FiltroProveedoresSolicitud filtro, ContextoConfiguracion seguridad, CancellationToken token);
    Task<ProveedorConsultaDto> ObtenerAsync(int id, ContextoConfiguracion seguridad, CancellationToken token);
    Task<ProveedorConsultaDto> GuardarAsync(int? id, GuardarProveedorSolicitud solicitud, ContextoConfiguracion seguridad, CancellationToken token);
    Task<ProveedorConsultaDto> CambiarEstadoAsync(int id, int estadoId, ContextoConfiguracion seguridad, CancellationToken token);
    Task<RespuestaPaginada<ProveedorProductoDto>> ListarProductosAsync(int id, FiltroConEstadoSolicitud filtro, ContextoConfiguracion seguridad, CancellationToken token);
    Task<ProveedorProductoDto> GuardarProductoAsync(int id, long? relacionId, GuardarProveedorProductoSolicitud solicitud, ContextoConfiguracion seguridad, CancellationToken token);
    Task<ProveedorProductoDto> CambiarEstadoProductoAsync(int id, long relacionId, int estadoId, ContextoConfiguracion seguridad, CancellationToken token);
}

public interface IServicioCompras
{
    Task<ReferenciasComprasDto> ObtenerReferenciasAsync(ContextoConfiguracion seguridad, CancellationToken token);
    Task<ProductoPresentacionesCompraDto> ListarPresentacionesAsync(long productoId, ContextoConfiguracion seguridad, CancellationToken token);
    Task<RespuestaPaginada<CompraResumenDto>> ListarAsync(FiltroComprasSolicitud filtro, ContextoConfiguracion seguridad, CancellationToken token);
    Task<CompraConsultaDto> ObtenerAsync(long id, ContextoConfiguracion seguridad, CancellationToken token);
    Task<CompraConsultaDto> GuardarAsync(long? id, GuardarCompraSolicitud solicitud, ContextoConfiguracion seguridad, CancellationToken token);
    Task<CompraConsultaDto> CambiarEstadoAsync(long id, string estado, ContextoConfiguracion seguridad, CancellationToken token);
    Task<RecepcionCompraDto> RecibirAsync(long id, CrearRecepcionSolicitud solicitud, ContextoConfiguracion seguridad, CancellationToken token);
    Task<RecepcionCompraDto> ObtenerRecepcionAsync(long id, ContextoConfiguracion seguridad, CancellationToken token);
}

public abstract class ExcepcionCompras(string mensaje, string codigo) : Exception(mensaje)
{
    public string Codigo { get; } = codigo;
}
public sealed class ValidacionComprasException(string mensaje) : ExcepcionCompras(mensaje, "VALIDACION_COMPRAS");
public sealed class ConflictoComprasException(string mensaje) : ExcepcionCompras(mensaje, "CONFLICTO_COMPRAS");
public sealed class RecursoComprasNoEncontradoException(string mensaje) : ExcepcionCompras(mensaje, "RECURSO_NO_ENCONTRADO");
