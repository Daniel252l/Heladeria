using Heladeria.Api.Seguridad.Autorizacion;
using Heladeria.Aplicacion.Compras;
using Heladeria.Aplicacion.Configuracion;
using Heladeria.Aplicacion.Seguridad.Autorizacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Heladeria.Api.Controllers;

[ApiController, Authorize, Route("api/compras/proveedores")]
public sealed class ProveedoresController(IServicioProveedores servicio) : ControllerBase
{
    [HttpGet, RequierePermiso(PermisosSistema.ProveedoresConsultar)]
    public async Task<ActionResult<RespuestaPaginada<ProveedorConsultaDto>>> Listar([FromQuery] FiltroProveedoresSolicitud filtro, CancellationToken token) =>
        Ok(await servicio.ListarAsync(filtro, User.ObtenerContextoConfiguracion(HttpContext), token));
    [HttpGet("{id:int}"), RequierePermiso(PermisosSistema.ProveedoresConsultar)]
    public async Task<ActionResult<ProveedorConsultaDto>> Obtener(int id, CancellationToken token) =>
        Ok(await servicio.ObtenerAsync(id, User.ObtenerContextoConfiguracion(HttpContext), token));
    [HttpPost, RequierePermiso(PermisosSistema.ProveedoresAdministrar)]
    public async Task<ActionResult<ProveedorConsultaDto>> Crear(GuardarProveedorSolicitud solicitud, CancellationToken token)
    {
        var resultado = await servicio.GuardarAsync(null, solicitud, User.ObtenerContextoConfiguracion(HttpContext), token);
        return CreatedAtAction(nameof(Obtener), new { id = resultado.Id }, resultado);
    }
    [HttpPut("{id:int}"), RequierePermiso(PermisosSistema.ProveedoresAdministrar)]
    public async Task<ActionResult<ProveedorConsultaDto>> Actualizar(int id, GuardarProveedorSolicitud solicitud, CancellationToken token) =>
        Ok(await servicio.GuardarAsync(id, solicitud, User.ObtenerContextoConfiguracion(HttpContext), token));
    [HttpPut("{id:int}/estado"), RequierePermiso(PermisosSistema.ProveedoresAdministrar)]
    public async Task<ActionResult<ProveedorConsultaDto>> Estado(int id, CambiarEstadoSolicitud solicitud, CancellationToken token) =>
        Ok(await servicio.CambiarEstadoAsync(id, solicitud.EstadoId, User.ObtenerContextoConfiguracion(HttpContext), token));
    [HttpGet("{id:int}/productos"), RequierePermiso(PermisosSistema.ProveedoresConsultar)]
    public async Task<ActionResult<RespuestaPaginada<ProveedorProductoDto>>> Productos(int id, [FromQuery] FiltroConEstadoSolicitud filtro, CancellationToken token) =>
        Ok(await servicio.ListarProductosAsync(id, filtro, User.ObtenerContextoConfiguracion(HttpContext), token));
    [HttpPost("{id:int}/productos"), RequierePermiso(PermisosSistema.ProveedoresAdministrar)]
    public async Task<ActionResult<ProveedorProductoDto>> Asociar(int id, GuardarProveedorProductoSolicitud solicitud, CancellationToken token) =>
        StatusCode(StatusCodes.Status201Created, await servicio.GuardarProductoAsync(id, null, solicitud, User.ObtenerContextoConfiguracion(HttpContext), token));
    [HttpPut("{id:int}/productos/{relacionId:long}"), RequierePermiso(PermisosSistema.ProveedoresAdministrar)]
    public async Task<ActionResult<ProveedorProductoDto>> ActualizarProducto(int id, long relacionId, GuardarProveedorProductoSolicitud solicitud, CancellationToken token) =>
        Ok(await servicio.GuardarProductoAsync(id, relacionId, solicitud, User.ObtenerContextoConfiguracion(HttpContext), token));
    [HttpPut("{id:int}/productos/{relacionId:long}/estado"), RequierePermiso(PermisosSistema.ProveedoresAdministrar)]
    public async Task<ActionResult<ProveedorProductoDto>> EstadoProducto(int id, long relacionId, CambiarEstadoSolicitud solicitud, CancellationToken token) =>
        Ok(await servicio.CambiarEstadoProductoAsync(id, relacionId, solicitud.EstadoId, User.ObtenerContextoConfiguracion(HttpContext), token));
}
