using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class Empresas
{
    public int Id { get; set; }

    public string RazonSocial { get; set; } = null!;

    public string Eslogan { get; set; } = null!;

    public string Direccion { get; set; } = null!;

    public string Nit { get; set; } = null!;

    public string? Dpi { get; set; }

    public string Telefono { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Imagen { get; set; } = null!;

    public string Icono { get; set; } = null!;

    public int EstadoId { get; set; }

    public string ImagenReportes { get; set; } = null!;

    public string RoleId { get; set; } = null!;

    public int? ClienteId { get; set; }

    public string? NombreComercial { get; set; }

    public string? Url { get; set; }

    public virtual ICollection<Bancos> Bancos { get; set; } = new List<Bancos>();

    public virtual ICollection<Bitacoras> Bitacoras { get; set; } = new List<Bitacoras>();

    public virtual ICollection<Bodegas> Bodegas { get; set; } = new List<Bodegas>();

    public virtual ICollection<Cajas> Cajas { get; set; } = new List<Cajas>();

    public virtual ICollection<Catalogos> Catalogos { get; set; } = new List<Catalogos>();

    public virtual ICollection<Categorias> Categorias { get; set; } = new List<Categorias>();

    public virtual Tipos? Cliente { get; set; }

    public virtual ICollection<Clientes> Clientes { get; set; } = new List<Clientes>();

    public virtual ICollection<CodigosBarrasProductos> CodigosBarrasProductos { get; set; } = new List<CodigosBarrasProductos>();

    public virtual ICollection<CombosDetalle> CombosDetalle { get; set; } = new List<CombosDetalle>();

    public virtual ICollection<ConceptosMovimientos> ConceptosMovimientos { get; set; } = new List<ConceptosMovimientos>();

    public virtual ICollection<ConteoFisicoDetalles> ConteoFisicoDetalles { get; set; } = new List<ConteoFisicoDetalles>();

    public virtual ICollection<ConteosFisicos> ConteosFisicos { get; set; } = new List<ConteosFisicos>();

    public virtual ICollection<ConversionesUnidad> ConversionesUnidad { get; set; } = new List<ConversionesUnidad>();

    public virtual ICollection<CostosProductos> CostosProductos { get; set; } = new List<CostosProductos>();

    public virtual ICollection<Cotizaciones> Cotizaciones { get; set; } = new List<Cotizaciones>();

    public virtual ICollection<CuentasPorCobrar> CuentasPorCobrar { get; set; } = new List<CuentasPorCobrar>();

    public virtual ICollection<CuentasPorPagar> CuentasPorPagar { get; set; } = new List<CuentasPorPagar>();

    public virtual ICollection<DevolucionesVentas> DevolucionesVentas { get; set; } = new List<DevolucionesVentas>();

    public virtual ICollection<DocumentosAdjuntos> DocumentosAdjuntos { get; set; } = new List<DocumentosAdjuntos>();

    public virtual ICollection<Empleados> Empleados { get; set; } = new List<Empleados>();

    public virtual Estados Estado { get; set; } = null!;

    public virtual ICollection<Existencias> Existencias { get; set; } = new List<Existencias>();

    public virtual ICollection<ExistenciasLote> ExistenciasLote { get; set; } = new List<ExistenciasLote>();

    public virtual ICollection<FacturasProveedores> FacturasProveedores { get; set; } = new List<FacturasProveedores>();

    public virtual ICollection<FormasPago> FormasPago { get; set; } = new List<FormasPago>();

    public virtual ICollection<HistoricoCostosProductos> HistoricoCostosProductos { get; set; } = new List<HistoricoCostosProductos>();

    public virtual ICollection<HistoricoPreciosCompra> HistoricoPreciosCompra { get; set; } = new List<HistoricoPreciosCompra>();

    public virtual ICollection<HistoricoPreciosVenta> HistoricoPreciosVenta { get; set; } = new List<HistoricoPreciosVenta>();

    public virtual ICollection<Impuestos> Impuestos { get; set; } = new List<Impuestos>();

    public virtual ICollection<Kardex> Kardex { get; set; } = new List<Kardex>();

    public virtual ICollection<ListasPrecios> ListasPrecios { get; set; } = new List<ListasPrecios>();

    public virtual ICollection<Lotes> Lotes { get; set; } = new List<Lotes>();

    public virtual ICollection<Marcas> Marcas { get; set; } = new List<Marcas>();

    public virtual ICollection<Monedas> Monedas { get; set; } = new List<Monedas>();

    public virtual ICollection<MovimientoDetalleSeries> MovimientoDetalleSeries { get; set; } = new List<MovimientoDetalleSeries>();

    public virtual ICollection<MovimientoDetalles> MovimientoDetalles { get; set; } = new List<MovimientoDetalles>();

    public virtual ICollection<Movimientos> Movimientos { get; set; } = new List<Movimientos>();

    public virtual ICollection<NotasCredito> NotasCredito { get; set; } = new List<NotasCredito>();

    public virtual ICollection<NotasDebito> NotasDebito { get; set; } = new List<NotasDebito>();

    public virtual ICollection<OrdenesCompra> OrdenesCompra { get; set; } = new List<OrdenesCompra>();

    public virtual ICollection<OrdenesProduccion> OrdenesProduccion { get; set; } = new List<OrdenesProduccion>();

    public virtual ICollection<Pagos> Pagos { get; set; } = new List<Pagos>();

    public virtual ICollection<ParametrosStockProductos> ParametrosStockProductos { get; set; } = new List<ParametrosStockProductos>();

    public virtual ICollection<PedidosVenta> PedidosVenta { get; set; } = new List<PedidosVenta>();

    public virtual ICollection<Presentaciones> Presentaciones { get; set; } = new List<Presentaciones>();

    public virtual ICollection<ProductoPresentaciones> ProductoPresentaciones { get; set; } = new List<ProductoPresentaciones>();

    public virtual ICollection<Productos> Productos { get; set; } = new List<Productos>();

    public virtual ICollection<ProgramasFidelidad> ProgramasFidelidad { get; set; } = new List<ProgramasFidelidad>();

    public virtual ICollection<Promociones> Promociones { get; set; } = new List<Promociones>();

    public virtual ICollection<Proveedores> Proveedores { get; set; } = new List<Proveedores>();

    public virtual ICollection<RecepcionesCompra> RecepcionesCompra { get; set; } = new List<RecepcionesCompra>();

    public virtual ICollection<Recetas> Recetas { get; set; } = new List<Recetas>();

    public virtual ICollection<ReglasAutorizacion> ReglasAutorizacion { get; set; } = new List<ReglasAutorizacion>();

    public virtual ICollection<ReservaInventarioDetalles> ReservaInventarioDetalles { get; set; } = new List<ReservaInventarioDetalles>();

    public virtual ICollection<ReservasInventario> ReservasInventario { get; set; } = new List<ReservasInventario>();

    public virtual Roles Role { get; set; } = null!;

    public virtual ICollection<SeriesDocumentos> SeriesDocumentos { get; set; } = new List<SeriesDocumentos>();

    public virtual ICollection<SeriesProductos> SeriesProductos { get; set; } = new List<SeriesProductos>();

    public virtual ICollection<Sucursales> Sucursales { get; set; } = new List<Sucursales>();

    public virtual ICollection<TiposCambio> TiposCambio { get; set; } = new List<TiposCambio>();

    public virtual ICollection<TiposProductos> TiposProductos { get; set; } = new List<TiposProductos>();

    public virtual ICollection<TrasladosInventario> TrasladosInventario { get; set; } = new List<TrasladosInventario>();

    public virtual ICollection<Ubicaciones> Ubicaciones { get; set; } = new List<Ubicaciones>();

    public virtual ICollection<UnidadesMedida> UnidadesMedida { get; set; } = new List<UnidadesMedida>();

    public virtual ICollection<Ventas> Ventas { get; set; } = new List<Ventas>();
}
