using Heladeria.Api.Seguridad.Autorizacion;
using Heladeria.Aplicacion.Configuracion;
using Heladeria.Aplicacion.Productos;
using Heladeria.Aplicacion.Seguridad.Autorizacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Heladeria.Api.Controllers;

[ApiController, Authorize, Route("api/productos/presentaciones")]
public sealed class PresentacionesController(IServicioCatalogosProductos servicio) : ControllerBase
{
    [HttpGet, RequierePermiso(PermisosSistema.ProductosConsultar)]
    public async Task<ActionResult<RespuestaPaginada<PresentacionConsultaDto>>> Listar([FromQuery] FiltroCatalogoProductoSolicitud filtro, CancellationToken token) => Ok(await servicio.ListarPresentacionesAsync(filtro, User.ObtenerContextoConfiguracion(HttpContext), token));
    [HttpGet("{id:int}"), RequierePermiso(PermisosSistema.ProductosConsultar)]
    public async Task<ActionResult<PresentacionConsultaDto>> Obtener(int id, CancellationToken token) => Ok(await servicio.ObtenerPresentacionAsync(id, User.ObtenerContextoConfiguracion(HttpContext), token));
    [HttpPost, RequierePermiso(PermisosSistema.ProductosAdministrar)]
    public async Task<ActionResult<PresentacionConsultaDto>> Crear(CrearPresentacionSolicitud solicitud, CancellationToken token)
    {
        var creado = await servicio.CrearPresentacionAsync(solicitud, User.ObtenerContextoConfiguracion(HttpContext), token);
        return CreatedAtAction(nameof(Obtener), new { id = creado.Id }, creado);
    }
    [HttpPut("{id:int}"), RequierePermiso(PermisosSistema.ProductosAdministrar)]
    public async Task<ActionResult<PresentacionConsultaDto>> Actualizar(int id, ActualizarPresentacionSolicitud solicitud, CancellationToken token) => Ok(await servicio.ActualizarPresentacionAsync(id, solicitud, User.ObtenerContextoConfiguracion(HttpContext), token));
    [HttpPut("{id:int}/estado"), RequierePermiso(PermisosSistema.ProductosAdministrar)]
    public async Task<ActionResult<PresentacionConsultaDto>> CambiarEstado(int id, CambiarEstadoSolicitud solicitud, CancellationToken token) => Ok(await servicio.CambiarEstadoPresentacionAsync(id, solicitud.EstadoId, User.ObtenerContextoConfiguracion(HttpContext), token));
}
