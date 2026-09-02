using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class ContactosClientes
{
    public long Id { get; set; }

    public int ClienteId { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Telefono { get; set; }

    public string? Correo { get; set; }

    public string? Cargo { get; set; }

    public bool EsPrincipal { get; set; }

    public int EstadoId { get; set; }

    public virtual Clientes Cliente { get; set; } = null!;

    public virtual Estados Estado { get; set; } = null!;
}
