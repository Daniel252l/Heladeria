using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class PromocionSucursales
{
    public long Id { get; set; }

    public long PromocionId { get; set; }

    public long SucursalId { get; set; }

    public virtual Promociones Promocion { get; set; } = null!;

    public virtual Sucursales Sucursal { get; set; } = null!;
}
