using Heladeria.Api.Seguridad.Autorizacion;
using Heladeria.Aplicacion.Configuracion;
using Heladeria.Aplicacion.Productos;
using Heladeria.Aplicacion.Seguridad.Autorizacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Heladeria.Api.Controllers;

[ApiController, Authorize, Route("api/productos/categorias")]
public sealed class CategoriasProductosController(IServicioCatalogosProductos servicio) : ControllerBase
{
    [HttpGet, RequierePermiso(PermisosSistema.ProductosConsultar)]
    public async Task<ActionResult<RespuestaPaginada<CategoriaConsultaDto>>> Listar([FromQuery] FiltroCatalogoProductoSolicitud filtro, CancellationToken token) => Ok(await servicio.ListarCategoriasAsync(filtro, User.ObtenerContextoConfiguracion(HttpContext), token));

    [HttpGet("{id:int}"), RequierePermiso(PermisosSistema.ProductosConsultar)]
    public async Task<ActionResult<CategoriaConsultaDto>> Obtener(int id, CancellationToken token) => Ok(await servicio.ObtenerCategoriaAsync(id, User.ObtenerContextoConfiguracion(HttpContext), token));

    [HttpPost, RequierePermiso(PermisosSistema.ProductosAdministrar)]
    public async Task<ActionResult<CategoriaConsultaDto>> Crear(CrearCategoriaSolicitud solicitud, CancellationToken token)
    {
        var creado = await servicio.CrearCategoriaAsync(solicitud, User.ObtenerContextoConfiguracion(HttpContext), token);
        return CreatedAtAction(nameof(Obtener), new { id = creado.Id }, creado);
    }

    [HttpPut("{id:int}"), RequierePermiso(PermisosSistema.ProductosAdministrar)]
    public async Task<ActionResult<CategoriaConsultaDto>> Actualizar(int id, ActualizarCategoriaSolicitud solicitud, CancellationToken token) => Ok(await servicio.ActualizarCategoriaAsync(id, solicitud, User.ObtenerContextoConfiguracion(HttpContext), token));

    [HttpPut("{id:int}/estado"), RequierePermiso(PermisosSistema.ProductosAdministrar)]
    public async Task<ActionResult<CategoriaConsultaDto>> CambiarEstado(int id, CambiarEstadoSolicitud solicitud, CancellationToken token) => Ok(await servicio.CambiarEstadoCategoriaAsync(id, solicitud.EstadoId, User.ObtenerContextoConfiguracion(HttpContext), token));
}
