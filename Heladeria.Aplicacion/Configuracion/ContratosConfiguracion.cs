using System.ComponentModel.DataAnnotations;

namespace Heladeria.Aplicacion.Configuracion;

public static class PermisosConfiguracion
{
    public const string Administrar = "CONFIGURACION.ADMINISTRAR";
}

public sealed record ContextoConfiguracion(
    int UsuarioId,
    string RolId,
    int EmpresaId,
    long SucursalId,
    string? DireccionIp,
    string? Equipo);

public sealed record RespuestaPaginada<T>(
    IReadOnlyList<T> Elementos,
    int Pagina,
    int TamanioPagina,
    int TotalRegistros)
{
    public int TotalPaginas => TotalRegistros == 0
        ? 0
        : (int)Math.Ceiling(TotalRegistros / (double)TamanioPagina);
}

public abstract class FiltroPaginadoBase
{
    [Range(1, int.MaxValue)]
    public int Pagina { get; init; } = 1;

    [Range(1, 100)]
    public int TamanioPagina { get; init; } = 20;

    [StringLength(150)]
    public string? Busqueda { get; init; }

    // Los contratos operativos existentes permiten páginas vacías fuera de rango; nunca desbordar a un offset negativo.
    public int CalcularDesplazamiento() => (int)Math.Clamp(((long)Pagina - 1) * TamanioPagina, 0, int.MaxValue);
}

public sealed class FiltroActivosSolicitud : FiltroPaginadoBase
{
    public bool? Activo { get; init; }
}

public sealed class FiltroEstadosSolicitud : FiltroPaginadoBase
{
    public bool? Activo { get; init; }

    [Range(1, int.MaxValue)]
    public int? ClaseEstadoId { get; init; }

    [StringLength(20)]
    public string? ModuloId { get; init; }
}

public sealed class FiltroConEstadoSolicitud : FiltroPaginadoBase
{
    [Range(1, int.MaxValue)]
    public int? EstadoId { get; init; }
}

public sealed class FiltroTiposSolicitud : FiltroPaginadoBase
{
    [Range(1, int.MaxValue)]
    public int? EstadoId { get; init; }

    [Range(1, int.MaxValue)]
    public int? ClaseTipoId { get; init; }

    [StringLength(20)]
    public string? ModuloId { get; init; }
}

public sealed class FiltroNivelesSolicitud : FiltroPaginadoBase;

public sealed class FiltroSucursalesSolicitud : FiltroPaginadoBase
{
    [Range(1, int.MaxValue)]
    public int? EstadoId { get; init; }

    [Range(1, int.MaxValue)]
    public int? EmpresaId { get; init; }
}

public sealed class FiltroReferenciasOrganizacionSolicitud : FiltroPaginadoBase
{
    [Required, RegularExpression("^(PROVEEDORES|CLIENTES|DEPARTAMENTOS|MUNICIPIOS)$")]
    public string Tipo { get; init; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int? EmpresaId { get; init; }

    [Range(1, int.MaxValue)]
    public int? DepartamentoId { get; init; }
}

public sealed class CambiarActivoSolicitud
{
    [Required]
    public bool? Activo { get; init; }
}

public sealed class CambiarEstadoSolicitud
{
    [Range(1, int.MaxValue)]
    public int EstadoId { get; init; }
}

public sealed class GuardarClaseEstadoSolicitud
{
    [Required, StringLength(200, MinimumLength = 1)]
    public string Nombre { get; init; } = string.Empty;

    public bool Activo { get; init; } = true;
}

public sealed class GuardarEstadoSolicitud
{
    [Required, StringLength(100, MinimumLength = 1)]
    public string Nombre { get; init; } = string.Empty;

    public bool Activo { get; init; } = true;

    [Range(1, int.MaxValue)]
    public int ClaseEstadoId { get; init; }

    [StringLength(20)]
    public string? ModuloId { get; init; }
}

public sealed class GuardarClaseTipoSolicitud
{
    [Required, StringLength(100, MinimumLength = 1)]
    public string Nombre { get; init; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int EstadoId { get; init; }
}

public sealed class GuardarTipoSolicitud
{
    [Required, StringLength(100, MinimumLength = 1)]
    public string Nombre { get; init; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int EstadoId { get; init; }

    [StringLength(20)]
    public string? ModuloId { get; init; }

    [Range(1, int.MaxValue)]
    public int ClaseTipoId { get; init; }
}

public sealed class GuardarModuloSolicitud
{
    [Required, StringLength(20, MinimumLength = 1)]
    [RegularExpression("^[A-Za-z0-9_-]+$")]
    public string Id { get; init; } = string.Empty;

