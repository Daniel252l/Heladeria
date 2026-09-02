using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class Usuarios
{
    public int Id { get; set; }

    public string Email { get; set; } = null!;

    public string UserName { get; set; } = null!;

    public string IdentityId { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string? SecurityStamp { get; set; }

    public string RoleId { get; set; } = null!;

    public int AccessFailedCount { get; set; }

    public TimeOnly HorarioInicio { get; set; }

    public TimeOnly HorarioFinal { get; set; }

    public DateOnly? UltimoCambioDeContrasenia { get; set; }

    public string Imagen { get; set; } = null!;

    public string Soundex { get; set; } = null!;

    public bool ContraseniaTemporal { get; set; }

    public int? AsociadoId { get; set; }

    public int EstadoId { get; set; }

    public string? PasswordTemporal { get; set; }

    public int? GrupoId { get; set; }

    public int TipoInicioSesionId { get; set; }

    public bool? UtilizarConfiguracionDeRole { get; set; }

    public virtual ICollection<AplicacionesCuentasPorCobrar> AplicacionesCuentasPorCobrar { get; set; } = new List<AplicacionesCuentasPorCobrar>();

    public virtual ICollection<AplicacionesCuentasPorPagar> AplicacionesCuentasPorPagar { get; set; } = new List<AplicacionesCuentasPorPagar>();

    public virtual ICollection<ArqueosCaja> ArqueosCaja { get; set; } = new List<ArqueosCaja>();

    public virtual ICollection<Bitacoras> Bitacoras { get; set; } = new List<Bitacoras>();

    public virtual ICollection<Bodegas> BodegasCreadoPorUsuario { get; set; } = new List<Bodegas>();

    public virtual ICollection<Bodegas> BodegasModificadoPorUsuario { get; set; } = new List<Bodegas>();

    public virtual ICollection<Catalogos> CatalogosCreadoPorUsuario { get; set; } = new List<Catalogos>();

    public virtual ICollection<Catalogos> CatalogosModificadoPorUsuario { get; set; } = new List<Catalogos>();

    public virtual ICollection<Categorias> CategoriasCreadoPorUsuario { get; set; } = new List<Categorias>();

    public virtual ICollection<Categorias> CategoriasModificadoPorUsuario { get; set; } = new List<Categorias>();

    public virtual ICollection<ClasesCatalogos> ClasesCatalogosCreadoPorUsuario { get; set; } = new List<ClasesCatalogos>();

    public virtual ICollection<ClasesCatalogos> ClasesCatalogosModificadoPorUsuario { get; set; } = new List<ClasesCatalogos>();

    public virtual ICollection<CodigosBarrasProductos> CodigosBarrasProductos { get; set; } = new List<CodigosBarrasProductos>();

    public virtual ICollection<ConceptosMovimientos> ConceptosMovimientosCreadoPorUsuario { get; set; } = new List<ConceptosMovimientos>();

    public virtual ICollection<ConceptosMovimientos> ConceptosMovimientosModificadoPorUsuario { get; set; } = new List<ConceptosMovimientos>();

    public virtual ICollection<ConteoFisicoDetalles> ConteoFisicoDetalles { get; set; } = new List<ConteoFisicoDetalles>();

    public virtual ICollection<ConteosFisicos> ConteosFisicosCerradoPorUsuario { get; set; } = new List<ConteosFisicos>();

    public virtual ICollection<ConteosFisicos> ConteosFisicosCreadoPorUsuario { get; set; } = new List<ConteosFisicos>();

    public virtual ICollection<ConteosFisicos> ConteosFisicosIniciadoPorUsuario { get; set; } = new List<ConteosFisicos>();

    public virtual ICollection<CostosProduccion> CostosProduccion { get; set; } = new List<CostosProduccion>();

    public virtual ICollection<Cotizaciones> Cotizaciones { get; set; } = new List<Cotizaciones>();

    public virtual ICollection<DevolucionesVentas> DevolucionesVentas { get; set; } = new List<DevolucionesVentas>();

    public virtual ICollection<DocumentosAdjuntos> DocumentosAdjuntos { get; set; } = new List<DocumentosAdjuntos>();

    public virtual ICollection<Empleados> Empleados { get; set; } = new List<Empleados>();

    public virtual Estados Estado { get; set; } = null!;

    public virtual ICollection<EstadoTransiciones> EstadoTransicionesCreadoPorUsuario { get; set; } = new List<EstadoTransiciones>();

    public virtual ICollection<EstadoTransiciones> EstadoTransicionesModificadoPorUsuario { get; set; } = new List<EstadoTransiciones>();

    public virtual ICollection<EstadosFlujo> EstadosFlujoCreadoPorUsuario { get; set; } = new List<EstadosFlujo>();

    public virtual ICollection<EstadosFlujo> EstadosFlujoModificadoPorUsuario { get; set; } = new List<EstadosFlujo>();

    public virtual ICollection<FlujosEstados> FlujosEstadosCreadoPorUsuario { get; set; } = new List<FlujosEstados>();

    public virtual ICollection<FlujosEstados> FlujosEstadosModificadoPorUsuario { get; set; } = new List<FlujosEstados>();

    public virtual ICollection<HistorialAccesos> HistorialAccesos { get; set; } = new List<HistorialAccesos>();

    public virtual ICollection<HistoricoCostosProductos> HistoricoCostosProductos { get; set; } = new List<HistoricoCostosProductos>();

    public virtual ICollection<HistoricoPreciosVenta> HistoricoPreciosVenta { get; set; } = new List<HistoricoPreciosVenta>();

    public virtual ICollection<Lotes> Lotes { get; set; } = new List<Lotes>();

    public virtual ICollection<Marcas> MarcasCreadoPorUsuario { get; set; } = new List<Marcas>();

    public virtual ICollection<Marcas> MarcasModificadoPorUsuario { get; set; } = new List<Marcas>();

    public virtual ICollection<Movimientos> MovimientosAnuladoPorUsuario { get; set; } = new List<Movimientos>();

    public virtual ICollection<Movimientos> MovimientosAplicadoPorUsuario { get; set; } = new List<Movimientos>();

    public virtual ICollection<MovimientosBancarios> MovimientosBancarios { get; set; } = new List<MovimientosBancarios>();

    public virtual ICollection<MovimientosCaja> MovimientosCaja { get; set; } = new List<MovimientosCaja>();

    public virtual ICollection<Movimientos> MovimientosCreadoPorUsuario { get; set; } = new List<Movimientos>();

    public virtual ICollection<OrdenesCompra> OrdenesCompra { get; set; } = new List<OrdenesCompra>();

    public virtual ICollection<OrdenesProduccion> OrdenesProduccion { get; set; } = new List<OrdenesProduccion>();

    public virtual ICollection<Pagos> Pagos { get; set; } = new List<Pagos>();

    public virtual ICollection<ParametrosStockProductos> ParametrosStockProductosCreadoPorUsuario { get; set; } = new List<ParametrosStockProductos>();

    public virtual ICollection<ParametrosStockProductos> ParametrosStockProductosModificadoPorUsuario { get; set; } = new List<ParametrosStockProductos>();

    public virtual ICollection<PedidosVenta> PedidosVenta { get; set; } = new List<PedidosVenta>();

    public virtual ICollection<Presentaciones> PresentacionesCreadoPorUsuario { get; set; } = new List<Presentaciones>();

    public virtual ICollection<Presentaciones> PresentacionesModificadoPorUsuario { get; set; } = new List<Presentaciones>();

    public virtual ICollection<ProductoPresentaciones> ProductoPresentacionesCreadoPorUsuario { get; set; } = new List<ProductoPresentaciones>();

    public virtual ICollection<ProductoPresentaciones> ProductoPresentacionesModificadoPorUsuario { get; set; } = new List<ProductoPresentaciones>();

    public virtual ICollection<Productos> ProductosCreadoPorUsuario { get; set; } = new List<Productos>();

    public virtual ICollection<Productos> ProductosModificadoPorUsuario { get; set; } = new List<Productos>();

    public virtual ICollection<RecepcionesCompra> RecepcionesCompra { get; set; } = new List<RecepcionesCompra>();

    public virtual ICollection<ReservasInventario> ReservasInventarioCreadoPorUsuario { get; set; } = new List<ReservasInventario>();

    public virtual ICollection<ReservasInventario> ReservasInventarioLiberadoPorUsuario { get; set; } = new List<ReservasInventario>();

    public virtual Roles Role { get; set; } = null!;

    public virtual ICollection<SeriesProductos> SeriesProductos { get; set; } = new List<SeriesProductos>();

    public virtual ICollection<SesionesUsuarios> SesionesUsuarios { get; set; } = new List<SesionesUsuarios>();

    public virtual ICollection<SolicitudesAutorizacion> SolicitudesAutorizacionAutorizadoPorUsuario { get; set; } = new List<SolicitudesAutorizacion>();

    public virtual ICollection<SolicitudesAutorizacion> SolicitudesAutorizacionSolicitadoPorUsuario { get; set; } = new List<SolicitudesAutorizacion>();

    public virtual Tipos TipoInicioSesion { get; set; } = null!;

    public virtual ICollection<TiposCambio> TiposCambio { get; set; } = new List<TiposCambio>();

    public virtual ICollection<TiposProductos> TiposProductosCreadoPorUsuario { get; set; } = new List<TiposProductos>();

    public virtual ICollection<TiposProductos> TiposProductosModificadoPorUsuario { get; set; } = new List<TiposProductos>();

    public virtual ICollection<TrasladosInventario> TrasladosInventario { get; set; } = new List<TrasladosInventario>();

    public virtual ICollection<TurnosCaja> TurnosCaja { get; set; } = new List<TurnosCaja>();

    public virtual ICollection<Ubicaciones> UbicacionesCreadoPorUsuario { get; set; } = new List<Ubicaciones>();

    public virtual ICollection<Ubicaciones> UbicacionesModificadoPorUsuario { get; set; } = new List<Ubicaciones>();

    public virtual ICollection<UnidadesMedida> UnidadesMedidaCreadoPorUsuario { get; set; } = new List<UnidadesMedida>();

    public virtual ICollection<UnidadesMedida> UnidadesMedidaModificadoPorUsuario { get; set; } = new List<UnidadesMedida>();

    public virtual ICollection<UsuariosSucursales> UsuariosSucursales { get; set; } = new List<UsuariosSucursales>();

    public virtual ICollection<Ventas> Ventas { get; set; } = new List<Ventas>();
}
