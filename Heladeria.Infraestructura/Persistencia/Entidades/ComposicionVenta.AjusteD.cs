namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class VentasDetalle
{
    // Snapshot del modo de descarga; null identifica líneas anteriores al Ajuste D.
    public bool? EsCompuesto { get; set; }
}

public partial class MovimientoDetalles
{
    public long? VentaDetalleId { get; set; }
    public int? UnidadBaseId { get; set; }
    public virtual VentasDetalle? VentaDetalle { get; set; }
}