    [Required, StringLength(50, MinimumLength = 1)]
    public string Nombre { get; init; } = string.Empty;

    [Required, StringLength(50, MinimumLength = 1)]
    public string Icono { get; init; } = string.Empty;

    public bool Activo { get; init; } = true;
}

public sealed class ActualizarModuloSolicitud
{
    [Required, StringLength(50, MinimumLength = 1)]
    public string Nombre { get; init; } = string.Empty;

    [Required, StringLength(50, MinimumLength = 1)]
    public string Icono { get; init; } = string.Empty;
}

public sealed class GuardarAccionSistemaSolicitud
{
    [Required, StringLength(50, MinimumLength = 1)]
    [RegularExpression("^[A-Za-z0-9_.-]+$")]
    public string Codigo { get; init; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 1)]
    public string Nombre { get; init; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int EstadoId { get; init; }
}

public sealed class GuardarNivelAccesoSolicitud
{
    [Required, StringLength(100, MinimumLength = 1)]
    public string Nombre { get; init; } = string.Empty;
}

public sealed class GuardarEmpresaSolicitud
{
    [Required, StringLength(150, MinimumLength = 1)]
    public string RazonSocial { get; init; } = string.Empty;

    [StringLength(150)]
    public string? NombreComercial { get; init; }

    [Required, StringLength(150, MinimumLength = 1)]
    public string Eslogan { get; init; } = string.Empty;

    [Required, StringLength(250, MinimumLength = 1)]
    public string Direccion { get; init; } = string.Empty;

    [Required, StringLength(25, MinimumLength = 1)]
    public string Nit { get; init; } = string.Empty;

    [StringLength(25)]
    public string? Dpi { get; init; }

    [Required, StringLength(100, MinimumLength = 1)]
    public string Telefono { get; init; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 1), EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 1)]
    public string Imagen { get; init; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 1)]
    public string Icono { get; init; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 1)]
    public string ImagenReportes { get; init; } = string.Empty;

    [StringLength(150)]
    public string? Url { get; init; }

    [Range(1, int.MaxValue)]
    public int EstadoId { get; init; }

    [Range(1, int.MaxValue)]
    public int? ClienteId { get; init; }
}

public sealed class GuardarSucursalSolicitud
{
    [Required, StringLength(150, MinimumLength = 1)]
    public string Nombre { get; init; } = string.Empty;

    [Required, StringLength(250, MinimumLength = 1)]
    public string Direccion { get; init; } = string.Empty;

    [Required, StringLength(50, MinimumLength = 1)]
    public string DireccionCorta { get; init; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 1)]
    public string Telefono { get; init; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 1), EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int EmpresaId { get; init; }

    [Range(1, int.MaxValue)]
    public int? MunicipioId { get; init; }

    [Range(1, int.MaxValue)]
    public int? DepartamentoId { get; init; }

    public bool EsCentral { get; init; }

    [Range(1, int.MaxValue)]
    public int? ClienteId { get; init; }

    [Range(1, int.MaxValue)]
    public int EstadoId { get; init; }

    [Range(1, int.MaxValue)]
    public int ProveedorId { get; init; }

    public bool EsBodega { get; init; }

    [Range(typeof(long), "1", "9223372036854775807")]
    public long? SucursalCentralId { get; init; }

    [StringLength(50)]
    public string? Imagen { get; init; }

    [Range(typeof(long), "1", "9223372036854775807")]
    public long? SucursalTransporteId { get; init; }

    [StringLength(100)]
    public string? RepresentanteLegal { get; init; }

    [StringLength(25)]
    public string? Codigo { get; init; }

    [StringLength(100)]
    public string? ImagenReportes { get; init; }

    public bool Transportes { get; init; }
}

