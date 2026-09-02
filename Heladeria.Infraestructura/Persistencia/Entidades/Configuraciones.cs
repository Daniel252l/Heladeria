using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class Configuraciones
{
    public int Id { get; set; }

    public int TipoConfiguracionId { get; set; }

    public int? TipoValorId { get; set; }

    public string? ModuloId { get; set; }

    public string Descripcion { get; set; } = null!;

    public string PorDefecto { get; set; } = null!;

    public bool Visible { get; set; }

    public virtual Modulos? Modulo { get; set; }

    public virtual Tipos TipoConfiguracion { get; set; } = null!;

    public virtual Tipos? TipoValor { get; set; }
}
