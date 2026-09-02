using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class AplicacionesCuentasPorPagar
{
    public long Id { get; set; }

    public long CuentaPorPagarId { get; set; }

    public long PagoId { get; set; }

    public DateTime FechaAplicacion { get; set; }

    public decimal MontoAplicado { get; set; }

    public int UsuarioId { get; set; }

    public virtual CuentasPorPagar CuentaPorPagar { get; set; } = null!;

    public virtual Pagos Pago { get; set; } = null!;

    public virtual Usuarios Usuario { get; set; } = null!;
}