public sealed record ClaseEstadoDto(int Id, string Nombre, bool Activo);
public sealed record EstadoDto(int Id, string Nombre, bool Activo, int ClaseEstadoId, string ClaseEstado, string? ModuloId);
public sealed record ClaseTipoDto(int Id, string Nombre, int EstadoId, string Estado);
public sealed record TipoDto(int Id, string Nombre, int EstadoId, string Estado, string? ModuloId, int ClaseTipoId, string ClaseTipo);
public sealed record ModuloDto(string Id, string Nombre, string Icono, bool Activo);
public sealed record AccionSistemaDto(int Id, string Codigo, string Nombre, int EstadoId, string Estado);
public sealed record NivelAccesoDto(int Id, string Nombre);
public sealed record EmpresaDto(int Id, string RazonSocial, string? NombreComercial, string Eslogan, string Direccion, string Nit, string? Dpi, string Telefono, string Email, string Imagen, string Icono, string ImagenReportes, string? Url, int EstadoId, string Estado, int? ClienteId);
public sealed record SucursalDto(long Id, string Nombre, string Direccion, string DireccionCorta, string Telefono, string Email, int EmpresaId, string Empresa, int? MunicipioId, int? DepartamentoId, bool EsCentral, int? ClienteId, int EstadoId, string Estado, int ProveedorId, string Proveedor, bool EsBodega, long? SucursalCentralId, string? Imagen, long? SucursalTransporteId, string? RepresentanteLegal, string? Codigo, string? ImagenReportes, bool Transportes, string CodigoNombre);
public sealed record OpcionReferenciaOrganizacionDto(int Id, string Codigo, string Nombre);

public interface IServicioEstadosTiposConfiguracion
{
    Task<RespuestaPaginada<ClaseEstadoDto>> ListarClasesEstadoAsync(FiltroActivosSolicitud filtro, CancellationToken cancellationToken);
    Task<ClaseEstadoDto> ObtenerClaseEstadoAsync(int id, CancellationToken cancellationToken);
    Task<ClaseEstadoDto> CrearClaseEstadoAsync(GuardarClaseEstadoSolicitud solicitud, ContextoConfiguracion contexto, CancellationToken cancellationToken);
    Task<ClaseEstadoDto> ActualizarClaseEstadoAsync(int id, GuardarClaseEstadoSolicitud solicitud, ContextoConfiguracion contexto, CancellationToken cancellationToken);
    Task<ClaseEstadoDto> CambiarActivoClaseEstadoAsync(int id, bool activo, ContextoConfiguracion contexto, CancellationToken cancellationToken);
    Task<RespuestaPaginada<EstadoDto>> ListarEstadosAsync(FiltroEstadosSolicitud filtro, CancellationToken cancellationToken);
    Task<EstadoDto> ObtenerEstadoAsync(int id, CancellationToken cancellationToken);
    Task<EstadoDto> CrearEstadoAsync(GuardarEstadoSolicitud solicitud, ContextoConfiguracion contexto, CancellationToken cancellationToken);
    Task<EstadoDto> ActualizarEstadoAsync(int id, GuardarEstadoSolicitud solicitud, ContextoConfiguracion contexto, CancellationToken cancellationToken);
    Task<EstadoDto> CambiarActivoEstadoAsync(int id, bool activo, ContextoConfiguracion contexto, CancellationToken cancellationToken);
    Task<RespuestaPaginada<ClaseTipoDto>> ListarClasesTipoAsync(FiltroConEstadoSolicitud filtro, CancellationToken cancellationToken);
    Task<ClaseTipoDto> ObtenerClaseTipoAsync(int id, CancellationToken cancellationToken);
    Task<ClaseTipoDto> CrearClaseTipoAsync(GuardarClaseTipoSolicitud solicitud, ContextoConfiguracion contexto, CancellationToken cancellationToken);
    Task<ClaseTipoDto> ActualizarClaseTipoAsync(int id, GuardarClaseTipoSolicitud solicitud, ContextoConfiguracion contexto, CancellationToken cancellationToken);
    Task<ClaseTipoDto> CambiarEstadoClaseTipoAsync(int id, int estadoId, ContextoConfiguracion contexto, CancellationToken cancellationToken);
    Task<RespuestaPaginada<TipoDto>> ListarTiposAsync(FiltroTiposSolicitud filtro, CancellationToken cancellationToken);
    Task<TipoDto> ObtenerTipoAsync(int id, CancellationToken cancellationToken);
    Task<TipoDto> CrearTipoAsync(GuardarTipoSolicitud solicitud, ContextoConfiguracion contexto, CancellationToken cancellationToken);
    Task<TipoDto> ActualizarTipoAsync(int id, GuardarTipoSolicitud solicitud, ContextoConfiguracion contexto, CancellationToken cancellationToken);
    Task<TipoDto> CambiarEstadoTipoAsync(int id, int estadoId, ContextoConfiguracion contexto, CancellationToken cancellationToken);
}

