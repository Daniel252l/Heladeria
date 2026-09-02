using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class ProduccionTerminada
{
    public long Id { get; set; }

    public long OrdenProduccionId { get; set; }

    public long ProductoId { get; set; }

    public long? LoteId { get; set; }

    public decimal CantidadProducida { get; set; }

    public decimal CostoMateriaPrima { get; set; }

    public decimal CostoManoObra { get; set; }

    public decimal CostoIndirecto { get; set; }

    public decimal? CostoTotal { get; set; }

    public DateTime FechaFinalizacion { get; set; }

    public virtual Lotes? Lote { get; set; }

    public virtual OrdenesProduccion OrdenProduccion { get; set; } = null!;

    public virtual Productos Producto { get; set; } = null!;
}
