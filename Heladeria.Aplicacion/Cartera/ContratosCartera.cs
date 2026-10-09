using System.ComponentModel.DataAnnotations;
using Heladeria.Aplicacion.Caja;
using Heladeria.Aplicacion.Configuracion;

namespace Heladeria.Aplicacion.Cartera;

public sealed class FiltroCartera : FiltroPaginadoBase
{
    [Range(1, int.MaxValue)] public int? TerceroId { get; init; }
    [Range(typeof(long), "1", "9223372036854775807")] public long? SucursalId { get; init; }
    [RegularExpression("^(PENDIENTE|PARCIAL|PAGADA|ANULADA)$")] public string? Estado { get; init; }
    [RegularExpression("^(VIGENTE|VENCIDA|PAGADA)$")] public string? Vencimiento { get; init; }
    public DateOnly? FechaDesde { get; init; }
    public DateOnly? FechaHasta { get; init; }
}
public sealed class AplicarPagoCarteraSolicitud
{
    public Guid ClaveOperacion { get; init; }
    [Required] public string VersionFila { get; init; } = string.Empty;
    [Range(typeof(long), "1", "9223372036854775807")] public long SesionCajaId { get; init; }
    [Required] public string VersionCaja { get; init; } = string.Empty;
    [Range(1, int.MaxValue)] public int FormaPagoId { get; init; }
    [Range(typeof(decimal), "0.01", "9999999999.99")] public decimal Monto { get; init; }
    [StringLength(30)] public string? Referencia { get; init; }
    [StringLength(500)] public string? Observacion { get; init; }
}
public sealed class RegistrarFacturaCreditoSolicitud
{
    public Guid ClaveOperacion { get; init; }
    [Range(typeof(long), "1", "9223372036854775807")] public long OrdenCompraId { get; init; }
    [Required] public string VersionCompra { get; init; } = string.Empty;
    [StringLength(30)] public string? Serie { get; init; }
    [Required, StringLength(50)] public string NumeroDocumento { get; init; } = string.Empty;
    public DateOnly FechaDocumento { get; init; }
    public DateOnly FechaVencimiento { get; init; }
}
public sealed record CuentaCarteraDto(long Id, int EmpresaId, long SucursalId, int TerceroId, string Tercero,
    long DocumentoOrigenId, string TipoDocumentoOrigen, string NumeroDocumentoOrigen, long? CompraId,
    int MonedaId, string Moneda, DateOnly FechaEmision, DateOnly FechaVencimiento,
    decimal ImporteOriginal, decimal TotalAplicado, decimal SaldoPendiente, string Estado, string Vencimiento,
    string VersionFila, bool Operable, int? UsuarioCreacionId, DateTime? FechaCreacion,
    int? UsuarioModificacionId, DateTime? FechaModificacion);
public sealed record AplicacionCarteraDto(long Id, long PagoId, DateTime Fecha, decimal Monto,
    int? FormaPagoId, string FormaPago, string? Referencia, Guid? ClaveOperacion, string? Observacion,
    int UsuarioId, string Usuario, long? SesionCajaId, IReadOnlyList<long> MovimientoIds, string Estado)
{
    public IReadOnlyList<DetalleAplicacionCarteraDto> Detalles { get; init; } = [];
}
public sealed record DetalleAplicacionCarteraDto(long Id, int FormaPagoId, string FormaPago, decimal Monto, string? Referencia);
public sealed record DetalleCarteraDto(CuentaCarteraDto Encabezado, IReadOnlyList<AplicacionCarteraDto> Historial);
public sealed record ResultadoPagoCarteraDto(long PagoId, DetalleCarteraDto Cuenta, bool Reintento);
public sealed record ResultadoFacturaCreditoDto(long FacturaId, DetalleCarteraDto Cuenta, bool Reintento);
public sealed record ResumenCarteraDto(int MonedaId, string Moneda, decimal TotalPendiente, decimal TotalVencido,
    decimal TotalVigente, int CantidadPendientes, int CantidadVencidas);
public interface IServicioCartera
{
    Task<RespuestaPaginada<CuentaCarteraDto>> ListarAsync(bool porPagar, FiltroCartera filtro, ContextoConfiguracion s, CancellationToken t);
    Task<DetalleCarteraDto> ObtenerAsync(bool porPagar, long id, ContextoConfiguracion s, CancellationToken t);
    Task<IReadOnlyList<ResumenCarteraDto>> ResumenAsync(bool porPagar, FiltroCartera filtro, ContextoConfiguracion s, CancellationToken t);
    Task<ReferenciasCajaDto> ReferenciasAsync(bool porPagar, ContextoConfiguracion s, CancellationToken t);
    Task<ResultadoPagoCarteraDto> AplicarAsync(bool porPagar, long id, AplicarPagoCarteraSolicitud solicitud, ContextoConfiguracion s, CancellationToken t);
    Task<ResultadoFacturaCreditoDto> RegistrarFacturaAsync(RegistrarFacturaCreditoSolicitud solicitud, ContextoConfiguracion s, CancellationToken t);
}
public sealed class ExcepcionCartera(string mensaje, string codigo = "CONFLICTO_CARTERA", int estadoHttp = 409)
    : ExcepcionCaja(mensaje, codigo, estadoHttp);
