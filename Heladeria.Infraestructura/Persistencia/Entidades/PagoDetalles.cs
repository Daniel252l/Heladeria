using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class PagoDetalles
{
    public long Id { get; set; }

    public long PagoId { get; set; }

    public int FormaPagoId { get; set; }

    public decimal Monto { get; set; }

    public string? Referencia { get; set; }

    public int? BancoId { get; set; }

    public long? CuentaBancariaId { get; set; }

    public virtual Bancos? Banco { get; set; }

    public virtual CuentasBancarias? CuentaBancaria { get; set; }

    public virtual FormasPago FormaPago { get; set; } = null!;

    public virtual Pagos Pago { get; set; } = null!;
}
