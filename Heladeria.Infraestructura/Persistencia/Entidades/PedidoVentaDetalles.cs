using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class PedidoVentaDetalles
{
    public long Id { get; set; }

    public long PedidoVentaId { get; set; }

    public int NumeroLinea { get; set; }

    public long ProductoId { get; set; }

    public long? ProductoPresentacionId { get; set; }

    public decimal Cantidad { get; set; }

    public decimal PrecioUnitario { get; set; }

    public decimal Descuento { get; set; }

    public decimal Impuesto { get; set; }

    public decimal? TotalLinea { get; set; }

    public virtual PedidosVenta PedidoVenta { get; set; } = null!;

    public virtual Productos Producto { get; set; } = null!;

    public virtual ProductoPresentaciones? ProductoPresentacion { get; set; }
}
