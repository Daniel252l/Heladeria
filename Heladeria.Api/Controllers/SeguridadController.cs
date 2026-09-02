using Heladeria.Api.Seguridad.Autorizacion;
using Heladeria.Aplicacion.Seguridad.Autorizacion;
using Microsoft.AspNetCore.Mvc;

namespace Heladeria.Api.Controllers;

[ApiController]
[Route("api/seguridad")]
public sealed class SeguridadController(IServicioPermisos servicioPermisos) : ControllerBase
{
    [HttpGet("permisos")]
    [RequierePermiso(PermisosSistema.SeguridadAdministrar)]
    [ProducesResponseType<IReadOnlyList<string>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<string>>> ObtenerPermisos(
        CancellationToken cancellationToken)
    {
        var identidad = User.ObtenerContextoSeguridad();
        var permisos = await servicioPermisos.ObtenerPermisosAsync(
            identidad.UsuarioId,
            cancellationToken);
        return Ok(permisos);
    }
}

