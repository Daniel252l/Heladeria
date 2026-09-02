using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class DireccionesClientes
{
    public long Id { get; set; }

    public int ClienteId { get; set; }

    public int? TipoDireccionId { get; set; }

    public string Direccion { get; set; } = null!;

    public int? MunicipioId { get; set; }

    public string? Referencia { get; set; }

    public bool EsPrincipal { get; set; }

    public int EstadoId { get; set; }

    public virtual Clientes Cliente { get; set; } = null!;

    public virtual Estados Estado { get; set; } = null!;

    public virtual Municipios? Municipio { get; set; }

    public virtual Tipos? TipoDireccion { get; set; }
}
