using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class ConceptosMovimientos
{
    public int Id { get; set; }

    public int EmpresaId { get; set; }

    public string Codigo { get; set; } = null!;

    public string Nombre { get; set; } = null!;

    public string TipoOperacion { get; set; } = null!;

    public bool RequiereReferencia { get; set; }

    public bool AfectaCosto { get; set; }

    public bool PermiteCostoCero { get; set; }

    public bool EsAutomatico { get; set; }

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

    public virtual ICollection<Movimientos> Movimientos { get; set; } = new List<Movimientos>();
}
