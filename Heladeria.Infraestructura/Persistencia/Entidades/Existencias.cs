using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class Existencias
{
    public long Id { get; set; }

    public int EmpresaId { get; set; }

    public long ProductoId { get; set; }

    public long BodegaId { get; set; }

    public long UbicacionId { get; set; }

    public decimal Cantidad { get; set; }

    public decimal CantidadReservada { get; set; }

    public decimal? CantidadDisponible { get; set; }

    public decimal CostoPromedio { get; set; }

    public DateTime? UltimoMovimiento { get; set; }

    public byte[] VersionFila { get; set; } = null!;

    public virtual Bodegas Bodegas { get; set; } = null!;

    public virtual Empresas Empresa { get; set; } = null!;

    public virtual ICollection<ExistenciasLote> ExistenciasLote { get; set; } = new List<ExistenciasLote>();

    public virtual Productos Productos { get; set; } = null!;

    public virtual Ubicaciones Ubicaciones { get; set; } = null!;
}
