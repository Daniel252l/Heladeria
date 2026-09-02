using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class CuentasPorPagar
{
    public long Id { get; set; }

    public int EmpresaId { get; set; }

    public int ProveedorId { get; set; }

    public long FacturaProveedorId { get; set; }

    public DateOnly FechaEmision { get; set; }

    public DateOnly FechaVencimiento { get; set; }

    public decimal MontoOriginal { get; set; }

    public decimal SaldoPendiente { get; set; }

    public int FlujoEstadoId { get; set; }

    public int EstadoId { get; set; }

    public virtual ICollection<AplicacionesCuentasPorPagar> AplicacionesCuentasPorPagar { get; set; } = new List<AplicacionesCuentasPorPagar>();

    public virtual Empresas Empresa { get; set; } = null!;

    public virtual EstadosFlujo EstadosFlujo { get; set; } = null!;

    public virtual FacturasProveedores FacturaProveedor { get; set; } = null!;

    public virtual Proveedores Proveedor { get; set; } = null!;
}
