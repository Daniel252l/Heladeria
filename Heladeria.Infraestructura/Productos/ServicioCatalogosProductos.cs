using Heladeria.Aplicacion.Configuracion;
using Heladeria.Aplicacion.Productos;
using Heladeria.Aplicacion.Seguridad.Configuracion;
using Heladeria.Infraestructura.Persistencia;
using Heladeria.Infraestructura.Persistencia.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Heladeria.Infraestructura.Fase4;

public sealed class ServicioCatalogosProductos(
    HeladeriaDbContext contexto,
    IAuditoriaFase4 auditoria,
    IOptions<OpcionesSeguridad> opciones,
    TimeProvider timeProvider)
    : ServicioFase4Base(contexto, auditoria, opciones, timeProvider),
        IServicioCatalogosProductos
{
    public async Task<RespuestaPaginada<CategoriaConsultaDto>> ListarCategoriasAsync(
        FiltroCatalogoProductoSolicitud filtro,
        ContextoConfiguracion seguridad,
        CancellationToken token)
    {
        var consulta = Contexto.Categorias.AsNoTracking()
            .Where(item => item.EmpresaId == seguridad.EmpresaId);
        consulta = Filtrar(consulta, filtro, item => item.Codigo, item => item.Nombre);
        var total = await consulta.CountAsync(token);
        var elementos = await Paginar(consulta.OrderBy(item => item.Nombre).ThenBy(item => item.Id), filtro)
            .Select(item => new CategoriaConsultaDto(
                item.Id, item.EmpresaId, item.CategoriaPadreId, item.Codigo, item.Nombre,
                item.Descripcion, item.Nivel, item.EstadoId, item.Estado.Nombre,
                Convert.ToBase64String(item.VersionFila)))
            .ToArrayAsync(token);
        return new(elementos, filtro.Pagina, filtro.TamanioPagina, total);
    }

    public async Task<CategoriaConsultaDto> ObtenerCategoriaAsync(
        int id,
        ContextoConfiguracion seguridad,
        CancellationToken token) => await Contexto.Categorias.AsNoTracking()
        .Where(item => item.Id == id && item.EmpresaId == seguridad.EmpresaId)
        .Select(item => new CategoriaConsultaDto(
            item.Id, item.EmpresaId, item.CategoriaPadreId, item.Codigo, item.Nombre,
            item.Descripcion, item.Nivel, item.EstadoId, item.Estado.Nombre,
            Convert.ToBase64String(item.VersionFila)))
        .SingleOrDefaultAsync(token)
        ?? throw new RecursoFase4NoEncontradoException("La categoría no existe.");

    public async Task<CategoriaConsultaDto> CrearCategoriaAsync(
        CrearCategoriaSolicitud solicitud,
        ContextoConfiguracion seguridad,
        CancellationToken token)
    {
        var nivel = await ValidarCategoriaAsync(solicitud, seguridad.EmpresaId, null, token);
        var entidad = new Categorias
        {
            EmpresaId = seguridad.EmpresaId,
            FechaCreacion = AhoraUtc,
            CreadoPorUsuarioId = seguridad.UsuarioId,
            VersionFila = []
        };
        AplicarCategoria(entidad, solicitud, nivel);
        Contexto.Categorias.Add(entidad);
        await GuardarCambiosAsync(token);
        Auditoria.Registrar(seguridad, "Categorias", entidad.Id.ToString(), "CREAR", "Categoría creada.");
        await GuardarCambiosAsync(token);
        return await ObtenerCategoriaAsync(entidad.Id, seguridad, token);
    }

    public async Task<CategoriaConsultaDto> ActualizarCategoriaAsync(
        int id,
        ActualizarCategoriaSolicitud solicitud,
        ContextoConfiguracion seguridad,
        CancellationToken token)
    {
        var entidad = await Contexto.Categorias.SingleOrDefaultAsync(
            item => item.Id == id && item.EmpresaId == seguridad.EmpresaId,
            token) ?? throw new RecursoFase4NoEncontradoException("La categoría no existe.");
        ValidarVersion(entidad.VersionFila, solicitud.VersionFila);
        var nivel = await ValidarCategoriaAsync(solicitud, seguridad.EmpresaId, id, token);
        if (!await EsEstadoOperativoAsync(solicitud.EstadoId, token))
            await ValidarCategoriaSinDependenciasAsync(entidad, token);
        AplicarCategoria(entidad, solicitud, nivel);
        entidad.FechaModificacion = AhoraUtc;
        entidad.ModificadoPorUsuarioId = seguridad.UsuarioId;
        Auditoria.Registrar(seguridad, "Categorias", id.ToString(), "ACTUALIZAR", "Categoría actualizada.");
        await GuardarCambiosAsync(token);
        return await ObtenerCategoriaAsync(id, seguridad, token);
    }

    public async Task<CategoriaConsultaDto> CambiarEstadoCategoriaAsync(
        int id,
        int estadoId,
        ContextoConfiguracion seguridad,
        CancellationToken token)
    {
        var entidad = await Contexto.Categorias.SingleOrDefaultAsync(
            item => item.Id == id && item.EmpresaId == seguridad.EmpresaId,
            token) ?? throw new RecursoFase4NoEncontradoException("La categoría no existe.");
        await ValidarEstadoAsync(estadoId, token);
        if (!await EsEstadoOperativoAsync(estadoId, token))
            await ValidarCategoriaSinDependenciasAsync(entidad, token);
        entidad.EstadoId = estadoId;
        entidad.FechaModificacion = AhoraUtc;
        entidad.ModificadoPorUsuarioId = seguridad.UsuarioId;
        Auditoria.Registrar(seguridad, "Categorias", id.ToString(), "CAMBIAR_ESTADO", $"EstadoId={estadoId}.");
        await GuardarCambiosAsync(token);
        return await ObtenerCategoriaAsync(id, seguridad, token);
    }

    public async Task<RespuestaPaginada<MarcaConsultaDto>> ListarMarcasAsync(
        FiltroCatalogoProductoSolicitud filtro,
        ContextoConfiguracion seguridad,
        CancellationToken token)
    {
        var consulta = Contexto.Marcas.AsNoTracking()
            .Where(item => item.EmpresaId == seguridad.EmpresaId);
        consulta = Filtrar(consulta, filtro, item => item.Codigo, item => item.Nombre);
        var total = await consulta.CountAsync(token);
        var elementos = await Paginar(consulta.OrderBy(item => item.Nombre).ThenBy(item => item.Id), filtro)
            .Select(item => new MarcaConsultaDto(
                item.Id, item.EmpresaId, item.Codigo, item.Nombre, item.Descripcion,
                item.EstadoId, item.Estado.Nombre, Convert.ToBase64String(item.VersionFila)))
            .ToArrayAsync(token);
        return new(elementos, filtro.Pagina, filtro.TamanioPagina, total);
    }

    public async Task<MarcaConsultaDto> ObtenerMarcaAsync(
        int id,
        ContextoConfiguracion seguridad,
        CancellationToken token) => await Contexto.Marcas.AsNoTracking()
        .Where(item => item.Id == id && item.EmpresaId == seguridad.EmpresaId)
        .Select(item => new MarcaConsultaDto(
            item.Id, item.EmpresaId, item.Codigo, item.Nombre, item.Descripcion,
            item.EstadoId, item.Estado.Nombre, Convert.ToBase64String(item.VersionFila)))
        .SingleOrDefaultAsync(token)
        ?? throw new RecursoFase4NoEncontradoException("La marca no existe.");

    public async Task<MarcaConsultaDto> CrearMarcaAsync(
        CrearMarcaSolicitud solicitud,
        ContextoConfiguracion seguridad,
        CancellationToken token)
    {
        await ValidarCatalogoSimpleAsync(Contexto.Marcas, solicitud.Codigo, solicitud.Nombre, solicitud.EstadoId, seguridad.EmpresaId, null, token);
        var entidad = new Marcas
        {
            EmpresaId = seguridad.EmpresaId,
            FechaCreacion = AhoraUtc,
            CreadoPorUsuarioId = seguridad.UsuarioId,
            VersionFila = []
        };
        AplicarMarca(entidad, solicitud);
        Contexto.Marcas.Add(entidad);
        await GuardarCambiosAsync(token);
        Auditoria.Registrar(seguridad, "Marcas", entidad.Id.ToString(), "CREAR", "Marca creada.");
        await GuardarCambiosAsync(token);
        return await ObtenerMarcaAsync(entidad.Id, seguridad, token);
    }

    public async Task<MarcaConsultaDto> ActualizarMarcaAsync(
        int id,
        ActualizarMarcaSolicitud solicitud,
        ContextoConfiguracion seguridad,
        CancellationToken token)
    {
        var entidad = await Contexto.Marcas.SingleOrDefaultAsync(
            item => item.Id == id && item.EmpresaId == seguridad.EmpresaId,
            token) ?? throw new RecursoFase4NoEncontradoException("La marca no existe.");
        ValidarVersion(entidad.VersionFila, solicitud.VersionFila);
        await ValidarCatalogoSimpleAsync(Contexto.Marcas, solicitud.Codigo, solicitud.Nombre, solicitud.EstadoId, seguridad.EmpresaId, id, token);
        if (!await EsEstadoOperativoAsync(solicitud.EstadoId, token)
            && await Contexto.Productos.AnyAsync(item => item.EmpresaId == seguridad.EmpresaId && item.MarcaId == id && item.Estado.Activo && EstadosOperativos.Contains(item.Estado.Nombre), token))
            throw new ConflictoFase4Exception("La marca tiene productos activos asociados.");
        AplicarMarca(entidad, solicitud);
        MarcarModificacion(entidad, seguridad);
        Auditoria.Registrar(seguridad, "Marcas", id.ToString(), "ACTUALIZAR", "Marca actualizada.");
        await GuardarCambiosAsync(token);
        return await ObtenerMarcaAsync(id, seguridad, token);
    }

    public async Task<MarcaConsultaDto> CambiarEstadoMarcaAsync(
        int id,
        int estadoId,
        ContextoConfiguracion seguridad,
        CancellationToken token)
    {
        var entidad = await Contexto.Marcas.SingleOrDefaultAsync(
            item => item.Id == id && item.EmpresaId == seguridad.EmpresaId,
            token) ?? throw new RecursoFase4NoEncontradoException("La marca no existe.");
        await ValidarEstadoAsync(estadoId, token);
        if (!await EsEstadoOperativoAsync(estadoId, token)
            && await Contexto.Productos.AnyAsync(item => item.EmpresaId == seguridad.EmpresaId && item.MarcaId == id && item.Estado.Activo && EstadosOperativos.Contains(item.Estado.Nombre), token))
            throw new ConflictoFase4Exception("La marca tiene productos activos asociados.");
        entidad.EstadoId = estadoId;
        MarcarModificacion(entidad, seguridad);
        Auditoria.Registrar(seguridad, "Marcas", id.ToString(), "CAMBIAR_ESTADO", $"EstadoId={estadoId}.");
        await GuardarCambiosAsync(token);
        return await ObtenerMarcaAsync(id, seguridad, token);
    }

    public async Task<RespuestaPaginada<TipoProductoConsultaDto>> ListarTiposProductosAsync(
        FiltroCatalogoProductoSolicitud filtro,
        ContextoConfiguracion seguridad,
        CancellationToken token)
    {
        var consulta = Contexto.TiposProductos.AsNoTracking()
            .Where(item => item.EmpresaId == seguridad.EmpresaId);
        consulta = Filtrar(consulta, filtro, item => item.Codigo, item => item.Nombre);
        var total = await consulta.CountAsync(token);
        var elementos = await Paginar(consulta.OrderBy(item => item.Nombre).ThenBy(item => item.Id), filtro)
            .Select(item => new TipoProductoConsultaDto(
                item.Id, item.EmpresaId, item.Codigo, item.Nombre, item.ManejaExistencia,
                item.EstadoId, item.Estado.Nombre, Convert.ToBase64String(item.VersionFila)))
            .ToArrayAsync(token);
        return new(elementos, filtro.Pagina, filtro.TamanioPagina, total);
    }

    public async Task<TipoProductoConsultaDto> ObtenerTipoProductoAsync(
        int id,
        ContextoConfiguracion seguridad,
        CancellationToken token) => await Contexto.TiposProductos.AsNoTracking()
        .Where(item => item.Id == id && item.EmpresaId == seguridad.EmpresaId)
        .Select(item => new TipoProductoConsultaDto(
            item.Id, item.EmpresaId, item.Codigo, item.Nombre, item.ManejaExistencia,
            item.EstadoId, item.Estado.Nombre, Convert.ToBase64String(item.VersionFila)))
        .SingleOrDefaultAsync(token)
        ?? throw new RecursoFase4NoEncontradoException("El tipo de producto no existe.");

    public async Task<TipoProductoConsultaDto> CrearTipoProductoAsync(
        CrearTipoProductoSolicitud solicitud,
        ContextoConfiguracion seguridad,
        CancellationToken token)
    {
        await ValidarCatalogoSimpleAsync(Contexto.TiposProductos, solicitud.Codigo, solicitud.Nombre, solicitud.EstadoId, seguridad.EmpresaId, null, token);
        var entidad = new TiposProductos
        {
            EmpresaId = seguridad.EmpresaId,
            FechaCreacion = AhoraUtc,
            CreadoPorUsuarioId = seguridad.UsuarioId,
            VersionFila = []
        };
        AplicarTipoProducto(entidad, solicitud);
        Contexto.TiposProductos.Add(entidad);
        await GuardarCambiosAsync(token);
        Auditoria.Registrar(seguridad, "TiposProductos", entidad.Id.ToString(), "CREAR", "Tipo de producto creado.");
        await GuardarCambiosAsync(token);
        return await ObtenerTipoProductoAsync(entidad.Id, seguridad, token);
    }

    public async Task<TipoProductoConsultaDto> ActualizarTipoProductoAsync(
        int id,
        ActualizarTipoProductoSolicitud solicitud,
        ContextoConfiguracion seguridad,
        CancellationToken token)
    {
        var entidad = await Contexto.TiposProductos.SingleOrDefaultAsync(
            item => item.Id == id && item.EmpresaId == seguridad.EmpresaId,
            token) ?? throw new RecursoFase4NoEncontradoException("El tipo de producto no existe.");
        ValidarVersion(entidad.VersionFila, solicitud.VersionFila);
        await ValidarCatalogoSimpleAsync(Contexto.TiposProductos, solicitud.Codigo, solicitud.Nombre, solicitud.EstadoId, seguridad.EmpresaId, id, token);
        if ((!solicitud.ManejaExistencia || !await EsEstadoOperativoAsync(solicitud.EstadoId, token))
            && await Contexto.Productos.AnyAsync(item => item.EmpresaId == seguridad.EmpresaId && item.TipoProductoId == id && item.Estado.Activo && EstadosOperativos.Contains(item.Estado.Nombre), token))
            throw new ConflictoFase4Exception("El tipo tiene productos activos que manejan existencia.");
        AplicarTipoProducto(entidad, solicitud);
        MarcarModificacion(entidad, seguridad);
        Auditoria.Registrar(seguridad, "TiposProductos", id.ToString(), "ACTUALIZAR", "Tipo de producto actualizado.");
        await GuardarCambiosAsync(token);
        return await ObtenerTipoProductoAsync(id, seguridad, token);
    }

    public async Task<TipoProductoConsultaDto> CambiarEstadoTipoProductoAsync(
        int id,
        int estadoId,
        ContextoConfiguracion seguridad,
        CancellationToken token)
    {
        var entidad = await Contexto.TiposProductos.SingleOrDefaultAsync(
            item => item.Id == id && item.EmpresaId == seguridad.EmpresaId,
            token) ?? throw new RecursoFase4NoEncontradoException("El tipo de producto no existe.");
        await ValidarEstadoAsync(estadoId, token);
        if (!await EsEstadoOperativoAsync(estadoId, token)
            && await Contexto.Productos.AnyAsync(item => item.EmpresaId == seguridad.EmpresaId && item.TipoProductoId == id && item.Estado.Activo && EstadosOperativos.Contains(item.Estado.Nombre), token))
            throw new ConflictoFase4Exception("El tipo tiene productos activos asociados.");
        entidad.EstadoId = estadoId;
        MarcarModificacion(entidad, seguridad);
        Auditoria.Registrar(seguridad, "TiposProductos", id.ToString(), "CAMBIAR_ESTADO", $"EstadoId={estadoId}.");
        await GuardarCambiosAsync(token);
        return await ObtenerTipoProductoAsync(id, seguridad, token);
    }

    public async Task<RespuestaPaginada<UnidadMedidaConsultaDto>> ListarUnidadesAsync(
        FiltroCatalogoProductoSolicitud filtro,
        ContextoConfiguracion seguridad,
        CancellationToken token)
    {
        var consulta = Contexto.UnidadesMedida.AsNoTracking()
            .Where(item => item.EmpresaId == seguridad.EmpresaId);
        consulta = Filtrar(consulta, filtro, item => item.Codigo, item => item.Nombre);
        var total = await consulta.CountAsync(token);
        var elementos = await Paginar(consulta.OrderBy(item => item.Nombre).ThenBy(item => item.Id), filtro)
            .Select(item => new UnidadMedidaConsultaDto(
                item.Id, item.EmpresaId, item.Codigo, item.Nombre, item.Abreviatura,
                item.PermiteDecimales, item.CantidadDecimales, item.EstadoId,
                item.Estado.Nombre, Convert.ToBase64String(item.VersionFila)))
            .ToArrayAsync(token);
        return new(elementos, filtro.Pagina, filtro.TamanioPagina, total);
    }

    public async Task<UnidadMedidaConsultaDto> ObtenerUnidadAsync(
        int id,
        ContextoConfiguracion seguridad,
        CancellationToken token) => await Contexto.UnidadesMedida.AsNoTracking()
        .Where(item => item.Id == id && item.EmpresaId == seguridad.EmpresaId)
        .Select(item => new UnidadMedidaConsultaDto(
            item.Id, item.EmpresaId, item.Codigo, item.Nombre, item.Abreviatura,
            item.PermiteDecimales, item.CantidadDecimales, item.EstadoId,
            item.Estado.Nombre, Convert.ToBase64String(item.VersionFila)))
        .SingleOrDefaultAsync(token)
        ?? throw new RecursoFase4NoEncontradoException("La unidad de medida no existe.");

    public async Task<UnidadMedidaConsultaDto> CrearUnidadAsync(
        CrearUnidadMedidaSolicitud solicitud,
        ContextoConfiguracion seguridad,
        CancellationToken token)
    {
        await ValidarCatalogoSimpleAsync(Contexto.UnidadesMedida, solicitud.Codigo, solicitud.Nombre, solicitud.EstadoId, seguridad.EmpresaId, null, token);
        ValidarDecimales(solicitud.PermiteDecimales, solicitud.CantidadDecimales);
        var entidad = new UnidadesMedida
        {
            EmpresaId = seguridad.EmpresaId,
            FechaCreacion = AhoraUtc,
            CreadoPorUsuarioId = seguridad.UsuarioId,
            VersionFila = []
        };
        AplicarUnidad(entidad, solicitud);
        Contexto.UnidadesMedida.Add(entidad);
        await GuardarCambiosAsync(token);
        Auditoria.Registrar(seguridad, "UnidadesMedida", entidad.Id.ToString(), "CREAR", "Unidad de medida creada.");
        await GuardarCambiosAsync(token);
        return await ObtenerUnidadAsync(entidad.Id, seguridad, token);
    }

    public async Task<UnidadMedidaConsultaDto> ActualizarUnidadAsync(
        int id,
        ActualizarUnidadMedidaSolicitud solicitud,
        ContextoConfiguracion seguridad,
        CancellationToken token)
    {
        var entidad = await Contexto.UnidadesMedida.SingleOrDefaultAsync(
            item => item.Id == id && item.EmpresaId == seguridad.EmpresaId,
            token) ?? throw new RecursoFase4NoEncontradoException("La unidad de medida no existe.");
        ValidarVersion(entidad.VersionFila, solicitud.VersionFila);
        await ValidarCatalogoSimpleAsync(Contexto.UnidadesMedida, solicitud.Codigo, solicitud.Nombre, solicitud.EstadoId, seguridad.EmpresaId, id, token);
        ValidarDecimales(solicitud.PermiteDecimales, solicitud.CantidadDecimales);
        if (!await EsEstadoOperativoAsync(solicitud.EstadoId, token))
            await ValidarUnidadSinDependenciasAsync(entidad, token);
        AplicarUnidad(entidad, solicitud);
        MarcarModificacion(entidad, seguridad);
        Auditoria.Registrar(seguridad, "UnidadesMedida", id.ToString(), "ACTUALIZAR", "Unidad de medida actualizada.");
        await GuardarCambiosAsync(token);
        return await ObtenerUnidadAsync(id, seguridad, token);
    }

    public async Task<UnidadMedidaConsultaDto> CambiarEstadoUnidadAsync(
        int id,
        int estadoId,
        ContextoConfiguracion seguridad,
        CancellationToken token)
    {
        var entidad = await Contexto.UnidadesMedida.SingleOrDefaultAsync(
            item => item.Id == id && item.EmpresaId == seguridad.EmpresaId,
            token) ?? throw new RecursoFase4NoEncontradoException("La unidad de medida no existe.");
        await ValidarEstadoAsync(estadoId, token);
        if (!await EsEstadoOperativoAsync(estadoId, token))
            await ValidarUnidadSinDependenciasAsync(entidad, token);
        entidad.EstadoId = estadoId;
        MarcarModificacion(entidad, seguridad);
        Auditoria.Registrar(seguridad, "UnidadesMedida", id.ToString(), "CAMBIAR_ESTADO", $"EstadoId={estadoId}.");
        await GuardarCambiosAsync(token);
        return await ObtenerUnidadAsync(id, seguridad, token);
    }

    public async Task<RespuestaPaginada<PresentacionConsultaDto>> ListarPresentacionesAsync(
        FiltroCatalogoProductoSolicitud filtro,
        ContextoConfiguracion seguridad,
        CancellationToken token)
    {
        var consulta = Contexto.Presentaciones.AsNoTracking()
            .Where(item => item.EmpresaId == seguridad.EmpresaId);
        consulta = Filtrar(consulta, filtro, item => item.Codigo, item => item.Nombre);
        var total = await consulta.CountAsync(token);
        var elementos = await Paginar(consulta.OrderBy(item => item.Nombre).ThenBy(item => item.Id), filtro)
            .Select(item => new PresentacionConsultaDto(
                item.Id, item.EmpresaId, item.Codigo, item.Nombre, item.Descripcion,
                item.EstadoId, item.Estado.Nombre, Convert.ToBase64String(item.VersionFila)))
            .ToArrayAsync(token);
        return new(elementos, filtro.Pagina, filtro.TamanioPagina, total);
    }

    public async Task<PresentacionConsultaDto> ObtenerPresentacionAsync(
        int id,
        ContextoConfiguracion seguridad,
        CancellationToken token) => await Contexto.Presentaciones.AsNoTracking()
        .Where(item => item.Id == id && item.EmpresaId == seguridad.EmpresaId)
        .Select(item => new PresentacionConsultaDto(
            item.Id, item.EmpresaId, item.Codigo, item.Nombre, item.Descripcion,
            item.EstadoId, item.Estado.Nombre, Convert.ToBase64String(item.VersionFila)))
        .SingleOrDefaultAsync(token)
        ?? throw new RecursoFase4NoEncontradoException("La presentación no existe.");

    public async Task<PresentacionConsultaDto> CrearPresentacionAsync(
        CrearPresentacionSolicitud solicitud,
        ContextoConfiguracion seguridad,
        CancellationToken token)
    {
        await ValidarCatalogoSimpleAsync(Contexto.Presentaciones, solicitud.Codigo, solicitud.Nombre, solicitud.EstadoId, seguridad.EmpresaId, null, token);
        var entidad = new Presentaciones
        {
            EmpresaId = seguridad.EmpresaId,
            FechaCreacion = AhoraUtc,
            CreadoPorUsuarioId = seguridad.UsuarioId,
            VersionFila = []
        };
        AplicarPresentacion(entidad, solicitud);
        Contexto.Presentaciones.Add(entidad);
        await GuardarCambiosAsync(token);
        Auditoria.Registrar(seguridad, "Presentaciones", entidad.Id.ToString(), "CREAR", "Presentación creada.");
        await GuardarCambiosAsync(token);
        return await ObtenerPresentacionAsync(entidad.Id, seguridad, token);
    }

    public async Task<PresentacionConsultaDto> ActualizarPresentacionAsync(
        int id,
        ActualizarPresentacionSolicitud solicitud,
        ContextoConfiguracion seguridad,
        CancellationToken token)
    {
        var entidad = await Contexto.Presentaciones.SingleOrDefaultAsync(
            item => item.Id == id && item.EmpresaId == seguridad.EmpresaId,
            token) ?? throw new RecursoFase4NoEncontradoException("La presentación no existe.");
        ValidarVersion(entidad.VersionFila, solicitud.VersionFila);
        await ValidarCatalogoSimpleAsync(Contexto.Presentaciones, solicitud.Codigo, solicitud.Nombre, solicitud.EstadoId, seguridad.EmpresaId, id, token);
        if (!await EsEstadoOperativoAsync(solicitud.EstadoId, token))
            await ValidarPresentacionSinDependenciasAsync(entidad, token);
        AplicarPresentacion(entidad, solicitud);
        MarcarModificacion(entidad, seguridad);
        Auditoria.Registrar(seguridad, "Presentaciones", id.ToString(), "ACTUALIZAR", "Presentación actualizada.");
        await GuardarCambiosAsync(token);
        return await ObtenerPresentacionAsync(id, seguridad, token);
    }

    public async Task<PresentacionConsultaDto> CambiarEstadoPresentacionAsync(
        int id,
        int estadoId,
        ContextoConfiguracion seguridad,
        CancellationToken token)
    {
        var entidad = await Contexto.Presentaciones.SingleOrDefaultAsync(
            item => item.Id == id && item.EmpresaId == seguridad.EmpresaId,
            token) ?? throw new RecursoFase4NoEncontradoException("La presentación no existe.");
        await ValidarEstadoAsync(estadoId, token);
        if (!await EsEstadoOperativoAsync(estadoId, token))
            await ValidarPresentacionSinDependenciasAsync(entidad, token);
        entidad.EstadoId = estadoId;
        MarcarModificacion(entidad, seguridad);
        Auditoria.Registrar(seguridad, "Presentaciones", id.ToString(), "CAMBIAR_ESTADO", $"EstadoId={estadoId}.");
        await GuardarCambiosAsync(token);
        return await ObtenerPresentacionAsync(id, seguridad, token);
    }

    public async Task<RespuestaPaginada<ConversionUnidadConsultaDto>> ListarConversionesAsync(
        FiltroCatalogoProductoSolicitud filtro,
        ContextoConfiguracion seguridad,
        CancellationToken token)
    {
        var consulta = Contexto.ConversionesUnidad.AsNoTracking()
            .Where(item => item.EmpresaId == seguridad.EmpresaId);
        var busqueda = NormalizarOpcional(filtro.Busqueda);
        if (busqueda is not null)
            consulta = consulta.Where(item => item.UnidadOrigen.Nombre.Contains(busqueda) || item.UnidadDestino.Nombre.Contains(busqueda));
        if (filtro.EstadoId.HasValue)
            consulta = consulta.Where(item => item.EstadoId == filtro.EstadoId.Value);
        var total = await consulta.CountAsync(token);
        var elementos = await Paginar(consulta.OrderBy(item => item.UnidadOrigen.Nombre).ThenBy(item => item.UnidadDestino.Nombre), filtro)
            .Select(item => new ConversionUnidadConsultaDto(
                item.Id, item.EmpresaId, item.UnidadOrigenId, item.UnidadOrigen.Nombre,
                item.UnidadDestinoId, item.UnidadDestino.Nombre, item.Factor,
                item.EstadoId, item.Estado.Nombre))
            .ToArrayAsync(token);
        return new(elementos, filtro.Pagina, filtro.TamanioPagina, total);
    }

    public async Task<ConversionUnidadConsultaDto> CrearConversionAsync(
        CrearConversionUnidadSolicitud solicitud,
        ContextoConfiguracion seguridad,
        CancellationToken token)
    {
        await ValidarConversionAsync(solicitud, seguridad.EmpresaId, null, token);
        var entidad = new ConversionesUnidad { EmpresaId = seguridad.EmpresaId };
        AplicarConversion(entidad, solicitud);
        Contexto.ConversionesUnidad.Add(entidad);
        await GuardarCambiosAsync(token);
        Auditoria.Registrar(seguridad, "ConversionesUnidad", entidad.Id.ToString(), "CREAR", "Conversión de unidad creada.");
        await GuardarCambiosAsync(token);
        return await ObtenerConversionAsync(entidad.Id, seguridad.EmpresaId, token);
    }

    public async Task<ConversionUnidadConsultaDto> ActualizarConversionAsync(
        long id,
        ActualizarConversionUnidadSolicitud solicitud,
        ContextoConfiguracion seguridad,
        CancellationToken token)
    {
        var entidad = await Contexto.ConversionesUnidad.SingleOrDefaultAsync(
            item => item.Id == id && item.EmpresaId == seguridad.EmpresaId,
            token) ?? throw new RecursoFase4NoEncontradoException("La conversión no existe.");
        await ValidarConversionAsync(solicitud, seguridad.EmpresaId, id, token);
        AplicarConversion(entidad, solicitud);
        Auditoria.Registrar(seguridad, "ConversionesUnidad", id.ToString(), "ACTUALIZAR", "Conversión de unidad actualizada.");
        await GuardarCambiosAsync(token);
        return await ObtenerConversionAsync(id, seguridad.EmpresaId, token);
    }

    public async Task<ConversionUnidadConsultaDto> CambiarEstadoConversionAsync(
        long id,
        int estadoId,
        ContextoConfiguracion seguridad,
        CancellationToken token)
    {
        var entidad = await Contexto.ConversionesUnidad.SingleOrDefaultAsync(
            item => item.Id == id && item.EmpresaId == seguridad.EmpresaId,
            token) ?? throw new RecursoFase4NoEncontradoException("La conversión no existe.");
        await ValidarEstadoAsync(estadoId, token);
        entidad.EstadoId = estadoId;
        Auditoria.Registrar(seguridad, "ConversionesUnidad", id.ToString(), "CAMBIAR_ESTADO", $"EstadoId={estadoId}.");
        await GuardarCambiosAsync(token);
        return await ObtenerConversionAsync(id, seguridad.EmpresaId, token);
    }

    private async Task<byte> ValidarCategoriaAsync(
        GuardarCategoriaSolicitudBase solicitud,
        int empresaId,
        int? id,
        CancellationToken token)
    {
        await ValidarEstadoAsync(solicitud.EstadoId, token);
        var codigo = NormalizarCodigo(solicitud.Codigo);
        var nombre = NormalizarTexto(solicitud.Nombre);
        if (await Contexto.Categorias.AnyAsync(item => item.EmpresaId == empresaId
            && (!id.HasValue || item.Id != id.Value)
            && (item.Codigo == codigo || item.Nombre.ToUpper() == nombre.ToUpper()), token))
            throw new ConflictoFase4Exception("Ya existe una categoría con el mismo código o nombre.");

        if (!solicitud.CategoriaPadreId.HasValue)
            return 1;
        if (solicitud.CategoriaPadreId == id)
            throw new ValidacionFase4Exception("La categoría no puede ser su propia categoría padre.");
        var padre = await Contexto.Categorias.AsNoTracking().SingleOrDefaultAsync(
            item => item.Id == solicitud.CategoriaPadreId && item.EmpresaId == empresaId,
            token) ?? throw new ValidacionFase4Exception("La categoría padre no existe en la empresa.");
        if (padre.Nivel >= 10)
            throw new ValidacionFase4Exception("La jerarquía de categorías no puede superar diez niveles.");
        if (id.HasValue)
            await ValidarSinCicloCategoriaAsync(id.Value, padre, empresaId, token);
        return checked((byte)(padre.Nivel + 1));
    }

    private async Task ValidarSinCicloCategoriaAsync(int id, Categorias padre, int empresaId, CancellationToken token)
    {
        var actual = padre;
        for (var nivel = 0; nivel < 10 && actual.CategoriaPadreId.HasValue; nivel++)
        {
            if (actual.Id == id)
                throw new ValidacionFase4Exception("La jerarquía de categorías produciría un ciclo.");
            actual = await Contexto.Categorias.AsNoTracking().SingleAsync(
                item => item.Id == actual.CategoriaPadreId && item.EmpresaId == empresaId,
                token);
        }
        if (actual.Id == id)
            throw new ValidacionFase4Exception("La jerarquía de categorías produciría un ciclo.");
    }

    private async Task ValidarCategoriaSinDependenciasAsync(Categorias entidad, CancellationToken token)
    {
        if (await Contexto.Categorias.AnyAsync(item => item.EmpresaId == entidad.EmpresaId && item.CategoriaPadreId == entidad.Id && item.Estado.Activo && EstadosOperativos.Contains(item.Estado.Nombre), token)
            || await Contexto.Productos.AnyAsync(item => item.EmpresaId == entidad.EmpresaId && item.CategoriaId == entidad.Id && item.Estado.Activo && EstadosOperativos.Contains(item.Estado.Nombre), token))
            throw new ConflictoFase4Exception("La categoría tiene categorías hijas o productos activos asociados.");
    }

    private async Task ValidarUnidadSinDependenciasAsync(UnidadesMedida entidad, CancellationToken token)
    {
        if (await Contexto.Productos.AnyAsync(item => item.EmpresaId == entidad.EmpresaId && item.UnidadBaseId == entidad.Id && item.Estado.Activo && EstadosOperativos.Contains(item.Estado.Nombre), token)
            || await Contexto.ProductoPresentaciones.AnyAsync(item => item.EmpresaId == entidad.EmpresaId && item.UnidadMedidaId == entidad.Id && item.Estado.Activo && EstadosOperativos.Contains(item.Estado.Nombre), token))
            throw new ConflictoFase4Exception("La unidad es utilizada por productos o presentaciones activas.");
    }

    private async Task ValidarPresentacionSinDependenciasAsync(Presentaciones entidad, CancellationToken token)
    {
        if (await Contexto.ProductoPresentaciones.AnyAsync(item => item.EmpresaId == entidad.EmpresaId && item.PresentacionId == entidad.Id && item.Estado.Activo && EstadosOperativos.Contains(item.Estado.Nombre), token))
            throw new ConflictoFase4Exception("La presentación es utilizada por productos activos.");
    }

    private async Task ValidarConversionAsync(
        CrearConversionUnidadSolicitud solicitud,
        int empresaId,
        long? id,
        CancellationToken token)
    {
        await ValidarEstadoAsync(solicitud.EstadoId, token);
        if (solicitud.UnidadOrigenId == solicitud.UnidadDestinoId)
            throw new ValidacionFase4Exception("Las unidades de origen y destino deben ser diferentes.");
        if (solicitud.Factor <= 0)
            throw new ValidacionFase4Exception("El factor de conversión debe ser positivo.");
        var unidades = await Contexto.UnidadesMedida.AsNoTracking()
            .CountAsync(item => item.EmpresaId == empresaId
                && (item.Id == solicitud.UnidadOrigenId || item.Id == solicitud.UnidadDestinoId)
                && item.Estado.Activo
                && EstadosOperativos.Contains(item.Estado.Nombre), token);
        if (unidades != 2)
            throw new ValidacionFase4Exception("Ambas unidades deben existir, estar activas y pertenecer a la misma empresa.");
        if (await Contexto.ConversionesUnidad.AnyAsync(item => item.EmpresaId == empresaId
            && item.UnidadOrigenId == solicitud.UnidadOrigenId
            && item.UnidadDestinoId == solicitud.UnidadDestinoId
            && (!id.HasValue || item.Id != id.Value), token))
            throw new ConflictoFase4Exception("La conversión indicada ya existe.");

        var aristas = await Contexto.ConversionesUnidad.AsNoTracking()
            .Where(item => item.EmpresaId == empresaId
                && (!id.HasValue || item.Id != id.Value)
                && item.Estado.Activo
                && EstadosOperativos.Contains(item.Estado.Nombre))
            .Select(item => new AristaConversion(item.UnidadOrigenId, item.UnidadDestinoId, item.Factor))
            .ToArrayAsync(token);
        var retorno = BuscarFactor(aristas, solicitud.UnidadDestinoId, solicitud.UnidadOrigenId);
        if (retorno.HasValue && Math.Abs(solicitud.Factor * retorno.Value - 1m) > 0.00000001m)
            throw new ConflictoFase4Exception("La conversión produciría un ciclo con factores incoherentes.");
    }

    private static decimal? BuscarFactor(IReadOnlyCollection<AristaConversion> aristas, int origen, int destino)
    {
        var pendientes = new Stack<(int Unidad, decimal Factor, HashSet<int> Visitadas)>();
        pendientes.Push((origen, 1m, [origen]));
        while (pendientes.Count > 0)
        {
            var actual = pendientes.Pop();
            if (actual.Unidad == destino)
                return actual.Factor;
            foreach (var arista in aristas.Where(item => item.Origen == actual.Unidad && !actual.Visitadas.Contains(item.Destino)))
            {
                var visitadas = new HashSet<int>(actual.Visitadas) { arista.Destino };
                pendientes.Push((arista.Destino, actual.Factor * arista.Factor, visitadas));
            }
        }
        return null;
    }

    private async Task<ConversionUnidadConsultaDto> ObtenerConversionAsync(long id, int empresaId, CancellationToken token) =>
        await Contexto.ConversionesUnidad.AsNoTracking()
            .Where(item => item.Id == id && item.EmpresaId == empresaId)
            .Select(item => new ConversionUnidadConsultaDto(
                item.Id, item.EmpresaId, item.UnidadOrigenId, item.UnidadOrigen.Nombre,
                item.UnidadDestinoId, item.UnidadDestino.Nombre, item.Factor,
                item.EstadoId, item.Estado.Nombre))
            .SingleAsync(token);

    private async Task ValidarCatalogoSimpleAsync<TEntity>(
        DbSet<TEntity> conjunto,
        string codigoSolicitado,
        string nombreSolicitado,
        int estadoId,
        int empresaId,
        int? id,
        CancellationToken token) where TEntity : class
    {
        await ValidarEstadoAsync(estadoId, token);
        var codigo = NormalizarCodigo(codigoSolicitado);
        var nombre = NormalizarTexto(nombreSolicitado);
        var duplicado = conjunto.AsQueryable().AnyAsync(item =>
            EF.Property<int>(item, "EmpresaId") == empresaId
            && (!id.HasValue || EF.Property<int>(item, "Id") != id.Value)
            && (EF.Property<string>(item, "Codigo") == codigo
                || EF.Property<string>(item, "Nombre").ToUpper() == nombre.ToUpper()), token);
        if (await duplicado)
            throw new ConflictoFase4Exception("Ya existe un registro con el mismo código o nombre.");
    }

    private static IQueryable<TEntity> Filtrar<TEntity>(
        IQueryable<TEntity> consulta,
        FiltroCatalogoProductoSolicitud filtro,
        System.Linq.Expressions.Expression<Func<TEntity, string>> codigo,
        System.Linq.Expressions.Expression<Func<TEntity, string>> nombre) where TEntity : class
    {
        var busqueda = NormalizarOpcional(filtro.Busqueda);
        if (busqueda is not null)
        {
            var parametro = codigo.Parameters[0];
            var contiene = typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!;
            var cuerpo = System.Linq.Expressions.Expression.OrElse(
                System.Linq.Expressions.Expression.Call(codigo.Body, contiene, System.Linq.Expressions.Expression.Constant(busqueda)),
                System.Linq.Expressions.Expression.Call(
                    new ReemplazarParametro(nombre.Parameters[0], parametro).Visit(nombre.Body)!,
                    contiene,
                    System.Linq.Expressions.Expression.Constant(busqueda)));
            consulta = consulta.Where(System.Linq.Expressions.Expression.Lambda<Func<TEntity, bool>>(cuerpo, parametro));
        }
        if (filtro.EstadoId.HasValue)
            consulta = consulta.Where(item => EF.Property<int>(item, "EstadoId") == filtro.EstadoId.Value);
        return consulta;
    }

    private static void AplicarCategoria(Categorias entidad, GuardarCategoriaSolicitudBase solicitud, byte nivel)
    {
        entidad.CategoriaPadreId = solicitud.CategoriaPadreId;
        entidad.Codigo = NormalizarCodigo(solicitud.Codigo);
        entidad.Nombre = NormalizarTexto(solicitud.Nombre);
        entidad.Descripcion = NormalizarOpcional(solicitud.Descripcion);
        entidad.Nivel = nivel;
        entidad.EstadoId = solicitud.EstadoId;
    }

    private static void AplicarMarca(Marcas entidad, GuardarMarcaSolicitudBase solicitud)
    {
        entidad.Codigo = NormalizarCodigo(solicitud.Codigo);
        entidad.Nombre = NormalizarTexto(solicitud.Nombre);
        entidad.Descripcion = NormalizarOpcional(solicitud.Descripcion);
        entidad.EstadoId = solicitud.EstadoId;
    }

    private static void AplicarTipoProducto(TiposProductos entidad, GuardarTipoProductoSolicitudBase solicitud)
    {
        entidad.Codigo = NormalizarCodigo(solicitud.Codigo);
        entidad.Nombre = NormalizarTexto(solicitud.Nombre);
        entidad.ManejaExistencia = solicitud.ManejaExistencia;
        entidad.EstadoId = solicitud.EstadoId;
    }

    private static void AplicarUnidad(UnidadesMedida entidad, GuardarUnidadMedidaSolicitudBase solicitud)
    {
        entidad.Codigo = NormalizarCodigo(solicitud.Codigo);
        entidad.Nombre = NormalizarTexto(solicitud.Nombre);
        entidad.Abreviatura = NormalizarTexto(solicitud.Abreviatura);
        entidad.PermiteDecimales = solicitud.PermiteDecimales;
        entidad.CantidadDecimales = solicitud.CantidadDecimales;
        entidad.EstadoId = solicitud.EstadoId;
    }

    private static void AplicarPresentacion(Presentaciones entidad, GuardarPresentacionSolicitudBase solicitud)
    {
        entidad.Codigo = NormalizarCodigo(solicitud.Codigo);
        entidad.Nombre = NormalizarTexto(solicitud.Nombre);
        entidad.Descripcion = NormalizarOpcional(solicitud.Descripcion);
        entidad.EstadoId = solicitud.EstadoId;
    }

    private static void AplicarConversion(ConversionesUnidad entidad, CrearConversionUnidadSolicitud solicitud)
    {
        entidad.UnidadOrigenId = solicitud.UnidadOrigenId;
        entidad.UnidadDestinoId = solicitud.UnidadDestinoId;
        entidad.Factor = solicitud.Factor;
        entidad.EstadoId = solicitud.EstadoId;
    }

    private void MarcarModificacion(Marcas entidad, ContextoConfiguracion seguridad)
    {
        entidad.FechaModificacion = AhoraUtc;
        entidad.ModificadoPorUsuarioId = seguridad.UsuarioId;
    }

    private void MarcarModificacion(TiposProductos entidad, ContextoConfiguracion seguridad)
    {
        entidad.FechaModificacion = AhoraUtc;
        entidad.ModificadoPorUsuarioId = seguridad.UsuarioId;
    }

    private void MarcarModificacion(UnidadesMedida entidad, ContextoConfiguracion seguridad)
    {
        entidad.FechaModificacion = AhoraUtc;
        entidad.ModificadoPorUsuarioId = seguridad.UsuarioId;
    }

    private void MarcarModificacion(Presentaciones entidad, ContextoConfiguracion seguridad)
    {
        entidad.FechaModificacion = AhoraUtc;
        entidad.ModificadoPorUsuarioId = seguridad.UsuarioId;
    }

    private static void ValidarDecimales(bool permiteDecimales, byte cantidadDecimales)
    {
        if (!permiteDecimales && cantidadDecimales != 0)
            throw new ValidacionFase4Exception("Una unidad que no permite decimales debe usar cero decimales.");
    }

    private sealed record AristaConversion(int Origen, int Destino, decimal Factor);

    private sealed class ReemplazarParametro(
        System.Linq.Expressions.ParameterExpression origen,
        System.Linq.Expressions.ParameterExpression destino) : System.Linq.Expressions.ExpressionVisitor
    {
        protected override System.Linq.Expressions.Expression VisitParameter(System.Linq.Expressions.ParameterExpression node) =>
            node == origen ? destino : base.VisitParameter(node);
    }
}
