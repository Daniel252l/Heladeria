using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class Productos
{
    public long Id { get; set; }

    public int EmpresaId { get; set; }

    public int CategoriaId { get; set; }

    public int? MarcaId { get; set; }

    public int TipoProductoId { get; set; }

    public int UnidadBaseId { get; set; }

    public long TipoSeguimientoId { get; set; }

    public long MetodoValuacionId { get; set; }

    public bool ControlaVencimiento { get; set; }

    public string SKU { get; set; } = null!;

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public string? Modelo { get; set; }

    public string? Imagen { get; set; }

    public decimal? Peso { get; set; }

    public bool EsComprable { get; set; }

    public bool EsVendible { get; set; }

    public bool PermiteDescuento { get; set; }

    public bool EsCombo { get; set; }

    public decimal? PrecioVentaReferencial { get; set; }

    public int EstadoId { get; set; }

    public DateTime FechaCreacion { get; set; }

    public int CreadoPorUsuarioId { get; set; }

    public DateTime? FechaModificacion { get; set; }

    public int? ModificadoPorUsuarioId { get; set; }

    public byte[] VersionFila { get; set; } = null!;

    public virtual Categorias Categorias { get; set; } = null!;

    public virtual ICollection<CodigosBarrasProductos> CodigosBarrasProductos { get; set; } = new List<CodigosBarrasProductos>();

    public virtual ICollection<CombosDetalle> CombosDetalleProductos { get; set; } = new List<CombosDetalle>();

    public virtual ICollection<CombosDetalle> CombosDetalleProductosNavigation { get; set; } = new List<CombosDetalle>();

    public virtual ICollection<ConsumosProduccion> ConsumosProduccion { get; set; } = new List<ConsumosProduccion>();

    public virtual ICollection<ConteoFisicoDetalles> ConteoFisicoDetalles { get; set; } = new List<ConteoFisicoDetalles>();

    public virtual ICollection<CostosProductos> CostosProductos { get; set; } = new List<CostosProductos>();

    public virtual ICollection<CotizacionDetalles> CotizacionDetalles { get; set; } = new List<CotizacionDetalles>();

    public virtual Usuarios CreadoPorUsuario { get; set; } = null!;

    public virtual Empresas Empresa { get; set; } = null!;

    public virtual Estados Estado { get; set; } = null!;

    public virtual ICollection<Existencias> Existencias { get; set; } = new List<Existencias>();

    public virtual ICollection<HistoricoCostosProductos> HistoricoCostosProductos { get; set; } = new List<HistoricoCostosProductos>();

    public virtual ICollection<HistoricoPreciosCompra> HistoricoPreciosCompra { get; set; } = new List<HistoricoPreciosCompra>();

    public virtual ICollection<HistoricoPreciosVenta> HistoricoPreciosVenta { get; set; } = new List<HistoricoPreciosVenta>();

    public virtual ICollection<Kardex> Kardex { get; set; } = new List<Kardex>();

    public virtual ICollection<Lotes> Lotes { get; set; } = new List<Lotes>();

    public virtual Marcas? Marcas { get; set; }

    public virtual Catalogos MetodoValuacion { get; set; } = null!;

    public virtual Usuarios? ModificadoPorUsuario { get; set; }

    public virtual ICollection<MovimientoDetalles> MovimientoDetalles { get; set; } = new List<MovimientoDetalles>();

    public virtual ICollection<OrdenProduccionDetalles> OrdenProduccionDetalles { get; set; } = new List<OrdenProduccionDetalles>();

    public virtual ICollection<OrdenesCompraDetalle> OrdenesCompraDetalle { get; set; } = new List<OrdenesCompraDetalle>();

    public virtual ICollection<OrdenesProduccion> OrdenesProduccion { get; set; } = new List<OrdenesProduccion>();

    public virtual ICollection<ParametrosStockProductos> ParametrosStockProductos { get; set; } = new List<ParametrosStockProductos>();

    public virtual ICollection<PedidoVentaDetalles> PedidoVentaDetalles { get; set; } = new List<PedidoVentaDetalles>();

    public virtual ICollection<PreciosDetalle> PreciosDetalle { get; set; } = new List<PreciosDetalle>();

    public virtual ICollection<ProduccionTerminada> ProduccionTerminada { get; set; } = new List<ProduccionTerminada>();

    public virtual ICollection<ProductoImpuestos> ProductoImpuestos { get; set; } = new List<ProductoImpuestos>();

    public virtual ICollection<ProductoPresentaciones> ProductoPresentaciones { get; set; } = new List<ProductoPresentaciones>();

    public virtual ICollection<PromocionProductos> PromocionProductos { get; set; } = new List<PromocionProductos>();

    public virtual ICollection<ProveedorProductos> ProveedorProductos { get; set; } = new List<ProveedorProductos>();

    public virtual ICollection<RecepcionCompraDetalles> RecepcionCompraDetalles { get; set; } = new List<RecepcionCompraDetalles>();

    public virtual ICollection<RecetaDetalles> RecetaDetalles { get; set; } = new List<RecetaDetalles>();

    public virtual ICollection<Recetas> Recetas { get; set; } = new List<Recetas>();

    public virtual ICollection<ReservaInventarioDetalles> ReservaInventarioDetalles { get; set; } = new List<ReservaInventarioDetalles>();

    public virtual ICollection<SeriesProductos> SeriesProductos { get; set; } = new List<SeriesProductos>();

    public virtual Catalogos TipoSeguimiento { get; set; } = null!;

    public virtual TiposProductos TiposProductos { get; set; } = null!;

    public virtual ICollection<TrasladoInventarioDetalles> TrasladoInventarioDetalles { get; set; } = new List<TrasladoInventarioDetalles>();

    public virtual UnidadesMedida UnidadesMedida { get; set; } = null!;

    public virtual ICollection<VentasDetalle> VentasDetalle { get; set; } = new List<VentasDetalle>();
}
