using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class RolesPermisos
{
    public long Id { get; set; }

    public string RoleId { get; set; } = null!;

    public long PermisoId { get; set; }

    public bool Permitido { get; set; }

    public virtual Permisos Permiso { get; set; } = null!;

    public virtual Roles Role { get; set; } = null!;
}
