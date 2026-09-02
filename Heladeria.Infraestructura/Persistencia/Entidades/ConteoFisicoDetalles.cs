using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class ConteoFisicoDetalles
{
    public long Id { get; set; }

    public int EmpresaId { get; set; }

    public long ConteoFisicoId { get; set; }

    public int NumeroLinea { get; set; }

    public long ProductoId { get; set; }

    public long UbicacionId { get; set; }

    public long? LoteId { get; set; }

    public long? SerieProductoId { get; set; }

    public decimal ExistenciaSistema { get; set; }

    public decimal? CantidadPrimerConteo { get; set; }

    public decimal? CantidadReconteo { get; set; }

    public decimal? CantidadFinal { get; set; }

    public decimal? Diferencia { get; set; }

    public int? ContadoPorUsuarioId { get; set; }

    public DateTime? FechaConteo { get; set; }

    public string? Observaciones { get; set; }

    public virtual Usuarios? ContadoPorUsuario { get; set; }

    public virtual ConteosFisicos ConteosFisicos { get; set; } = null!;

    public virtual Empresas Empresa { get; set; } = null!;

    public virtual Lotes? Lote { get; set; }

    public virtual Productos Producto { get; set; } = null!;

    public virtual SeriesProductos? SerieProducto { get; set; }

    public virtual Ubicaciones Ubicacion { get; set; } = null!;
}
