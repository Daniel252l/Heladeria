using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class MovimientoDetalles
{
    public long Id { get; set; }

    public int EmpresaId { get; set; }

    public long MovimientoId { get; set; }

    public int NumeroLinea { get; set; }

    public long ProductoId { get; set; }

    public long? ProductoPresentacionId { get; set; }

    public int UnidadMedidaId { get; set; }

    public long? UbicacionOrigenId { get; set; }

    public long? UbicacionDestinoId { get; set; }

    public long? LoteId { get; set; }

    public decimal Cantidad { get; set; }

    public decimal FactorConversion { get; set; }

    public decimal CantidadBase { get; set; }

    public decimal CostoUnitario { get; set; }

    public decimal? CostoTotal { get; set; }

    public string? Observaciones { get; set; }

    public virtual Empresas Empresa { get; set; } = null!;

    public virtual ICollection<Kardex> Kardex { get; set; } = new List<Kardex>();

    public virtual Lotes? Lotes { get; set; }

    public virtual ICollection<MovimientoDetalleSeries> MovimientoDetalleSeries { get; set; } = new List<MovimientoDetalleSeries>();

    public virtual Movimientos Movimientos { get; set; } = null!;

    public virtual ProductoPresentaciones? ProductoPresentaciones { get; set; }

    public virtual Productos Productos { get; set; } = null!;

    public virtual Ubicaciones? Ubicaciones { get; set; }

    public virtual Ubicaciones? UbicacionesNavigation { get; set; }

    public virtual UnidadesMedida UnidadesMedida { get; set; } = null!;
}
