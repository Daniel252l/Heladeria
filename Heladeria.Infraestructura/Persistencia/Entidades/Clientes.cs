using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class Clientes
{
    public int Id { get; set; }

    public int EmpresaId { get; set; }

    public string CodigoCliente { get; set; } = null!;

    public string NombreCliente { get; set; } = null!;

    public string? Nit { get; set; }

    public string? Telefono { get; set; }

    public string? Correo { get; set; }

    public decimal LimiteCredito { get; set; }

    public short DiasCredito { get; set; }

    public int? ListaPrecioId { get; set; }

    public int? EstadoCreditoId { get; set; }

    public int EstadoId { get; set; }

    public virtual ICollection<ClientesFidelidad> ClientesFidelidad { get; set; } = new List<ClientesFidelidad>();

    public virtual ICollection<ContactosClientes> ContactosClientes { get; set; } = new List<ContactosClientes>();

    public virtual ICollection<Cotizaciones> Cotizaciones { get; set; } = new List<Cotizaciones>();

    public virtual ICollection<CuentasPorCobrar> CuentasPorCobrar { get; set; } = new List<CuentasPorCobrar>();

    public virtual ICollection<DireccionesClientes> DireccionesClientes { get; set; } = new List<DireccionesClientes>();

    public virtual Empresas Empresa { get; set; } = null!;

    public virtual Estados Estado { get; set; } = null!;

    public virtual Estados? EstadoCredito { get; set; }

    public virtual ListasPrecios? ListaPrecio { get; set; }

    public virtual ICollection<NotasCredito> NotasCredito { get; set; } = new List<NotasCredito>();

    public virtual ICollection<NotasDebito> NotasDebito { get; set; } = new List<NotasDebito>();

    public virtual ICollection<Pagos> Pagos { get; set; } = new List<Pagos>();

    public virtual ICollection<PedidosVenta> PedidosVenta { get; set; } = new List<PedidosVenta>();

    public virtual ICollection<Sucursales> Sucursales { get; set; } = new List<Sucursales>();

    public virtual ICollection<Ventas> Ventas { get; set; } = new List<Ventas>();
}
