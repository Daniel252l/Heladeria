using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class Pagos
{
    public long Id { get; set; }

    public int EmpresaId { get; set; }

    public long? SucursalId { get; set; }

    public int? ClienteId { get; set; }

    public int? ProveedorId { get; set; }

    public DateTime Fecha { get; set; }

    public int MonedaId { get; set; }

    public decimal TipoCambio { get; set; }

    public decimal MontoTotal { get; set; }

    public int TipoPagoOperacionId { get; set; }

    public long? TurnoCajaId { get; set; }

    public int UsuarioId { get; set; }

    public int EstadoId { get; set; }

    public virtual ICollection<AplicacionesCuentasPorCobrar> AplicacionesCuentasPorCobrar { get; set; } = new List<AplicacionesCuentasPorCobrar>();

    public virtual ICollection<AplicacionesCuentasPorPagar> AplicacionesCuentasPorPagar { get; set; } = new List<AplicacionesCuentasPorPagar>();

    public virtual Clientes? Cliente { get; set; }

    public virtual Empresas Empresa { get; set; } = null!;

    public virtual Estados Estado { get; set; } = null!;

    public virtual Monedas Moneda { get; set; } = null!;

    public virtual ICollection<MovimientosBancarios> MovimientosBancarios { get; set; } = new List<MovimientosBancarios>();

    public virtual ICollection<PagoDetalles> PagoDetalles { get; set; } = new List<PagoDetalles>();

    public virtual Proveedores? Proveedor { get; set; }

    public virtual Sucursales? Sucursal { get; set; }

    public virtual Tipos TipoPagoOperacion { get; set; } = null!;

    public virtual TurnosCaja? TurnoCaja { get; set; }

    public virtual Usuarios Usuario { get; set; } = null!;
}
