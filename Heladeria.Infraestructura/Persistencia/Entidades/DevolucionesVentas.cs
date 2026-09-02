using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class DevolucionesVentas
{
    public long Id { get; set; }

    public int EmpresaId { get; set; }

    public long VentaId { get; set; }

    public string NumeroDevolucion { get; set; } = null!;

    public DateTime Fecha { get; set; }

    public decimal Total { get; set; }

    public long? MotivoId { get; set; }

    public int FlujoEstadoId { get; set; }

    public int EstadoId { get; set; }

    public int UsuarioId { get; set; }

    public virtual ICollection<DevolucionVentaDetalles> DevolucionVentaDetalles { get; set; } = new List<DevolucionVentaDetalles>();

    public virtual Empresas Empresa { get; set; } = null!;

    public virtual EstadosFlujo EstadosFlujo { get; set; } = null!;

    public virtual Catalogos? Motivo { get; set; }

    public virtual Usuarios Usuario { get; set; } = null!;

    public virtual Ventas Venta { get; set; } = null!;
}
