using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class OrdenProduccionDetalles
{
    public long Id { get; set; }

    public long OrdenProduccionId { get; set; }

    public long ProductoInsumoId { get; set; }

    public long? ProductoPresentacionId { get; set; }

    public int UnidadMedidaId { get; set; }

    public decimal CantidadPlanificada { get; set; }

    public decimal FactorConversion { get; set; }

    public decimal CantidadPlanificadaBase { get; set; }

    public decimal? CostoPlanificado { get; set; }

    public virtual OrdenesProduccion OrdenProduccion { get; set; } = null!;

    public virtual Productos ProductoInsumo { get; set; } = null!;

    public virtual ProductoPresentaciones? ProductoPresentacion { get; set; }

    public virtual UnidadesMedida UnidadMedida { get; set; } = null!;
}
