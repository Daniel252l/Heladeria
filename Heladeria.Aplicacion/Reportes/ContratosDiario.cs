using System.ComponentModel.DataAnnotations;
using Heladeria.Aplicacion.Configuracion;

namespace Heladeria.Aplicacion.Reportes;

public sealed class FiltroDiario : FiltroPaginadoBase, IValidatableObject
{
    public DateOnly? Fecha { get; init; }
    [Range(1, long.MaxValue)] public long? SucursalId { get; init; }
    [Range(1, long.MaxValue)] public long? CajaId { get; init; }
    [Range(1, int.MaxValue)] public int? FormaPagoId { get; init; }
    public bool? EsCredito { get; init; }
    [Range(1, int.MaxValue)] public int PaginaOperaciones { get; init; } = 1;
    [Range(1, int.MaxValue)] public int PaginaCajas { get; init; } = 1;
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (new[] { Pagina, PaginaOperaciones, PaginaCajas }.Any(p => ((long)p - 1) * TamanioPagina > int.MaxValue))
            yield return new("Página fuera de rango.");
        if (Fecha is { Year: < 1900 or > 9998 }) yield return new("Fecha fuera de rango.", [nameof(Fecha)]);
    }
}
public sealed record VentaDiarioDto(long Id, string Numero, DateTime Fecha, string? Cliente, bool EsCredito,
    string Estado, decimal Total, string Moneda, string Usuario, bool TieneComprobante, IReadOnlyList<string> FormasPago);
public sealed record OperacionDiarioDto(long Id, DateTime Fecha, string Tipo, string? Documento, string? Referencia,
    string? FormaPago, string? Moneda, decimal? Ingreso, decimal? Egreso, decimal Apertura, string Usuario);
public sealed record FinanzasDiarioDto(int? MonedaId, string? Moneda, decimal Aperturas, decimal Ingresos,
    decimal Egresos, decimal CobrosVentas, decimal CobrosCxc, decimal PagosCxp, int SinClasificar);
public sealed record CreditoDiarioDto(int MonedaId, string Moneda, int Cantidad, decimal ImporteOriginal);
public sealed record ReporteDiarioDto(PeriodoReporte Periodo, DateTime DesdeUtc, DateTime HastaUtcExclusivo,
    DateTime GeneradoUtc, string Sucursal, IReadOnlyList<TotalVentasDto> VentasTotales,
    IReadOnlyList<CobrosFormaReporteDto> CobrosPorForma, IReadOnlyList<FinanzasDiarioDto> Finanzas,
    IReadOnlyList<CreditoDiarioDto> CreditosGenerados, RespuestaPaginada<VentaDiarioDto> Ventas,
    RespuestaPaginada<OperacionDiarioDto> Operaciones, RespuestaPaginada<SesionCajaReporteDto> Cajas);
