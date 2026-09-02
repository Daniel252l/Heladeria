using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class ConsumosProduccion
{
    public long Id { get; set; }

    public long OrdenProduccionId { get; set; }

    public long ProductoId { get; set; }

    public long? LoteId { get; set; }

    public int UnidadMedidaId { get; set; }

    public decimal CantidadConsumida { get; set; }

    public decimal CostoUnitario { get; set; }

    public DateTime FechaConsumo { get; set; }

    public virtual Lotes? Lote { get; set; }

    public virtual OrdenesProduccion OrdenProduccion { get; set; } = null!;

    public virtual Productos Producto { get; set; } = null!;

    public virtual UnidadesMedida UnidadMedida { get; set; } = null!;
}
