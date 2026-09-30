using System.ComponentModel.DataAnnotations;
using Heladeria.Aplicacion.Configuracion;

namespace Heladeria.Aplicacion.Inventario;

public sealed class FiltroExistenciasSolicitud : FiltroPaginadoBase
{
    [Range(typeof(long), "1", "9223372036854775807")]
    public long? SucursalId { get; init; }

    [Range(typeof(long), "1", "9223372036854775807")]
    public long? ProductoId { get; init; }

    [Range(1, int.MaxValue)]
    public int? CategoriaId { get; init; }

    public bool? SoloExistenciaPositiva { get; init; }
    public bool? SoloBajoMinimo { get; init; }
}

public sealed class AjusteInventarioSolicitud
{
    [Range(typeof(long), "1", "9223372036854775807")]
    public long SucursalId { get; init; }

    [Range(typeof(long), "1", "9223372036854775807")]
    public long BodegaId { get; init; }

    [Range(typeof(long), "1", "9223372036854775807")]
    public long UbicacionId { get; init; }

    [Range(typeof(long), "1", "9223372036854775807")]
    public long ProductoId { get; init; }

    [Range(typeof(long), "1", "9223372036854775807")]
    public long? ProductoPresentacionId { get; init; }

    [Required, RegularExpression("^(ENTRADA|SALIDA)$")]
    public string Tipo { get; init; } = string.Empty;

    [Range(typeof(decimal), "0.000001", "9999999999999.999999")]
    public decimal Cantidad { get; init; }

    [Range(typeof(decimal), "0", "9999999999999.999999")]
    // Con presentación: costo por una presentación. Sin presentación: costo por unidad base.
    public decimal? CostoUnitario { get; init; }

    [StringLength(100)]
    public string? Referencia { get; init; }

    [StringLength(500)]
    public string? Observaciones { get; init; }
}

public sealed record ExistenciaConsultaDto(
    long Id, int EmpresaId, long SucursalId, string Sucursal,
    long BodegaId, string Bodega, long UbicacionId, string Ubicacion,
    long ProductoId, string SKU, string Producto, int CategoriaId,
    string Categoria, int UnidadBaseId, string UnidadBase,
    decimal Cantidad, decimal CantidadReservada, decimal CantidadDisponible,
    decimal CostoPromedio, decimal? StockMinimo, bool BajoMinimo,
    DateTime? UltimoMovimiento, string VersionFila);

public sealed record AjusteInventarioResultadoDto(
    long MovimientoId, long MovimientoDetalleId, long ExistenciaId,
    string Tipo, decimal CantidadSolicitada, decimal FactorConversion,
    decimal CantidadBase, decimal SaldoAnterior, decimal SaldoNuevo,
    // Se conserva el nombre del contrato; el valor siempre está expresado por unidad base.
    decimal CostoUnitario, string UnidadBase);

public sealed record AlmacenInventarioDto(
    long SucursalId, string Sucursal, long BodegaId, string Bodega,
    long UbicacionId, string Ubicacion, bool EsBodegaPrincipal);

public interface IServicioInventario
{
    Task<IReadOnlyList<AlmacenInventarioDto>> ListarAlmacenesAsync(ContextoConfiguracion contexto, CancellationToken token);
    Task<RespuestaPaginada<ExistenciaConsultaDto>> ListarExistenciasAsync(FiltroExistenciasSolicitud filtro, ContextoConfiguracion contexto, CancellationToken token);
    Task<AjusteInventarioResultadoDto> AjustarAsync(AjusteInventarioSolicitud solicitud, ContextoConfiguracion contexto, CancellationToken token);
}
