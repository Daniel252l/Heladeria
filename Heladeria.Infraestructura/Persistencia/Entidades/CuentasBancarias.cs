using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class CuentasBancarias
{
    public long Id { get; set; }

    public int BancoId { get; set; }

    public int MonedaId { get; set; }

    public string NumeroCuenta { get; set; } = null!;

    public string NombreCuenta { get; set; } = null!;

    public int? TipoCuentaId { get; set; }

    public int EstadoId { get; set; }

    public virtual Bancos Banco { get; set; } = null!;

    public virtual Estados Estado { get; set; } = null!;

    public virtual Monedas Moneda { get; set; } = null!;

    public virtual ICollection<MovimientosBancarios> MovimientosBancarios { get; set; } = new List<MovimientosBancarios>();

    public virtual ICollection<PagoDetalles> PagoDetalles { get; set; } = new List<PagoDetalles>();

    public virtual Tipos? TipoCuenta { get; set; }
}
