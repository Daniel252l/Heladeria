using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class Cotizaciones
{
    public long Id { get; set; }

    public int EmpresaId { get; set; }

    public long SucursalId { get; set; }

    public int? ClienteId { get; set; }

    public string NumeroCotizacion { get; set; } = null!;

    public DateTime Fecha { get; set; }

    public DateOnly? VigenciaHasta { get; set; }

    public int MonedaId { get; set; }

    public decimal Total { get; set; }

    public int FlujoEstadoId { get; set; }

    public int EstadoId { get; set; }

    public int CreadoPorUsuarioId { get; set; }

    public virtual Clientes? Cliente { get; set; }

    public virtual ICollection<CotizacionDetalles> CotizacionDetalles { get; set; } = new List<CotizacionDetalles>();

    public virtual Usuarios CreadoPorUsuario { get; set; } = null!;

    public virtual Empresas Empresa { get; set; } = null!;

    public virtual EstadosFlujo EstadosFlujo { get; set; } = null!;

    public virtual Monedas Moneda { get; set; } = null!;

    public virtual ICollection<PedidosVenta> PedidosVenta { get; set; } = new List<PedidosVenta>();

    public virtual Sucursales Sucursal { get; set; } = null!;
}
