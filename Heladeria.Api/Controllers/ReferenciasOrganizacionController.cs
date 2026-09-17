using Heladeria.Api.Seguridad.Autorizacion;
using Heladeria.Aplicacion.Configuracion;
using Heladeria.Aplicacion.Seguridad.Autorizacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Heladeria.Api.Controllers;

[ApiController, Authorize]
[Route("api/configuracion/referencias-organizacion")]
public sealed class ReferenciasOrganizacionController(
    IServicioOrganizacionConfiguracion servicio) : ControllerBase
{
    [HttpGet, RequierePermiso(PermisosSistema.ConfiguracionAdministrar)]
    public async Task<ActionResult<RespuestaPaginada<OpcionReferenciaOrganizacionDto>>> Listar(
        [FromQuery] FiltroReferenciasOrganizacionSolicitud filtro,
        CancellationToken token) => Ok(await servicio.ListarReferenciasAsync(
            filtro,
            User.ObtenerContextoConfiguracion(HttpContext),
            token));
}
