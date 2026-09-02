using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class Lotes
{
    public long Id { get; set; }

    public int EmpresaId { get; set; }

    public long ProductoId { get; set; }

    public string NumeroLote { get; set; } = null!;

    public DateOnly? FechaFabricacion { get; set; }

    public DateOnly? FechaVencimiento { get; set; }

    public int FlujoEstadoId { get; set; }

    public int EstadoId { get; set; }

    public string? Observaciones { get; set; }

    public DateTime FechaCreacion { get; set; }

    public int CreadoPorUsuarioId { get; set; }

    public byte[] VersionFila { get; set; } = null!;

    public virtual ICollection<ConsumosProduccion> ConsumosProduccion { get; set; } = new List<ConsumosProduccion>();

    public virtual ICollection<ConteoFisicoDetalles> ConteoFisicoDetalles { get; set; } = new List<ConteoFisicoDetalles>();

    public virtual Usuarios CreadoPorUsuario { get; set; } = null!;

    public virtual Empresas Empresa { get; set; } = null!;

    public virtual EstadosFlujo EstadosFlujo { get; set; } = null!;

    public virtual ICollection<ExistenciasLote> ExistenciasLote { get; set; } = new List<ExistenciasLote>();

    public virtual ICollection<Kardex> Kardex { get; set; } = new List<Kardex>();

    public virtual ICollection<MovimientoDetalles> MovimientoDetalles { get; set; } = new List<MovimientoDetalles>();

    public virtual ICollection<ProduccionTerminada> ProduccionTerminada { get; set; } = new List<ProduccionTerminada>();

    public virtual Productos Productos { get; set; } = null!;

    public virtual ICollection<RecepcionCompraDetalles> RecepcionCompraDetalles { get; set; } = new List<RecepcionCompraDetalles>();

    public virtual ICollection<ReservaInventarioDetalles> ReservaInventarioDetalles { get; set; } = new List<ReservaInventarioDetalles>();

    public virtual ICollection<SeriesProductos> SeriesProductos { get; set; } = new List<SeriesProductos>();

    public virtual ICollection<TrasladoInventarioDetalles> TrasladoInventarioDetalles { get; set; } = new List<TrasladoInventarioDetalles>();
}
