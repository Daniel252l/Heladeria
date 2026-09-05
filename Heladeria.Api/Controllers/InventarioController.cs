using Heladeria.Api.Seguridad.Autorizacion;
using Heladeria.Aplicacion.Configuracion;
using Heladeria.Aplicacion.Inventario;
using Heladeria.Aplicacion.Seguridad.Autorizacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Heladeria.Api.Controllers;

[ApiController, Authorize, Route("api/inventario")]
public sealed class InventarioController(IServicioInventario servicio) : ControllerBase
{
    [HttpGet("almacenes"), RequierePermiso(PermisosSistema.InventarioConsultar)]
    public async Task<ActionResult<IReadOnlyList<AlmacenInventarioDto>>> ListarAlmacenes(CancellationToken token) => Ok(await servicio.ListarAlmacenesAsync(User.ObtenerContextoConfiguracion(HttpContext), token));

    [HttpGet("existencias"), RequierePermiso(PermisosSistema.InventarioConsultar)]
    public async Task<ActionResult<RespuestaPaginada<ExistenciaConsultaDto>>> ListarExistencias([FromQuery] FiltroExistenciasSolicitud filtro, CancellationToken token) => Ok(await servicio.ListarExistenciasAsync(filtro, User.ObtenerContextoConfiguracion(HttpContext), token));

    [HttpPost("ajustes"), RequierePermiso(PermisosSistema.InventarioAdministrar)]
    public async Task<ActionResult<AjusteInventarioResultadoDto>> Ajustar(AjusteInventarioSolicitud solicitud, CancellationToken token) => StatusCode(StatusCodes.Status201Created, await servicio.AjustarAsync(solicitud, User.ObtenerContextoConfiguracion(HttpContext), token));
}
