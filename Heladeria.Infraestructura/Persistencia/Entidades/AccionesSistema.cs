using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class AccionesSistema
{
    public int Id { get; set; }

    public string Codigo { get; set; } = null!;

    public string Nombre { get; set; } = null!;

    public int EstadoId { get; set; }

    public virtual Estados Estado { get; set; } = null!;

    public virtual ICollection<Permisos> Permisos { get; set; } = new List<Permisos>();
}
