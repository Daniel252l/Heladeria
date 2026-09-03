using Heladeria.Api.Seguridad.Autorizacion;
using Heladeria.Aplicacion.Configuracion;
using Heladeria.Aplicacion.Seguridad.Autorizacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Heladeria.Api.Controllers;

[ApiController, Authorize]
[Route("api/configuracion/acciones-sistema")]
public sealed class AccionesSistemaController(IServicioCatalogosConfiguracion servicio) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<RespuestaPaginada<AccionSistemaDto>>> Listar([FromQuery] FiltroConEstadoSolicitud filtro, CancellationToken token) => Ok(await servicio.ListarAccionesAsync(filtro, token));
    [HttpGet("{id:int}")]
    public async Task<ActionResult<AccionSistemaDto>> Obtener(int id, CancellationToken token) => Ok(await servicio.ObtenerAccionAsync(id, token));
    [HttpPost, RequierePermiso(PermisosSistema.ConfiguracionAdministrar)]
    public async Task<ActionResult<AccionSistemaDto>> Crear(GuardarAccionSistemaSolicitud solicitud, CancellationToken token)
    {
        var creado = await servicio.CrearAccionAsync(solicitud, User.ObtenerContextoConfiguracion(HttpContext), token);
        return CreatedAtAction(nameof(Obtener), new { id = creado.Id }, creado);
    }
    [HttpPut("{id:int}"), RequierePermiso(PermisosSistema.ConfiguracionAdministrar)]
    public async Task<ActionResult<AccionSistemaDto>> Actualizar(int id, GuardarAccionSistemaSolicitud solicitud, CancellationToken token) => Ok(await servicio.ActualizarAccionAsync(id, solicitud, User.ObtenerContextoConfiguracion(HttpContext), token));
    [HttpPut("{id:int}/estado"), RequierePermiso(PermisosSistema.ConfiguracionAdministrar)]
    public async Task<ActionResult<AccionSistemaDto>> CambiarEstado(int id, CambiarEstadoSolicitud solicitud, CancellationToken token) => Ok(await servicio.CambiarEstadoAccionAsync(id, solicitud.EstadoId, User.ObtenerContextoConfiguracion(HttpContext), token));
}
