using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class Modulos
{
    public string Id { get; set; } = null!;

    public string Nombre { get; set; } = null!;

    public string Icono { get; set; } = null!;

    public bool Activo { get; set; }

    public virtual ICollection<Bitacoras> Bitacoras { get; set; } = new List<Bitacoras>();

    public virtual ICollection<Configuraciones> Configuraciones { get; set; } = new List<Configuraciones>();

    public virtual ICollection<DocumentosAdjuntos> DocumentosAdjuntos { get; set; } = new List<DocumentosAdjuntos>();

    public virtual ICollection<Estados> Estados { get; set; } = new List<Estados>();

    public virtual ICollection<Permisos> Permisos { get; set; } = new List<Permisos>();

    public virtual ICollection<ReglasAutorizacion> ReglasAutorizacion { get; set; } = new List<ReglasAutorizacion>();

    public virtual ICollection<Reportes> Reportes { get; set; } = new List<Reportes>();

    public virtual ICollection<TipoNotificaciones> TipoNotificaciones { get; set; } = new List<TipoNotificaciones>();

    public virtual ICollection<Tipos> Tipos { get; set; } = new List<Tipos>();
}
