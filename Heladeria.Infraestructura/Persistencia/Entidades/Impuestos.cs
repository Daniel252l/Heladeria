using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class Impuestos
{
    public int Id { get; set; }

    public int EmpresaId { get; set; }

    public string Codigo { get; set; } = null!;

    public string Nombre { get; set; } = null!;

    public decimal Porcentaje { get; set; }

    public bool IncluidoEnPrecio { get; set; }

    public DateOnly FechaVigenciaDesde { get; set; }

    public DateOnly? FechaVigenciaHasta { get; set; }

    public int EstadoId { get; set; }

    public virtual Empresas Empresa { get; set; } = null!;

    public virtual Estados Estado { get; set; } = null!;

    public virtual ICollection<ProductoImpuestos> ProductoImpuestos { get; set; } = new List<ProductoImpuestos>();

    public virtual ICollection<VentaImpuestos> VentaImpuestos { get; set; } = new List<VentaImpuestos>();
}
