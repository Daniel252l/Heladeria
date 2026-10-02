using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class OrdenesProduccion
{
    public long Id { get; set; }

    public int EmpresaId { get; set; }

    public long SucursalId { get; set; }

    public long BodegaProduccionId { get; set; }

    public long ProductoAProducirId { get; set; }

    public long RecetaId { get; set; }

    public long? ProductoPresentacionId { get; set; }

    public string NumeroOrden { get; set; } = null!;

    public decimal CantidadPlanificada { get; set; }

    public decimal FactorConversion { get; set; }

    public decimal CantidadPlanificadaBase { get; set; }

    public decimal? CantidadProducida { get; set; }

    public DateTime FechaInicio { get; set; }

    public DateTime? FechaFin { get; set; }

    public int FlujoEstadoId { get; set; }

    public int EstadoId { get; set; }

    public int ResponsableUsuarioId { get; set; }

    public byte[] VersionFila { get; set; } = null!;

    public virtual Bodegas BodegaProduccion { get; set; } = null!;

    public virtual ICollection<ConsumosProduccion> ConsumosProduccion { get; set; } = new List<ConsumosProduccion>();

    public virtual ICollection<CostosProduccion> CostosProduccion { get; set; } = new List<CostosProduccion>();

    public virtual Empresas Empresa { get; set; } = null!;

    public virtual EstadosFlujo EstadosFlujo { get; set; } = null!;

    public virtual ICollection<OrdenProduccionDetalles> OrdenProduccionDetalles { get; set; } = new List<OrdenProduccionDetalles>();

    public virtual ICollection<ProduccionTerminada> ProduccionTerminada { get; set; } = new List<ProduccionTerminada>();

    public virtual Productos ProductoAProducir { get; set; } = null!;

    public virtual ProductoPresentaciones? ProductoPresentacion { get; set; }

    public virtual Recetas Receta { get; set; } = null!;

    public virtual Usuarios ResponsableUsuario { get; set; } = null!;

    public virtual Sucursales Sucursal { get; set; } = null!;
}
