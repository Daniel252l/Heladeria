using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class NotasDebito
{
    public long Id { get; set; }

    public int EmpresaId { get; set; }

    public int ClienteId { get; set; }

    public long? VentaId { get; set; }

    public string NumeroNota { get; set; } = null!;

    public DateTime Fecha { get; set; }

    public decimal Monto { get; set; }

    public string Motivo { get; set; } = null!;

    public int EstadoId { get; set; }

    public virtual Clientes Cliente { get; set; } = null!;

    public virtual Empresas Empresa { get; set; } = null!;

    public virtual Estados Estado { get; set; } = null!;

    public virtual Ventas? Venta { get; set; }
}
