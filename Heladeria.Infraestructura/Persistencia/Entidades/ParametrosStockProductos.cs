using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class ParametrosStockProductos
{
    public long Id { get; set; }

    public int EmpresaId { get; set; }

    public long ProductoId { get; set; }

    public long BodegaId { get; set; }

    public long? UbicacionPredeterminadaId { get; set; }

    public decimal StockMinimo { get; set; }

    public decimal? StockMaximo { get; set; }

    public decimal PuntoReorden { get; set; }

    public decimal? CantidadReorden { get; set; }

    public short? DiasReposicion { get; set; }

    public int EstadoId { get; set; }

    public DateTime FechaCreacion { get; set; }

    public int CreadoPorUsuarioId { get; set; }

    public DateTime? FechaModificacion { get; set; }

    public int? ModificadoPorUsuarioId { get; set; }

    public byte[] VersionFila { get; set; } = null!;

    public virtual Bodegas Bodegas { get; set; } = null!;

    public virtual Usuarios CreadoPorUsuario { get; set; } = null!;

    public virtual Empresas Empresa { get; set; } = null!;

    public virtual Estados Estado { get; set; } = null!;

    public virtual Usuarios? ModificadoPorUsuario { get; set; }

    public virtual Productos Productos { get; set; } = null!;

    public virtual Ubicaciones? Ubicaciones { get; set; }
}
