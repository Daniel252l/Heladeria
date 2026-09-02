using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class Kardex
{
    public long Id { get; set; }

    public int EmpresaId { get; set; }

    public long ProductoId { get; set; }

    public long BodegaId { get; set; }

    public long UbicacionId { get; set; }

    public long? LoteId { get; set; }

    public long MovimientoDetalleId { get; set; }

    public DateTime FechaMovimiento { get; set; }

    public string TipoAfectacion { get; set; } = null!;

    public decimal Cantidad { get; set; }

    public decimal CostoUnitario { get; set; }

    public decimal? CostoTotal { get; set; }

    public decimal SaldoCantidad { get; set; }

    public decimal SaldoCosto { get; set; }

    public DateTime FechaRegistro { get; set; }

    public virtual Bodegas Bodegas { get; set; } = null!;

    public virtual Empresas Empresa { get; set; } = null!;

    public virtual Lotes? Lotes { get; set; }

    public virtual MovimientoDetalles MovimientoDetalles { get; set; } = null!;

    public virtual Productos Productos { get; set; } = null!;

    public virtual Ubicaciones Ubicaciones { get; set; } = null!;
}
