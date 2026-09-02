using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class CuentasPorCobrar
{
    public long Id { get; set; }

    public int EmpresaId { get; set; }

    public int ClienteId { get; set; }

    public long VentaId { get; set; }

    public DateOnly FechaEmision { get; set; }

    public DateOnly FechaVencimiento { get; set; }

    public decimal MontoOriginal { get; set; }

    public decimal SaldoPendiente { get; set; }

    public int FlujoEstadoId { get; set; }

    public int EstadoId { get; set; }

    public virtual ICollection<AplicacionesCuentasPorCobrar> AplicacionesCuentasPorCobrar { get; set; } = new List<AplicacionesCuentasPorCobrar>();

    public virtual Clientes Cliente { get; set; } = null!;

    public virtual Empresas Empresa { get; set; } = null!;

    public virtual EstadosFlujo EstadosFlujo { get; set; } = null!;

    public virtual Ventas Venta { get; set; } = null!;
}
