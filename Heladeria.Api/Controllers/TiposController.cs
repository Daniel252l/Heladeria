using Heladeria.Api.Seguridad.Autorizacion;
using Heladeria.Aplicacion.Configuracion;
using Heladeria.Aplicacion.Seguridad.Autorizacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Heladeria.Api.Controllers;

[ApiController, Authorize]
[Route("api/configuracion/tipos")]
public sealed class TiposController(IServicioEstadosTiposConfiguracion servicio) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<RespuestaPaginada<TipoDto>>> Listar([FromQuery] FiltroTiposSolicitud filtro, CancellationToken token) => Ok(await servicio.ListarTiposAsync(filtro, token));
    [HttpGet("{id:int}")]
    public async Task<ActionResult<TipoDto>> Obtener(int id, CancellationToken token) => Ok(await servicio.ObtenerTipoAsync(id, token));
    [HttpPost, RequierePermiso(PermisosSistema.ConfiguracionAdministrar)]
    public async Task<ActionResult<TipoDto>> Crear(GuardarTipoSolicitud solicitud, CancellationToken token)
    {
        var creado = await servicio.CrearTipoAsync(solicitud, User.ObtenerContextoConfiguracion(HttpContext), token);
        return CreatedAtAction(nameof(Obtener), new { id = creado.Id }, creado);
    }
    [HttpPut("{id:int}"), RequierePermiso(PermisosSistema.ConfiguracionAdministrar)]
    public async Task<ActionResult<TipoDto>> Actualizar(int id, GuardarTipoSolicitud solicitud, CancellationToken token) => Ok(await servicio.ActualizarTipoAsync(id, solicitud, User.ObtenerContextoConfiguracion(HttpContext), token));
    [HttpPut("{id:int}/estado"), RequierePermiso(PermisosSistema.ConfiguracionAdministrar)]
    public async Task<ActionResult<TipoDto>> CambiarEstado(int id, CambiarEstadoSolicitud solicitud, CancellationToken token) => Ok(await servicio.CambiarEstadoTipoAsync(id, solicitud.EstadoId, User.ObtenerContextoConfiguracion(HttpContext), token));
}
