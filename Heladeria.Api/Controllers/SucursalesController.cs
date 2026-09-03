using Heladeria.Api.Seguridad.Autorizacion;
using Heladeria.Aplicacion.Configuracion;
using Heladeria.Aplicacion.Seguridad.Autorizacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Heladeria.Api.Controllers;

[ApiController, Authorize]
[Route("api/configuracion/sucursales")]
public sealed class SucursalesController(IServicioOrganizacionConfiguracion servicio) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<RespuestaPaginada<SucursalDto>>> Listar([FromQuery] FiltroSucursalesSolicitud filtro, CancellationToken token)
    {
        var contexto = User.ObtenerContextoConfiguracion(HttpContext);
        return Ok(await servicio.ListarSucursalesAsync(filtro, contexto, token));
    }
    [HttpGet("{id:long}")]
    public async Task<ActionResult<SucursalDto>> Obtener(long id, CancellationToken token) => Ok(await servicio.ObtenerSucursalAsync(id, User.ObtenerContextoConfiguracion(HttpContext), token));
    [HttpPost, RequierePermiso(PermisosSistema.ConfiguracionAdministrar)]
    public async Task<ActionResult<SucursalDto>> Crear(GuardarSucursalSolicitud solicitud, CancellationToken token)
    {
        var creado = await servicio.CrearSucursalAsync(solicitud, User.ObtenerContextoConfiguracion(HttpContext), token);
        return CreatedAtAction(nameof(Obtener), new { id = creado.Id }, creado);
    }
    [HttpPut("{id:long}"), RequierePermiso(PermisosSistema.ConfiguracionAdministrar)]
    public async Task<ActionResult<SucursalDto>> Actualizar(long id, GuardarSucursalSolicitud solicitud, CancellationToken token) => Ok(await servicio.ActualizarSucursalAsync(id, solicitud, User.ObtenerContextoConfiguracion(HttpContext), token));
    [HttpPut("{id:long}/estado"), RequierePermiso(PermisosSistema.ConfiguracionAdministrar)]
    public async Task<ActionResult<SucursalDto>> CambiarEstado(long id, CambiarEstadoSolicitud solicitud, CancellationToken token) => Ok(await servicio.CambiarEstadoSucursalAsync(id, solicitud.EstadoId, User.ObtenerContextoConfiguracion(HttpContext), token));
}
