using Heladeria.Api.Configuracion;
using Heladeria.Api.Seguridad.Autorizacion;
using Heladeria.Api.Seguridad.Contrasenas;
using Heladeria.Aplicacion.Seguridad.Autenticacion;
using Heladeria.Aplicacion.Seguridad.Configuracion;
using Heladeria.Aplicacion.Seguridad.Excepciones;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace Heladeria.Api.Controllers;

[ApiController]
[Route("api/autenticacion")]
public sealed class AutenticacionController(
    IServicioAutenticacion servicioAutenticacion,
    IOptions<OpcionesSeguridad> opciones,
    IWebHostEnvironment ambiente) : ControllerBase
{
    private readonly OpcionesSeguridad _opciones = opciones.Value;

    [HttpPost("iniciar-sesion")]
    [AllowAnonymous]
    [EnableRateLimiting(ConfiguracionRateLimiting.Login)]
    [ProducesResponseType<IniciarSesionRespuesta>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<IniciarSesionRespuesta>> IniciarSesion(
        IniciarSesionSolicitud solicitud,
        CancellationToken cancellationToken)
    {
        var resultado = await servicioAutenticacion.IniciarSesionAsync(
            solicitud,
            CrearContextoAcceso(),
            cancellationToken);
        EscribirCookieRefresh(resultado);
        return Ok(resultado.Respuesta);
    }

    [HttpPost("renovar")]
    [AllowAnonymous]
    [EnableRateLimiting(ConfiguracionRateLimiting.Renovacion)]
    [ProducesResponseType<IniciarSesionRespuesta>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<IniciarSesionRespuesta>> Renovar(
        CancellationToken cancellationToken)
    {
        var resultado = await servicioAutenticacion.RenovarAsync(
            ObtenerRefreshToken(),
            CrearContextoAcceso(),
            cancellationToken);
        EscribirCookieRefresh(resultado);
        return Ok(resultado.Respuesta);
    }

    [HttpPost("seleccionar-sucursal")]
    [Authorize]
    [ProducesResponseType<IniciarSesionRespuesta>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IniciarSesionRespuesta>> SeleccionarSucursal(
        SeleccionarSucursalSolicitud solicitud,
        CancellationToken cancellationToken)
    {
        var identidad = User.ObtenerContextoSeguridad();
        var resultado = await servicioAutenticacion.SeleccionarSucursalAsync(
            identidad.UsuarioId,
            identidad.SesionId,
            identidad.SucursalId,
            ObtenerRefreshToken(),
            solicitud,
            CrearContextoAcceso(),
            cancellationToken);
        EscribirCookieRefresh(resultado);
        return Ok(resultado.Respuesta);
    }

    [HttpPost("cerrar-sesion")]
    [Authorize]
    [PermitirContrasenaTemporal]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> CerrarSesion(CancellationToken cancellationToken)
    {
        var identidad = User.ObtenerContextoSeguridad();
        await servicioAutenticacion.CerrarSesionAsync(
            identidad.UsuarioId,
            identidad.SesionId,
            identidad.SucursalId,
            CrearContextoAcceso(),
            cancellationToken);
        EliminarCookieRefresh();
        return NoContent();
    }

    [HttpPost("cambiar-contrasena")]
    [Authorize]
    [PermitirContrasenaTemporal]
    [EnableRateLimiting(ConfiguracionRateLimiting.CambioContrasena)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CambiarContrasena(
        CambiarContrasenaSolicitud solicitud,
        CancellationToken cancellationToken)
    {
        var identidad = User.ObtenerContextoSeguridad();
        await servicioAutenticacion.CambiarContrasenaAsync(
            identidad.UsuarioId,
            identidad.SucursalId,
            solicitud,
            CrearContextoAcceso(),
            cancellationToken);
        EliminarCookieRefresh();
        return Ok(new
        {
            exito = true,
            mensaje = "Contraseña actualizada. Inicie sesión nuevamente."
        });
    }

    [HttpGet("sesion")]
    [Authorize]
    [ProducesResponseType<SesionActualRespuesta>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SesionActualRespuesta>> ObtenerSesion(
        CancellationToken cancellationToken)
    {
        var identidad = User.ObtenerContextoSeguridad();
        var sesion = await servicioAutenticacion.ObtenerSesionAsync(
            identidad.UsuarioId,
            identidad.SucursalId,
            cancellationToken);
        return Ok(sesion);
    }

    private string ObtenerRefreshToken() =>
        Request.Cookies.TryGetValue(_opciones.CookieRefresh.Nombre, out var refreshToken)
            && !string.IsNullOrWhiteSpace(refreshToken)
                ? refreshToken
                : throw new TokenRenovacionInvalidoException();

    private ContextoAcceso CrearContextoAcceso() => new(
        HttpContext.Connection.RemoteIpAddress?.ToString(),
        Request.Headers.UserAgent.ToString());

    private void EscribirCookieRefresh(ResultadoAutenticacion resultado)
    {
        Response.Cookies.Append(
            _opciones.CookieRefresh.Nombre,
            resultado.RefreshToken,
            CrearOpcionesCookie(resultado.ExpiraRefreshTokenUtc));
    }

    private void EliminarCookieRefresh()
    {
        Response.Cookies.Delete(
            _opciones.CookieRefresh.Nombre,
            CrearOpcionesCookie(DateTimeOffset.UnixEpoch));
    }

    private CookieOptions CrearOpcionesCookie(DateTimeOffset expiracion) => new()
    {
        HttpOnly = true,
        Secure = !ambiente.IsDevelopment() || Request.IsHttps,
        SameSite = Enum.TryParse<SameSiteMode>(
            _opciones.CookieRefresh.SameSite,
            ignoreCase: true,
            out var sameSite)
                ? sameSite
                : SameSiteMode.Strict,
        Path = "/api/autenticacion",
        Expires = expiracion,
        IsEssential = true
    };
}
