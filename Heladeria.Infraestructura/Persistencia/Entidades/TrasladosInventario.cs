using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class TrasladosInventario
{
    public long Id { get; set; }

    public int EmpresaId { get; set; }

    public long SucursalOrigenId { get; set; }

    public long SucursalDestinoId { get; set; }

    public long BodegaOrigenId { get; set; }

    public long BodegaDestinoId { get; set; }

    public string NumeroTraslado { get; set; } = null!;

    public DateTime FechaTraslado { get; set; }

    public int FlujoEstadoId { get; set; }

    public int EstadoId { get; set; }

    public int CreadoPorUsuarioId { get; set; }

    public string? Observaciones { get; set; }

    public virtual Bodegas BodegaDestino { get; set; } = null!;

    public virtual Bodegas BodegaOrigen { get; set; } = null!;

    public virtual Usuarios CreadoPorUsuario { get; set; } = null!;

    public virtual Empresas Empresa { get; set; } = null!;

    public virtual EstadosFlujo EstadosFlujo { get; set; } = null!;

    public virtual Sucursales SucursalDestino { get; set; } = null!;

    public virtual Sucursales SucursalOrigen { get; set; } = null!;

    public virtual ICollection<TrasladoInventarioDetalles> TrasladoInventarioDetalles { get; set; } = new List<TrasladoInventarioDetalles>();
}
