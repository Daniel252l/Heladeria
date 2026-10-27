using System.ComponentModel.DataAnnotations;
using Heladeria.Aplicacion.Configuracion;

namespace Heladeria.Aplicacion.Reportes;

public abstract class FiltroPaginadoReporte : FiltroPaginadoBase, IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (((long)Pagina - 1) * TamanioPagina > int.MaxValue)
            yield return new ValidationResult("Página fuera de rango.", [nameof(Pagina)]);
    }
}
public class FiltroReporte : FiltroPaginadoReporte
{
    [Range(1, long.MaxValue)] public long? SucursalId { get; init; }
    public DateOnly? FechaDesde { get; init; }
    public DateOnly? FechaHasta { get; init; }
}
public class FiltroVentasReporte : FiltroReporte
{
    [Range(1, int.MaxValue)] public int? ClienteId { get; init; }
    [RegularExpression("BORRADOR|CONFIRMADA|COMPLETADA|ANULADA")] public string? Estado { get; init; }
    [Range(1, int.MaxValue)] public int? MonedaId { get; init; }
}
public sealed class FiltroProductosVendidos : FiltroVentasReporte
{
    [Range(1, long.MaxValue)] public long? ProductoId { get; init; }
}
public sealed class FiltroInventarioReporte : FiltroPaginadoReporte
{
    [Range(1, long.MaxValue)] public long? SucursalId { get; init; }
    [Range(1, long.MaxValue)] public long? ProductoId { get; init; }
    [Range(1, long.MaxValue)] public long? BodegaId { get; init; }
    [Range(1, long.MaxValue)] public long? UbicacionId { get; init; }
}
public sealed class FiltroKardexReporte : FiltroReporte
{
    [Range(1, long.MaxValue)] public long? ProductoId { get; init; }
    [Range(1, long.MaxValue)] public long? BodegaId { get; init; }
    [StringLength(30)] public string? Concepto { get; init; }
    [RegularExpression("ENTRADA|SALIDA")] public string? Tipo { get; init; }
}
public sealed class FiltroComprasReporte : FiltroReporte
{
    [Range(1, int.MaxValue)] public int? ProveedorId { get; init; }
    [Range(1, int.MaxValue)] public int? MonedaId { get; init; }
    [RegularExpression("BORRADOR|CONFIRMADA|PARCIALMENTE_RECIBIDA|RECIBIDA|ANULADA")] public string? Estado { get; init; }
    public bool? EsCredito { get; init; }
}
public sealed class FiltroCajaReporte : FiltroReporte
{
    [Range(1, long.MaxValue)] public long? CajaId { get; init; }
    [Range(1, int.MaxValue)] public int? UsuarioId { get; init; }
    [RegularExpression("ABIERTA|CERRADA")] public string? Estado { get; init; }
}
public sealed class FiltroCarteraReporte : FiltroReporte
{
    [Range(1, int.MaxValue)] public int? TerceroId { get; init; }
    [Range(1, int.MaxValue)] public int? MonedaId { get; init; }
    [RegularExpression("PENDIENTE|PARCIAL|PAGADA|ANULADA")] public string? Estado { get; init; }
    [RegularExpression("VIGENTE|VENCIDA|PAGADA")] public string? Vencimiento { get; init; }
}
public sealed class FiltroAuditoria : FiltroReporte
{
    [Range(1, int.MaxValue)] public int? UsuarioId { get; init; }
    [StringLength(50)] public string? Modulo { get; init; }
    [StringLength(100)] public string? Accion { get; init; }
    [StringLength(100)] public string? Entidad { get; init; }
}

public sealed record PeriodoReporte(DateOnly FechaDesde, DateOnly FechaHasta, string ZonaHoraria, long SucursalId);
public sealed record ReporteDto<T, TResumen>(RespuestaPaginada<T> Datos, IReadOnlyList<TResumen> Totales, PeriodoReporte Periodo);
public sealed record VentaReporteDto(long Id, string Numero, DateTime Fecha, string Estado, int? ClienteId,
    string? Cliente, bool EsCredito, int MonedaId, string Moneda, decimal Subtotal, decimal Descuentos,
    decimal Impuestos, decimal Total, decimal? Costo, decimal? MargenBruto, decimal Cobrado, string EstadoCobro);
public sealed record TotalVentasDto(int MonedaId, string Moneda, string Estado, int Cantidad, decimal Subtotal,
    decimal Descuentos, decimal Impuestos, decimal Total, decimal? Costo, decimal? MargenBruto);
public sealed record CobrosFormaReporteDto(int MonedaId, string Moneda, int FormaPagoId, string FormaPago, decimal Aplicado);
public sealed record ReporteVentasDto(ReporteDto<VentaReporteDto, TotalVentasDto> Ventas, IReadOnlyList<CobrosFormaReporteDto> CobrosPorForma);
public sealed record ProductoVendidoDto(long ProductoId, string Sku, string Producto, long? ProductoPresentacionId,
    string? Presentacion, string? Unidad, string? UnidadBase, int MonedaId, string Moneda,
    decimal Cantidad, decimal? CantidadBase, decimal? Importe, decimal? Costo, decimal? MargenBruto);
