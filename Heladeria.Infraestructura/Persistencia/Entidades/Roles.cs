using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class Roles
{
    public string Id { get; set; } = null!;

    public string Nombre { get; set; } = null!;

    public int NivelDeAccesoId { get; set; }

    public int CambioDeContrasenia { get; set; }

    public int TipoId { get; set; }

    public bool AplicarConfiguracionesPorRol { get; set; }

    public virtual ICollection<Empresas> Empresas { get; set; } = new List<Empresas>();

    public virtual NivelesDeAccesos NivelDeAcceso { get; set; } = null!;

    public virtual ICollection<ReglasAutorizacion> ReglasAutorizacion { get; set; } = new List<ReglasAutorizacion>();

    public virtual ICollection<RolesPermisos> RolesPermisos { get; set; } = new List<RolesPermisos>();

    public virtual Tipos Tipo { get; set; } = null!;

    public virtual ICollection<Usuarios> Usuarios { get; set; } = new List<Usuarios>();
}
