using Heladeria.Api.Seguridad.Autorizacion;
using Heladeria.Aplicacion.Configuracion;
using Heladeria.Aplicacion.Seguridad.Autorizacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Heladeria.Api.Controllers;

[ApiController, Authorize]
[Route("api/configuracion/clases-tipo")]
public sealed class ClasesTipoController(IServicioEstadosTiposConfiguracion servicio) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<RespuestaPaginada<ClaseTipoDto>>> Listar([FromQuery] FiltroConEstadoSolicitud filtro, CancellationToken token) => Ok(await servicio.ListarClasesTipoAsync(filtro, token));
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ClaseTipoDto>> Obtener(int id, CancellationToken token) => Ok(await servicio.ObtenerClaseTipoAsync(id, token));
    [HttpPost, RequierePermiso(PermisosSistema.ConfiguracionAdministrar)]
    public async Task<ActionResult<ClaseTipoDto>> Crear(GuardarClaseTipoSolicitud solicitud, CancellationToken token)
    {
        var creado = await servicio.CrearClaseTipoAsync(solicitud, User.ObtenerContextoConfiguracion(HttpContext), token);
        return CreatedAtAction(nameof(Obtener), new { id = creado.Id }, creado);
    }
    [HttpPut("{id:int}"), RequierePermiso(PermisosSistema.ConfiguracionAdministrar)]
    public async Task<ActionResult<ClaseTipoDto>> Actualizar(int id, GuardarClaseTipoSolicitud solicitud, CancellationToken token) => Ok(await servicio.ActualizarClaseTipoAsync(id, solicitud, User.ObtenerContextoConfiguracion(HttpContext), token));
    [HttpPut("{id:int}/estado"), RequierePermiso(PermisosSistema.ConfiguracionAdministrar)]
    public async Task<ActionResult<ClaseTipoDto>> CambiarEstado(int id, CambiarEstadoSolicitud solicitud, CancellationToken token) => Ok(await servicio.CambiarEstadoClaseTipoAsync(id, solicitud.EstadoId, User.ObtenerContextoConfiguracion(HttpContext), token));
}
