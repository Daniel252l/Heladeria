using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class SolicitudesAutorizacion
{
    public long Id { get; set; }

    public long ReglaAutorizacionId { get; set; }

    public string Entidad { get; set; } = null!;

    public long EntidadId { get; set; }

    public string? Motivo { get; set; }

    public int SolicitadoPorUsuarioId { get; set; }

    public DateTime FechaSolicitud { get; set; }

    public int? AutorizadoPorUsuarioId { get; set; }

    public DateTime? FechaResolucion { get; set; }

    public int FlujoEstadoId { get; set; }

    public int EstadoId { get; set; }

    public string? ComentarioResolucion { get; set; }

    public virtual Usuarios? AutorizadoPorUsuario { get; set; }

    public virtual EstadosFlujo EstadosFlujo { get; set; } = null!;

    public virtual ReglasAutorizacion ReglaAutorizacion { get; set; } = null!;

    public virtual Usuarios SolicitadoPorUsuario { get; set; } = null!;
}
