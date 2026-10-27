using Heladeria.Api.Seguridad.Autorizacion;
using Heladeria.Aplicacion.Configuracion;
using Heladeria.Aplicacion.Reportes;
using Heladeria.Aplicacion.Seguridad.Autorizacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Heladeria.Api.Controllers;

[ApiController, Authorize, Route("api/reportes"), RequierePermiso(PermisosSistema.ReportesConsultar)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class ReportesController(IServicioReportes servicio) : ControllerBase
{
    private ContextoConfiguracion Seguridad => User.ObtenerContextoConfiguracion(HttpContext);
    [HttpGet("diario")]
    public async Task<ActionResult<ReporteDiarioDto>> Diario([FromQuery] FiltroDiario f, CancellationToken t) =>
        Ok(await servicio.DiarioAsync(f, Seguridad, t));
    [HttpGet("ventas")]
    public async Task<ActionResult<ReporteVentasDto>> Ventas([FromQuery] FiltroVentasReporte f, CancellationToken t) =>
        Ok(await servicio.VentasAsync(f, Seguridad, t));
    [HttpGet("productos-vendidos")]
    public async Task<ActionResult<RespuestaPaginada<ProductoVendidoDto>>> Productos([FromQuery] FiltroProductosVendidos f, CancellationToken t) =>
        Ok(await servicio.ProductosAsync(f, Seguridad, t));
    [HttpGet("inventario")]
    public async Task<ActionResult<ReporteInventarioDto>> Inventario([FromQuery] FiltroInventarioReporte f, CancellationToken t) =>
        Ok(await servicio.InventarioAsync(f, Seguridad, t));
    [HttpGet("kardex")]
    public async Task<ActionResult<RespuestaPaginada<KardexReporteDto>>> Kardex([FromQuery] FiltroKardexReporte f, CancellationToken t) =>
        Ok(await servicio.KardexAsync(f, Seguridad, t));
    [HttpGet("compras")]
    public async Task<ActionResult<ReporteDto<CompraReporteDto, TotalComprasDto>>> Compras([FromQuery] FiltroComprasReporte f, CancellationToken t) =>
        Ok(await servicio.ComprasAsync(f, Seguridad, t));
    [HttpGet("caja")]
    public async Task<ActionResult<ReporteDto<SesionCajaReporteDto, FormaCajaReporteDto>>> Caja([FromQuery] FiltroCajaReporte f, CancellationToken t) =>
        Ok(await servicio.CajaAsync(f, Seguridad, t));
    [HttpGet("cuentas-por-cobrar")]
    public async Task<ActionResult<ReporteDto<CuentaReporteDto, TotalCarteraReporteDto>>> Cxc([FromQuery] FiltroCarteraReporte f, CancellationToken t) =>
        Ok(await servicio.CarteraAsync(false, f, Seguridad, t));
    [HttpGet("cuentas-por-pagar")]
    public async Task<ActionResult<ReporteDto<CuentaReporteDto, TotalCarteraReporteDto>>> Cxp([FromQuery] FiltroCarteraReporte f, CancellationToken t) =>
        Ok(await servicio.CarteraAsync(true, f, Seguridad, t));
}
