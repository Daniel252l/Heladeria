using System.ComponentModel.DataAnnotations;
using Heladeria.Aplicacion.Configuracion;
using Heladeria.Aplicacion.Caja;

namespace Heladeria.Aplicacion.Ventas;

public sealed class FiltroVentasSolicitud : FiltroPaginadoBase
{
    [Range(1, int.MaxValue)] public int? EstadoId { get; init; }
    public DateTime? FechaDesde { get; init; }
    public DateTime? FechaHasta { get; init; }
}
public sealed class FiltroProductosPosSolicitud : FiltroPaginadoBase
{
    [Range(1, int.MaxValue)] public int? CategoriaId { get; init; }
    [Range(typeof(long), "1", "9223372036854775807")] public long? BodegaId { get; init; }
    [Range(typeof(long), "1", "9223372036854775807")] public long? UbicacionId { get; init; }
}
public abstract class GuardarVentaSolicitud
{
    public bool? EsCredito { get; init; }
    public DateOnly? FechaVencimientoCredito { get; init; }
    [Range(typeof(long), "1", "9223372036854775807")] public long SucursalId { get; init; }
    [Range(typeof(long), "1", "9223372036854775807")] public long BodegaId { get; init; }
    [Range(typeof(long), "1", "9223372036854775807")] public long UbicacionId { get; init; }
    [Range(1, int.MaxValue)] public int? ClienteId { get; init; }
    [Range(1, int.MaxValue)] public int MonedaId { get; init; }
    [Required, MinLength(1), MaxLength(100)] public LineaVentaSolicitud[] Detalles { get; init; } = [];
}
public sealed class CrearVentaSolicitud : GuardarVentaSolicitud;
public sealed class ActualizarVentaSolicitud : GuardarVentaSolicitud
{
    [Required] public string VersionFila { get; init; } = string.Empty;
}
public sealed class LineaVentaSolicitud
{
    [Range(typeof(long), "1", "9223372036854775807")] public long ProductoId { get; init; }
    [Range(typeof(long), "1", "9223372036854775807")] public long? ProductoPresentacionId { get; init; }
    [Range(typeof(decimal), "0.000001", "9999999999999.999999")] public decimal Cantidad { get; init; }
    [Range(typeof(decimal), "0", "100")] public decimal PorcentajeDescuento { get; init; }
}
public sealed class ConfirmarVentaSolicitud
{
    [Required] public string VersionFila { get; init; } = string.Empty;
}
public sealed class CompletarVentaSolicitud
{
    public CobroAlCompletarSolicitud? Cobro { get; init; }
    [Required] public string VersionFila { get; init; } = string.Empty;
    public Guid ClaveOperacion { get; init; }
}
public sealed class CobroAlCompletarSolicitud
{
    [Range(typeof(long), "1", "9223372036854775807")] public long SesionCajaId { get; init; }
    [Required] public string VersionCaja { get; init; } = string.Empty;
    [Required, MinLength(1), MaxLength(10)] public FormaCobroSolicitud[] Pagos { get; init; } = [];
}
public sealed class AnularVentaSolicitud
{
    [Required] public string VersionFila { get; init; } = string.Empty;
    [Required, StringLength(500, MinimumLength = 3)] public string Motivo { get; init; } = string.Empty;
}
public sealed record ImpuestoVentaDto(int ImpuestoId, string Codigo, string Nombre, decimal Porcentaje,
    bool IncluidoEnPrecio, decimal BaseImponible, decimal Monto);
public sealed record VentaLineaDto(long Id, int NumeroLinea, long ProductoId, string SKU, string Producto,
    long? ProductoPresentacionId, string? Presentacion, decimal Cantidad, decimal? FactorConversion,
    decimal? CantidadBase, string? Unidad, string? UnidadBase, bool? ManejaExistencia,
    decimal PrecioUnitario, string? FuentePrecio, decimal? SubtotalNeto, decimal PorcentajeDescuento,
    decimal MontoDescuento, decimal? DescuentoPrecioCatalogo, decimal Impuesto, decimal? TotalLinea,
    decimal? CostoUnitarioBase, decimal? CostoTotal, IReadOnlyList<ImpuestoVentaDto> Impuestos);
public sealed record VentaResumenDto(long Id, int EmpresaId, long SucursalId, string Sucursal,
    long? BodegaId, long? UbicacionId, string NumeroVenta, DateTime FechaVenta, int? ClienteId,
    string? Cliente, int MonedaId, string Moneda, decimal TipoCambio, decimal Subtotal,
    decimal Descuento, decimal Impuesto, decimal Total, string Estado, int EstadoId,
    int UsuarioCreacionId, string Usuario, string VersionFila, DateTime? FechaCompletada,
    DateTime? FechaAnulacion, string? MotivoAnulacion, Guid? ClaveOperacion)
{
    public bool EsCredito { get; init; }
    public DateOnly? FechaVencimientoCredito { get; init; }
}
public sealed record MovimientoVentaDto(long Id, string Concepto, string Tipo, DateTime Fecha);
public sealed record VentaConsultaDto(VentaResumenDto Encabezado, IReadOnlyList<VentaLineaDto> Detalles,
    IReadOnlyList<MovimientoVentaDto> Movimientos)
{
    public CobroVentaDto? Cobro { get; init; }
    public DatosComprobanteDto? Comprobante { get; init; }
}
public sealed record DatosComprobanteDto(string Empresa, string RazonSocial, string DireccionSucursal,
    string TelefonoSucursal, string? CodigoCliente, string? UsuarioFinalizacion, string ZonaHoraria,
    long? CuentaPorCobrarId, decimal? SaldoInicialCredito, IReadOnlyList<PagoComprobanteDto> Pagos);
