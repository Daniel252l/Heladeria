using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class UsuariosSucursales
{
    public long Id { get; set; }

    public int UsuarioId { get; set; }

    public long SucursalId { get; set; }

    public bool EsPrincipal { get; set; }

    public int EstadoId { get; set; }

    public virtual Estados Estado { get; set; } = null!;

    public virtual Sucursales Sucursal { get; set; } = null!;

    public virtual Usuarios Usuario { get; set; } = null!;
}
