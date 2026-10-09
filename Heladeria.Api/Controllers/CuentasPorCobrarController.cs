using Heladeria.Api.Seguridad.Autorizacion;
using Heladeria.Aplicacion.Cartera;
using Heladeria.Aplicacion.Configuracion;
using Heladeria.Aplicacion.Seguridad.Autorizacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Heladeria.Api.Controllers;

[ApiController, Authorize, Route("api/cuentas-por-cobrar")]
public sealed class CuentasPorCobrarController(IServicioCartera servicio) : ControllerBase
{
    private ContextoConfiguracion Seguridad => User.ObtenerContextoConfiguracion(HttpContext);
    [HttpGet, RequierePermiso(PermisosSistema.CxcConsultar)]
    public async Task<IActionResult> Listar([FromQuery] FiltroCartera filtro, CancellationToken t) =>
        Ok(await servicio.ListarAsync(false, filtro, Seguridad, t));
    [HttpGet("{id:long}"), RequierePermiso(PermisosSistema.CxcConsultar)]
    public async Task<IActionResult> Obtener(long id, CancellationToken t) =>
        Ok(await servicio.ObtenerAsync(false, id, Seguridad, t));
    [HttpGet("resumen"), RequierePermiso(PermisosSistema.CxcConsultar)]
    public async Task<IActionResult> Resumen([FromQuery] FiltroCartera filtro, CancellationToken t) =>
        Ok(await servicio.ResumenAsync(false, filtro, Seguridad, t));
    [HttpGet("referencias"), RequierePermiso(PermisosSistema.CxcConsultar)]
    public async Task<IActionResult> Referencias(CancellationToken t) =>
        Ok(await servicio.ReferenciasAsync(false, Seguridad, t));
    [HttpPost("{id:long}/abonos"), RequierePermiso(PermisosSistema.CxcCobrar),
        RequierePermiso(PermisosSistema.CajaOperar), RequierePermiso(PermisosSistema.CajaCobrar)]
    public async Task<IActionResult> Aplicar(long id, AplicarPagoCarteraSolicitud solicitud, CancellationToken t) =>
        Ok(await servicio.AplicarAsync(false, id, solicitud, Seguridad, t));

}
