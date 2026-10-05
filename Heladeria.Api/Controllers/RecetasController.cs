using Heladeria.Api.Seguridad.Autorizacion;
using Heladeria.Aplicacion.Configuracion;
using Heladeria.Aplicacion.Produccion;
using Heladeria.Aplicacion.Seguridad.Autorizacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Heladeria.Api.Controllers;

[ApiController, Authorize, Route("api/recetas")]
public sealed class RecetasController(IServicioRecetas servicio) : ControllerBase
{
    [HttpGet("referencias"), RequierePermiso(PermisosSistema.RecetasConsultar)]
    public async Task<ActionResult<ReferenciasRecetasDto>> Referencias(CancellationToken token) =>
        Ok(await servicio.ObtenerReferenciasAsync(User.ObtenerContextoConfiguracion(HttpContext), token));

    [HttpGet, RequierePermiso(PermisosSistema.RecetasConsultar)]
    public async Task<ActionResult<RespuestaPaginada<RecetaResumenDto>>> Listar(
        [FromQuery] FiltroRecetasSolicitud filtro, CancellationToken token) =>
        Ok(await servicio.ListarAsync(filtro, User.ObtenerContextoConfiguracion(HttpContext), token));

    [HttpGet("{id:long}"), RequierePermiso(PermisosSistema.RecetasConsultar)]
    public async Task<ActionResult<RecetaConsultaDto>> Obtener(long id, CancellationToken token) =>
        Ok(await servicio.ObtenerAsync(id, User.ObtenerContextoConfiguracion(HttpContext), token));

    [HttpGet("productos/{productoId:long}/presentaciones"), RequierePermiso(PermisosSistema.RecetasConsultar)]
    public async Task<ActionResult<ProductoPresentacionesProduccionDto>> Presentaciones(
        long productoId, CancellationToken token) =>
        Ok(await servicio.ListarPresentacionesAsync(
            productoId, User.ObtenerContextoConfiguracion(HttpContext), token));

    [HttpPost, RequierePermiso(PermisosSistema.RecetasAdministrar)]
    public async Task<ActionResult<RecetaConsultaDto>> Crear(
        CrearRecetaSolicitud solicitud, CancellationToken token)
    {
        var resultado = await servicio.CrearAsync(
            solicitud, User.ObtenerContextoConfiguracion(HttpContext), token);
        return CreatedAtAction(nameof(Obtener), new { id = resultado.Encabezado.Id }, resultado);
    }

    [HttpPut("{id:long}"), RequierePermiso(PermisosSistema.RecetasAdministrar)]
    public async Task<ActionResult<RecetaConsultaDto>> Actualizar(
        long id, ActualizarRecetaSolicitud solicitud, CancellationToken token) =>
        Ok(await servicio.ActualizarAsync(
            id, solicitud, User.ObtenerContextoConfiguracion(HttpContext), token));
}
