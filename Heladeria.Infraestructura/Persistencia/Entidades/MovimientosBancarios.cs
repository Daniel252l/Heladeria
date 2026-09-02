using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class MovimientosBancarios
{
    public long Id { get; set; }

    public long CuentaBancariaId { get; set; }

    public DateTime Fecha { get; set; }

    public int TipoMovimientoId { get; set; }

    public decimal Monto { get; set; }

    public string? Referencia { get; set; }

    public long? PagoId { get; set; }

    public string? Descripcion { get; set; }

    public int UsuarioId { get; set; }

    public virtual CuentasBancarias CuentaBancaria { get; set; } = null!;

    public virtual Pagos? Pago { get; set; }

    public virtual Tipos TipoMovimiento { get; set; } = null!;

    public virtual Usuarios Usuario { get; set; } = null!;
}
