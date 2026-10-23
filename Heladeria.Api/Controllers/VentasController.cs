using Heladeria.Api.Seguridad.Autorizacion;
using Heladeria.Aplicacion.Configuracion;
using Heladeria.Aplicacion.Seguridad.Autorizacion;
using Heladeria.Aplicacion.Ventas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Heladeria.Api.Controllers;

[ApiController, Authorize, Route("api/ventas")]
public sealed class VentasController(IServicioVentas servicio, IAuthorizationService autorizacion) : ControllerBase
{
    private ContextoConfiguracion Seguridad => User.ObtenerContextoConfiguracion(HttpContext);

    [HttpGet("estados"), RequierePermiso(PermisosSistema.VentasConsultar)]
    public async Task<ActionResult<IReadOnlyList<ReferenciaVentaDto>>> Estados(CancellationToken t) =>
        Ok(await servicio.EstadosAsync(Seguridad, t));

    [HttpGet, RequierePermiso(PermisosSistema.VentasConsultar)]
    public async Task<ActionResult<RespuestaPaginada<VentaResumenDto>>> Listar([FromQuery] FiltroVentasSolicitud filtro, CancellationToken t) =>
        Ok(await servicio.ListarAsync(filtro, Seguridad, t));

    [HttpGet("{id:long}"), RequierePermiso(PermisosSistema.VentasConsultar)]
    public async Task<ActionResult<VentaConsultaDto>> Obtener(long id, CancellationToken t) =>
        Ok(await servicio.ObtenerAsync(id, Seguridad, t));

    [HttpGet("referencias"), RequierePermiso(PermisosSistema.PosOperar)]
    public async Task<ActionResult<ReferenciasVentasDto>> Referencias(CancellationToken t) =>
        Ok(await servicio.ReferenciasAsync(Seguridad, t));

    [HttpGet("productos"), RequierePermiso(PermisosSistema.PosOperar)]
    public async Task<ActionResult<RespuestaPaginada<ProductoPosDto>>> Productos([FromQuery] FiltroProductosPosSolicitud filtro, CancellationToken t) =>
        Ok(await servicio.ProductosAsync(filtro, Seguridad, t));

    [HttpGet("productos/{productoId:long}/presentaciones"), RequierePermiso(PermisosSistema.PosOperar)]
    public async Task<ActionResult<ProductoPosDto>> Presentaciones(long productoId, CancellationToken t) =>
        Ok(await servicio.PresentacionesAsync(productoId, Seguridad, t));

    [HttpPost, RequierePermiso(PermisosSistema.VentasAdministrar)]
    public async Task<ActionResult<VentaConsultaDto>> Crear(CrearVentaSolicitud solicitud, CancellationToken t)
    {
        var resultado = await servicio.CrearAsync(solicitud, Seguridad, t);
        return CreatedAtAction(nameof(Obtener), new { id = resultado.Encabezado.Id }, resultado);
    }

    [HttpPut("{id:long}"), RequierePermiso(PermisosSistema.VentasAdministrar)]
    public async Task<ActionResult<VentaConsultaDto>> Actualizar(long id, ActualizarVentaSolicitud solicitud, CancellationToken t) =>
        Ok(await servicio.ActualizarAsync(id, solicitud, Seguridad, t));

    [HttpPost("{id:long}/confirmar"), RequierePermiso(PermisosSistema.VentasAdministrar)]
    public async Task<ActionResult<VentaConsultaDto>> Confirmar(long id, ConfirmarVentaSolicitud solicitud, CancellationToken t) =>
        Ok(await servicio.ConfirmarAsync(id, solicitud, Seguridad, t));

    [HttpPost("{id:long}/completar"), RequierePermiso(PermisosSistema.VentasVender), RequierePermiso(PermisosSistema.PosOperar)]
    public async Task<ActionResult<VentaConsultaDto>> Completar(long id, CompletarVentaSolicitud solicitud, CancellationToken t)
    {
        // Crédito conserva sus permisos actuales. El cobro integrado exige los mismos permisos de Caja que el cobro separado.
        if (solicitud.Cobro is not null)
            foreach (var permiso in new[] { PermisosSistema.CajaOperar, PermisosSistema.CajaCobrar })
                if (!(await autorizacion.AuthorizeAsync(User, null, ProveedorPoliticasPermisos.CrearNombrePolitica(permiso))).Succeeded)
                    return Forbid();
        return Ok(await servicio.CompletarAsync(id, solicitud, Seguridad, t));
    }

    [HttpPost("{id:long}/anular"), RequierePermiso(PermisosSistema.VentasAnular)]
    public async Task<ActionResult<VentaConsultaDto>> Anular(long id, AnularVentaSolicitud solicitud, CancellationToken t) =>
        Ok(await servicio.AnularAsync(id, solicitud, Seguridad, t));
}
