using Heladeria.Api.Seguridad.Autorizacion;
using Heladeria.Aplicacion.Cartera;
using Heladeria.Aplicacion.Configuracion;
using Heladeria.Aplicacion.Seguridad.Autorizacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Heladeria.Api.Controllers;

[ApiController, Authorize, Route("api/cuentas-por-pagar")]
public sealed class CuentasPorPagarController(IServicioCartera servicio) : ControllerBase
{
    private ContextoConfiguracion Seguridad => User.ObtenerContextoConfiguracion(HttpContext);
    [HttpGet, RequierePermiso(PermisosSistema.CxpConsultar)]
    public async Task<IActionResult> Listar([FromQuery] FiltroCartera filtro, CancellationToken t) =>
        Ok(await servicio.ListarAsync(true, filtro, Seguridad, t));
    [HttpGet("{id:long}"), RequierePermiso(PermisosSistema.CxpConsultar)]
    public async Task<IActionResult> Obtener(long id, CancellationToken t) =>
        Ok(await servicio.ObtenerAsync(true, id, Seguridad, t));
    [HttpGet("resumen"), RequierePermiso(PermisosSistema.CxpConsultar)]
    public async Task<IActionResult> Resumen([FromQuery] FiltroCartera filtro, CancellationToken t) =>
        Ok(await servicio.ResumenAsync(true, filtro, Seguridad, t));
    [HttpGet("referencias"), RequierePermiso(PermisosSistema.CxpConsultar)]
    public async Task<IActionResult> Referencias(CancellationToken t) =>
        Ok(await servicio.ReferenciasAsync(true, Seguridad, t));
    [HttpPost("{id:long}/pagos"), RequierePermiso(PermisosSistema.CxpPagar),
        RequierePermiso(PermisosSistema.CajaOperar)]
    public async Task<IActionResult> Aplicar(long id, AplicarPagoCarteraSolicitud solicitud, CancellationToken t) =>
        Ok(await servicio.AplicarAsync(true, id, solicitud, Seguridad, t));
    [HttpPost("desde-compra"), RequierePermiso(PermisosSistema.CxpAdministrar)]
    public async Task<IActionResult> RegistrarFactura(RegistrarFacturaCreditoSolicitud solicitud, CancellationToken t) =>
        Ok(await servicio.RegistrarFacturaAsync(solicitud, Seguridad, t));
}
