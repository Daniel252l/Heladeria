using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class ArqueosCaja
{
    public long Id { get; set; }

    public long TurnoCajaId { get; set; }

    public DateTime Fecha { get; set; }

    public decimal MontoEsperado { get; set; }

    public decimal MontoContado { get; set; }

    public decimal? Diferencia { get; set; }

    public int UsuarioId { get; set; }

    public string? Observaciones { get; set; }

    public virtual ICollection<ArqueoCajaDetalles> ArqueoCajaDetalles { get; set; } = new List<ArqueoCajaDetalles>();

    public virtual TurnosCaja TurnoCaja { get; set; } = null!;

    public virtual Usuarios Usuario { get; set; } = null!;
}
