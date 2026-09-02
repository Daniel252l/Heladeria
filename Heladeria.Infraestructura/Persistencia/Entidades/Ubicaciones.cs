using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class Ubicaciones
{
    public long Id { get; set; }

    public int EmpresaId { get; set; }

    public long BodegaId { get; set; }

    public long? UbicacionPadreId { get; set; }

    public string Codigo { get; set; } = null!;

    public string Nombre { get; set; } = null!;

    public long TipoUbicacionId { get; set; }

    public bool EsRecepcion { get; set; }

    public bool EsDespacho { get; set; }

    public int EstadoId { get; set; }

    public DateTime FechaCreacion { get; set; }

    public int CreadoPorUsuarioId { get; set; }

    public DateTime? FechaModificacion { get; set; }

    public int? ModificadoPorUsuarioId { get; set; }

    public byte[] VersionFila { get; set; } = null!;

    public virtual Bodegas Bodegas { get; set; } = null!;

    public virtual ICollection<ConteoFisicoDetalles> ConteoFisicoDetalles { get; set; } = new List<ConteoFisicoDetalles>();

    public virtual Usuarios CreadoPorUsuario { get; set; } = null!;

    public virtual Empresas Empresa { get; set; } = null!;

    public virtual Estados Estado { get; set; } = null!;

    public virtual ICollection<Existencias> Existencias { get; set; } = new List<Existencias>();

    public virtual ICollection<Ubicaciones> InverseUbicacionesNavigation { get; set; } = new List<Ubicaciones>();

    public virtual ICollection<Kardex> Kardex { get; set; } = new List<Kardex>();

    public virtual Usuarios? ModificadoPorUsuario { get; set; }

    public virtual ICollection<MovimientoDetalles> MovimientoDetallesUbicaciones { get; set; } = new List<MovimientoDetalles>();

    public virtual ICollection<MovimientoDetalles> MovimientoDetallesUbicacionesNavigation { get; set; } = new List<MovimientoDetalles>();

    public virtual ICollection<ParametrosStockProductos> ParametrosStockProductos { get; set; } = new List<ParametrosStockProductos>();

    public virtual ICollection<ReservaInventarioDetalles> ReservaInventarioDetalles { get; set; } = new List<ReservaInventarioDetalles>();

    public virtual ICollection<SeriesProductos> SeriesProductos { get; set; } = new List<SeriesProductos>();

    public virtual Catalogos TipoUbicacion { get; set; } = null!;

    public virtual Ubicaciones? UbicacionesNavigation { get; set; }
}
