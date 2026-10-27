using System.Data;
using Heladeria.Aplicacion.Configuracion;
using Heladeria.Aplicacion.Productos;
using Heladeria.Aplicacion.Seguridad.Configuracion;
using Heladeria.Infraestructura.Persistencia;
using Heladeria.Infraestructura.Inventario;
using Heladeria.Infraestructura.Persistencia.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Heladeria.Infraestructura.Fase4;

public sealed class ServicioProductos(
    HeladeriaDbContext contexto,
    IAuditoriaFase4 auditoria,
    IOptions<OpcionesSeguridad> opciones,
    TimeProvider timeProvider,
    CoordinadorAjustesInventario coordinador)
    : ServicioFase4Base(contexto, auditoria, opciones, timeProvider), IServicioProductos
{
    private const string ClaseTipoSeguimiento = "TIPO_SEGUIMIENTO_PRODUCTO";
    private const string ClaseMetodoValuacion = "METODO_VALUACION_INVENTARIO";
    private readonly ReglasComposicionVenta composicion = new(contexto, opciones.Value.EstadosActivos,
        new ServicioInventario(contexto, auditoria, opciones, timeProvider, coordinador));

    public async Task<ReferenciasProductoDto> ObtenerReferenciasAsync(
        ContextoConfiguracion seguridad,
        CancellationToken token)
    {
        var opciones = await Contexto.Catalogos.AsNoTracking()
            .Where(item => (item.EmpresaId == null || item.EmpresaId == seguridad.EmpresaId)
                && (item.ClaseCatalogo.Codigo == ClaseTipoSeguimiento
                    || item.ClaseCatalogo.Codigo == ClaseMetodoValuacion)
                && item.ClaseCatalogo.Estado.Activo
                && EstadosOperativos.Contains(item.ClaseCatalogo.Estado.Nombre)
                && item.Estado.Activo
                && EstadosOperativos.Contains(item.Estado.Nombre))
            .OrderBy(item => item.Nombre)
            .Select(item => new
            {
                Clase = item.ClaseCatalogo.Codigo,
                Opcion = new OpcionCatalogoProductoDto(item.Id, item.Codigo, item.Nombre)
            })
            .ToArrayAsync(token);

        return new ReferenciasProductoDto(
            opciones.Where(item => item.Clase == ClaseTipoSeguimiento).Select(item => item.Opcion).ToArray(),
            opciones.Where(item => item.Clase == ClaseMetodoValuacion).Select(item => item.Opcion).ToArray());
    }

    public async Task<RespuestaPaginada<ProductoConsultaDto>> ListarAsync(
        FiltroProductosSolicitud filtro,
        ContextoConfiguracion seguridad,
        CancellationToken token)
    {
        var consulta = Contexto.Productos.AsNoTracking()
            .Where(item => item.EmpresaId == seguridad.EmpresaId);
        if (filtro.SoloComponentesVenta)
            consulta = ReglasComposicionVenta.Terminales(Contexto, seguridad.EmpresaId, EstadosOperativos);
        var busqueda = NormalizarOpcional(filtro.Busqueda);
        if (busqueda is not null)
        {
            var termino = busqueda.ToUpperInvariant();
            consulta = consulta.Where(item => item.SKU.ToUpper().Contains(termino) || item.Nombre.ToUpper().Contains(termino));
        }
        if (filtro.CategoriaId.HasValue)
            consulta = consulta.Where(item => item.CategoriaId == filtro.CategoriaId.Value);
        if (filtro.MarcaId.HasValue)
            consulta = consulta.Where(item => item.MarcaId == filtro.MarcaId.Value);
        if (filtro.TipoProductoId.HasValue)
            consulta = consulta.Where(item => item.TipoProductoId == filtro.TipoProductoId.Value);
        if (filtro.EstadoId.HasValue)
            consulta = consulta.Where(item => item.EstadoId == filtro.EstadoId.Value);
        var total = await consulta.CountAsync(token);
        var elementos = await Proyectar(
                Paginar(consulta.OrderBy(item => item.Nombre).ThenBy(item => item.Id), filtro))
            .ToArrayAsync(token);
        return new(elementos, filtro.Pagina, filtro.TamanioPagina, total);
    }

    public async Task<ProductoConsultaDto> ObtenerAsync(
        long id,
        ContextoConfiguracion seguridad,
        CancellationToken token)
    {
        var producto = await Proyectar(Contexto.Productos.AsNoTracking()
            .Where(item => item.Id == id && item.EmpresaId == seguridad.EmpresaId)).SingleOrDefaultAsync(token)
            ?? throw new RecursoFase4NoEncontradoException("El producto no existe.");
        return producto with { Componentes = await ReglasComposicionVenta.ConsultarAsync(Contexto, id, seguridad.EmpresaId, token) };
    }

    public Task<ProductoConsultaDto> CrearAsync(CrearProductoSolicitud solicitud, ContextoConfiguracion seguridad, CancellationToken token) =>
        ProtegerConversionAsync(seguridad, () => CrearInternoAsync(solicitud, seguridad, token), token);

    private async Task<ProductoConsultaDto> CrearInternoAsync(
        CrearProductoSolicitud solicitud,
        ContextoConfiguracion seguridad,
        CancellationToken token)
    {
        await ValidarProductoAsync(solicitud, seguridad.EmpresaId, null, token);
        await ValidarComposicionAsync(null, solicitud, seguridad.EmpresaId, token);
        var entidad = new Productos
        {
            EmpresaId = seguridad.EmpresaId,
            FechaCreacion = AhoraUtc,
            CreadoPorUsuarioId = seguridad.UsuarioId,
            VersionFila = []
        };
        Aplicar(entidad, solicitud);
        Contexto.Productos.Add(entidad);
        await GuardarCambiosAsync(token);
        await ReemplazarComposicionAsync(entidad, solicitud.Componentes, token);
        Auditoria.Registrar(seguridad, "Productos", entidad.Id.ToString(), "CREAR", $"Producto {entidad.SKU} creado.");
        await GuardarCambiosAsync(token);
        return await ObtenerAsync(entidad.Id, seguridad, token);
    }

    public Task<ProductoConsultaDto> ActualizarAsync(
        long id, ActualizarProductoSolicitud solicitud, ContextoConfiguracion seguridad, CancellationToken token) =>
        ProtegerConversionAsync(seguridad, () => ActualizarProductoInternoAsync(id, solicitud, seguridad, token), token);

    private async Task<ProductoConsultaDto> ActualizarProductoInternoAsync(
        long id,
        ActualizarProductoSolicitud solicitud,
        ContextoConfiguracion seguridad,
        CancellationToken token)
    {
        var entidad = await Contexto.Productos.SingleOrDefaultAsync(
            item => item.Id == id && item.EmpresaId == seguridad.EmpresaId,
            token) ?? throw new RecursoFase4NoEncontradoException("El producto no existe.");
        ValidarVersion(entidad.VersionFila, solicitud.VersionFila);
        await ValidarProductoAsync(solicitud, seguridad.EmpresaId, id, token);
        await ValidarComposicionAsync(id, solicitud, seguridad.EmpresaId, token);
        if (entidad.UnidadBaseId != solicitud.UnidadBaseId
            && (await Contexto.Existencias.AnyAsync(item => item.EmpresaId == seguridad.EmpresaId && item.ProductoId == id, token)
                || await Contexto.ProductoPresentaciones.AnyAsync(item => item.EmpresaId == seguridad.EmpresaId && item.ProductoId == id, token)
                || await Contexto.OrdenesCompraDetalle.AnyAsync(item => item.ProductoId == id && item.OrdenCompra.EmpresaId == seguridad.EmpresaId, token)
                || await Contexto.CombosDetalle.AnyAsync(item => item.EmpresaId == seguridad.EmpresaId && (item.ComboProductoId == id || item.ProductoIncluidoId == id), token)
                || await Contexto.MovimientoDetalles.AnyAsync(item => item.EmpresaId == seguridad.EmpresaId && item.ProductoId == id, token)))
            throw new ConflictoFase4Exception("No se puede cambiar la unidad base de un producto con existencias o presentaciones.");
        Aplicar(entidad, solicitud);
        await ReemplazarComposicionAsync(entidad, solicitud.Componentes, token);
        entidad.FechaModificacion = AhoraUtc;
        entidad.ModificadoPorUsuarioId = seguridad.UsuarioId;
        Auditoria.Registrar(seguridad, "Productos", id.ToString(), "ACTUALIZAR", $"Producto {entidad.SKU} actualizado.");
        await GuardarCambiosAsync(token);
        return await ObtenerAsync(id, seguridad, token);
    }

    public async Task<ProductoConsultaDto> CambiarEstadoAsync(
        long id,
        int estadoId,
        ContextoConfiguracion seguridad,
        CancellationToken token)
    {
        var entidad = await Contexto.Productos.SingleOrDefaultAsync(
            item => item.Id == id && item.EmpresaId == seguridad.EmpresaId,
            token) ?? throw new RecursoFase4NoEncontradoException("El producto no existe.");
        await ValidarEstadoAsync(estadoId, token);
        entidad.EstadoId = estadoId;
        entidad.FechaModificacion = AhoraUtc;
        entidad.ModificadoPorUsuarioId = seguridad.UsuarioId;
        Auditoria.Registrar(seguridad, "Productos", id.ToString(), "CAMBIAR_ESTADO", $"EstadoId={estadoId}.");
        await GuardarCambiosAsync(token);
        return await ObtenerAsync(id, seguridad, token);
    }

    public async Task<IReadOnlyList<ProductoPresentacionConsultaDto>> ListarPresentacionesAsync(
        long productoId,
        ContextoConfiguracion seguridad,
        CancellationToken token)
    {
        if (!await Contexto.Productos.AsNoTracking().AnyAsync(
                item => item.Id == productoId && item.EmpresaId == seguridad.EmpresaId,
                token))
            throw new RecursoFase4NoEncontradoException("El producto no existe.");
        return await ProyectarPresentaciones(Contexto.ProductoPresentaciones.AsNoTracking()
                .Where(item => item.EmpresaId == seguridad.EmpresaId && item.ProductoId == productoId)
                .OrderByDescending(item => item.EsPresentacionBase)
                .ThenBy(item => item.Presentaciones.Nombre))
            .ToArrayAsync(token);
    }

    public async Task<ProductoPresentacionConsultaDto> CrearPresentacionAsync(
        long productoId,
        CrearProductoPresentacionSolicitud solicitud,
        ContextoConfiguracion seguridad,
        CancellationToken token)
    {
        await ValidarPresentacionProductoAsync(productoId, solicitud, seguridad.EmpresaId, null, token);
        var entidad = new ProductoPresentaciones
        {
            EmpresaId = seguridad.EmpresaId,
            ProductoId = productoId,
            FechaCreacion = AhoraUtc,
            CreadoPorUsuarioId = seguridad.UsuarioId,
            VersionFila = []
        };
        Aplicar(entidad, solicitud);
        Contexto.ProductoPresentaciones.Add(entidad);
        await GuardarCambiosAsync(token);
        Auditoria.Registrar(seguridad, "ProductoPresentaciones", entidad.Id.ToString(), "CREAR", "Presentación de producto creada.");
        await GuardarCambiosAsync(token);
        return await ObtenerPresentacionProductoAsync(productoId, entidad.Id, seguridad.EmpresaId, token);
    }

    public Task<ProductoPresentacionConsultaDto> ActualizarPresentacionAsync(
        long productoId, long id, ActualizarProductoPresentacionSolicitud solicitud, ContextoConfiguracion seguridad, CancellationToken token) =>
        ProtegerConversionAsync(seguridad, () => ActualizarPresentacionInternoAsync(productoId, id, solicitud, seguridad, token), token);

    private async Task<ProductoPresentacionConsultaDto> ActualizarPresentacionInternoAsync(
        long productoId,
        long id,
        ActualizarProductoPresentacionSolicitud solicitud,
        ContextoConfiguracion seguridad,
        CancellationToken token)
    {
        var entidad = await Contexto.ProductoPresentaciones.SingleOrDefaultAsync(
            item => item.Id == id && item.ProductoId == productoId && item.EmpresaId == seguridad.EmpresaId,
            token) ?? throw new RecursoFase4NoEncontradoException("La presentación del producto no existe.");
        ValidarVersion(entidad.VersionFila, solicitud.VersionFila);
        await ValidarPresentacionProductoAsync(productoId, solicitud, seguridad.EmpresaId, id, token);
        if ((entidad.FactorConversion != solicitud.FactorConversion || entidad.UnidadMedidaId != solicitud.UnidadMedidaId)
            && await Contexto.OrdenesCompraDetalle.AnyAsync(item => item.ProductoPresentacionId == id
                && item.OrdenCompra.EmpresaId == seguridad.EmpresaId, token))
            throw new ConflictoFase4Exception("No se puede cambiar el factor o unidad de una presentación utilizada en órdenes de compra.");
        Aplicar(entidad, solicitud);
        entidad.FechaModificacion = AhoraUtc;
        entidad.ModificadoPorUsuarioId = seguridad.UsuarioId;
        Auditoria.Registrar(seguridad, "ProductoPresentaciones", id.ToString(), "ACTUALIZAR", "Presentación de producto actualizada.");
        await GuardarCambiosAsync(token);
        return await ObtenerPresentacionProductoAsync(productoId, id, seguridad.EmpresaId, token);
    }

    public async Task<ProductoPresentacionConsultaDto> CambiarEstadoPresentacionAsync(
        long productoId,
        long id,
        int estadoId,
        ContextoConfiguracion seguridad,
        CancellationToken token)
    {
        var entidad = await Contexto.ProductoPresentaciones.SingleOrDefaultAsync(
            item => item.Id == id && item.ProductoId == productoId && item.EmpresaId == seguridad.EmpresaId,
            token) ?? throw new RecursoFase4NoEncontradoException("La presentación del producto no existe.");
        await ValidarEstadoAsync(estadoId, token);
        entidad.EstadoId = estadoId;
        entidad.FechaModificacion = AhoraUtc;
        entidad.ModificadoPorUsuarioId = seguridad.UsuarioId;
        Auditoria.Registrar(seguridad, "ProductoPresentaciones", id.ToString(), "CAMBIAR_ESTADO", $"EstadoId={estadoId}.");
        await GuardarCambiosAsync(token);
        return await ObtenerPresentacionProductoAsync(productoId, id, seguridad.EmpresaId, token);
    }

    private async Task<T> ProtegerConversionAsync<T>(ContextoConfiguracion seguridad, Func<Task<T>> operacion, CancellationToken token)
    {
        // Impide que una edición validada antes de crear una orden cambie su factor después del commit.
        var clave = seguridad.EmpresaId.ToString(System.Globalization.CultureInfo.InvariantCulture);
        await using var local = await coordinador.AdquirirAsync(clave, token);
        if (Contexto.ChangeTracker.HasChanges()) throw new ConflictoFase4Exception("El contexto contiene cambios pendientes.");
        Contexto.ChangeTracker.Clear();
        await using var transaccion = Contexto.Database.IsRelational()
            ? await Contexto.Database.BeginTransactionAsync(IsolationLevel.Serializable, token) : null;
        try
        {
            if (Contexto.Database.IsSqlServer())
                await Contexto.Database.ExecuteSqlInterpolatedAsync($"""
                    DECLARE @resultado int;
                    EXEC @resultado = sys.sp_getapplock @Resource={"Heladeria.Inventario." + clave},
                        @LockMode=N'Exclusive', @LockOwner=N'Transaction', @LockTimeout=10000;
                    IF @resultado<0 THROW 51002, 'Reserva de conversión no disponible.', 1;
                    """, token);
            var resultado = await operacion();
            if (transaccion is not null) await transaccion.CommitAsync(token);
            return resultado;
        }
        catch (Exception error)
        {
            if (transaccion is not null) await transaccion.RollbackAsync(CancellationToken.None);
            Contexto.ChangeTracker.Clear();
            if (error is Microsoft.Data.SqlClient.SqlException { Number: 51002 or 1205 or 1222 })
                throw new ConflictoFase4Exception("Otra operación está modificando el inventario/conversión. Reintente.");
            throw;
        }
    }

    private async Task ValidarComposicionAsync(long? id, GuardarProductoSolicitudBase solicitud, int empresa, CancellationToken t)
    {
        var filas = solicitud.Componentes;
        if (filas is null && id.HasValue)
            filas = await Contexto.CombosDetalle.AsNoTracking().Where(x => x.EmpresaId == empresa && x.ComboProductoId == id)
                .Select(x => new ComponenteVentaSolicitud { ProductoId = x.ProductoIncluidoId,
                    ProductoPresentacionId = x.ProductoPresentacionId, Cantidad = x.Cantidad }).ToArrayAsync(t);
        if (filas is null || filas.Length == 0) return;
        if (!solicitud.EsVendible)
            throw new ValidacionFase4Exception("Un producto con composición de venta debe ser vendible.");
        await composicion.ValidarAsync(id, filas, empresa, t);
    }

    private async Task ReemplazarComposicionAsync(Productos producto, ComponenteVentaSolicitud[]? filas, CancellationToken t)
    {
        if (filas is null)
        {
            if (await Contexto.CombosDetalle.AnyAsync(x => x.EmpresaId == producto.EmpresaId && x.ComboProductoId == producto.Id, t))
                producto.EsCombo = true;
            return;
        }
        var anteriores = await Contexto.CombosDetalle.Where(x => x.EmpresaId == producto.EmpresaId && x.ComboProductoId == producto.Id).ToArrayAsync(t);
        Contexto.CombosDetalle.RemoveRange(anteriores);
        await GuardarCambiosAsync(t); // Libera la clave natural antes de insertar la nueva composición.
        foreach (var fila in filas)
            Contexto.CombosDetalle.Add(new() { EmpresaId = producto.EmpresaId, ComboProductoId = producto.Id,
                ProductoIncluidoId = fila.ProductoId, ProductoPresentacionId = fila.ProductoPresentacionId, Cantidad = fila.Cantidad });
        producto.EsCombo = filas.Length > 0;
    }

    private async Task ValidarProductoAsync(
        GuardarProductoSolicitudBase solicitud,
        int empresaId,
        long? id,
        CancellationToken token)
    {
        await ValidarEstadoAsync(solicitud.EstadoId, token);
        var sku = NormalizarCodigo(solicitud.SKU);
        var nombre = NormalizarTexto(solicitud.Nombre);
        if (await Contexto.Productos.AnyAsync(item => item.EmpresaId == empresaId
            && (!id.HasValue || item.Id != id.Value)
            && (item.SKU == sku || item.Nombre.ToUpper() == nombre.ToUpper()), token))
            throw new ConflictoFase4Exception("Ya existe un producto con el mismo SKU o nombre.");
        if (!await Contexto.Categorias.AnyAsync(item => item.Id == solicitud.CategoriaId && item.EmpresaId == empresaId && item.Estado.Activo && EstadosOperativos.Contains(item.Estado.Nombre), token))
            throw new ValidacionFase4Exception("La categoría no existe o no está activa en la empresa.");
        if (solicitud.MarcaId.HasValue
            && !await Contexto.Marcas.AnyAsync(item => item.Id == solicitud.MarcaId && item.EmpresaId == empresaId && item.Estado.Activo && EstadosOperativos.Contains(item.Estado.Nombre), token))
            throw new ValidacionFase4Exception("La marca no existe o no está activa en la empresa.");
        if (!await Contexto.TiposProductos.AnyAsync(item => item.Id == solicitud.TipoProductoId && item.EmpresaId == empresaId && item.Estado.Activo && EstadosOperativos.Contains(item.Estado.Nombre), token))
            throw new ValidacionFase4Exception("El tipo de producto no existe o no está activo en la empresa.");
        if (!await Contexto.UnidadesMedida.AnyAsync(item => item.Id == solicitud.UnidadBaseId && item.EmpresaId == empresaId && item.Estado.Activo && EstadosOperativos.Contains(item.Estado.Nombre), token))
            throw new ValidacionFase4Exception("La unidad base no existe o no está activa en la empresa.");
        if (!await CatalogoValidoAsync(solicitud.TipoSeguimientoId, ClaseTipoSeguimiento, empresaId, token))
            throw new ValidacionFase4Exception("El tipo de seguimiento no es válido para la empresa.");
        if (!await CatalogoValidoAsync(solicitud.MetodoValuacionId, ClaseMetodoValuacion, empresaId, token))
            throw new ValidacionFase4Exception("El método de valuación no es válido para la empresa.");
        if (solicitud.Peso < 0 || solicitud.PrecioVentaReferencial < 0)
            throw new ValidacionFase4Exception("Peso y precio no pueden ser negativos.");
    }

    private Task<bool> CatalogoValidoAsync(long id, string claseCodigo, int empresaId, CancellationToken token) =>
        Contexto.Catalogos.AsNoTracking().AnyAsync(item => item.Id == id
            && (item.EmpresaId == null || item.EmpresaId == empresaId)
            && item.ClaseCatalogo.Codigo == claseCodigo
            && item.Estado.Activo
            && EstadosOperativos.Contains(item.Estado.Nombre), token);

    private async Task ValidarPresentacionProductoAsync(
        long productoId,
        GuardarProductoPresentacionSolicitudBase solicitud,
        int empresaId,
        long? id,
        CancellationToken token)
    {
        await ValidarEstadoAsync(solicitud.EstadoId, token);
        if (solicitud.FactorConversion <= 0)
            throw new ValidacionFase4Exception("El factor de conversión debe ser positivo.");
        var producto = await Contexto.Productos.AsNoTracking().SingleOrDefaultAsync(item => item.Id == productoId
            && item.EmpresaId == empresaId
            && item.Estado.Activo
            && EstadosOperativos.Contains(item.Estado.Nombre), token)
            ?? throw new ValidacionFase4Exception("El producto no existe o no está activo.");
        if (!await Contexto.Presentaciones.AnyAsync(item => item.Id == solicitud.PresentacionId && item.EmpresaId == empresaId && item.Estado.Activo && EstadosOperativos.Contains(item.Estado.Nombre), token))
            throw new ValidacionFase4Exception("La presentación no existe o no está activa en la empresa.");
        if (!await Contexto.UnidadesMedida.AnyAsync(item => item.Id == solicitud.UnidadMedidaId && item.EmpresaId == empresaId && item.Estado.Activo && EstadosOperativos.Contains(item.Estado.Nombre), token))
            throw new ValidacionFase4Exception("La unidad no existe o no está activa en la empresa.");
        if (solicitud.EsPresentacionBase
            && (solicitud.UnidadMedidaId != producto.UnidadBaseId || solicitud.FactorConversion != 1m))
            throw new ValidacionFase4Exception("La presentación base debe usar la unidad base con factor uno.");
        if (await Contexto.ProductoPresentaciones.AnyAsync(item => item.EmpresaId == empresaId
            && item.ProductoId == productoId
            && (!id.HasValue || item.Id != id.Value)
            && (item.PresentacionId == solicitud.PresentacionId
                || item.UnidadMedidaId == solicitud.UnidadMedidaId && item.FactorConversion == solicitud.FactorConversion
                || solicitud.EsPresentacionBase && item.EsPresentacionBase), token))
            throw new ConflictoFase4Exception("La presentación está duplicada, es equivalente a otra o ya existe una presentación base.");
    }

    private Task<ProductoPresentacionConsultaDto> ObtenerPresentacionProductoAsync(long productoId, long id, int empresaId, CancellationToken token) =>
        ProyectarPresentaciones(Contexto.ProductoPresentaciones.AsNoTracking()
            .Where(item => item.Id == id && item.ProductoId == productoId && item.EmpresaId == empresaId))
        .SingleAsync(token);

    private static IQueryable<ProductoConsultaDto> Proyectar(IQueryable<Productos> consulta) =>
        consulta.Select(item => new ProductoConsultaDto(
            item.Id, item.EmpresaId, item.SKU, item.Nombre, item.Descripcion,
            item.CategoriaId, item.Categorias.Nombre, item.MarcaId,
            item.Marcas == null ? null : item.Marcas.Nombre,
            item.TipoProductoId, item.TiposProductos.Nombre, item.TiposProductos.ManejaExistencia,
            item.UnidadBaseId, item.UnidadesMedida.Nombre, item.UnidadesMedida.Abreviatura,
            item.TipoSeguimientoId, item.TipoSeguimiento.Nombre,
            item.MetodoValuacionId, item.MetodoValuacion.Nombre,
            item.ControlaVencimiento, item.Modelo, item.Imagen, item.Peso,
            item.EsComprable, item.EsVendible, item.PermiteDescuento, item.EsCombo,
            item.PrecioVentaReferencial, item.EstadoId, item.Estado.Nombre,
            Convert.ToBase64String(item.VersionFila)));

    private static IQueryable<ProductoPresentacionConsultaDto> ProyectarPresentaciones(IQueryable<ProductoPresentaciones> consulta) =>
        consulta.Select(item => new ProductoPresentacionConsultaDto(
            item.Id, item.EmpresaId, item.ProductoId, item.PresentacionId,
            item.Presentaciones.Nombre, item.UnidadMedidaId, item.UnidadesMedida.Nombre,
            item.FactorConversion, item.EsPresentacionBase, item.PermiteCompra,
            item.PermiteVenta, item.PrecioVentaEstandar, item.EstadoId,
            item.Estado.Nombre, Convert.ToBase64String(item.VersionFila))
            { AbreviaturaUnidadMedida = item.UnidadesMedida.Abreviatura });

    private static void Aplicar(Productos entidad, GuardarProductoSolicitudBase solicitud)
    {
        entidad.CategoriaId = solicitud.CategoriaId;
        entidad.MarcaId = solicitud.MarcaId;
        entidad.TipoProductoId = solicitud.TipoProductoId;
        entidad.UnidadBaseId = solicitud.UnidadBaseId;
        entidad.TipoSeguimientoId = solicitud.TipoSeguimientoId;
        entidad.MetodoValuacionId = solicitud.MetodoValuacionId;
        entidad.ControlaVencimiento = solicitud.ControlaVencimiento;
        entidad.SKU = NormalizarCodigo(solicitud.SKU);
        entidad.Nombre = NormalizarTexto(solicitud.Nombre);
        entidad.Descripcion = NormalizarOpcional(solicitud.Descripcion);
        entidad.Modelo = NormalizarOpcional(solicitud.Modelo);
        entidad.Imagen = NormalizarOpcional(solicitud.Imagen);
        entidad.Peso = solicitud.Peso;
        entidad.EsComprable = solicitud.EsComprable;
        entidad.EsVendible = solicitud.EsVendible;
        entidad.PermiteDescuento = solicitud.PermiteDescuento;
        entidad.EsCombo = solicitud.EsCombo;
        entidad.PrecioVentaReferencial = solicitud.PrecioVentaReferencial;
        entidad.EstadoId = solicitud.EstadoId;
    }

    private static void Aplicar(ProductoPresentaciones entidad, GuardarProductoPresentacionSolicitudBase solicitud)
    {
        entidad.PresentacionId = solicitud.PresentacionId;
        entidad.UnidadMedidaId = solicitud.UnidadMedidaId;
        entidad.FactorConversion = solicitud.FactorConversion;
        entidad.EsPresentacionBase = solicitud.EsPresentacionBase;
        entidad.PermiteCompra = solicitud.PermiteCompra;
        entidad.PermiteVenta = solicitud.PermiteVenta;
        entidad.PrecioVentaEstandar = solicitud.PrecioVentaEstandar;
        entidad.EstadoId = solicitud.EstadoId;
    }
}
