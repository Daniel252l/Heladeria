using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class RecepcionesCompra
{
    public long Id { get; set; }

    public int EmpresaId { get; set; }

    public long SucursalId { get; set; }

    public long OrdenCompraId { get; set; }

    public string NumeroRecepcion { get; set; } = null!;

    public DateTime FechaRecepcion { get; set; }

    public int RecibidoPorUsuarioId { get; set; }

    public int FlujoEstadoId { get; set; }

    public int EstadoId { get; set; }

    public string? Observaciones { get; set; }

    public virtual Empresas Empresa { get; set; } = null!;

    public virtual EstadosFlujo EstadosFlujo { get; set; } = null!;

    public virtual ICollection<HistoricoPreciosCompra> HistoricoPreciosCompra { get; set; } = new List<HistoricoPreciosCompra>();

    public virtual OrdenesCompra OrdenCompra { get; set; } = null!;

    public virtual ICollection<RecepcionCompraDetalles> RecepcionCompraDetalles { get; set; } = new List<RecepcionCompraDetalles>();

    public virtual Usuarios RecibidoPorUsuario { get; set; } = null!;

    public virtual Sucursales Sucursal { get; set; } = null!;
}
