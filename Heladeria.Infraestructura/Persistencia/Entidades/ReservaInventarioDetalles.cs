using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class ReservaInventarioDetalles
{
    public long Id { get; set; }

    public int EmpresaId { get; set; }

    public long ReservaId { get; set; }

    public int NumeroLinea { get; set; }

    public long ProductoId { get; set; }

    public long BodegaId { get; set; }

    public long UbicacionId { get; set; }

    public long? LoteId { get; set; }

    public long? SerieProductoId { get; set; }

    public decimal Cantidad { get; set; }

    public decimal CantidadAplicada { get; set; }

    public decimal CantidadLiberada { get; set; }

    public byte[] VersionFila { get; set; } = null!;

    public virtual Bodegas Bodega { get; set; } = null!;

    public virtual Empresas Empresa { get; set; } = null!;

    public virtual Lotes? Lote { get; set; }

    public virtual Productos Producto { get; set; } = null!;

    public virtual ReservasInventario ReservasInventario { get; set; } = null!;

    public virtual SeriesProductos? SerieProducto { get; set; }

    public virtual Ubicaciones Ubicacion { get; set; } = null!;
}
