using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class Notificaciones
{
    public long Id { get; set; }

    public string TipoNotificacionId { get; set; } = null!;

    public DateOnly Fecha { get; set; }

    public string Comentario { get; set; } = null!;

    public string Enlace { get; set; } = null!;

    public long? SucursalId { get; set; }

    public bool Generado { get; set; }

    public virtual Sucursales? Sucursal { get; set; }

    public virtual TipoNotificaciones TipoNotificacion { get; set; } = null!;
}
