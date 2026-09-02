using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class ProductoImpuestos
{
    public long Id { get; set; }

    public long ProductoId { get; set; }

    public int ImpuestoId { get; set; }

    public int EstadoId { get; set; }

    public virtual Estados Estado { get; set; } = null!;

    public virtual Impuestos Impuesto { get; set; } = null!;

    public virtual Productos Producto { get; set; } = null!;
}
