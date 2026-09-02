using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class Movimientos
{
    public long Id { get; set; }

    public int EmpresaId { get; set; }

    public long SucursalId { get; set; }

    public int ConceptoMovimientoId { get; set; }

    public long? BodegaOrigenId { get; set; }

    public long? BodegaDestinoId { get; set; }

    public string SerieDocumento { get; set; } = null!;

    public long NumeroDocumento { get; set; }

    public DateTime FechaMovimiento { get; set; }

    public int FlujoEstadoId { get; set; }

    public int EstadoId { get; set; }

    public string? ReferenciaExterna { get; set; }

    public string? ModuloOrigen { get; set; }

    public long? DocumentoOrigenId { get; set; }

    public string? Observaciones { get; set; }

    public int? AplicadoPorUsuarioId { get; set; }

    public DateTime? FechaAplicacion { get; set; }

    public int? AnuladoPorUsuarioId { get; set; }

    public DateTime? FechaAnulacion { get; set; }

    public long? MotivoAnulacionId { get; set; }

    public string? ComentarioAnulacion { get; set; }

    public DateTime FechaCreacion { get; set; }

    public int CreadoPorUsuarioId { get; set; }

    public virtual Usuarios? AnuladoPorUsuario { get; set; }

    public virtual Usuarios? AplicadoPorUsuario { get; set; }

    public virtual Bodegas? Bodegas { get; set; }

    public virtual Bodegas? BodegasNavigation { get; set; }

    public virtual ConceptosMovimientos ConceptosMovimientos { get; set; } = null!;

    public virtual ICollection<ConteosFisicos> ConteosFisicos { get; set; } = new List<ConteosFisicos>();

    public virtual Usuarios CreadoPorUsuario { get; set; } = null!;

    public virtual Empresas Empresa { get; set; } = null!;

    public virtual EstadosFlujo EstadosFlujo { get; set; } = null!;

    public virtual Catalogos? MotivoAnulacion { get; set; }

    public virtual ICollection<MovimientoDetalles> MovimientoDetalles { get; set; } = new List<MovimientoDetalles>();

    public virtual Sucursales Sucursal { get; set; } = null!;
}
