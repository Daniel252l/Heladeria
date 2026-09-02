using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class Cajas
{
    public long Id { get; set; }

    public int EmpresaId { get; set; }

    public long SucursalId { get; set; }

    public string Codigo { get; set; } = null!;

    public string Nombre { get; set; } = null!;

    public int EstadoId { get; set; }

    public virtual Empresas Empresa { get; set; } = null!;

    public virtual Estados Estado { get; set; } = null!;

    public virtual Sucursales Sucursal { get; set; } = null!;

    public virtual ICollection<TurnosCaja> TurnosCaja { get; set; } = new List<TurnosCaja>();
}