public interface IServicioCatalogosConfiguracion
{
    Task<RespuestaPaginada<ModuloDto>> ListarModulosAsync(FiltroActivosSolicitud filtro, CancellationToken cancellationToken);
    Task<ModuloDto> ObtenerModuloAsync(string id, CancellationToken cancellationToken);
    Task<ModuloDto> CrearModuloAsync(GuardarModuloSolicitud solicitud, ContextoConfiguracion contexto, CancellationToken cancellationToken);
    Task<ModuloDto> ActualizarModuloAsync(string id, ActualizarModuloSolicitud solicitud, ContextoConfiguracion contexto, CancellationToken cancellationToken);
    Task<ModuloDto> CambiarActivoModuloAsync(string id, bool activo, ContextoConfiguracion contexto, CancellationToken cancellationToken);
    Task<RespuestaPaginada<AccionSistemaDto>> ListarAccionesAsync(FiltroConEstadoSolicitud filtro, CancellationToken cancellationToken);
    Task<AccionSistemaDto> ObtenerAccionAsync(int id, CancellationToken cancellationToken);
    Task<AccionSistemaDto> CrearAccionAsync(GuardarAccionSistemaSolicitud solicitud, ContextoConfiguracion contexto, CancellationToken cancellationToken);
    Task<AccionSistemaDto> ActualizarAccionAsync(int id, GuardarAccionSistemaSolicitud solicitud, ContextoConfiguracion contexto, CancellationToken cancellationToken);
    Task<AccionSistemaDto> CambiarEstadoAccionAsync(int id, int estadoId, ContextoConfiguracion contexto, CancellationToken cancellationToken);
    Task<RespuestaPaginada<NivelAccesoDto>> ListarNivelesAsync(FiltroNivelesSolicitud filtro, CancellationToken cancellationToken);
    Task<NivelAccesoDto> ObtenerNivelAsync(int id, CancellationToken cancellationToken);
    Task<NivelAccesoDto> CrearNivelAsync(GuardarNivelAccesoSolicitud solicitud, ContextoConfiguracion contexto, CancellationToken cancellationToken);
    Task<NivelAccesoDto> ActualizarNivelAsync(int id, GuardarNivelAccesoSolicitud solicitud, ContextoConfiguracion contexto, CancellationToken cancellationToken);
}

public interface IServicioOrganizacionConfiguracion
{
    Task<RespuestaPaginada<OpcionReferenciaOrganizacionDto>> ListarReferenciasAsync(FiltroReferenciasOrganizacionSolicitud filtro, ContextoConfiguracion contexto, CancellationToken cancellationToken);
    Task<RespuestaPaginada<EmpresaDto>> ListarEmpresasAsync(FiltroConEstadoSolicitud filtro, ContextoConfiguracion contexto, CancellationToken cancellationToken);
    Task<EmpresaDto> ObtenerEmpresaAsync(int id, ContextoConfiguracion contexto, CancellationToken cancellationToken);
    Task<EmpresaDto> CrearEmpresaAsync(GuardarEmpresaSolicitud solicitud, ContextoConfiguracion contexto, CancellationToken cancellationToken);
    Task<EmpresaDto> ActualizarEmpresaAsync(int id, GuardarEmpresaSolicitud solicitud, ContextoConfiguracion contexto, CancellationToken cancellationToken);
    Task<EmpresaDto> CambiarEstadoEmpresaAsync(int id, int estadoId, ContextoConfiguracion contexto, CancellationToken cancellationToken);
    Task<RespuestaPaginada<SucursalDto>> ListarSucursalesAsync(FiltroSucursalesSolicitud filtro, ContextoConfiguracion contexto, CancellationToken cancellationToken);
    Task<SucursalDto> ObtenerSucursalAsync(long id, ContextoConfiguracion contexto, CancellationToken cancellationToken);
    Task<SucursalDto> CrearSucursalAsync(GuardarSucursalSolicitud solicitud, ContextoConfiguracion contexto, CancellationToken cancellationToken);
    Task<SucursalDto> ActualizarSucursalAsync(long id, GuardarSucursalSolicitud solicitud, ContextoConfiguracion contexto, CancellationToken cancellationToken);
    Task<SucursalDto> CambiarEstadoSucursalAsync(long id, int estadoId, ContextoConfiguracion contexto, CancellationToken cancellationToken);
}

public interface IAuditoriaConfiguracion
{
    void Registrar(ContextoConfiguracion contexto, string entidad, string? entidadId, string accion, string comentario);
}
