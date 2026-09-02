using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class DevolucionVentaDetalles
{
    public long Id { get; set; }

    public long DevolucionVentaId { get; set; }

    public long VentaDetalleId { get; set; }

    public decimal Cantidad { get; set; }

    public decimal Monto { get; set; }

    public virtual DevolucionesVentas DevolucionVenta { get; set; } = null!;

    public virtual VentasDetalle VentaDetalle { get; set; } = null!;
}
