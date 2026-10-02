using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class RecetaDetalles
{
    public long Id { get; set; }

    public long RecetaId { get; set; }

    public long ProductoInsumoId { get; set; }

    public long? ProductoPresentacionId { get; set; }

    public int UnidadMedidaId { get; set; }

    public decimal CantidadRequerida { get; set; }

    public decimal FactorConversion { get; set; }

    public decimal CantidadBase { get; set; }

    public string? Observacion { get; set; }

    public decimal PorcentajeDesperdicioTecnico { get; set; }

    public virtual Productos ProductoInsumo { get; set; } = null!;

    public virtual ProductoPresentaciones? ProductoPresentacion { get; set; }

    public virtual Recetas Receta { get; set; } = null!;

    public virtual UnidadesMedida UnidadMedida { get; set; } = null!;
}
