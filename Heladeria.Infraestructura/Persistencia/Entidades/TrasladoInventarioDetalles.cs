using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class TrasladoInventarioDetalles
{
    public long Id { get; set; }

    public long TrasladoId { get; set; }

    public long ProductoId { get; set; }

    public int UnidadMedidaId { get; set; }

    public long? LoteId { get; set; }

    public decimal Cantidad { get; set; }

    public virtual Lotes? Lote { get; set; }

    public virtual Productos Producto { get; set; } = null!;

    public virtual TrasladosInventario Traslado { get; set; } = null!;

    public virtual UnidadesMedida UnidadMedida { get; set; } = null!;
}
