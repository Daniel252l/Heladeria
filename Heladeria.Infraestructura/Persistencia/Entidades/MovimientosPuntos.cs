using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class MovimientosPuntos
{
    public long Id { get; set; }

    public long ClienteFidelidadId { get; set; }

    public long? VentaId { get; set; }

    public int TipoMovimientoId { get; set; }

    public int Puntos { get; set; }

    public DateTime FechaMovimiento { get; set; }

    public string? Descripcion { get; set; }

    public virtual ClientesFidelidad ClienteFidelidad { get; set; } = null!;

    public virtual Tipos TipoMovimiento { get; set; } = null!;

    public virtual Ventas? Venta { get; set; }
}
