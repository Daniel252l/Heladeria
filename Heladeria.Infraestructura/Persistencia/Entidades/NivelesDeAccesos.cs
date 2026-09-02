using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class NivelesDeAccesos
{
    public int Id { get; set; }

    public string Nombre { get; set; } = null!;

    public virtual ICollection<Roles> Roles { get; set; } = new List<Roles>();
}
