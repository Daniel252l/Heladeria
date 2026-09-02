using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class ContactosProveedores
{
    public long Id { get; set; }

    public int ProveedorId { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Telefono { get; set; }

    public string? Correo { get; set; }

    public string? Cargo { get; set; }

    public bool EsPrincipal { get; set; }

    public int EstadoId { get; set; }

    public virtual Estados Estado { get; set; } = null!;

    public virtual Proveedores Proveedor { get; set; } = null!;
}
