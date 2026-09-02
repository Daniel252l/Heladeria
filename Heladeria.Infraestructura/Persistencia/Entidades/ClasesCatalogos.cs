using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class ClasesCatalogos
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

    public virtual ICollection<Catalogos> Catalogos { get; set; } = new List<Catalogos>();

    public virtual Usuarios CreadoPorUsuario { get; set; } = null!;

    public virtual Estados Estado { get; set; } = null!;

    public virtual Usuarios? ModificadoPorUsuario { get; set; }
}
