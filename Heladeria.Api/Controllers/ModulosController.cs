using Heladeria.Api.Seguridad.Autorizacion;
using Heladeria.Aplicacion.Configuracion;
using Heladeria.Aplicacion.Seguridad.Autorizacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Heladeria.Api.Controllers;

[ApiController, Authorize]
[Route("api/configuracion/modulos")]
public sealed class ModulosController(IServicioCatalogosConfiguracion servicio) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<RespuestaPaginada<ModuloDto>>> Listar([FromQuery] FiltroActivosSolicitud filtro, CancellationToken token) => Ok(await servicio.ListarModulosAsync(filtro, token));
    [HttpGet("{id}")]
    public async Task<ActionResult<ModuloDto>> Obtener(string id, CancellationToken token) => Ok(await servicio.ObtenerModuloAsync(id, token));
    [HttpPost, RequierePermiso(PermisosSistema.ConfiguracionAdministrar)]
    public async Task<ActionResult<ModuloDto>> Crear(GuardarModuloSolicitud solicitud, CancellationToken token)
    {
        var creado = await servicio.CrearModuloAsync(solicitud, User.ObtenerContextoConfiguracion(HttpContext), token);
        return CreatedAtAction(nameof(Obtener), new { id = creado.Id }, creado);
    }
    [HttpPut("{id}"), RequierePermiso(PermisosSistema.ConfiguracionAdministrar)]
    public async Task<ActionResult<ModuloDto>> Actualizar(string id, ActualizarModuloSolicitud solicitud, CancellationToken token) => Ok(await servicio.ActualizarModuloAsync(id, solicitud, User.ObtenerContextoConfiguracion(HttpContext), token));
    [HttpPut("{id}/estado"), RequierePermiso(PermisosSistema.ConfiguracionAdministrar)]
    public async Task<ActionResult<ModuloDto>> CambiarEstado(string id, CambiarActivoSolicitud solicitud, CancellationToken token) => Ok(await servicio.CambiarActivoModuloAsync(id, solicitud.Activo!.Value, User.ObtenerContextoConfiguracion(HttpContext), token));
}
