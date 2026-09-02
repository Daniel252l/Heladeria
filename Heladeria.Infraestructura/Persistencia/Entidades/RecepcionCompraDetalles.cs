using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class RecepcionCompraDetalles
{
    public long Id { get; set; }

    public long RecepcionCompraId { get; set; }

    public long OrdenCompraDetalleId { get; set; }

    public long ProductoId { get; set; }

    public decimal CantidadRecibida { get; set; }

    public long? LoteId { get; set; }

    public DateOnly? FechaVencimiento { get; set; }

    public decimal CostoUnitario { get; set; }

    public virtual Lotes? Lote { get; set; }

    public virtual OrdenesCompraDetalle OrdenCompraDetalle { get; set; } = null!;

    public virtual Productos Producto { get; set; } = null!;

    public virtual RecepcionesCompra RecepcionCompra { get; set; } = null!;
}
