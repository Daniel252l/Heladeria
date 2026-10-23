using System.ComponentModel.DataAnnotations;
using Heladeria.Aplicacion.Configuracion;

namespace Heladeria.Aplicacion.Caja;

public sealed class FiltroCaja : FiltroPaginadoBase;
public sealed class FiltroSesionesCaja : FiltroPaginadoBase
{
    [Range(typeof(long), "1", "9223372036854775807")] public long? CajaId { get; init; }
    public bool? Abierta { get; init; }
}
public sealed class CrearCajaSolicitud
{
    [Required, StringLength(20, MinimumLength = 1)] public string Codigo { get; init; } = string.Empty;
    [Required, StringLength(100, MinimumLength = 1)] public string Nombre { get; init; } = string.Empty;
}
public sealed class AbrirCajaSolicitud
{
    [Range(typeof(long), "1", "9223372036854775807")] public long CajaId { get; init; }
    [Range(1, int.MaxValue)] public int MonedaId { get; init; }
    [Range(typeof(decimal), "0", "9999999999.99")] public decimal SaldoInicial { get; init; }
}
public sealed class MovimientoCajaSolicitud
{
    [Required] public string VersionFila { get; init; } = string.Empty;
    [Required, RegularExpression("^(INGRESO|EGRESO)$")] public string Tipo { get; init; } = string.Empty;
    [Range(typeof(decimal), "0.01", "9999999999.99")] public decimal Monto { get; init; }
    [Required, StringLength(150, MinimumLength = 3)] public string Motivo { get; init; } = string.Empty;
    [StringLength(140)] public string? Observacion { get; init; }
}
public sealed class CobrarVentaSolicitud
{
    [Required] public string VersionFila { get; init; } = string.Empty;
    [Required] public string VersionVenta { get; init; } = string.Empty;
    [Range(typeof(long), "1", "9223372036854775807")] public long VentaId { get; init; }
    public Guid ClaveOperacion { get; init; }
    [Required, MinLength(1), MaxLength(10)] public FormaCobroSolicitud[] Pagos { get; init; } = [];
}
public sealed class FormaCobroSolicitud
{
    [Range(1, int.MaxValue)] public int FormaPagoId { get; init; }
    [Range(typeof(decimal), "0.01", "9999999999.99")] public decimal MontoAplicado { get; init; }
    [Range(typeof(decimal), "0.01", "9999999999.99")] public decimal MontoRecibido { get; init; }
    [StringLength(100)] public string? Referencia { get; init; }
}
public sealed class ArqueoCajaSolicitud
{
    [Required] public string VersionFila { get; init; } = string.Empty;
    [Range(typeof(decimal), "0", "9999999999.99")] public decimal EfectivoContado { get; init; }
    [StringLength(500)] public string? Observaciones { get; init; }
}
public sealed class CerrarCajaSolicitud
{
    [Required] public string VersionFila { get; init; } = string.Empty;
    [Range(typeof(long), "1", "9223372036854775807")] public long ArqueoId { get; init; }
}
public sealed record CajaDto(long Id, int EmpresaId, long SucursalId, string Codigo, string Nombre, int EstadoId, string Estado, long? SesionAbiertaId);
public sealed record FormaPagoCajaDto(int Id, string Codigo, string Nombre, bool EsEfectivo, bool RequiereReferencia, bool PermiteCambio);
public sealed record SaldoUltimoCierreDto(long SesionId, long CajaId, string NumeroCierre, DateTime FechaCierre, decimal EfectivoContado);
public sealed record ReferenciasCajaDto(int EmpresaId, long SucursalId, int MonedaId, string Moneda, IReadOnlyList<FormaPagoCajaDto> FormasPago)
{
    public SaldoUltimoCierreDto? SaldoUltimoCierre { get; init; }
}
public sealed record SesionCajaResumenDto(long Id, long CajaId, string Caja, int EmpresaId, long SucursalId,
    int UsuarioAperturaId, string UsuarioApertura, DateTime FechaApertura, decimal SaldoInicial, string Estado,
    int? MonedaId, string? Moneda, string? NumeroApertura, string? NumeroCierre, DateTime? FechaCierre,
    int? UsuarioCierreId, decimal? EfectivoEsperadoCierre, decimal? EfectivoContadoCierre, decimal? DiferenciaCierre,
    decimal? TotalIngresosCierre, decimal? TotalEgresosCierre, long? ArqueoCierreId, string VersionFila, bool EsFase8);
