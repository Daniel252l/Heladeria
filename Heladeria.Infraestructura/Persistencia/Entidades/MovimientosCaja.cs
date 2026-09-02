using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class MovimientosCaja
{
    public long Id { get; set; }

    public long TurnoCajaId { get; set; }

    public int TipoMovimientoId { get; set; }

    public DateTime Fecha { get; set; }

    public decimal Monto { get; set; }

    public int? FormaPagoId { get; set; }

    public string? Referencia { get; set; }

    public string? Descripcion { get; set; }

    public int UsuarioId { get; set; }

    public virtual FormasPago? FormaPago { get; set; }

    public virtual Tipos TipoMovimiento { get; set; } = null!;

    public virtual TurnosCaja TurnoCaja { get; set; } = null!;

    public virtual Usuarios Usuario { get; set; } = null!;
}
