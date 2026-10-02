using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class Recetas
{
    public long Id { get; set; }

    public int EmpresaId { get; set; }

    public long ProductoTerminadoId { get; set; }

    public long? ProductoPresentacionRendimientoId { get; set; }

    public string Codigo { get; set; } = null!;

    public string Nombre { get; set; } = null!;

    public decimal Rendimiento { get; set; }

    public int UnidadRendimientoId { get; set; }

    public decimal FactorConversion { get; set; }

    public decimal RendimientoBase { get; set; }

    public int Version { get; set; }

    public DateOnly VigenteDesde { get; set; }

    public DateOnly? VigenteHasta { get; set; }

    public int EstadoId { get; set; }

    public byte[] VersionFila { get; set; } = null!;

    public virtual Empresas Empresa { get; set; } = null!;

    public virtual Estados Estado { get; set; } = null!;

    public virtual ICollection<OrdenesProduccion> OrdenesProduccion { get; set; } = new List<OrdenesProduccion>();

    public virtual Productos ProductoTerminado { get; set; } = null!;

    public virtual ProductoPresentaciones? ProductoPresentacionRendimiento { get; set; }

    public virtual ICollection<RecetaDetalles> RecetaDetalles { get; set; } = new List<RecetaDetalles>();

    public virtual UnidadesMedida UnidadRendimiento { get; set; } = null!;
}
