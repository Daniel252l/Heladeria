using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class TipoNotificaciones
{
    public string Id { get; set; } = null!;

    public int TipoId { get; set; }

    public string ModuloId { get; set; } = null!;

    public string Descripcion { get; set; } = null!;

    public int EstadoId { get; set; }

    public bool PorSucursal { get; set; }

    public virtual Estados Estado { get; set; } = null!;

    public virtual Modulos Modulo { get; set; } = null!;

    public virtual ICollection<Notificaciones> Notificaciones { get; set; } = new List<Notificaciones>();

    public virtual Tipos Tipo { get; set; } = null!;
}
