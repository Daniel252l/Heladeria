using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using Heladeria.Aplicacion.Compras;
using Heladeria.Aplicacion.Configuracion;
using Heladeria.Aplicacion.Seguridad.Configuracion;
using Heladeria.Infraestructura.Inventario;
using Heladeria.Infraestructura.Persistencia;
using Heladeria.Infraestructura.Persistencia.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Heladeria.Infraestructura.Compras;

public sealed class ServicioProveedores(HeladeriaDbContext contexto, IOptions<OpcionesSeguridad> opciones,
    TimeProvider reloj, CoordinadorAjustesInventario coordinador)
    : ServicioComprasBase(contexto, opciones, reloj, coordinador), IServicioProveedores
{
    private static readonly Expression<Func<Proveedores, ProveedorConsultaDto>> Proyectar = item => new(
        item.Id, item.EmpresaId, item.CodigoProveedor, item.NombreProveedor, item.Nit, item.Telefono, item.Correo,
        item.Direccion, item.DiasCredito, item.LimiteCredito, item.MonedaId, item.EstadoId, item.Estado.Nombre);
    private static readonly Expression<Func<ProveedorProductos, ProveedorProductoDto>> ProyectarProducto = item => new(
        item.Id, item.ProveedorId, item.ProductoId, item.Producto.Nombre, item.CodigoProveedor, item.PrecioUltimaCompra,
        item.EsPreferido, item.EstadoId, item.Estado.Nombre);

    public async Task<RespuestaPaginada<ProveedorConsultaDto>> ListarAsync(FiltroProveedoresSolicitud filtro,
        ContextoConfiguracion seguridad, CancellationToken token)
    {
        Validar(filtro);
        await ValidarSucursalAsync(seguridad, token);
        var query = Contexto.Proveedores.AsNoTracking().Where(item => item.EmpresaId == seguridad.EmpresaId);
        if (Opcional(filtro.Codigo)?.ToUpperInvariant() is { } codigo) query = query.Where(item => item.CodigoProveedor.ToUpper().Contains(codigo));
        if (Opcional(filtro.Nombre)?.ToUpperInvariant() is { } nombre) query = query.Where(item => item.NombreProveedor.ToUpper().Contains(nombre));
        if (Opcional(filtro.Nit)?.ToUpperInvariant() is { } nit) query = query.Where(item => item.Nit != null && item.Nit.ToUpper().Contains(nit));
        if (Opcional(filtro.Correo)?.ToUpperInvariant() is { } correo) query = query.Where(item => item.Correo != null && item.Correo.ToUpper().Contains(correo));
        if (filtro.EstadoId is { } estado) query = query.Where(item => item.EstadoId == estado);
        if (Opcional(filtro.Busqueda)?.ToUpperInvariant() is { } buscar)
            query = query.Where(item => item.CodigoProveedor.ToUpper().Contains(buscar) || item.NombreProveedor.ToUpper().Contains(buscar)
                || item.Nit != null && item.Nit.ToUpper().Contains(buscar) || item.Correo != null && item.Correo.ToUpper().Contains(buscar));
        var total = await query.CountAsync(token);
        return new(await Paginar(query.OrderBy(item => item.NombreProveedor).ThenBy(item => item.Id), filtro)
            .Select(Proyectar).ToArrayAsync(token), filtro.Pagina, filtro.TamanioPagina, total);
    }

    public async Task<ProveedorConsultaDto> ObtenerAsync(int id, ContextoConfiguracion seguridad, CancellationToken token)
    {
        await ValidarSucursalAsync(seguridad, token);
        return await Contexto.Proveedores.AsNoTracking().Where(item => item.Id == id && item.EmpresaId == seguridad.EmpresaId)
            .Select(Proyectar).SingleOrDefaultAsync(token) ?? throw new RecursoComprasNoEncontradoException("Proveedor no encontrado.");
    }

    public Task<ProveedorConsultaDto> GuardarAsync(int? id, GuardarProveedorSolicitud solicitud,
        ContextoConfiguracion seguridad, CancellationToken token) => EscribirAsync(seguridad, async () =>
    {
        Validar(solicitud);
        await ValidarEstadoAsync(solicitud.EstadoId, token);
        var codigo = Codigo(solicitud.CodigoProveedor);
        var nit = Opcional(solicitud.Nit)?.ToUpperInvariant();
        var correo = Opcional(solicitud.Correo)?.ToLowerInvariant();
        if (codigo.Length == 0 || Opcional(solicitud.NombreProveedor) is null)
            throw new ValidacionComprasException("Código y nombre son obligatorios.");
        if (correo is not null && !new EmailAddressAttribute().IsValid(correo))
            throw new ValidacionComprasException("El correo no es válido.");
        if (solicitud.LimiteCredito is { } limite) Precision(limite, 2, MaximoMonetario, "Límite de crédito");
        await ValidarMonedaAsync(solicitud.MonedaId, seguridad.EmpresaId, token);
        var entidad = id.HasValue ? await BuscarAsync(id.Value, seguridad, token) : new Proveedores { EmpresaId = seguridad.EmpresaId };
        // No existe algoritmo de NIT en Fase 3: se conserva normalización/longitud y unicidad por empresa.
        if (await Contexto.Proveedores.AnyAsync(item => item.EmpresaId == seguridad.EmpresaId && (!id.HasValue || item.Id != id)
            && (item.CodigoProveedor.ToUpper() == codigo || nit != null && item.Nit != null && item.Nit.ToUpper() == nit
                || correo != null && item.Correo != null && item.Correo.ToLower() == correo), token))
            throw new ConflictoComprasException("Ya existe un proveedor con el mismo código, NIT o correo en la empresa.");
        await ProtegerProveedorAsync(entidad, solicitud.EstadoId, token);
        entidad.CodigoProveedor = codigo; entidad.NombreProveedor = solicitud.NombreProveedor.Trim();
        entidad.Nit = nit; entidad.Telefono = Opcional(solicitud.Telefono); entidad.Correo = correo;
        entidad.Direccion = Opcional(solicitud.Direccion); entidad.DiasCredito = solicitud.DiasCredito;
        entidad.LimiteCredito = solicitud.LimiteCredito; entidad.MonedaId = solicitud.MonedaId; entidad.EstadoId = solicitud.EstadoId;
        if (!id.HasValue) Contexto.Proveedores.Add(entidad);
        await Contexto.SaveChangesAsync(token);
        Auditar(seguridad, "Proveedores", entidad.Id, id.HasValue ? "PROVEEDOR_ACTUALIZADO" : "PROVEEDOR_CREADO", "Mantenimiento de proveedor.", "PROVEEDORES");
        return await ObtenerAsync(entidad.Id, seguridad, token);
    }, token);

    public Task<ProveedorConsultaDto> CambiarEstadoAsync(int id, int estadoId, ContextoConfiguracion seguridad,
        CancellationToken token) => EscribirAsync(seguridad, async () =>
    {
        var entidad = await BuscarAsync(id, seguridad, token);
        await ValidarEstadoAsync(estadoId, token);
        await ProtegerProveedorAsync(entidad, estadoId, token);
        entidad.EstadoId = estadoId;
        await Contexto.SaveChangesAsync(token);
        Auditar(seguridad, "Proveedores", id, "PROVEEDOR_ESTADO_CAMBIADO", "Cambio de estado de proveedor.", "PROVEEDORES");
        return await ObtenerAsync(id, seguridad, token);
    }, token);

    public async Task<RespuestaPaginada<ProveedorProductoDto>> ListarProductosAsync(int id, FiltroConEstadoSolicitud filtro,
        ContextoConfiguracion seguridad, CancellationToken token)
    {
        await ObtenerAsync(id, seguridad, token);
        Validar(filtro);
        var query = Contexto.ProveedorProductos.AsNoTracking().Where(item => item.ProveedorId == id
            && item.Producto.EmpresaId == seguridad.EmpresaId);
        if (filtro.EstadoId is { } estado) query = query.Where(item => item.EstadoId == estado);
        if (Opcional(filtro.Busqueda)?.ToUpperInvariant() is { } buscar) query = query.Where(item => item.Producto.Nombre.ToUpper().Contains(buscar)
            || item.CodigoProveedor != null && item.CodigoProveedor.ToUpper().Contains(buscar));
        var total = await query.CountAsync(token);
        return new(await Paginar(query.OrderBy(item => item.Producto.Nombre).ThenBy(item => item.Id), filtro)
            .Select(ProyectarProducto).ToArrayAsync(token), filtro.Pagina, filtro.TamanioPagina, total);
    }

    public Task<ProveedorProductoDto> GuardarProductoAsync(int id, long? relacionId, GuardarProveedorProductoSolicitud solicitud,
        ContextoConfiguracion seguridad, CancellationToken token) => EscribirAsync(seguridad, async () =>
    {
        await BuscarAsync(id, seguridad, token);
        Validar(solicitud);
        await ValidarEstadoAsync(solicitud.EstadoId, token);
        if (solicitud.PrecioUltimaCompra is { } precio) Precision(precio, 2, MaximoMonetario, "Precio referencial");
        if (!await Contexto.Productos.AnyAsync(item => item.Id == solicitud.ProductoId && item.EmpresaId == seguridad.EmpresaId
            && item.EsComprable && item.Estado.Activo && Operativos.Contains(item.Estado.Nombre), token))
            throw new ValidacionComprasException("El producto no es comprable o no pertenece a la empresa.");
        var entidad = relacionId.HasValue ? await BuscarRelacionAsync(id, relacionId.Value, seguridad, token)
            : new ProveedorProductos { ProveedorId = id };
        if (await Contexto.ProveedorProductos.AnyAsync(item => item.ProveedorId == id && item.ProductoId == solicitud.ProductoId
            && (!relacionId.HasValue || item.Id != relacionId), token))
            throw new ConflictoComprasException("El proveedor ya tiene asociado ese producto.");
        entidad.ProductoId = solicitud.ProductoId; entidad.CodigoProveedor = Opcional(solicitud.CodigoProveedor);
        entidad.PrecioUltimaCompra = solicitud.PrecioUltimaCompra; entidad.EsPreferido = solicitud.EsPreferido; entidad.EstadoId = solicitud.EstadoId;
        if (!relacionId.HasValue) Contexto.ProveedorProductos.Add(entidad);
        await Contexto.SaveChangesAsync(token);
        Auditar(seguridad, "ProveedorProductos", entidad.Id, "PROVEEDOR_PRODUCTO_GUARDADO", "Asociación de producto.", "PROVEEDORES");
        return await Contexto.ProveedorProductos.AsNoTracking().Where(item => item.Id == entidad.Id).Select(ProyectarProducto).SingleAsync(token);
    }, token);

    public Task<ProveedorProductoDto> CambiarEstadoProductoAsync(int id, long relacionId, int estadoId,
        ContextoConfiguracion seguridad, CancellationToken token) => EscribirAsync(seguridad, async () =>
    {
        var entidad = await BuscarRelacionAsync(id, relacionId, seguridad, token);
        await ValidarEstadoAsync(estadoId, token); entidad.EstadoId = estadoId;
        await Contexto.SaveChangesAsync(token);
        Auditar(seguridad, "ProveedorProductos", relacionId, "PROVEEDOR_PRODUCTO_ESTADO", "Cambio de estado de asociación.", "PROVEEDORES");
        return await Contexto.ProveedorProductos.AsNoTracking().Where(item => item.Id == relacionId).Select(ProyectarProducto).SingleAsync(token);
    }, token);

    private async Task<Proveedores> BuscarAsync(int id, ContextoConfiguracion seguridad, CancellationToken token) =>
        await Contexto.Proveedores.SingleOrDefaultAsync(item => item.Id == id && item.EmpresaId == seguridad.EmpresaId, token)
        ?? throw new RecursoComprasNoEncontradoException("Proveedor no encontrado.");
    private async Task<ProveedorProductos> BuscarRelacionAsync(int proveedorId, long id, ContextoConfiguracion seguridad, CancellationToken token) =>
        await Contexto.ProveedorProductos.SingleOrDefaultAsync(item => item.Id == id && item.ProveedorId == proveedorId
            && item.Proveedor.EmpresaId == seguridad.EmpresaId && item.Producto.EmpresaId == seguridad.EmpresaId, token)
        ?? throw new RecursoComprasNoEncontradoException("Asociación no encontrada.");
    private async Task ValidarMonedaAsync(int? id, int empresaId, CancellationToken token)
    {
        if (id.HasValue && !await Contexto.Monedas.AnyAsync(item => item.Id == id && item.EmpresaId == empresaId
            && item.Estado.Activo && Operativos.Contains(item.Estado.Nombre), token))
            throw new ValidacionComprasException("La moneda no pertenece a la empresa o no está activa.");
    }
    private async Task ProtegerProveedorAsync(Proveedores entidad, int estado, CancellationToken token)
    {
        if (entidad.Id == 0 || entidad.EstadoId == estado || await Contexto.Estados.AnyAsync(item => item.Id == estado
            && item.Activo && Operativos.Contains(item.Nombre), token)) return;
        if (await Contexto.OrdenesCompra.AnyAsync(item => item.ProveedorId == entidad.Id && !item.EstadosFlujo.EsFinal, token))
            throw new ConflictoComprasException("El proveedor tiene órdenes pendientes; no puede inactivarse.");
    }
}
