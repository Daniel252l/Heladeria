using Heladeria.Api.Seguridad.Autorizacion;
using Heladeria.Aplicacion.Configuracion;
using Heladeria.Aplicacion.Productos;
using Heladeria.Aplicacion.Seguridad.Autorizacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Heladeria.Api.Controllers;

[ApiController, Authorize, Route("api/productos")]
public sealed class ProductosController(IServicioProductos servicio) : ControllerBase
{
    [HttpGet("referencias"), RequierePermiso(PermisosSistema.ProductosConsultar)]
    public async Task<ActionResult<ReferenciasProductoDto>> ObtenerReferencias(CancellationToken token) => Ok(await servicio.ObtenerReferenciasAsync(User.ObtenerContextoConfiguracion(HttpContext), token));
    [HttpGet, RequierePermiso(PermisosSistema.ProductosConsultar)]
    public async Task<ActionResult<RespuestaPaginada<ProductoConsultaDto>>> Listar([FromQuery] FiltroProductosSolicitud filtro, CancellationToken token) => Ok(await servicio.ListarAsync(filtro, User.ObtenerContextoConfiguracion(HttpContext), token));
    [HttpGet("{id:long}"), RequierePermiso(PermisosSistema.ProductosConsultar)]
    public async Task<ActionResult<ProductoConsultaDto>> Obtener(long id, CancellationToken token) => Ok(await servicio.ObtenerAsync(id, User.ObtenerContextoConfiguracion(HttpContext), token));
    [HttpPost, RequierePermiso(PermisosSistema.ProductosAdministrar)]
    public async Task<ActionResult<ProductoConsultaDto>> Crear(CrearProductoSolicitud solicitud, CancellationToken token)
    {
        var creado = await servicio.CrearAsync(solicitud, User.ObtenerContextoConfiguracion(HttpContext), token);
        return CreatedAtAction(nameof(Obtener), new { id = creado.Id }, creado);
    }
    [HttpPut("{id:long}"), RequierePermiso(PermisosSistema.ProductosAdministrar)]
    public async Task<ActionResult<ProductoConsultaDto>> Actualizar(long id, ActualizarProductoSolicitud solicitud, CancellationToken token) => Ok(await servicio.ActualizarAsync(id, solicitud, User.ObtenerContextoConfiguracion(HttpContext), token));
    [HttpPut("{id:long}/estado"), RequierePermiso(PermisosSistema.ProductosAdministrar)]
    public async Task<ActionResult<ProductoConsultaDto>> CambiarEstado(long id, CambiarEstadoSolicitud solicitud, CancellationToken token) => Ok(await servicio.CambiarEstadoAsync(id, solicitud.EstadoId, User.ObtenerContextoConfiguracion(HttpContext), token));

    [HttpGet("{productoId:long}/presentaciones"), RequierePermiso(PermisosSistema.ProductosConsultar)]
    public async Task<ActionResult<IReadOnlyList<ProductoPresentacionConsultaDto>>> ListarPresentaciones(long productoId, CancellationToken token) => Ok(await servicio.ListarPresentacionesAsync(productoId, User.ObtenerContextoConfiguracion(HttpContext), token));
    [HttpPost("{productoId:long}/presentaciones"), RequierePermiso(PermisosSistema.ProductosAdministrar)]
    public async Task<ActionResult<ProductoPresentacionConsultaDto>> CrearPresentacion(long productoId, CrearProductoPresentacionSolicitud solicitud, CancellationToken token) => StatusCode(StatusCodes.Status201Created, await servicio.CrearPresentacionAsync(productoId, solicitud, User.ObtenerContextoConfiguracion(HttpContext), token));
    [HttpPut("{productoId:long}/presentaciones/{id:long}"), RequierePermiso(PermisosSistema.ProductosAdministrar)]
    public async Task<ActionResult<ProductoPresentacionConsultaDto>> ActualizarPresentacion(long productoId, long id, ActualizarProductoPresentacionSolicitud solicitud, CancellationToken token) => Ok(await servicio.ActualizarPresentacionAsync(productoId, id, solicitud, User.ObtenerContextoConfiguracion(HttpContext), token));
    [HttpPut("{productoId:long}/presentaciones/{id:long}/estado"), RequierePermiso(PermisosSistema.ProductosAdministrar)]
    public async Task<ActionResult<ProductoPresentacionConsultaDto>> CambiarEstadoPresentacion(long productoId, long id, CambiarEstadoSolicitud solicitud, CancellationToken token) => Ok(await servicio.CambiarEstadoPresentacionAsync(productoId, id, solicitud.EstadoId, User.ObtenerContextoConfiguracion(HttpContext), token));
}
