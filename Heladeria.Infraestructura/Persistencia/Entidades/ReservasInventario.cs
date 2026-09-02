using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class ReservasInventario
{
    public long Id { get; set; }

    public int EmpresaId { get; set; }

    public long SucursalId { get; set; }

    public string CodigoReserva { get; set; } = null!;

    public string ModuloOrigen { get; set; } = null!;

    public long? DocumentoOrigenId { get; set; }

    public DateTime FechaReserva { get; set; }

    public DateTime? FechaExpiracion { get; set; }

    public int FlujoEstadoId { get; set; }

    public int EstadoId { get; set; }

    public string? Observaciones { get; set; }

    public int CreadoPorUsuarioId { get; set; }

    public int? LiberadoPorUsuarioId { get; set; }

    public DateTime? FechaLiberacion { get; set; }

    public byte[] VersionFila { get; set; } = null!;

    public virtual Usuarios CreadoPorUsuario { get; set; } = null!;

    public virtual Empresas Empresa { get; set; } = null!;

    public virtual EstadosFlujo EstadosFlujo { get; set; } = null!;

    public virtual Usuarios? LiberadoPorUsuario { get; set; }

    public virtual ICollection<ReservaInventarioDetalles> ReservaInventarioDetalles { get; set; } = new List<ReservaInventarioDetalles>();

    public virtual Sucursales Sucursal { get; set; } = null!;
}
