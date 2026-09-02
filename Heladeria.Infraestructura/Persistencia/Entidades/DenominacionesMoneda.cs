using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class DenominacionesMoneda
{
    public long Id { get; set; }

    public int MonedaId { get; set; }

    public decimal Valor { get; set; }

    public int? TipoId { get; set; }

    public int EstadoId { get; set; }

    public virtual ICollection<ArqueoCajaDetalles> ArqueoCajaDetalles { get; set; } = new List<ArqueoCajaDetalles>();

    public virtual Estados Estado { get; set; } = null!;

    public virtual Monedas Moneda { get; set; } = null!;

    public virtual Tipos? Tipo { get; set; }
}
