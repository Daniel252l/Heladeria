using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class ClaseTipos
{
    public int Id { get; set; }

    public string Nombre { get; set; } = null!;

    public int EstadoId { get; set; }

    public virtual Estados Estado { get; set; } = null!;

    public virtual ICollection<Tipos> Tipos { get; set; } = new List<Tipos>();
}
