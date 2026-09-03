using System.Security.Claims;
using Heladeria.Aplicacion.Configuracion;
using Heladeria.Aplicacion.Seguridad.Autorizacion;
using Heladeria.Aplicacion.Seguridad.Excepciones;

namespace Heladeria.Api.Seguridad.Autorizacion;

public static class ExtensionesClaimsPrincipal
{
    public static bool TryObtenerContextoSeguridad(
        this ClaimsPrincipal usuario,
        out int usuarioId,
        out long sesionId,
        out long sucursalId,
        out string rolId)
    {
        rolId = usuario.FindFirst(ClaimsSeguridad.RolId)?.Value ?? string.Empty;
        usuarioId = 0;
        sesionId = 0;
        sucursalId = 0;

        return int.TryParse(usuario.FindFirst(ClaimsSeguridad.Sujeto)?.Value, out usuarioId)
            && long.TryParse(usuario.FindFirst(ClaimsSeguridad.SesionId)?.Value, out sesionId)
            && long.TryParse(usuario.FindFirst(ClaimsSeguridad.SucursalId)?.Value, out sucursalId)
            && !string.IsNullOrWhiteSpace(rolId);
    }

    public static (int UsuarioId, long SesionId, long SucursalId) ObtenerContextoSeguridad(
        this ClaimsPrincipal usuario)
    {
        if (!usuario.TryObtenerContextoSeguridad(
                out var usuarioId,
                out var sesionId,
                out var sucursalId,
                out _))
        {
            throw new AccesoDenegadoException("El contexto de la sesión es inválido.");
        }

        return (usuarioId, sesionId, sucursalId);
    }

    public static ContextoConfiguracion ObtenerContextoConfiguracion(
        this ClaimsPrincipal usuario,
        HttpContext contextoHttp)
    {
        if (!usuario.TryObtenerContextoSeguridad(
                out var usuarioId,
                out _,
                out var sucursalId,
                out var rolId)
            || !int.TryParse(
                usuario.FindFirst(ClaimsSeguridad.EmpresaId)?.Value,
                out var empresaId))
        {
            throw new AccesoDenegadoException("El contexto de la sesión es inválido.");
        }

        return new ContextoConfiguracion(
            usuarioId,
            rolId,
            empresaId,
            sucursalId,
            contextoHttp.Connection.RemoteIpAddress?.ToString(),
            contextoHttp.Request.Headers.UserAgent.ToString());
    }
}
