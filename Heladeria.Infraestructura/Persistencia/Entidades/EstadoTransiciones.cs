using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class EstadoTransiciones
{
    public long Id { get; set; }

    public int FlujoEstadoId { get; set; }

    public int EstadoOrigenId { get; set; }

    public int EstadoDestinoId { get; set; }

    public bool RequiereAutorizacion { get; set; }

    public bool RequiereComentario { get; set; }

    public string? PermisoCodigo { get; set; }

    public int EstadoId { get; set; }

    public DateTime FechaCreacion { get; set; }

    public int CreadoPorUsuarioId { get; set; }

    public DateTime? FechaModificacion { get; set; }

    public int? ModificadoPorUsuarioId { get; set; }

    public byte[] VersionFila { get; set; } = null!;

    public virtual Usuarios CreadoPorUsuario { get; set; } = null!;

    public virtual Estados Estado { get; set; } = null!;

    public virtual EstadosFlujo EstadosFlujo { get; set; } = null!;

    public virtual EstadosFlujo EstadosFlujoNavigation { get; set; } = null!;

    public virtual FlujosEstados FlujoEstado { get; set; } = null!;

    public virtual Usuarios? ModificadoPorUsuario { get; set; }
}
