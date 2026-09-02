using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class Sucursales
{
    public long Id { get; set; }

    public string Nombre { get; set; } = null!;

    public string Direccion { get; set; } = null!;

    public string Telefono { get; set; } = null!;

    public string Email { get; set; } = null!;

    public int EmpresaId { get; set; }

    public int? LugarId { get; set; }

    public int? MunicipioId { get; set; }

    public int? DepartamentoId { get; set; }

    public bool EsCentral { get; set; }

    public int? ClienteId { get; set; }

    public int EstadoId { get; set; }

    public int ProveedorId { get; set; }

    public bool EsBodega { get; set; }

    public long? SucursalCentralId { get; set; }

    public string? Imagen { get; set; }

    public long? SucursalTransporteId { get; set; }

    public string? RepresentanteLegal { get; set; }

    public string DireccionCorta { get; set; } = null!;

    public string? Codigo { get; set; }

    public string? ImagenReportes { get; set; }

    public bool Transportes { get; set; }

    public string CodigoNombre { get; set; } = null!;

    public virtual ICollection<AsistenciaPersonal> AsistenciaPersonal { get; set; } = new List<AsistenciaPersonal>();

    public virtual ICollection<Bitacoras> Bitacoras { get; set; } = new List<Bitacoras>();

    public virtual ICollection<Bodegas> Bodegas { get; set; } = new List<Bodegas>();

    public virtual ICollection<Cajas> Cajas { get; set; } = new List<Cajas>();

    public virtual Clientes? Cliente { get; set; }

    public virtual ICollection<ConteosFisicos> ConteosFisicos { get; set; } = new List<ConteosFisicos>();

    public virtual ICollection<Cotizaciones> Cotizaciones { get; set; } = new List<Cotizaciones>();

    public virtual Departamentos? Departamento { get; set; }

    public virtual ICollection<Empleados> Empleados { get; set; } = new List<Empleados>();

    public virtual Empresas Empresa { get; set; } = null!;

    public virtual Estados Estado { get; set; } = null!;

    public virtual ICollection<Sucursales> InverseSucursalCentral { get; set; } = new List<Sucursales>();

    public virtual ICollection<Sucursales> InverseSucursalTransporte { get; set; } = new List<Sucursales>();

    public virtual ICollection<ListasPrecios> ListasPrecios { get; set; } = new List<ListasPrecios>();

    public virtual ICollection<Movimientos> Movimientos { get; set; } = new List<Movimientos>();

    public virtual Municipios? Municipio { get; set; }

    public virtual ICollection<Notificaciones> Notificaciones { get; set; } = new List<Notificaciones>();

    public virtual ICollection<OrdenesCompra> OrdenesCompra { get; set; } = new List<OrdenesCompra>();

    public virtual ICollection<OrdenesProduccion> OrdenesProduccion { get; set; } = new List<OrdenesProduccion>();

    public virtual ICollection<Pagos> Pagos { get; set; } = new List<Pagos>();

    public virtual ICollection<PedidosVenta> PedidosVenta { get; set; } = new List<PedidosVenta>();

    public virtual ICollection<PromocionSucursales> PromocionSucursales { get; set; } = new List<PromocionSucursales>();

    public virtual Proveedores Proveedor { get; set; } = null!;

    public virtual ICollection<RecepcionesCompra> RecepcionesCompra { get; set; } = new List<RecepcionesCompra>();

    public virtual ICollection<ReservasInventario> ReservasInventario { get; set; } = new List<ReservasInventario>();

    public virtual ICollection<SeriesDocumentos> SeriesDocumentos { get; set; } = new List<SeriesDocumentos>();

    public virtual Sucursales? SucursalCentral { get; set; }

    public virtual Sucursales? SucursalTransporte { get; set; }

    public virtual ICollection<TrasladosInventario> TrasladosInventarioSucursalDestino { get; set; } = new List<TrasladosInventario>();

    public virtual ICollection<TrasladosInventario> TrasladosInventarioSucursalOrigen { get; set; } = new List<TrasladosInventario>();

    public virtual ICollection<UsuariosSucursales> UsuariosSucursales { get; set; } = new List<UsuariosSucursales>();

    public virtual ICollection<Ventas> Ventas { get; set; } = new List<Ventas>();
}
