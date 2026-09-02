using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class Bitacoras
{
    public long Id { get; set; }

    public int? EmpresaId { get; set; }

    public long? SucursalId { get; set; }

    public string? ModuloId { get; set; }

    public string Entidad { get; set; } = null!;

    public string? EntidadId { get; set; }

    public string Accion { get; set; } = null!;

    public DateTime Fecha { get; set; }

    public int? UsuarioId { get; set; }

    public string? DireccionIp { get; set; }

    public string? Equipo { get; set; }

    public string? ValorAnterior { get; set; }

    public string? ValorNuevo { get; set; }

    public string? Comentario { get; set; }

    public virtual Empresas? Empresa { get; set; }

    public virtual Modulos? Modulo { get; set; }

    public virtual Sucursales? Sucursal { get; set; }

    public virtual Usuarios? Usuario { get; set; }
}
