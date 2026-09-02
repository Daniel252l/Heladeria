using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class TiposCambio
{
    public long Id { get; set; }

    public int EmpresaId { get; set; }

    public int MonedaOrigenId { get; set; }

    public int MonedaDestinoId { get; set; }

    public DateTime Fecha { get; set; }

    public decimal TasaCompra { get; set; }

    public decimal TasaVenta { get; set; }

    public int UsuarioId { get; set; }

    public virtual Empresas Empresa { get; set; } = null!;

    public virtual Monedas MonedaDestino { get; set; } = null!;

    public virtual Monedas MonedaOrigen { get; set; } = null!;

    public virtual Usuarios Usuario { get; set; } = null!;
}
