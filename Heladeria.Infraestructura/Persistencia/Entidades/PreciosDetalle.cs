using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class PreciosDetalle
{
    public long Id { get; set; }

    public int ListaPrecioId { get; set; }

    public long ProductoId { get; set; }

    public long? ProductoPresentacionId { get; set; }

    public decimal PrecioVenta { get; set; }

    public virtual ListasPrecios ListaPrecio { get; set; } = null!;

    public virtual Productos Producto { get; set; } = null!;

    public virtual ProductoPresentaciones? ProductoPresentacion { get; set; }
}
