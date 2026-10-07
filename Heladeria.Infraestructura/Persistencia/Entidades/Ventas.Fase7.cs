namespace Heladeria.Infraestructura.Persistencia.Entidades;

// Columnas del script Database First 20261007_Fase7_VentasPos.sql.
// Los snapshots nullable distinguen históricos no conciliados de ventas Fase 7.
public partial class Ventas
{
    public long? BodegaId { get; set; }
    public long? UbicacionId { get; set; }
    public Guid? ClaveOperacion { get; set; }
    public DateTime? FechaModificacion { get; set; }
    public int? ModificadoPorUsuarioId { get; set; }
    public DateTime? FechaCompletada { get; set; }
    public int? CompletadoPorUsuarioId { get; set; }
    public DateTime? FechaAnulacion { get; set; }
    public int? AnuladoPorUsuarioId { get; set; }
    public string? MotivoAnulacion { get; set; }
    public byte[] VersionFila { get; set; } = [];
}
public partial class VentasDetalle
{
    public decimal? FactorConversion { get; set; }
    public decimal? CantidadBase { get; set; }
    public int? UnidadMedidaId { get; set; }
    public int? UnidadBaseId { get; set; }
    public bool? ManejaExistencia { get; set; }
    public decimal? SubtotalNeto { get; set; }
    public decimal? DescuentoPrecioCatalogo { get; set; }
    public decimal? TotalLinea { get; set; }
    public decimal? CostoTotal { get; set; }
    public string? FuentePrecio { get; set; }
    public virtual UnidadesMedida? UnidadMedida { get; set; }
    public virtual UnidadesMedida? UnidadBase { get; set; }
    public virtual ICollection<VentaDetalleImpuestos> ImpuestosSnapshot { get; set; } = new List<VentaDetalleImpuestos>();
}
public partial class VentaDetalleImpuestos
{
    public long Id { get; set; }
    public long VentaDetalleId { get; set; }
    public int ImpuestoId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public decimal Porcentaje { get; set; }
    public bool IncluidoEnPrecio { get; set; }
    public decimal BaseImponible { get; set; }
    public decimal Monto { get; set; }
    public virtual VentasDetalle VentaDetalle { get; set; } = null!;
}
