using System.ComponentModel.DataAnnotations;

namespace Heladeria.Aplicacion.Seguridad.Autenticacion;

public sealed class IniciarSesionSolicitud
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Identificador { get; init; } = string.Empty;

    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Contrasena { get; init; } = string.Empty;

    public long? SucursalId { get; init; }
}

public sealed class CambiarContrasenaSolicitud
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string ContrasenaActual { get; init; } = string.Empty;

    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string NuevaContrasena { get; init; } = string.Empty;

    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Confirmacion { get; init; } = string.Empty;
}

public sealed class SeleccionarSucursalSolicitud
{
    public long SucursalId { get; init; }
}

public sealed record ContextoAcceso(string? DireccionIp, string? Dispositivo);

public sealed record SucursalUsuarioRespuesta(
    long Id,
    string Nombre,
    int EmpresaId,
    string Empresa,
    bool EsPrincipal)
{
    public string ZonaHoraria { get; init; } = "UTC";
}

public sealed record UsuarioAutenticadoRespuesta(
    int Id,
    string NombreUsuario,
    string Correo,
    string RolId,
    string Rol,
    SucursalUsuarioRespuesta SucursalActiva,
    IReadOnlyList<SucursalUsuarioRespuesta> Sucursales);

public sealed record IniciarSesionRespuesta(
    bool Exito,
    string AccessToken,
    DateTimeOffset ExpiraAccessTokenUtc,
    UsuarioAutenticadoRespuesta Usuario,
    bool RequiereCambioContrasena);

public sealed record ResultadoAutenticacion(
    IniciarSesionRespuesta Respuesta,
    string RefreshToken,
    DateTimeOffset ExpiraRefreshTokenUtc);

public sealed record SesionActualRespuesta(
    UsuarioAutenticadoRespuesta Usuario,
    bool RequiereCambioContrasena,
    IReadOnlyList<string> Permisos);
