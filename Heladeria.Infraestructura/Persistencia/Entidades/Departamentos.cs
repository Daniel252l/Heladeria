using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class Departamentos
{
    public int Id { get; set; }

    public string Nombre { get; set; } = null!;

    public string Codigo { get; set; } = null!;

    public virtual ICollection<Municipios> Municipios { get; set; } = new List<Municipios>();

    public virtual ICollection<Sucursales> Sucursales { get; set; } = new List<Sucursales>();
}
