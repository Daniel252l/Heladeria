using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class Reportes
{
    public string Id { get; set; } = null!;

    public string ModuloId { get; set; } = null!;

    public string Descripcion { get; set; } = null!;

    public int EstadoId { get; set; }

    public int? ClienteId { get; set; }

    public string? Comentario { get; set; }

    public virtual Tipos? Cliente { get; set; }

    public virtual Estados Estado { get; set; } = null!;

    public virtual Modulos Modulo { get; set; } = null!;
}
