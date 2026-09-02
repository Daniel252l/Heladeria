using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class PromocionHorarios
{
    public long Id { get; set; }

    public long PromocionId { get; set; }

    public byte DiaSemana { get; set; }

    public TimeOnly HoraInicio { get; set; }

    public TimeOnly HoraFin { get; set; }

    public virtual Promociones Promocion { get; set; } = null!;
}
