using Heladeria.Api.Seguridad.Autorizacion;
using Heladeria.Aplicacion.Configuracion;
using Heladeria.Aplicacion.Productos;
using Heladeria.Aplicacion.Seguridad.Autorizacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Heladeria.Api.Controllers;

[ApiController, Authorize, Route("api/productos/conversiones-unidad")]
public sealed class ConversionesUnidadController(IServicioCatalogosProductos servicio) : ControllerBase
{
    [HttpGet, RequierePermiso(PermisosSistema.ProductosConsultar)]
    public async Task<ActionResult<RespuestaPaginada<ConversionUnidadConsultaDto>>> Listar([FromQuery] FiltroCatalogoProductoSolicitud filtro, CancellationToken token) => Ok(await servicio.ListarConversionesAsync(filtro, User.ObtenerContextoConfiguracion(HttpContext), token));
    [HttpPost, RequierePermiso(PermisosSistema.ProductosAdministrar)]
    public async Task<ActionResult<ConversionUnidadConsultaDto>> Crear(CrearConversionUnidadSolicitud solicitud, CancellationToken token) => StatusCode(StatusCodes.Status201Created, await servicio.CrearConversionAsync(solicitud, User.ObtenerContextoConfiguracion(HttpContext), token));
    [HttpPut("{id:long}"), RequierePermiso(PermisosSistema.ProductosAdministrar)]
    public async Task<ActionResult<ConversionUnidadConsultaDto>> Actualizar(long id, ActualizarConversionUnidadSolicitud solicitud, CancellationToken token) => Ok(await servicio.ActualizarConversionAsync(id, solicitud, User.ObtenerContextoConfiguracion(HttpContext), token));
    [HttpPut("{id:long}/estado"), RequierePermiso(PermisosSistema.ProductosAdministrar)]
    public async Task<ActionResult<ConversionUnidadConsultaDto>> CambiarEstado(long id, CambiarEstadoSolicitud solicitud, CancellationToken token) => Ok(await servicio.CambiarEstadoConversionAsync(id, solicitud.EstadoId, User.ObtenerContextoConfiguracion(HttpContext), token));
}
