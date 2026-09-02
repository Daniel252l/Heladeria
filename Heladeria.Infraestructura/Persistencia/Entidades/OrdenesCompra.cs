using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class OrdenesCompra
{
    public long Id { get; set; }

    public int EmpresaId { get; set; }

    public long SucursalId { get; set; }

    public int ProveedorId { get; set; }

    public string NumeroOrden { get; set; } = null!;

    public DateTime FechaEmision { get; set; }

    public DateTime? FechaEntregaEsperada { get; set; }

    public int MonedaId { get; set; }

    public decimal TipoCambio { get; set; }

    public decimal SubTotal { get; set; }

    public decimal Impuestos { get; set; }

    public decimal TotalEstimado { get; set; }

    public int FlujoEstadoId { get; set; }

    public int EstadoId { get; set; }

    public int UsuarioCreacionId { get; set; }

    public virtual Empresas Empresa { get; set; } = null!;

    public virtual EstadosFlujo EstadosFlujo { get; set; } = null!;

    public virtual ICollection<FacturasProveedores> FacturasProveedores { get; set; } = new List<FacturasProveedores>();

    public virtual Monedas Moneda { get; set; } = null!;

    public virtual ICollection<OrdenesCompraDetalle> OrdenesCompraDetalle { get; set; } = new List<OrdenesCompraDetalle>();

    public virtual Proveedores Proveedor { get; set; } = null!;

    public virtual ICollection<RecepcionesCompra> RecepcionesCompra { get; set; } = new List<RecepcionesCompra>();

    public virtual Sucursales Sucursal { get; set; } = null!;

    public virtual Usuarios UsuarioCreacion { get; set; } = null!;
}
