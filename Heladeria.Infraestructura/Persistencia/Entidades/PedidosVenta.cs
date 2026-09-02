using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class PedidosVenta
{
    public long Id { get; set; }

    public int EmpresaId { get; set; }

    public long SucursalId { get; set; }

    public int? ClienteId { get; set; }

    public long? CotizacionId { get; set; }

    public string NumeroPedido { get; set; } = null!;

    public DateTime FechaPedido { get; set; }

    public DateTime? FechaEntrega { get; set; }

    public decimal Total { get; set; }

    public int FlujoEstadoId { get; set; }

    public int EstadoId { get; set; }

    public int CreadoPorUsuarioId { get; set; }

    public virtual Clientes? Cliente { get; set; }

    public virtual Cotizaciones? Cotizacion { get; set; }

    public virtual Usuarios CreadoPorUsuario { get; set; } = null!;

    public virtual Empresas Empresa { get; set; } = null!;

    public virtual EstadosFlujo EstadosFlujo { get; set; } = null!;

    public virtual ICollection<PedidoVentaDetalles> PedidoVentaDetalles { get; set; } = new List<PedidoVentaDetalles>();

    public virtual Sucursales Sucursal { get; set; } = null!;

    public virtual ICollection<Ventas> Ventas { get; set; } = new List<Ventas>();
}
