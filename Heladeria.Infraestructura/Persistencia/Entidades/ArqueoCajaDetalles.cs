using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class ArqueoCajaDetalles
{
    public long Id { get; set; }

    public long ArqueoCajaId { get; set; }

    public long DenominacionId { get; set; }

    public int Cantidad { get; set; }

    public decimal Monto { get; set; }

    public virtual ArqueosCaja ArqueoCaja { get; set; } = null!;

    public virtual DenominacionesMoneda Denominacion { get; set; } = null!;
}
