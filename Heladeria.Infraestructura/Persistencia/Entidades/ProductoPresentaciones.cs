using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class ProductoPresentaciones
{
    public long Id { get; set; }

    public int EmpresaId { get; set; }

    public long ProductoId { get; set; }

    public int PresentacionId { get; set; }

    public int UnidadMedidaId { get; set; }

    public decimal FactorConversion { get; set; }

    public bool EsPresentacionBase { get; set; }

    public bool PermiteCompra { get; set; }

    public bool PermiteVenta { get; set; }

    public decimal? PrecioVentaEstandar { get; set; }

    public int EstadoId { get; set; }

    public DateTime FechaCreacion { get; set; }

    public int CreadoPorUsuarioId { get; set; }

    public DateTime? FechaModificacion { get; set; }

    public int? ModificadoPorUsuarioId { get; set; }

    public byte[] VersionFila { get; set; } = null!;

    public virtual ICollection<CodigosBarrasProductos> CodigosBarrasProductos { get; set; } = new List<CodigosBarrasProductos>();

    public virtual ICollection<CotizacionDetalles> CotizacionDetalles { get; set; } = new List<CotizacionDetalles>();

    public virtual Usuarios CreadoPorUsuario { get; set; } = null!;

    public virtual Empresas Empresa { get; set; } = null!;

    public virtual Estados Estado { get; set; } = null!;

    public virtual ICollection<HistoricoPreciosVenta> HistoricoPreciosVenta { get; set; } = new List<HistoricoPreciosVenta>();

    public virtual Usuarios? ModificadoPorUsuario { get; set; }

    public virtual ICollection<MovimientoDetalles> MovimientoDetalles { get; set; } = new List<MovimientoDetalles>();

    public virtual ICollection<OrdenesCompraDetalle> OrdenesCompraDetalle { get; set; } = new List<OrdenesCompraDetalle>();

    public virtual ICollection<PedidoVentaDetalles> PedidoVentaDetalles { get; set; } = new List<PedidoVentaDetalles>();

    public virtual ICollection<PreciosDetalle> PreciosDetalle { get; set; } = new List<PreciosDetalle>();

    public virtual Presentaciones Presentaciones { get; set; } = null!;

    public virtual Productos Productos { get; set; } = null!;

    public virtual UnidadesMedida UnidadesMedida { get; set; } = null!;

    public virtual ICollection<VentasDetalle> VentasDetalle { get; set; } = new List<VentasDetalle>();
}
