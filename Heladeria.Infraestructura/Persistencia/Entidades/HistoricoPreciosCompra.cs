using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class HistoricoPreciosCompra
{
    public long Id { get; set; }

    public int EmpresaId { get; set; }

    public int ProveedorId { get; set; }

    public long ProductoId { get; set; }

    public decimal PrecioCompra { get; set; }

    public int MonedaId { get; set; }

    public DateTime Fecha { get; set; }

    public long? RecepcionCompraId { get; set; }

    public virtual Empresas Empresa { get; set; } = null!;

    public virtual Monedas Moneda { get; set; } = null!;

    public virtual Productos Producto { get; set; } = null!;

    public virtual Proveedores Proveedor { get; set; } = null!;

    public virtual RecepcionesCompra? RecepcionCompra { get; set; }
}
