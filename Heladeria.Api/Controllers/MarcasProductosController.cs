using Heladeria.Api.Seguridad.Autorizacion;
using Heladeria.Aplicacion.Configuracion;
using Heladeria.Aplicacion.Productos;
using Heladeria.Aplicacion.Seguridad.Autorizacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Heladeria.Api.Controllers;

[ApiController, Authorize, Route("api/productos/marcas")]
public sealed class MarcasProductosController(IServicioCatalogosProductos servicio) : ControllerBase
{
    [HttpGet, RequierePermiso(PermisosSistema.ProductosConsultar)]
    public async Task<ActionResult<RespuestaPaginada<MarcaConsultaDto>>> Listar([FromQuery] FiltroCatalogoProductoSolicitud filtro, CancellationToken token) => Ok(await servicio.ListarMarcasAsync(filtro, User.ObtenerContextoConfiguracion(HttpContext), token));
    [HttpGet("{id:int}"), RequierePermiso(PermisosSistema.ProductosConsultar)]
    public async Task<ActionResult<MarcaConsultaDto>> Obtener(int id, CancellationToken token) => Ok(await servicio.ObtenerMarcaAsync(id, User.ObtenerContextoConfiguracion(HttpContext), token));
    [HttpPost, RequierePermiso(PermisosSistema.ProductosAdministrar)]
    public async Task<ActionResult<MarcaConsultaDto>> Crear(CrearMarcaSolicitud solicitud, CancellationToken token)
    {
        var creado = await servicio.CrearMarcaAsync(solicitud, User.ObtenerContextoConfiguracion(HttpContext), token);
        return CreatedAtAction(nameof(Obtener), new { id = creado.Id }, creado);
    }
    [HttpPut("{id:int}"), RequierePermiso(PermisosSistema.ProductosAdministrar)]
    public async Task<ActionResult<MarcaConsultaDto>> Actualizar(int id, ActualizarMarcaSolicitud solicitud, CancellationToken token) => Ok(await servicio.ActualizarMarcaAsync(id, solicitud, User.ObtenerContextoConfiguracion(HttpContext), token));
    [HttpPut("{id:int}/estado"), RequierePermiso(PermisosSistema.ProductosAdministrar)]
    public async Task<ActionResult<MarcaConsultaDto>> CambiarEstado(int id, CambiarEstadoSolicitud solicitud, CancellationToken token) => Ok(await servicio.CambiarEstadoMarcaAsync(id, solicitud.EstadoId, User.ObtenerContextoConfiguracion(HttpContext), token));
}
