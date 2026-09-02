using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class Marcas
{
    public int Id { get; set; }

    public int EmpresaId { get; set; }

    public string Codigo { get; set; } = null!;

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public int EstadoId { get; set; }

    public DateTime FechaCreacion { get; set; }

    public int CreadoPorUsuarioId { get; set; }

    public DateTime? FechaModificacion { get; set; }

    public int? ModificadoPorUsuarioId { get; set; }

    public byte[] VersionFila { get; set; } = null!;

    public virtual Usuarios CreadoPorUsuario { get; set; } = null!;

    public virtual Empresas Empresa { get; set; } = null!;

    public virtual Estados Estado { get; set; } = null!;

    public virtual Usuarios? ModificadoPorUsuario { get; set; }

    public virtual ICollection<Productos> Productos { get; set; } = new List<Productos>();
}
