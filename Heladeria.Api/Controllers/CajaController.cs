using Heladeria.Api.Seguridad.Autorizacion;
using Heladeria.Aplicacion.Caja;
using Heladeria.Aplicacion.Configuracion;
using Heladeria.Aplicacion.Seguridad.Autorizacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Heladeria.Api.Controllers;

[ApiController, Authorize, Route("api/caja")]
public sealed class CajaController(IServicioCaja servicio) : ControllerBase
{
    private ContextoConfiguracion Seguridad => User.ObtenerContextoConfiguracion(HttpContext);
    [HttpGet("cajas"), RequierePermiso(PermisosSistema.CajaConsultar)]
    public async Task<ActionResult<RespuestaPaginada<CajaDto>>> Cajas([FromQuery] FiltroCaja filtro, CancellationToken t) =>
        Ok(await servicio.CajasAsync(filtro, Seguridad, t));
    [HttpPost("cajas"), RequierePermiso(PermisosSistema.CajaOperar)]
    public async Task<ActionResult<CajaDto>> CrearCaja(CrearCajaSolicitud solicitud, CancellationToken t) =>
        Ok(await servicio.CrearCajaAsync(solicitud, Seguridad, t));
    [HttpGet("sesiones"), RequierePermiso(PermisosSistema.CajaConsultar)]
    public async Task<ActionResult<RespuestaPaginada<SesionCajaResumenDto>>> Sesiones([FromQuery] FiltroSesionesCaja filtro, CancellationToken t) =>
        Ok(await servicio.SesionesAsync(filtro, Seguridad, t));
    [HttpGet("sesiones/{id:long}"), RequierePermiso(PermisosSistema.CajaConsultar)]
    public async Task<ActionResult<SesionCajaDto>> Obtener(long id, CancellationToken t) => Ok(await servicio.ObtenerAsync(id, Seguridad, t));
    [HttpGet("referencias"), RequierePermiso(PermisosSistema.CajaConsultar)]
    public async Task<ActionResult<ReferenciasCajaDto>> Referencias(CancellationToken t, [FromQuery] long? cajaId = null) =>
        Ok(await servicio.ReferenciasAsync(Seguridad, t, cajaId));
    [HttpGet("ventas-pendientes"), RequierePermiso(PermisosSistema.CajaConsultar)]
    public async Task<ActionResult<RespuestaPaginada<VentaPendienteCajaDto>>> VentasPendientes([FromQuery] FiltroCaja filtro, CancellationToken t) =>
        Ok(await servicio.VentasPendientesAsync(filtro, Seguridad, t));
    [HttpPost("sesiones/abrir"), RequierePermiso(PermisosSistema.CajaAbrir)]
    public async Task<ActionResult<SesionCajaDto>> Abrir(AbrirCajaSolicitud solicitud, CancellationToken t)
    {
        var resultado = await servicio.AbrirAsync(solicitud, Seguridad, t);
        return CreatedAtAction(nameof(Obtener), new { id = resultado.Encabezado.Id }, resultado);
    }
    [HttpPost("sesiones/{id:long}/movimientos"), RequierePermiso(PermisosSistema.CajaOperar), RequierePermiso(PermisosSistema.CajaMovimientoManual)]
    public async Task<ActionResult<SesionCajaDto>> Movimiento(long id, MovimientoCajaSolicitud solicitud, CancellationToken t) =>
        Ok(await servicio.MovimientoAsync(id, solicitud, Seguridad, t));
    [HttpPost("sesiones/{id:long}/cobrar-venta"), RequierePermiso(PermisosSistema.CajaOperar), RequierePermiso(PermisosSistema.CajaCobrar)]
    public async Task<ActionResult<ResultadoCobroCajaDto>> Cobrar(long id, CobrarVentaSolicitud solicitud, CancellationToken t) =>
        Ok(await servicio.CobrarAsync(id, solicitud, Seguridad, t));
    [HttpPost("sesiones/{id:long}/arqueo"), RequierePermiso(PermisosSistema.CajaOperar)]
    public async Task<ActionResult<SesionCajaDto>> Arqueo(long id, ArqueoCajaSolicitud solicitud, CancellationToken t) =>
        Ok(await servicio.ArqueoAsync(id, solicitud, Seguridad, t));
    [HttpPost("sesiones/{id:long}/cerrar"), RequierePermiso(PermisosSistema.CajaCerrar)]
    public async Task<ActionResult<SesionCajaDto>> Cerrar(long id, CerrarCajaSolicitud solicitud, CancellationToken t) =>
        Ok(await servicio.CerrarAsync(id, solicitud, Seguridad, t));
}
