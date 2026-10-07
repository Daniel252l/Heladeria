namespace Heladeria.Infraestructura.Persistencia.Entidades;

// Ampliación Database First. NULL distingue históricos no conciliados; no se infieren importes.
public partial class TurnosCaja
{
    public int? MonedaId { get; set; }
    public int? UsuarioCierreId { get; set; }
    public DateTime? FechaModificacion { get; set; }
    public string? NumeroApertura { get; set; }
    public string? NumeroCierre { get; set; }
    public decimal? TotalIngresos { get; set; }
    public decimal? TotalEgresos { get; set; }
    public long? ArqueoCierreId { get; set; }
    public byte[] VersionFila { get; set; } = [];
    public virtual Monedas? Moneda { get; set; }
}
public partial class Pagos
{
    public long? VentaId { get; set; }
    public Guid? ClaveOperacion { get; set; }
    public virtual Ventas? Venta { get; set; }
}
public partial class PagoDetalles
{
    public decimal? MontoRecibido { get; set; }
    public bool? EsEfectivo { get; set; }
}
public partial class MovimientosCaja
{
    public long? PagoDetalleId { get; set; }
    public string? CodigoConcepto { get; set; }
    public bool? EsEfectivo { get; set; }
    public string? NumeroDocumento { get; set; }
    public virtual PagoDetalles? PagoDetalle { get; set; }
}
public partial class ArqueosCaja
{
    public byte[]? VersionTurno { get; set; }
}
