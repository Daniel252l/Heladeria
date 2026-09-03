using Heladeria.Api.Seguridad.Autorizacion;
using Heladeria.Aplicacion.Configuracion;
using Heladeria.Aplicacion.Seguridad.Autorizacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Heladeria.Api.Controllers;

[ApiController, Authorize]
[Route("api/configuracion/niveles-acceso")]
public sealed class NivelesAccesoController(IServicioCatalogosConfiguracion servicio) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<RespuestaPaginada<NivelAccesoDto>>> Listar([FromQuery] FiltroNivelesSolicitud filtro, CancellationToken token) => Ok(await servicio.ListarNivelesAsync(filtro, token));
    [HttpGet("{id:int}")]
    public async Task<ActionResult<NivelAccesoDto>> Obtener(int id, CancellationToken token) => Ok(await servicio.ObtenerNivelAsync(id, token));
    [HttpPost, RequierePermiso(PermisosSistema.ConfiguracionAdministrar)]
    public async Task<ActionResult<NivelAccesoDto>> Crear(GuardarNivelAccesoSolicitud solicitud, CancellationToken token)
    {
        var creado = await servicio.CrearNivelAsync(solicitud, User.ObtenerContextoConfiguracion(HttpContext), token);
        return CreatedAtAction(nameof(Obtener), new { id = creado.Id }, creado);
    }
    [HttpPut("{id:int}"), RequierePermiso(PermisosSistema.ConfiguracionAdministrar)]
    public async Task<ActionResult<NivelAccesoDto>> Actualizar(int id, GuardarNivelAccesoSolicitud solicitud, CancellationToken token) => Ok(await servicio.ActualizarNivelAsync(id, solicitud, User.ObtenerContextoConfiguracion(HttpContext), token));
}
