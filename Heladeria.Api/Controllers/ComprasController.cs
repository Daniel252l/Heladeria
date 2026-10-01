using Heladeria.Api.Seguridad.Autorizacion;
using Heladeria.Aplicacion.Compras;
using Heladeria.Aplicacion.Configuracion;
using Heladeria.Aplicacion.Seguridad.Autorizacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Heladeria.Api.Controllers;

[ApiController, Authorize, Route("api/compras")]
public sealed class ComprasController(IServicioCompras servicio) : ControllerBase
{
    [HttpGet("referencias"), RequierePermiso(PermisosSistema.ComprasConsultar)]
    public async Task<ActionResult<ReferenciasComprasDto>> Referencias(CancellationToken token) =>
        Ok(await servicio.ObtenerReferenciasAsync(User.ObtenerContextoConfiguracion(HttpContext), token));
    [HttpGet("productos/{productoId:long}/presentaciones"), RequierePermiso(PermisosSistema.ComprasConsultar)]
    public async Task<ActionResult<ProductoPresentacionesCompraDto>> Presentaciones(long productoId, CancellationToken token) =>
        Ok(await servicio.ListarPresentacionesAsync(productoId, User.ObtenerContextoConfiguracion(HttpContext), token));
    [HttpGet, RequierePermiso(PermisosSistema.ComprasConsultar)]
    public async Task<ActionResult<RespuestaPaginada<CompraResumenDto>>> Listar([FromQuery] FiltroComprasSolicitud filtro, CancellationToken token) =>
        Ok(await servicio.ListarAsync(filtro, User.ObtenerContextoConfiguracion(HttpContext), token));
    [HttpGet("{id:long}"), RequierePermiso(PermisosSistema.ComprasConsultar)]
    public async Task<ActionResult<CompraConsultaDto>> Obtener(long id, CancellationToken token) =>
        Ok(await servicio.ObtenerAsync(id, User.ObtenerContextoConfiguracion(HttpContext), token));
    [HttpPost, RequierePermiso(PermisosSistema.ComprasAdministrar)]
    public async Task<ActionResult<CompraConsultaDto>> Crear(GuardarCompraSolicitud solicitud, CancellationToken token)
    {
        var resultado = await servicio.GuardarAsync(null, solicitud, User.ObtenerContextoConfiguracion(HttpContext), token);
        return CreatedAtAction(nameof(Obtener), new { id = resultado.Encabezado.Id }, resultado);
    }
    [HttpPut("{id:long}"), RequierePermiso(PermisosSistema.ComprasAdministrar)]
    public async Task<ActionResult<CompraConsultaDto>> Actualizar(long id, GuardarCompraSolicitud solicitud, CancellationToken token) =>
        Ok(await servicio.GuardarAsync(id, solicitud, User.ObtenerContextoConfiguracion(HttpContext), token));
    [HttpPut("{id:long}/estado"), RequierePermiso(PermisosSistema.ComprasAdministrar)]
    public async Task<ActionResult<CompraConsultaDto>> Estado(long id, CambiarEstadoCompraSolicitud solicitud, CancellationToken token) =>
        Ok(await servicio.CambiarEstadoAsync(id, solicitud.Estado, User.ObtenerContextoConfiguracion(HttpContext), token));
    [HttpPost("{id:long}/confirmar"), RequierePermiso(PermisosSistema.ComprasAdministrar)]
    public async Task<ActionResult<CompraConsultaDto>> Confirmar(long id, CancellationToken token) =>
        Ok(await servicio.CambiarEstadoAsync(id, "CONFIRMADA", User.ObtenerContextoConfiguracion(HttpContext), token));
    [HttpPost("{id:long}/recepciones"), RequierePermiso(PermisosSistema.ComprasRecibir)]
    public async Task<ActionResult<RecepcionCompraDto>> Recibir(long id, CrearRecepcionSolicitud solicitud, CancellationToken token)
    {
        var resultado = await servicio.RecibirAsync(id, solicitud, User.ObtenerContextoConfiguracion(HttpContext), token);
        return CreatedAtAction(nameof(Recepcion), new { id = resultado.Id }, resultado);
    }
    [HttpGet("recepciones/{id:long}"), RequierePermiso(PermisosSistema.ComprasConsultar)]
    public async Task<ActionResult<RecepcionCompraDto>> Recepcion(long id, CancellationToken token) =>
        Ok(await servicio.ObtenerRecepcionAsync(id, User.ObtenerContextoConfiguracion(HttpContext), token));
}
