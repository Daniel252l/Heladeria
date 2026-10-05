using Heladeria.Api.Seguridad.Autorizacion;
using Heladeria.Aplicacion.Configuracion;
using Heladeria.Aplicacion.Produccion;
using Heladeria.Aplicacion.Seguridad.Autorizacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Heladeria.Api.Controllers;

[ApiController, Authorize, Route("api/produccion")]
public sealed class ProduccionController(IServicioProduccion servicio, IServicioRecetas recetas) : ControllerBase
{
    [HttpGet("productos"), RequierePermiso(PermisosSistema.ProduccionConsultar)]
    public async Task<ActionResult<RespuestaPaginada<OpcionRecetaDto>>> Productos(
        [FromQuery] FiltroProductosProduccionSolicitud filtro, CancellationToken token) =>
        Ok(await recetas.ListarProductosAsync(filtro, User.ObtenerContextoConfiguracion(HttpContext), token));

    [HttpGet("recetas"), RequierePermiso(PermisosSistema.ProduccionConsultar)]
    public async Task<ActionResult<RespuestaPaginada<RecetaResumenDto>>> Recetas(
        [FromQuery] FiltroRecetasSolicitud filtro, CancellationToken token) =>
        Ok(await recetas.ListarAsync(filtro, User.ObtenerContextoConfiguracion(HttpContext), token));

    [HttpGet("recetas/{id:long}"), RequierePermiso(PermisosSistema.ProduccionConsultar)]
    public async Task<ActionResult<RecetaConsultaDto>> Receta(long id, CancellationToken token) =>
        Ok(await recetas.ObtenerAsync(id, User.ObtenerContextoConfiguracion(HttpContext), token));

    [HttpGet("productos/{productoId:long}/presentaciones"), RequierePermiso(PermisosSistema.ProduccionConsultar)]
    public async Task<ActionResult<ProductoPresentacionesProduccionDto>> Presentaciones(long productoId, CancellationToken token) =>
        Ok(await recetas.ListarPresentacionesAsync(productoId, User.ObtenerContextoConfiguracion(HttpContext), token));

    [HttpGet("referencias"), RequierePermiso(PermisosSistema.ProduccionConsultar)]
    public async Task<ActionResult<ReferenciasProduccionDto>> Referencias(CancellationToken token) =>
        Ok(await servicio.ObtenerReferenciasAsync(User.ObtenerContextoConfiguracion(HttpContext), token));

    [HttpGet, RequierePermiso(PermisosSistema.ProduccionConsultar)]
    public async Task<ActionResult<RespuestaPaginada<OrdenProduccionResumenDto>>> Listar(
        [FromQuery] FiltroProduccionSolicitud filtro, CancellationToken token) =>
        Ok(await servicio.ListarAsync(filtro, User.ObtenerContextoConfiguracion(HttpContext), token));

    [HttpGet("{id:long}"), RequierePermiso(PermisosSistema.ProduccionConsultar)]
    public async Task<ActionResult<OrdenProduccionConsultaDto>> Obtener(long id, CancellationToken token) =>
        Ok(await servicio.ObtenerAsync(id, User.ObtenerContextoConfiguracion(HttpContext), token));

    [HttpPost, RequierePermiso(PermisosSistema.ProduccionAdministrar)]
    public async Task<ActionResult<OrdenProduccionConsultaDto>> Crear(
        CrearOrdenProduccionSolicitud solicitud, CancellationToken token)
    {
        var resultado = await servicio.CrearAsync(
            solicitud, User.ObtenerContextoConfiguracion(HttpContext), token);
        return CreatedAtAction(nameof(Obtener), new { id = resultado.Encabezado.Id }, resultado);
    }

    [HttpPut("{id:long}"), RequierePermiso(PermisosSistema.ProduccionAdministrar)]
    public async Task<ActionResult<OrdenProduccionConsultaDto>> Actualizar(
        long id, ActualizarOrdenProduccionSolicitud solicitud, CancellationToken token) =>
        Ok(await servicio.ActualizarAsync(
            id, solicitud, User.ObtenerContextoConfiguracion(HttpContext), token));

    [HttpPost("{id:long}/confirmar"), RequierePermiso(PermisosSistema.ProduccionAdministrar)]
    public async Task<ActionResult<OrdenProduccionConsultaDto>> Confirmar(
        long id, CambiarEstadoProduccionSolicitud solicitud, CancellationToken token) =>
        Ok(await servicio.ConfirmarAsync(
            id, solicitud, User.ObtenerContextoConfiguracion(HttpContext), token));

    [HttpPost("{id:long}/finalizar"), RequierePermiso(PermisosSistema.ProduccionProducir)]
    public async Task<ActionResult<OrdenProduccionConsultaDto>> Finalizar(
        long id, FinalizarProduccionSolicitud solicitud, CancellationToken token) =>
        Ok(await servicio.FinalizarAsync(
            id, solicitud, User.ObtenerContextoConfiguracion(HttpContext), token));

    [HttpPost("{id:long}/anular"), RequierePermiso(PermisosSistema.ProduccionAdministrar)]
    public async Task<ActionResult<OrdenProduccionConsultaDto>> Anular(
        long id, CambiarEstadoProduccionSolicitud solicitud, CancellationToken token) =>
        Ok(await servicio.AnularAsync(
            id, solicitud, User.ObtenerContextoConfiguracion(HttpContext), token));
}
