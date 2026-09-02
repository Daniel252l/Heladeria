using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class EstadosFlujo
{
    public int Id { get; set; }

    public int FlujoEstadoId { get; set; }

    public string Codigo { get; set; } = null!;

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public short Orden { get; set; }

    public bool EsInicial { get; set; }

    public bool EsFinal { get; set; }

    public bool PermiteEdicion { get; set; }

    public bool RequiereMotivo { get; set; }

    public bool EsSistema { get; set; }

    public int EstadoId { get; set; }

    public DateTime FechaCreacion { get; set; }

    public int CreadoPorUsuarioId { get; set; }

    public DateTime? FechaModificacion { get; set; }

    public int? ModificadoPorUsuarioId { get; set; }

    public byte[] VersionFila { get; set; } = null!;

    public virtual ICollection<ConteosFisicos> ConteosFisicos { get; set; } = new List<ConteosFisicos>();

    public virtual ICollection<Cotizaciones> Cotizaciones { get; set; } = new List<Cotizaciones>();

    public virtual Usuarios CreadoPorUsuario { get; set; } = null!;

    public virtual ICollection<CuentasPorCobrar> CuentasPorCobrar { get; set; } = new List<CuentasPorCobrar>();

    public virtual ICollection<CuentasPorPagar> CuentasPorPagar { get; set; } = new List<CuentasPorPagar>();

    public virtual ICollection<DevolucionesVentas> DevolucionesVentas { get; set; } = new List<DevolucionesVentas>();

    public virtual Estados Estado { get; set; } = null!;

    public virtual ICollection<EstadoTransiciones> EstadoTransicionesEstadosFlujo { get; set; } = new List<EstadoTransiciones>();

    public virtual ICollection<EstadoTransiciones> EstadoTransicionesEstadosFlujoNavigation { get; set; } = new List<EstadoTransiciones>();

    public virtual FlujosEstados FlujoEstado { get; set; } = null!;

    public virtual ICollection<Lotes> Lotes { get; set; } = new List<Lotes>();

    public virtual Usuarios? ModificadoPorUsuario { get; set; }

    public virtual ICollection<Movimientos> Movimientos { get; set; } = new List<Movimientos>();

    public virtual ICollection<OrdenesCompra> OrdenesCompra { get; set; } = new List<OrdenesCompra>();

    public virtual ICollection<OrdenesProduccion> OrdenesProduccion { get; set; } = new List<OrdenesProduccion>();

    public virtual ICollection<PedidosVenta> PedidosVenta { get; set; } = new List<PedidosVenta>();

    public virtual ICollection<RecepcionesCompra> RecepcionesCompra { get; set; } = new List<RecepcionesCompra>();

    public virtual ICollection<ReservasInventario> ReservasInventario { get; set; } = new List<ReservasInventario>();

    public virtual ICollection<SeriesProductos> SeriesProductos { get; set; } = new List<SeriesProductos>();

    public virtual ICollection<SolicitudesAutorizacion> SolicitudesAutorizacion { get; set; } = new List<SolicitudesAutorizacion>();

    public virtual ICollection<TrasladosInventario> TrasladosInventario { get; set; } = new List<TrasladosInventario>();

    public virtual ICollection<TurnosCaja> TurnosCaja { get; set; } = new List<TurnosCaja>();

    public virtual ICollection<Ventas> Ventas { get; set; } = new List<Ventas>();
}
