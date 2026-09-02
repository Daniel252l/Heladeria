using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class ProgramasFidelidad
{
    public long Id { get; set; }

    public int EmpresaId { get; set; }

    public string Codigo { get; set; } = null!;

    public string Nombre { get; set; } = null!;

    public decimal MontoPorPunto { get; set; }

    public decimal ValorPunto { get; set; }

    public DateOnly FechaInicio { get; set; }

    public DateOnly? FechaFin { get; set; }

    public int EstadoId { get; set; }

    public virtual ICollection<ClientesFidelidad> ClientesFidelidad { get; set; } = new List<ClientesFidelidad>();

    public virtual Empresas Empresa { get; set; } = null!;

    public virtual Estados Estado { get; set; } = null!;

    public virtual ICollection<NivelesFidelidad> NivelesFidelidad { get; set; } = new List<NivelesFidelidad>();
}
