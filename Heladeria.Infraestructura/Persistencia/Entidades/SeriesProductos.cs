using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class SeriesProductos
{
    public long Id { get; set; }

    public int EmpresaId { get; set; }

    public long ProductoId { get; set; }

    public long? LoteId { get; set; }

    public string NumeroSerie { get; set; } = null!;

    public int FlujoEstadoId { get; set; }

    public int EstadoId { get; set; }

    public long? BodegaId { get; set; }

    public long? UbicacionId { get; set; }

    public DateTime? FechaIngreso { get; set; }

    public DateTime? FechaSalida { get; set; }

    public DateTime FechaCreacion { get; set; }

    public int CreadoPorUsuarioId { get; set; }

    public byte[] VersionFila { get; set; } = null!;

    public virtual Bodegas? Bodegas { get; set; }

    public virtual ICollection<ConteoFisicoDetalles> ConteoFisicoDetalles { get; set; } = new List<ConteoFisicoDetalles>();

    public virtual Usuarios CreadoPorUsuario { get; set; } = null!;

    public virtual Empresas Empresa { get; set; } = null!;

    public virtual EstadosFlujo EstadosFlujo { get; set; } = null!;

    public virtual Lotes? Lotes { get; set; }

    public virtual ICollection<MovimientoDetalleSeries> MovimientoDetalleSeries { get; set; } = new List<MovimientoDetalleSeries>();

    public virtual Productos Productos { get; set; } = null!;

    public virtual ICollection<ReservaInventarioDetalles> ReservaInventarioDetalles { get; set; } = new List<ReservaInventarioDetalles>();

    public virtual Ubicaciones? Ubicaciones { get; set; }
}
