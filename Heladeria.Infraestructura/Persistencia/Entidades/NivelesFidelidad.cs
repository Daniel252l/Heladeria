using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class NivelesFidelidad
{
    public long Id { get; set; }

    public long ProgramaId { get; set; }

    public string Nombre { get; set; } = null!;

    public int PuntosMinimos { get; set; }

    public decimal Multiplicador { get; set; }

    public int EstadoId { get; set; }

    public virtual ICollection<ClientesFidelidad> ClientesFidelidad { get; set; } = new List<ClientesFidelidad>();

    public virtual Estados Estado { get; set; } = null!;

    public virtual ProgramasFidelidad Programa { get; set; } = null!;
}