public sealed record MovimientoCajaDto(long Id, string? NumeroDocumento, string Tipo, DateTime Fecha, decimal Monto,
    int? FormaPagoId, string? FormaPago, bool? EsEfectivo, string? Referencia, string? Observacion,
    int UsuarioId, string Usuario, long? PagoId, long? VentaId, string? NumeroVenta);
public sealed record ArqueoCajaDto(long Id, DateTime Fecha, decimal Esperado, decimal Contado, decimal Diferencia,
    int UsuarioId, string Usuario, string? Observaciones, bool Vigente);
public sealed record TotalTipoCajaDto(string Tipo, decimal Total);
public sealed record TotalFormaCajaDto(int? FormaPagoId, string FormaPago, bool EsEfectivo, decimal SaldoInicial, decimal Ingresos, decimal Egresos, decimal Neto);
public sealed record CobroDetalleDto(long Id, int FormaPagoId, string FormaPago, bool EsEfectivo, decimal Aplicado,
    decimal Recibido, decimal Vuelto, string? Referencia);
public sealed record CobroCajaDto(long Id, long VentaId, string NumeroVenta, long SesionId, DateTime Fecha,
    int UsuarioId, decimal Total, int MonedaId, string Moneda, Guid ClaveOperacion, IReadOnlyList<CobroDetalleDto> Detalles);
public sealed record SesionCajaDto(SesionCajaResumenDto Encabezado, IReadOnlyList<MovimientoCajaDto> Movimientos,
    IReadOnlyList<TotalTipoCajaDto> TotalesPorTipo, IReadOnlyList<TotalFormaCajaDto> TotalesPorFormaPago,
    decimal? SaldoTeorico, decimal? EfectivoEsperado, IReadOnlyList<CobroCajaDto> Cobros, IReadOnlyList<ArqueoCajaDto> Arqueos)
{
    public string? UsuarioCierre { get; init; }
}
public sealed record ResultadoCobroCajaDto(CobroCajaDto Cobro, SesionCajaDto Sesion, bool Reintento);
public sealed record VentaPendienteCajaDto(long Id, string NumeroVenta, DateTime Fecha, int? ClienteId, string? Cliente,
    decimal Total, decimal SaldoPendiente, int MonedaId, string Moneda, string VersionFila);

public interface IServicioCaja
{
    Task<RespuestaPaginada<CajaDto>> CajasAsync(FiltroPaginadoBase filtro, ContextoConfiguracion seguridad, CancellationToken t);
    Task<CajaDto> CrearCajaAsync(CrearCajaSolicitud solicitud, ContextoConfiguracion seguridad, CancellationToken t);
    Task<RespuestaPaginada<SesionCajaResumenDto>> SesionesAsync(FiltroSesionesCaja filtro, ContextoConfiguracion seguridad, CancellationToken t);
    Task<SesionCajaDto> ObtenerAsync(long id, ContextoConfiguracion seguridad, CancellationToken t);
    Task<ReferenciasCajaDto> ReferenciasAsync(ContextoConfiguracion seguridad, CancellationToken t, long? cajaId = null);
    Task<RespuestaPaginada<VentaPendienteCajaDto>> VentasPendientesAsync(FiltroPaginadoBase filtro, ContextoConfiguracion seguridad, CancellationToken t);
    Task<SesionCajaDto> AbrirAsync(AbrirCajaSolicitud solicitud, ContextoConfiguracion seguridad, CancellationToken t);
    Task<SesionCajaDto> MovimientoAsync(long id, MovimientoCajaSolicitud solicitud, ContextoConfiguracion seguridad, CancellationToken t);
    Task<ResultadoCobroCajaDto> CobrarAsync(long id, CobrarVentaSolicitud solicitud, ContextoConfiguracion seguridad, CancellationToken t);
    Task<SesionCajaDto> ArqueoAsync(long id, ArqueoCajaSolicitud solicitud, ContextoConfiguracion seguridad, CancellationToken t);
    Task<SesionCajaDto> CerrarAsync(long id, CerrarCajaSolicitud solicitud, ContextoConfiguracion seguridad, CancellationToken t);
}
public class ExcepcionCaja(string mensaje, string codigo = "CONFLICTO_CAJA", int estadoHttp = 409) : Exception(mensaje)
{
    public string Codigo { get; } = codigo;
    public int EstadoHttp { get; } = estadoHttp;
}
public sealed class ValidacionCajaException(string mensaje) : ExcepcionCaja(mensaje, "VALIDACION_CAJA", 400);
public sealed class CajaNoEncontradaException(string mensaje) : ExcepcionCaja(mensaje, "RECURSO_NO_ENCONTRADO", 404);
