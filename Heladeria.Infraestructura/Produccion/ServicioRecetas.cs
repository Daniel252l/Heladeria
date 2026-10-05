using Heladeria.Aplicacion.Configuracion;
using Heladeria.Aplicacion.Produccion;
using Heladeria.Aplicacion.Seguridad.Configuracion;
using Heladeria.Infraestructura.Inventario;
using Heladeria.Infraestructura.Persistencia;
using Heladeria.Infraestructura.Persistencia.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Heladeria.Infraestructura.Produccion;

public sealed class ServicioRecetas(
    HeladeriaDbContext contexto,
    IOptions<OpcionesSeguridad> opciones,
    TimeProvider reloj,
    CoordinadorAjustesInventario coordinador,
    ServicioInventario inventario)
    : ServicioProduccionBase(contexto, opciones, reloj, coordinador, inventario), IServicioRecetas
{
    public async Task<RespuestaPaginada<OpcionRecetaDto>> ListarProductosAsync(
        FiltroProductosProduccionSolicitud filtro, ContextoConfiguracion seguridad, CancellationToken token)
    {
        Validar(filtro);
        await ValidarSucursalAsync(seguridad, token);
        var consulta = Contexto.Productos.AsNoTracking().Where(item => item.EmpresaId == seguridad.EmpresaId);
        if (Opcional(filtro.Busqueda)?.ToUpperInvariant() is { } texto)
            consulta = consulta.Where(item => item.Nombre.ToUpper().Contains(texto) || item.SKU.ToUpper().Contains(texto));
        var total = await consulta.CountAsync(token);
        var productos = await Paginar(consulta.OrderBy(item => item.Nombre), filtro)
            .Select(item => new OpcionRecetaDto(item.Id, item.SKU, item.Nombre)).ToArrayAsync(token);
        return new(productos, filtro.Pagina, filtro.TamanioPagina, total);
    }

    public async Task<ReferenciasRecetasDto> ObtenerReferenciasAsync(
        ContextoConfiguracion seguridad, CancellationToken token)
    {
        await ValidarSucursalAsync(seguridad, token);
        var productos = await Contexto.Productos.AsNoTracking()
            .Where(item => item.EmpresaId == seguridad.EmpresaId)
            .OrderBy(item => item.Nombre)
            .Select(item => new OpcionRecetaDto(item.Id, item.SKU, item.Nombre)).ToArrayAsync(token);
        var estados = await Contexto.Estados.AsNoTracking().Where(item => item.Activo)
            .OrderBy(item => item.Nombre)
            .Select(item => new OpcionRecetaDto(item.Id, string.Empty, item.Nombre)).ToArrayAsync(token);
        return new(productos, estados);
    }

    public async Task<RespuestaPaginada<RecetaResumenDto>> ListarAsync(
        FiltroRecetasSolicitud filtro, ContextoConfiguracion seguridad, CancellationToken token)
    {
        Validar(filtro);
        await ValidarSucursalAsync(seguridad, token);
        var consulta = Contexto.Recetas.AsNoTracking().Where(item => item.EmpresaId == seguridad.EmpresaId);
        if (Opcional(filtro.Codigo)?.ToUpperInvariant() is { } codigo)
            consulta = consulta.Where(item => item.Codigo.ToUpper() == codigo);
        if (filtro.ProductoTerminadoId is { } producto)
            consulta = consulta.Where(item => item.ProductoTerminadoId == producto);
        if (filtro.SoloVigentes is true)
            consulta = consulta.Where(item => item.VigenteHasta == null);
        if (filtro.SoloVigentes is false)
            consulta = consulta.Where(item => item.VigenteHasta != null);
        if (filtro.EstadoId is { } estado)
            consulta = consulta.Where(item => item.EstadoId == estado);
        if (filtro.Version is { } version)
            consulta = consulta.Where(item => item.Version == version);
        if (Opcional(filtro.Busqueda)?.ToUpperInvariant() is { } texto)
            consulta = consulta.Where(item => item.Codigo.ToUpper().Contains(texto)
                || item.Nombre.ToUpper().Contains(texto)
                || item.ProductoTerminado.Nombre.ToUpper().Contains(texto));
        var total = await consulta.CountAsync(token);
        var elementos = await Paginar(consulta.OrderBy(item => item.ProductoTerminado.Nombre)
                .ThenByDescending(item => item.Version), filtro)
            .Select(item => new RecetaResumenDto(
                item.Id, item.EmpresaId, item.ProductoTerminadoId, item.ProductoTerminado.Nombre,
                item.Codigo, item.Nombre, item.Rendimiento, item.ProductoPresentacionRendimientoId,
                item.UnidadRendimientoId, item.UnidadRendimiento.Nombre,
                item.UnidadRendimiento.Abreviatura, item.FactorConversion, item.RendimientoBase,
                item.Version, item.VigenteDesde, item.VigenteHasta, item.VigenteHasta == null,
                item.EstadoId, item.Estado.Nombre, Convert.ToBase64String(item.VersionFila)))
            .ToArrayAsync(token);
        return new(elementos, filtro.Pagina, filtro.TamanioPagina, total);
    }

    public async Task<RecetaConsultaDto> ObtenerAsync(
        long id, ContextoConfiguracion seguridad, CancellationToken token)
    {
        await ValidarSucursalAsync(seguridad, token);
        var encabezado = await Contexto.Recetas.AsNoTracking()
            .Where(item => item.Id == id && item.EmpresaId == seguridad.EmpresaId)
            .Select(item => new RecetaResumenDto(
                item.Id, item.EmpresaId, item.ProductoTerminadoId, item.ProductoTerminado.Nombre,
                item.Codigo, item.Nombre, item.Rendimiento, item.ProductoPresentacionRendimientoId,
                item.UnidadRendimientoId, item.UnidadRendimiento.Nombre,
                item.UnidadRendimiento.Abreviatura, item.FactorConversion, item.RendimientoBase,
                item.Version, item.VigenteDesde, item.VigenteHasta, item.VigenteHasta == null,
                item.EstadoId, item.Estado.Nombre, Convert.ToBase64String(item.VersionFila)))
            .SingleOrDefaultAsync(token)
            ?? throw new RecursoProduccionNoEncontradoException("Receta no encontrada.");
        var detalles = await Contexto.RecetaDetalles.AsNoTracking()
            .Where(item => item.RecetaId == id)
            .OrderBy(item => item.Id)
            .Select(item => new RecetaDetalleDto(
                item.Id, item.ProductoInsumoId, item.ProductoInsumo.Nombre,
                item.ProductoPresentacionId,
                item.ProductoPresentacion == null ? null : item.ProductoPresentacion.Presentaciones.Nombre,
                item.UnidadMedidaId, item.UnidadMedida.Nombre, item.UnidadMedida.Abreviatura,
                item.CantidadRequerida, item.FactorConversion, item.CantidadBase,
                item.ProductoInsumo.UnidadBaseId, item.ProductoInsumo.UnidadesMedida.Nombre,
                item.ProductoInsumo.UnidadesMedida.Abreviatura, item.Observacion))
            .ToArrayAsync(token);
        var metadata = await Contexto.Recetas.AsNoTracking().Where(item => item.Id == id
                && item.EmpresaId == seguridad.EmpresaId)
            .Select(item => new
            {
                FueUtilizada = item.OrdenesProduccion.Any(),
                UnidadBase = item.ProductoTerminado.UnidadesMedida.Nombre,
                Abreviatura = item.ProductoTerminado.UnidadesMedida.Abreviatura,
                Presentacion = item.ProductoPresentacionRendimiento == null ? null
                    : item.ProductoPresentacionRendimiento.Presentaciones.Nombre
            }).SingleAsync(token);
        return new(encabezado, detalles)
        {
            FueUtilizada = metadata.FueUtilizada, UnidadBase = metadata.UnidadBase,
            AbreviaturaUnidadBase = metadata.Abreviatura, PresentacionRendimiento = metadata.Presentacion
        };
    }

    public async Task<ProductoPresentacionesProduccionDto> ListarPresentacionesAsync(
        long productoId, ContextoConfiguracion seguridad, CancellationToken token)
    {
        await ValidarSucursalAsync(seguridad, token);
        await ValidarProductoAsync(productoId, seguridad, token);
        var producto = await Contexto.Productos.AsNoTracking()
            .Where(item => item.Id == productoId && item.EmpresaId == seguridad.EmpresaId)
            .Select(item => new
            {
                item.Id,
                item.Nombre,
                item.UnidadBaseId,
                Codigo = item.UnidadesMedida.Codigo,
                Unidad = item.UnidadesMedida.Nombre,
                item.UnidadesMedida.Abreviatura
            }).SingleAsync(token);
        var presentaciones = await Contexto.ProductoPresentaciones.AsNoTracking()
            .Where(item => item.ProductoId == productoId && item.EmpresaId == seguridad.EmpresaId
                && item.Estado.Activo && Operativos.Contains(item.Estado.Nombre)
                && item.Presentaciones.Estado.Activo && Operativos.Contains(item.Presentaciones.Estado.Nombre)
                && item.UnidadesMedida.Estado.Activo && Operativos.Contains(item.UnidadesMedida.Estado.Nombre))
            .OrderByDescending(item => item.EsPresentacionBase)
            .ThenBy(item => item.Presentaciones.Nombre)
            .Select(item => new PresentacionProduccionDto(
                item.Id, item.PresentacionId, item.Presentaciones.Codigo,
                item.Presentaciones.Nombre, item.UnidadMedidaId,
                item.UnidadesMedida.Codigo, item.UnidadesMedida.Nombre,
                item.UnidadesMedida.Abreviatura, item.FactorConversion,
                item.EsPresentacionBase))
            .ToArrayAsync(token);
        return new(producto.Id, producto.Nombre, producto.UnidadBaseId,
            producto.Codigo, producto.Unidad, producto.Abreviatura, presentaciones);
    }

    public Task<RecetaConsultaDto> CrearAsync(
        CrearRecetaSolicitud solicitud, ContextoConfiguracion seguridad, CancellationToken token) =>
        GuardarAsync(null, solicitud, null, seguridad, token);

    public Task<RecetaConsultaDto> ActualizarAsync(
        long id, ActualizarRecetaSolicitud solicitud, ContextoConfiguracion seguridad, CancellationToken token) =>
        GuardarAsync(id, solicitud, solicitud.VersionFila, seguridad, token);

    private Task<RecetaConsultaDto> GuardarAsync(
        long? id, GuardarRecetaSolicitudBase solicitud, string? versionFila,
        ContextoConfiguracion seguridad, CancellationToken token) =>
        EscribirAsync(seguridad, async () =>
        {
            Validar(solicitud);
            if (solicitud.VigenteDesde == default)
                throw new ValidacionProduccionException("VigenteDesde es obligatorio.");
            if (solicitud.Detalles.Any(item => item is null))
                throw new ValidacionProduccionException("Los detalles no pueden contener elementos nulos.");
            if (solicitud.Detalles.DistinctBy(item => item.ProductoInsumoId).Count() != solicitud.Detalles.Length)
                throw new ValidacionProduccionException("No se puede repetir un ingrediente dentro de la receta.");
            Precision(solicitud.Rendimiento, "Rendimiento");
            await ValidarEstadoAsync(solicitud.EstadoId, token);
            var terminado = await ValidarProductoAsync(solicitud.ProductoTerminadoId, seguridad, token);
            await ValidarPresentacionAsync(solicitud.ProductoTerminadoId,
                solicitud.ProductoPresentacionRendimientoId, seguridad, token);
            var salida = await ConvertirAsync(solicitud.ProductoTerminadoId,
                solicitud.ProductoPresentacionRendimientoId, solicitud.Rendimiento, seguridad, token);

            var detalles = new List<DetallePreparado>();
            foreach (var fila in solicitud.Detalles)
            {
                Validar(fila);
                Precision(fila.Cantidad, "Cantidad de ingrediente");
                if (fila.ProductoInsumoId == solicitud.ProductoTerminadoId)
                    throw new ValidacionProduccionException("Un producto no puede ser ingrediente directo de sí mismo.");
                await ValidarProductoAsync(fila.ProductoInsumoId, seguridad, token);
                await ValidarPresentacionAsync(fila.ProductoInsumoId,
                    fila.ProductoPresentacionId, seguridad, token);
                var conversion = await ConvertirAsync(fila.ProductoInsumoId,
                    fila.ProductoPresentacionId, fila.Cantidad, seguridad, token);
                detalles.Add(new(fila, conversion));
            }
            await ValidarCiclosAsync(solicitud.ProductoTerminadoId,
                detalles.Select(item => item.Solicitud.ProductoInsumoId).ToArray(),
                seguridad.EmpresaId, token);

            Recetas entidad;
            var versionada = false;
            if (!id.HasValue)
            {
                if (await Contexto.Recetas.AnyAsync(item => item.EmpresaId == seguridad.EmpresaId
                    && item.ProductoTerminadoId == solicitud.ProductoTerminadoId
                    && item.VigenteHasta == null, token))
                    throw new ConflictoProduccionException("El producto ya tiene una receta vigente.");
                if (await Contexto.Recetas.AnyAsync(item => item.EmpresaId == seguridad.EmpresaId
                    && item.Codigo == Codigo(solicitud.Codigo), token))
                    throw new ConflictoProduccionException("El código de receta ya existe en la empresa.");
                entidad = new Recetas { EmpresaId = seguridad.EmpresaId, Version = 1, VersionFila = [] };
                Contexto.Recetas.Add(entidad);
            }
            else
            {
                entidad = await Contexto.Recetas.Include(item => item.RecetaDetalles)
                    .SingleOrDefaultAsync(item => item.Id == id && item.EmpresaId == seguridad.EmpresaId, token)
                    ?? throw new RecursoProduccionNoEncontradoException("Receta no encontrada.");
                ValidarVersion(entidad.VersionFila, versionFila!);
                if (entidad.VigenteHasta is not null)
                    throw new ConflictoProduccionException("Una versión histórica de receta es inmutable.");
                if (entidad.ProductoTerminadoId != solicitud.ProductoTerminadoId)
                    throw new ValidacionProduccionException("No se puede cambiar el producto terminado de una receta existente.");
                if (!string.Equals(entidad.Codigo, Codigo(solicitud.Codigo), StringComparison.Ordinal)
                    && await Contexto.Recetas.AnyAsync(item => item.EmpresaId == seguridad.EmpresaId
                        && item.Codigo == Codigo(solicitud.Codigo) && item.Id != entidad.Id, token))
                    throw new ConflictoProduccionException("El código de receta ya existe en la empresa.");

                if (await Contexto.OrdenesProduccion.AnyAsync(item => item.RecetaId == entidad.Id, token))
                {
                    if (!string.Equals(entidad.Codigo, Codigo(solicitud.Codigo), StringComparison.Ordinal))
                        throw new ValidacionProduccionException(
                            "El código no puede cambiar al versionar una receta que ya fue utilizada.");
                    if (solicitud.VigenteDesde <= entidad.VigenteDesde)
                        throw new ValidacionProduccionException(
                            "La nueva versión debe iniciar después de la versión utilizada.");
                    entidad.VigenteHasta = solicitud.VigenteDesde.AddDays(-1);
                    await Contexto.SaveChangesAsync(token);
                    var siguiente = await Contexto.Recetas
                        .Where(item => item.EmpresaId == seguridad.EmpresaId && item.Codigo == entidad.Codigo)
                        .MaxAsync(item => item.Version, token) + 1;
                    entidad = new Recetas
                    {
                        EmpresaId = seguridad.EmpresaId,
                        ProductoTerminadoId = solicitud.ProductoTerminadoId,
                        Codigo = entidad.Codigo,
                        Version = siguiente,
                        VersionFila = []
                    };
                    Contexto.Recetas.Add(entidad);
                    versionada = true;
                }
                else
                {
                    Contexto.RecetaDetalles.RemoveRange(entidad.RecetaDetalles);
                }
            }

            entidad.ProductoTerminadoId = terminado.Id;
            entidad.ProductoPresentacionRendimientoId = solicitud.ProductoPresentacionRendimientoId;
            entidad.Codigo = Codigo(solicitud.Codigo);
            entidad.Nombre = Texto(solicitud.Nombre);
            entidad.Rendimiento = solicitud.Rendimiento;
            entidad.UnidadRendimientoId = salida.UnidadEntradaId;
            entidad.FactorConversion = salida.Factor;
            entidad.RendimientoBase = salida.CantidadBase;
            entidad.VigenteDesde = solicitud.VigenteDesde;
            entidad.VigenteHasta = null;
            entidad.EstadoId = solicitud.EstadoId;
            await Contexto.SaveChangesAsync(token);

            foreach (var fila in detalles)
                Contexto.RecetaDetalles.Add(new RecetaDetalles
                {
                    RecetaId = entidad.Id,
                    ProductoInsumoId = fila.Solicitud.ProductoInsumoId,
                    ProductoPresentacionId = fila.Solicitud.ProductoPresentacionId,
                    UnidadMedidaId = fila.Conversion.UnidadEntradaId,
                    CantidadRequerida = fila.Solicitud.Cantidad,
                    FactorConversion = fila.Conversion.Factor,
                    CantidadBase = fila.Conversion.CantidadBase,
                    Observacion = Opcional(fila.Solicitud.Observacion),
                    PorcentajeDesperdicioTecnico = 0m
                });
            await Contexto.SaveChangesAsync(token);
            Auditar(seguridad, "Recetas", entidad.Id,
                !id.HasValue ? "RECETA_CREADA" : versionada ? "RECETA_VERSIONADA" : "RECETA_ACTUALIZADA",
                $"Receta {entidad.Codigo}, versión {entidad.Version}; cantidades base persistidas.", "RECETAS");
            await Contexto.SaveChangesAsync(token);
            return await ObtenerAsync(entidad.Id, seguridad, token);
        }, token);

    private async Task ValidarEstadoAsync(int estadoId, CancellationToken token)
    {
        if (!await Contexto.Estados.AsNoTracking().AnyAsync(item => item.Id == estadoId && item.Activo, token))
            throw new ValidacionProduccionException("El estado no existe o no está disponible.");
    }

    private async Task ValidarCiclosAsync(
        long productoRaiz, IReadOnlyCollection<long> ingredientes,
        int empresaId, CancellationToken token)
    {
        var relaciones = await Contexto.RecetaDetalles.AsNoTracking()
            .Where(item => item.Receta.EmpresaId == empresaId && item.Receta.VigenteHasta == null)
            .Select(item => new { item.Receta.ProductoTerminadoId, item.ProductoInsumoId })
            .ToArrayAsync(token);
        var grafo = relaciones.GroupBy(item => item.ProductoTerminadoId)
            .ToDictionary(grupo => grupo.Key, grupo => grupo.Select(item => item.ProductoInsumoId).Distinct().ToArray());
        grafo[productoRaiz] = ingredientes.Distinct().ToArray();

        var visitados = new HashSet<long>();
        var ruta = new HashSet<long>();
        bool HayCiclo(long producto)
        {
            if (!ruta.Add(producto)) return true;
            if (visitados.Add(producto) && grafo.TryGetValue(producto, out var hijos))
                foreach (var hijo in hijos)
                    if (HayCiclo(hijo)) return true;
            ruta.Remove(producto);
            return false;
        }
        if (HayCiclo(productoRaiz))
            throw new ValidacionProduccionException("La receta generaría un ciclo directo o indirecto entre productos.");
    }

    private sealed record DetallePreparado(
        RecetaDetalleSolicitud Solicitud, ConversionProduccion Conversion);
}
