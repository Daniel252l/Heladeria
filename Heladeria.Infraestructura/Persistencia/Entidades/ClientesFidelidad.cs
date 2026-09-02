using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class ClientesFidelidad
{
    public long Id { get; set; }

    public int ClienteId { get; set; }

    public long ProgramaId { get; set; }

    public long? NivelId { get; set; }

    public int SaldoPuntos { get; set; }

    public int EstadoId { get; set; }

    public virtual Clientes Cliente { get; set; } = null!;

    public virtual Estados Estado { get; set; } = null!;

    public virtual ICollection<MovimientosPuntos> MovimientosPuntos { get; set; } = new List<MovimientosPuntos>();

    public virtual NivelesFidelidad? Nivel { get; set; }

    public virtual ProgramasFidelidad Programa { get; set; } = null!;
}