public sealed record PagoComprobanteDto(string FormaPago, decimal Aplicado, decimal? Recibido,
    decimal? Vuelto, bool? EsEfectivo);
public sealed record CobroVentaDto(long PagoId, long SesionCajaId, Guid ClaveOperacion, IReadOnlyList<FormaCobroSolicitud> Pagos);
public sealed record ImpuestoPosDto(int Id, string Codigo, string Nombre, decimal Porcentaje, bool IncluidoEnPrecio);
public sealed record PresentacionPosDto(long? ProductoPresentacionId, string Codigo, string Nombre,
    int UnidadMedidaId, string Unidad, string Abreviatura, decimal FactorConversion, decimal? Precio,
    string? FuentePrecio, bool PrecioValido);
public sealed record ProductoPosDto(long Id, string SKU, string Nombre, string? Imagen, int CategoriaId,
    string Categoria, int UnidadBaseId, string UnidadBase, string AbreviaturaBase, bool ManejaExistencia,
    bool PermiteDescuento, decimal? DisponibleBase, string Moneda,
    IReadOnlyList<PresentacionPosDto> Presentaciones, IReadOnlyList<ImpuestoPosDto> Impuestos);
public sealed record ReferenciaVentaDto(long Id, string Codigo, string Nombre);
public sealed record AlmacenVentaDto(long BodegaId, string Bodega, long UbicacionId, string Ubicacion);
public sealed record ReferenciasVentasDto(int EmpresaId, long SucursalId, int MonedaId, string Moneda,
    decimal TipoCambio, IReadOnlyList<ReferenciaVentaDto> Estados,
    IReadOnlyList<ReferenciaVentaDto> Clientes, IReadOnlyList<ReferenciaVentaDto> Categorias,
    IReadOnlyList<AlmacenVentaDto> Almacenes);
public interface IServicioVentas
{
    Task<IReadOnlyList<ReferenciaVentaDto>> EstadosAsync(ContextoConfiguracion seguridad, CancellationToken token);
    Task<RespuestaPaginada<VentaResumenDto>> ListarAsync(FiltroVentasSolicitud filtro, ContextoConfiguracion seguridad, CancellationToken token);
    Task<VentaConsultaDto> ObtenerAsync(long id, ContextoConfiguracion seguridad, CancellationToken token);
    Task<ReferenciasVentasDto> ReferenciasAsync(ContextoConfiguracion seguridad, CancellationToken token);
    Task<RespuestaPaginada<ProductoPosDto>> ProductosAsync(FiltroProductosPosSolicitud filtro, ContextoConfiguracion seguridad, CancellationToken token);
    Task<ProductoPosDto> PresentacionesAsync(long productoId, ContextoConfiguracion seguridad, CancellationToken token);
    Task<VentaConsultaDto> CrearAsync(CrearVentaSolicitud solicitud, ContextoConfiguracion seguridad, CancellationToken token);
    Task<VentaConsultaDto> ActualizarAsync(long id, ActualizarVentaSolicitud solicitud, ContextoConfiguracion seguridad, CancellationToken token);
    Task<VentaConsultaDto> ConfirmarAsync(long id, ConfirmarVentaSolicitud solicitud, ContextoConfiguracion seguridad, CancellationToken token);
    Task<VentaConsultaDto> CompletarAsync(long id, CompletarVentaSolicitud solicitud, ContextoConfiguracion seguridad, CancellationToken token);
    Task<VentaConsultaDto> AnularAsync(long id, AnularVentaSolicitud solicitud, ContextoConfiguracion seguridad, CancellationToken token);
}
public abstract class ExcepcionVentas(string mensaje, string codigo) : Exception(mensaje) { public string Codigo { get; } = codigo; }
public sealed class ValidacionVentasException(string mensaje) : ExcepcionVentas(mensaje, "VALIDACION_VENTAS");
public sealed class ConflictoVentasException(string mensaje) : ExcepcionVentas(mensaje, "CONFLICTO_VENTAS");
public sealed class VentaNoEncontradaException(string mensaje) : ExcepcionVentas(mensaje, "RECURSO_NO_ENCONTRADO");
public sealed class StockVentaInsuficienteException(string mensaje) : ExcepcionVentas(mensaje, "EXISTENCIA_INSUFICIENTE");
