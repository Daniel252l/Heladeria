using Heladeria.Api.Seguridad.Autorizacion;
using Heladeria.Aplicacion.Clientes;
using Heladeria.Aplicacion.Configuracion;
using Heladeria.Aplicacion.Seguridad.Autorizacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Heladeria.Api.Controllers;

[ApiController, Authorize, Route("api/clientes")]
public sealed class ClientesController(IServicioClientes servicio) : ControllerBase
{
    private ContextoConfiguracion Seguridad => User.ObtenerContextoConfiguracion(HttpContext);

    [HttpGet, RequierePermiso(PermisosSistema.VentasConsultar)]
    public async Task<ActionResult<RespuestaPaginada<ClienteConsultaDto>>> Listar([FromQuery] FiltroConEstadoSolicitud filtro, CancellationToken token) =>
        Ok(await servicio.ListarAsync(filtro, Seguridad, token));

    [HttpGet("{id:int}"), RequierePermiso(PermisosSistema.VentasConsultar)]
    public async Task<ActionResult<ClienteConsultaDto>> Obtener(int id, CancellationToken token) =>
        Ok(await servicio.ObtenerAsync(id, Seguridad, token));

    [HttpPost, RequierePermiso(PermisosSistema.VentasAdministrar)]
    public async Task<ActionResult<ClienteConsultaDto>> Crear(CrearClienteSolicitud solicitud, CancellationToken token)
    {
        var resultado = await servicio.CrearAsync(solicitud, Seguridad, token);
        return CreatedAtAction(nameof(Obtener), new { id = resultado.Id }, resultado);
    }
}
