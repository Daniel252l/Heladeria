using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class ExistenciasLote
{
    public long Id { get; set; }

    public int EmpresaId { get; set; }

    public long ProductoId { get; set; }

    public long ExistenciaId { get; set; }

    public long LoteId { get; set; }

    public decimal Cantidad { get; set; }

    public decimal CantidadReservada { get; set; }

    public decimal? CantidadDisponible { get; set; }

    public DateTime? UltimoMovimiento { get; set; }

    public byte[] VersionFila { get; set; } = null!;

    public virtual Empresas Empresa { get; set; } = null!;

    public virtual Existencias Existencias { get; set; } = null!;

    public virtual Lotes Lotes { get; set; } = null!;
}
