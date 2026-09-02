using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class Empleados
{
    public int Id { get; set; }

    public int EmpresaId { get; set; }

    public long SucursalId { get; set; }

    public int? UsuarioId { get; set; }

    public string CodigoEmpleado { get; set; } = null!;

    public string Nombres { get; set; } = null!;

    public string Apellidos { get; set; } = null!;

    public string? Dpi { get; set; }

    public int CargoId { get; set; }

    public decimal? SalarioBase { get; set; }

    public int EstadoId { get; set; }

    public virtual ICollection<AsistenciaPersonal> AsistenciaPersonal { get; set; } = new List<AsistenciaPersonal>();

    public virtual Tipos Cargo { get; set; } = null!;

    public virtual Empresas Empresa { get; set; } = null!;

    public virtual Estados Estado { get; set; } = null!;

    public virtual Sucursales Sucursal { get; set; } = null!;

    public virtual Usuarios? Usuario { get; set; }
}
