using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class HistorialAccesos
{
    public long Id { get; set; }

    public int? UsuarioId { get; set; }

    public string? UserName { get; set; }

    public DateTime Fecha { get; set; }

    public string? DireccionIp { get; set; }

    public bool Exitoso { get; set; }

    public string? Motivo { get; set; }

    public virtual Usuarios? Usuario { get; set; }
}
