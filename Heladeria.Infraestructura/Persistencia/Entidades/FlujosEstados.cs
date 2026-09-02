using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class FlujosEstados
{
    public int Id { get; set; }

    public string Codigo { get; set; } = null!;

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public int EstadoId { get; set; }

    public DateTime FechaCreacion { get; set; }

    public int CreadoPorUsuarioId { get; set; }

    public DateTime? FechaModificacion { get; set; }

    public int? ModificadoPorUsuarioId { get; set; }

    public virtual Usuarios CreadoPorUsuario { get; set; } = null!;

    public virtual Estados Estado { get; set; } = null!;

    public virtual ICollection<EstadoTransiciones> EstadoTransiciones { get; set; } = new List<EstadoTransiciones>();

    public virtual ICollection<EstadosFlujo> EstadosFlujo { get; set; } = new List<EstadosFlujo>();

    public virtual Usuarios? ModificadoPorUsuario { get; set; }
}
