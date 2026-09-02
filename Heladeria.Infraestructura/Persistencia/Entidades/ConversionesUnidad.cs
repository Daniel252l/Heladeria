using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class ConversionesUnidad
{
    public long Id { get; set; }

    public int EmpresaId { get; set; }

    public int UnidadOrigenId { get; set; }

    public int UnidadDestinoId { get; set; }

    public decimal Factor { get; set; }

    public int EstadoId { get; set; }

    public virtual Empresas Empresa { get; set; } = null!;

    public virtual Estados Estado { get; set; } = null!;

    public virtual UnidadesMedida UnidadDestino { get; set; } = null!;

    public virtual UnidadesMedida UnidadOrigen { get; set; } = null!;
}
