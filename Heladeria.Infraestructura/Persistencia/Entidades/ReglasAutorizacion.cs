using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class ReglasAutorizacion
{
    public long Id { get; set; }

    public int EmpresaId { get; set; }

    public string ModuloId { get; set; } = null!;

    public string Codigo { get; set; } = null!;

    public string Nombre { get; set; } = null!;

    public int TipoReglaId { get; set; }

    public decimal? ValorUmbral { get; set; }

    public string? RoleAutorizadorId { get; set; }

    public int EstadoId { get; set; }

    public virtual Empresas Empresa { get; set; } = null!;

    public virtual Estados Estado { get; set; } = null!;

    public virtual Modulos Modulo { get; set; } = null!;

    public virtual Roles? RoleAutorizador { get; set; }

    public virtual ICollection<SolicitudesAutorizacion> SolicitudesAutorizacion { get; set; } = new List<SolicitudesAutorizacion>();

    public virtual Tipos TipoRegla { get; set; } = null!;
}
