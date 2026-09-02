using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class Ventas
{
    public long Id { get; set; }

    public int EmpresaId { get; set; }

    public long SucursalId { get; set; }

    public long? TurnoCajaId { get; set; }

    public int? ClienteId { get; set; }

    public long? PedidoVentaId { get; set; }

    public string NumeroFactura { get; set; } = null!;

    public DateTime FechaVenta { get; set; }

    public int MonedaId { get; set; }

    public decimal TipoCambio { get; set; }

    public bool EsCredito { get; set; }

    public decimal SubTotal { get; set; }

    public decimal Descuento { get; set; }

    public decimal Impuestos { get; set; }

    public decimal TotalVenta { get; set; }

    public int FlujoEstadoId { get; set; }

    public int EstadoId { get; set; }

    public int UsuarioCreacionId { get; set; }

    public virtual Clientes? Cliente { get; set; }

    public virtual CuentasPorCobrar? CuentasPorCobrar { get; set; }

    public virtual ICollection<DevolucionesVentas> DevolucionesVentas { get; set; } = new List<DevolucionesVentas>();

    public virtual Empresas Empresa { get; set; } = null!;

    public virtual EstadosFlujo EstadosFlujo { get; set; } = null!;

    public virtual Monedas Moneda { get; set; } = null!;

    public virtual ICollection<MovimientosPuntos> MovimientosPuntos { get; set; } = new List<MovimientosPuntos>();

    public virtual ICollection<NotasCredito> NotasCredito { get; set; } = new List<NotasCredito>();

    public virtual ICollection<NotasDebito> NotasDebito { get; set; } = new List<NotasDebito>();

    public virtual PedidosVenta? PedidoVenta { get; set; }

    public virtual Sucursales Sucursal { get; set; } = null!;

    public virtual TurnosCaja? TurnoCaja { get; set; }

    public virtual Usuarios UsuarioCreacion { get; set; } = null!;

    public virtual ICollection<VentaImpuestos> VentaImpuestos { get; set; } = new List<VentaImpuestos>();

    public virtual ICollection<VentasDetalle> VentasDetalle { get; set; } = new List<VentasDetalle>();
}
