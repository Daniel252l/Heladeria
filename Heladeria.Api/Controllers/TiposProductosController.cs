using Heladeria.Api.Seguridad.Autorizacion;
using Heladeria.Aplicacion.Configuracion;
using Heladeria.Aplicacion.Productos;
using Heladeria.Aplicacion.Seguridad.Autorizacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Heladeria.Api.Controllers;

[ApiController, Authorize, Route("api/productos/tipos")]
public sealed class TiposProductosController(IServicioCatalogosProductos servicio) : ControllerBase
{
    [HttpGet, RequierePermiso(PermisosSistema.ProductosConsultar)]
    public async Task<ActionResult<RespuestaPaginada<TipoProductoConsultaDto>>> Listar([FromQuery] FiltroCatalogoProductoSolicitud filtro, CancellationToken token) => Ok(await servicio.ListarTiposProductosAsync(filtro, User.ObtenerContextoConfiguracion(HttpContext), token));
    [HttpGet("{id:int}"), RequierePermiso(PermisosSistema.ProductosConsultar)]
    public async Task<ActionResult<TipoProductoConsultaDto>> Obtener(int id, CancellationToken token) => Ok(await servicio.ObtenerTipoProductoAsync(id, User.ObtenerContextoConfiguracion(HttpContext), token));
    [HttpPost, RequierePermiso(PermisosSistema.ProductosAdministrar)]
    public async Task<ActionResult<TipoProductoConsultaDto>> Crear(CrearTipoProductoSolicitud solicitud, CancellationToken token)
    {
        var creado = await servicio.CrearTipoProductoAsync(solicitud, User.ObtenerContextoConfiguracion(HttpContext), token);
        return CreatedAtAction(nameof(Obtener), new { id = creado.Id }, creado);
    }
    [HttpPut("{id:int}"), RequierePermiso(PermisosSistema.ProductosAdministrar)]
    public async Task<ActionResult<TipoProductoConsultaDto>> Actualizar(int id, ActualizarTipoProductoSolicitud solicitud, CancellationToken token) => Ok(await servicio.ActualizarTipoProductoAsync(id, solicitud, User.ObtenerContextoConfiguracion(HttpContext), token));
    [HttpPut("{id:int}/estado"), RequierePermiso(PermisosSistema.ProductosAdministrar)]
    public async Task<ActionResult<TipoProductoConsultaDto>> CambiarEstado(int id, CambiarEstadoSolicitud solicitud, CancellationToken token) => Ok(await servicio.CambiarEstadoTipoProductoAsync(id, solicitud.EstadoId, User.ObtenerContextoConfiguracion(HttpContext), token));
}
