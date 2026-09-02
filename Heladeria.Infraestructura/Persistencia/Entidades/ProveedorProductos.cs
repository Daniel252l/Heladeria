using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class ProveedorProductos
{
    public long Id { get; set; }

    public int ProveedorId { get; set; }

    public long ProductoId { get; set; }

    public string? CodigoProveedor { get; set; }

    public decimal? PrecioUltimaCompra { get; set; }

    public bool EsPreferido { get; set; }

    public int EstadoId { get; set; }

    public virtual Estados Estado { get; set; } = null!;

    public virtual Productos Producto { get; set; } = null!;

    public virtual Proveedores Proveedor { get; set; } = null!;
}
