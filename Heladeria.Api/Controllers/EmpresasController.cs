using Heladeria.Api.Seguridad.Autorizacion;
using Heladeria.Aplicacion.Configuracion;
using Heladeria.Aplicacion.Seguridad.Autorizacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Heladeria.Api.Controllers;

[ApiController, Authorize]
[Route("api/configuracion/empresas")]
public sealed class EmpresasController(IServicioOrganizacionConfiguracion servicio) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<RespuestaPaginada<EmpresaDto>>> Listar([FromQuery] FiltroConEstadoSolicitud filtro, CancellationToken token)
    {
        var contexto = User.ObtenerContextoConfiguracion(HttpContext);
        return Ok(await servicio.ListarEmpresasAsync(filtro, contexto, token));
    }
    [HttpGet("{id:int}")]
    public async Task<ActionResult<EmpresaDto>> Obtener(int id, CancellationToken token) => Ok(await servicio.ObtenerEmpresaAsync(id, User.ObtenerContextoConfiguracion(HttpContext), token));
    [HttpPost, RequierePermiso(PermisosSistema.ConfiguracionAdministrar)]
    public async Task<ActionResult<EmpresaDto>> Crear(GuardarEmpresaSolicitud solicitud, CancellationToken token)
    {
        var creado = await servicio.CrearEmpresaAsync(solicitud, User.ObtenerContextoConfiguracion(HttpContext), token);
        return CreatedAtAction(nameof(Obtener), new { id = creado.Id }, creado);
    }
    [HttpPut("{id:int}"), RequierePermiso(PermisosSistema.ConfiguracionAdministrar)]
    public async Task<ActionResult<EmpresaDto>> Actualizar(int id, GuardarEmpresaSolicitud solicitud, CancellationToken token) => Ok(await servicio.ActualizarEmpresaAsync(id, solicitud, User.ObtenerContextoConfiguracion(HttpContext), token));
    [HttpPut("{id:int}/estado"), RequierePermiso(PermisosSistema.ConfiguracionAdministrar)]
    public async Task<ActionResult<EmpresaDto>> CambiarEstado(int id, CambiarEstadoSolicitud solicitud, CancellationToken token) => Ok(await servicio.CambiarEstadoEmpresaAsync(id, solicitud.EstadoId, User.ObtenerContextoConfiguracion(HttpContext), token));
}
