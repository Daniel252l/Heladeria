using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class ListasPrecios
{
    public int Id { get; set; }

    public int EmpresaId { get; set; }

    public string Codigo { get; set; } = null!;

    public string Nombre { get; set; } = null!;

    public DateOnly VigenciaDesde { get; set; }

    public DateOnly? VigenciaHasta { get; set; }

    public long? AplicaSucursalId { get; set; }

    public int? CanalVentaId { get; set; }

    public int MonedaId { get; set; }

    public int EstadoId { get; set; }

    public virtual Sucursales? AplicaSucursal { get; set; }

    public virtual Tipos? CanalVenta { get; set; }

    public virtual ICollection<Clientes> Clientes { get; set; } = new List<Clientes>();

    public virtual Empresas Empresa { get; set; } = null!;

    public virtual Estados Estado { get; set; } = null!;

    public virtual Monedas Moneda { get; set; } = null!;

    public virtual ICollection<PreciosDetalle> PreciosDetalle { get; set; } = new List<PreciosDetalle>();
}
