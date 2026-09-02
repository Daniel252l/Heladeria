using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class VentaImpuestos
{
    public long Id { get; set; }

    public long VentaId { get; set; }

    public int ImpuestoId { get; set; }

    public decimal Porcentaje { get; set; }

    public decimal BaseImponible { get; set; }

    public decimal Monto { get; set; }

    public virtual Impuestos Impuesto { get; set; } = null!;

    public virtual Ventas Venta { get; set; } = null!;
}
