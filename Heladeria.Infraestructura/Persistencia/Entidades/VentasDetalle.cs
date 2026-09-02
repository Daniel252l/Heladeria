using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class VentasDetalle
{
    public long Id { get; set; }

    public long VentaId { get; set; }

    public int NumeroLinea { get; set; }

    public long ProductoId { get; set; }

    public long? ProductoPresentacionId { get; set; }

    public decimal Cantidad { get; set; }

    public decimal PrecioUnitario { get; set; }

    public decimal PorcentajeDescuento { get; set; }

    public decimal MontoDescuento { get; set; }

    public decimal Impuesto { get; set; }

    public decimal? SubTotal { get; set; }

    public decimal? CostoUnitario { get; set; }

    public virtual ICollection<DevolucionVentaDetalles> DevolucionVentaDetalles { get; set; } = new List<DevolucionVentaDetalles>();

    public virtual Productos Producto { get; set; } = null!;

    public virtual ProductoPresentaciones? ProductoPresentacion { get; set; }

    public virtual Ventas Venta { get; set; } = null!;
}
