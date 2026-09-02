using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class MovimientoDetalleSeries
{
    public long Id { get; set; }

    public int EmpresaId { get; set; }

    public long MovimientoDetalleId { get; set; }

    public long SerieProductoId { get; set; }

    public virtual Empresas Empresa { get; set; } = null!;

    public virtual MovimientoDetalles MovimientoDetalles { get; set; } = null!;

    public virtual SeriesProductos SeriesProductos { get; set; } = null!;
}
