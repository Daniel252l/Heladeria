using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class Bodegas
{
    public long Id { get; set; }

    public int EmpresaId { get; set; }

    public long SucursalId { get; set; }

    public long? BodegaPadreId { get; set; }

    public string Codigo { get; set; } = null!;

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public bool PermiteExistenciaNegativa { get; set; }

    public bool EsPrincipal { get; set; }

    public int EstadoId { get; set; }

    public DateTime FechaCreacion { get; set; }

    public int CreadoPorUsuarioId { get; set; }

    public DateTime? FechaModificacion { get; set; }

    public int? ModificadoPorUsuarioId { get; set; }

    public byte[] VersionFila { get; set; } = null!;

    public virtual Bodegas? BodegasNavigation { get; set; }

    public virtual ICollection<ConteosFisicos> ConteosFisicos { get; set; } = new List<ConteosFisicos>();

    public virtual Usuarios CreadoPorUsuario { get; set; } = null!;

    public virtual Empresas Empresa { get; set; } = null!;

    public virtual Estados Estado { get; set; } = null!;

    public virtual ICollection<Existencias> Existencias { get; set; } = new List<Existencias>();

    public virtual ICollection<Bodegas> InverseBodegasNavigation { get; set; } = new List<Bodegas>();

    public virtual ICollection<Kardex> Kardex { get; set; } = new List<Kardex>();

    public virtual Usuarios? ModificadoPorUsuario { get; set; }

    public virtual ICollection<Movimientos> MovimientosBodegas { get; set; } = new List<Movimientos>();

    public virtual ICollection<Movimientos> MovimientosBodegasNavigation { get; set; } = new List<Movimientos>();

    public virtual ICollection<OrdenesProduccion> OrdenesProduccion { get; set; } = new List<OrdenesProduccion>();

    public virtual ICollection<ParametrosStockProductos> ParametrosStockProductos { get; set; } = new List<ParametrosStockProductos>();

    public virtual ICollection<ReservaInventarioDetalles> ReservaInventarioDetalles { get; set; } = new List<ReservaInventarioDetalles>();

    public virtual ICollection<SeriesProductos> SeriesProductos { get; set; } = new List<SeriesProductos>();

    public virtual Sucursales Sucursal { get; set; } = null!;

    public virtual ICollection<TrasladosInventario> TrasladosInventarioBodegaDestino { get; set; } = new List<TrasladosInventario>();

    public virtual ICollection<TrasladosInventario> TrasladosInventarioBodegaOrigen { get; set; } = new List<TrasladosInventario>();

    public virtual ICollection<Ubicaciones> Ubicaciones { get; set; } = new List<Ubicaciones>();
}
