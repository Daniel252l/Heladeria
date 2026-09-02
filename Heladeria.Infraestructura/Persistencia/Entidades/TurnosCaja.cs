using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class TurnosCaja
{
    public long Id { get; set; }

    public long CajaId { get; set; }

    public int UsuarioId { get; set; }

    public DateTime FechaApertura { get; set; }

    public decimal MontoInicial { get; set; }

    public DateTime? FechaCierre { get; set; }

    public decimal? MontoEsperado { get; set; }

    public decimal? MontoFisico { get; set; }

    public decimal? Diferencia { get; set; }

    public int FlujoEstadoId { get; set; }

    public int EstadoId { get; set; }

    public virtual ICollection<ArqueosCaja> ArqueosCaja { get; set; } = new List<ArqueosCaja>();

    public virtual Cajas Caja { get; set; } = null!;

    public virtual EstadosFlujo EstadosFlujo { get; set; } = null!;

    public virtual ICollection<MovimientosCaja> MovimientosCaja { get; set; } = new List<MovimientosCaja>();

    public virtual ICollection<Pagos> Pagos { get; set; } = new List<Pagos>();

    public virtual Usuarios Usuario { get; set; } = null!;

    public virtual ICollection<Ventas> Ventas { get; set; } = new List<Ventas>();
}