public sealed record InventarioReporteDto(long Id, long ProductoId, string Sku, string Producto, long BodegaId,
    string Bodega, long UbicacionId, string Ubicacion, string UnidadBase, decimal Existencia, decimal Reservada,
    decimal Disponible, decimal CostoPromedio, decimal ValorInventario);
public sealed record ReporteInventarioDto(RespuestaPaginada<InventarioReporteDto> Datos, decimal ValorInventario,
    string? MonedaBase, long SucursalId);
public sealed record KardexReporteDto(long Id, DateTime Fecha, string Concepto, string Tipo, long ProductoId,
    string Sku, string Producto, string UnidadBase, long BodegaId, string Bodega, string Ubicacion,
    decimal Cantidad, decimal CostoUnitario, decimal Costo, decimal SaldoCantidad, decimal SaldoCosto,
    long MovimientoId, string? ModuloOrigen, long? DocumentoOrigenId, string Documento, string Usuario);
public sealed record CompraReporteDto(long Id, string Numero, DateOnly Fecha, int ProveedorId, string Proveedor,
    bool? EsCredito, string Estado, int MonedaId, string Moneda, decimal Total,
    int Lineas, int LineasRecibidasCompletas, int Recepciones, long? CuentaPorPagarId, decimal? SaldoPorPagar);
public sealed record TotalComprasDto(int MonedaId, string Moneda, string Estado, int Cantidad, decimal Total);
public sealed record SesionCajaReporteDto(long Id, string Caja, DateTime Apertura, DateTime? Cierre,
    string Usuario, string Estado, int? MonedaId, string? Moneda, decimal SaldoInicial,
    decimal? IngresosOperativos, decimal? EgresosOperativos, decimal? SaldoTeorico,
    decimal? EfectivoEsperado, decimal? EfectivoContado, decimal? Diferencia, bool Conciliable);
public sealed record FormaCajaReporteDto(int? MonedaId, string? Moneda, int? FormaPagoId, string? FormaPago,
    bool? EsEfectivo, decimal Apertura, decimal IngresosOperativos, decimal EgresosOperativos);
public sealed record CuentaReporteDto(long Id, int TerceroId, string Tercero, string Documento, long OrigenId,
    long? CompraId, DateOnly Emision, DateOnly Vencimiento, int MonedaId, string Moneda,
    decimal ImporteOriginal, decimal Aplicado, decimal Saldo, string Estado, string Clasificacion);
public sealed record TotalCarteraReporteDto(int MonedaId, string Moneda, int Cantidad, int CantidadPendientes,
    decimal Pendiente, decimal Vigente, decimal Vencido);
public sealed record AuditoriaDto(long Id, DateTime Fecha, int? UsuarioId, string? Usuario, string? Modulo,
    string Entidad, string? EntidadId, string Accion, int EmpresaId, long SucursalId, string Resultado);

public interface IServicioReportes
{
    Task<ReporteDiarioDto> DiarioAsync(FiltroDiario f, ContextoConfiguracion s, CancellationToken t);
    Task<ReporteVentasDto> VentasAsync(FiltroVentasReporte f, ContextoConfiguracion s, CancellationToken t);
    Task<RespuestaPaginada<ProductoVendidoDto>> ProductosAsync(FiltroProductosVendidos f, ContextoConfiguracion s, CancellationToken t);
    Task<ReporteInventarioDto> InventarioAsync(FiltroInventarioReporte f, ContextoConfiguracion s, CancellationToken t);
    Task<RespuestaPaginada<KardexReporteDto>> KardexAsync(FiltroKardexReporte f, ContextoConfiguracion s, CancellationToken t);
    Task<ReporteDto<CompraReporteDto, TotalComprasDto>> ComprasAsync(FiltroComprasReporte f, ContextoConfiguracion s, CancellationToken t);
    Task<ReporteDto<SesionCajaReporteDto, FormaCajaReporteDto>> CajaAsync(FiltroCajaReporte f, ContextoConfiguracion s, CancellationToken t);
    Task<ReporteDto<CuentaReporteDto, TotalCarteraReporteDto>> CarteraAsync(bool pagar, FiltroCarteraReporte f, ContextoConfiguracion s, CancellationToken t);
    Task<RespuestaPaginada<AuditoriaDto>> AuditoriaAsync(FiltroAuditoria f, ContextoConfiguracion s, CancellationToken t);
}
public sealed class ExcepcionReporte(string mensaje, string codigo = "VALIDACION_REPORTE", int estadoHttp = 400) : Exception(mensaje)
{
    public string Codigo { get; } = codigo;
    public int EstadoHttp { get; } = estadoHttp;
}
