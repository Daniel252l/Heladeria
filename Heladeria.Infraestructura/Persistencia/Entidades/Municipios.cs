using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class Municipios
{
    public int Id { get; set; }

    public int DepartamentoId { get; set; }

    public string Nombre { get; set; } = null!;

    public string Codigo { get; set; } = null!;

    public virtual Departamentos Departamento { get; set; } = null!;

    public virtual ICollection<DireccionesClientes> DireccionesClientes { get; set; } = new List<DireccionesClientes>();

    public virtual ICollection<Sucursales> Sucursales { get; set; } = new List<Sucursales>();
}
