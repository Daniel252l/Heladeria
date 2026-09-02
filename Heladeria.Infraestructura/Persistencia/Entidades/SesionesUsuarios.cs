using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class SesionesUsuarios
{
    public long Id { get; set; }

    public int UsuarioId { get; set; }

    public DateTime FechaInicio { get; set; }

    public DateTime? FechaFin { get; set; }

    public string? DireccionIp { get; set; }

    public string? Dispositivo { get; set; }

    public string? TokenIdentificador { get; set; }

    public bool Cerrada { get; set; }

    public virtual Usuarios Usuario { get; set; } = null!;
}
