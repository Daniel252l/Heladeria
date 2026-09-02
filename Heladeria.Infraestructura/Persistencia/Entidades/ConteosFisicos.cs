using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class ConteosFisicos
{
    public long Id { get; set; }

    public int EmpresaId { get; set; }

    public long SucursalId { get; set; }

    public long BodegaId { get; set; }

    public string CodigoConteo { get; set; } = null!;

    public long TipoConteoId { get; set; }

    public DateTime FechaCorte { get; set; }

    public int FlujoEstadoId { get; set; }

    public int EstadoId { get; set; }

    public bool PermiteReconteo { get; set; }

    public string? Observaciones { get; set; }

    public int? IniciadoPorUsuarioId { get; set; }

    public DateTime? FechaInicio { get; set; }

    public int? CerradoPorUsuarioId { get; set; }

    public DateTime? FechaCierre { get; set; }

    public long? MovimientoAjusteId { get; set; }

    public int CreadoPorUsuarioId { get; set; }

    public byte[] VersionFila { get; set; } = null!;

    public virtual Bodegas Bodega { get; set; } = null!;

    public virtual Usuarios? CerradoPorUsuario { get; set; }

    public virtual ICollection<ConteoFisicoDetalles> ConteoFisicoDetalles { get; set; } = new List<ConteoFisicoDetalles>();

    public virtual Usuarios CreadoPorUsuario { get; set; } = null!;

    public virtual Empresas Empresa { get; set; } = null!;

    public virtual EstadosFlujo EstadosFlujo { get; set; } = null!;

    public virtual Usuarios? IniciadoPorUsuario { get; set; }

    public virtual Movimientos? MovimientoAjuste { get; set; }

    public virtual Sucursales Sucursal { get; set; } = null!;

    public virtual Catalogos TipoConteo { get; set; } = null!;
}
