using Heladeria.Api.Seguridad.Autorizacion;
using Heladeria.Aplicacion.Configuracion;
using Heladeria.Aplicacion.Seguridad.Autorizacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Heladeria.Api.Controllers;

[ApiController, Authorize]
[Route("api/configuracion/clases-estado")]
public sealed class ClasesEstadoController(IServicioEstadosTiposConfiguracion servicio) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<RespuestaPaginada<ClaseEstadoDto>>> Listar([FromQuery] FiltroActivosSolicitud filtro, CancellationToken token) =>
        Ok(await servicio.ListarClasesEstadoAsync(filtro, token));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ClaseEstadoDto>> Obtener(int id, CancellationToken token) =>
        Ok(await servicio.ObtenerClaseEstadoAsync(id, token));

    [HttpPost, RequierePermiso(PermisosSistema.ConfiguracionAdministrar)]
    public async Task<ActionResult<ClaseEstadoDto>> Crear(GuardarClaseEstadoSolicitud solicitud, CancellationToken token)
    {
        var creado = await servicio.CrearClaseEstadoAsync(solicitud, User.ObtenerContextoConfiguracion(HttpContext), token);
        return CreatedAtAction(nameof(Obtener), new { id = creado.Id }, creado);
    }

    [HttpPut("{id:int}"), RequierePermiso(PermisosSistema.ConfiguracionAdministrar)]
    public async Task<ActionResult<ClaseEstadoDto>> Actualizar(int id, GuardarClaseEstadoSolicitud solicitud, CancellationToken token) =>
        Ok(await servicio.ActualizarClaseEstadoAsync(id, solicitud, User.ObtenerContextoConfiguracion(HttpContext), token));

    [HttpPut("{id:int}/estado"), RequierePermiso(PermisosSistema.ConfiguracionAdministrar)]
    public async Task<ActionResult<ClaseEstadoDto>> CambiarEstado(int id, CambiarActivoSolicitud solicitud, CancellationToken token) =>
        Ok(await servicio.CambiarActivoClaseEstadoAsync(id, solicitud.Activo!.Value, User.ObtenerContextoConfiguracion(HttpContext), token));
}
