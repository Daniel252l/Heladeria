using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class Permisos
{
    public long Id { get; set; }

    public string ModuloId { get; set; } = null!;

    public int AccionId { get; set; }

    public string Recurso { get; set; } = null!;

    public string Codigo { get; set; } = null!;

    public int EstadoId { get; set; }

    public virtual AccionesSistema Accion { get; set; } = null!;

    public virtual Estados Estado { get; set; } = null!;

    public virtual Modulos Modulo { get; set; } = null!;

    public virtual ICollection<RolesPermisos> RolesPermisos { get; set; } = new List<RolesPermisos>();
}
