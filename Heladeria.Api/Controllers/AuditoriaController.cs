using Heladeria.Api.Seguridad.Autorizacion;
using Heladeria.Aplicacion.Configuracion;
using Heladeria.Aplicacion.Reportes;
using Heladeria.Aplicacion.Seguridad.Autorizacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Heladeria.Api.Controllers;

[ApiController, Authorize, Route("api/auditoria"), RequierePermiso(PermisosSistema.AuditoriaConsultar)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class AuditoriaController(IServicioReportes servicio) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<RespuestaPaginada<AuditoriaDto>>> Listar([FromQuery] FiltroAuditoria f, CancellationToken t) =>
        Ok(await servicio.AuditoriaAsync(f, User.ObtenerContextoConfiguracion(HttpContext), t));
}
