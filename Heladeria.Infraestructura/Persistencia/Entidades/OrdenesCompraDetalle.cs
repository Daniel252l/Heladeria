using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class OrdenesCompraDetalle
{
    public long Id { get; set; }

    public long OrdenCompraId { get; set; }

    public int NumeroLinea { get; set; }

    public long ProductoId { get; set; }

    public long? ProductoPresentacionId { get; set; }

    public decimal Cantidad { get; set; }

    public decimal PrecioUnitario { get; set; }

    public decimal Descuento { get; set; }

    public decimal Impuesto { get; set; }

    public decimal? TotalLinea { get; set; }

    public virtual OrdenesCompra OrdenCompra { get; set; } = null!;

    public virtual Productos Producto { get; set; } = null!;

    public virtual ProductoPresentaciones? ProductoPresentacion { get; set; }

    public virtual ICollection<RecepcionCompraDetalles> RecepcionCompraDetalles { get; set; } = new List<RecepcionCompraDetalles>();
}
