using Heladeria.Api.Seguridad.Autorizacion;
using Heladeria.Aplicacion.Configuracion;
using Heladeria.Aplicacion.Seguridad.Autorizacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Heladeria.Api.Controllers;

[ApiController, Authorize]
[Route("api/configuracion/estados")]
public sealed class EstadosController(IServicioEstadosTiposConfiguracion servicio) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<RespuestaPaginada<EstadoDto>>> Listar([FromQuery] FiltroEstadosSolicitud filtro, CancellationToken token) => Ok(await servicio.ListarEstadosAsync(filtro, token));
    [HttpGet("{id:int}")]
    public async Task<ActionResult<EstadoDto>> Obtener(int id, CancellationToken token) => Ok(await servicio.ObtenerEstadoAsync(id, token));
    [HttpPost, RequierePermiso(PermisosSistema.ConfiguracionAdministrar)]
    public async Task<ActionResult<EstadoDto>> Crear(GuardarEstadoSolicitud solicitud, CancellationToken token)
    {
        var creado = await servicio.CrearEstadoAsync(solicitud, User.ObtenerContextoConfiguracion(HttpContext), token);
        return CreatedAtAction(nameof(Obtener), new { id = creado.Id }, creado);
    }
    [HttpPut("{id:int}"), RequierePermiso(PermisosSistema.ConfiguracionAdministrar)]
    public async Task<ActionResult<EstadoDto>> Actualizar(int id, GuardarEstadoSolicitud solicitud, CancellationToken token) => Ok(await servicio.ActualizarEstadoAsync(id, solicitud, User.ObtenerContextoConfiguracion(HttpContext), token));
    [HttpPut("{id:int}/estado"), RequierePermiso(PermisosSistema.ConfiguracionAdministrar)]
    public async Task<ActionResult<EstadoDto>> CambiarEstado(int id, CambiarActivoSolicitud solicitud, CancellationToken token) => Ok(await servicio.CambiarActivoEstadoAsync(id, solicitud.Activo!.Value, User.ObtenerContextoConfiguracion(HttpContext), token));
}
