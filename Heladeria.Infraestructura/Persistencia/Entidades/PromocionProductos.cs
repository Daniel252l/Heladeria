using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class PromocionProductos
{
    public long Id { get; set; }

    public long PromocionId { get; set; }

    public long ProductoId { get; set; }

    public bool EsRequerido { get; set; }

    public virtual Productos Producto { get; set; } = null!;

    public virtual Promociones Promocion { get; set; } = null!;
}
