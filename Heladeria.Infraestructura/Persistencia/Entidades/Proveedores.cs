using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class Proveedores
{
    public int Id { get; set; }

    public int EmpresaId { get; set; }

    public string CodigoProveedor { get; set; } = null!;

    public string NombreProveedor { get; set; } = null!;

    public string? Nit { get; set; }

    public string? Telefono { get; set; }

    public string? Correo { get; set; }

    public string? Direccion { get; set; }

    public short DiasCredito { get; set; }

    public decimal? LimiteCredito { get; set; }

    public int? MonedaId { get; set; }

    public int EstadoId { get; set; }

    public virtual ICollection<ContactosProveedores> ContactosProveedores { get; set; } = new List<ContactosProveedores>();

    public virtual ICollection<CuentasPorPagar> CuentasPorPagar { get; set; } = new List<CuentasPorPagar>();

    public virtual Empresas Empresa { get; set; } = null!;

    public virtual Estados Estado { get; set; } = null!;

    public virtual ICollection<FacturasProveedores> FacturasProveedores { get; set; } = new List<FacturasProveedores>();

    public virtual ICollection<HistoricoPreciosCompra> HistoricoPreciosCompra { get; set; } = new List<HistoricoPreciosCompra>();

    public virtual Monedas? Moneda { get; set; }

    public virtual ICollection<OrdenesCompra> OrdenesCompra { get; set; } = new List<OrdenesCompra>();

    public virtual ICollection<Pagos> Pagos { get; set; } = new List<Pagos>();

    public virtual ICollection<ProveedorProductos> ProveedorProductos { get; set; } = new List<ProveedorProductos>();

    public virtual ICollection<Sucursales> Sucursales { get; set; } = new List<Sucursales>();
}
