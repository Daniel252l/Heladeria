using Heladeria.Aplicacion.Productos;
using Heladeria.Infraestructura.Inventario;
using Heladeria.Infraestructura.Persistencia;
using Heladeria.Infraestructura.Persistencia.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Heladeria.Infraestructura.Fase4;

internal sealed class ReglasComposicionVenta(HeladeriaDbContext db, string[] operativos, ServicioInventario inventario)
{
    internal static IQueryable<Productos> Terminales(HeladeriaDbContext db, int empresa, string[] operativos) =>
        db.Productos.AsNoTracking().Where(p => p.EmpresaId == empresa && !p.EsCombo
            && !p.CombosDetalleProductos.Any() && p.TiposProductos.ManejaExistencia && !p.ControlaVencimiento
            && p.Estado.Activo && operativos.Contains(p.Estado.Nombre)
            && p.TiposProductos.Estado.Activo && operativos.Contains(p.TiposProductos.Estado.Nombre)
            && p.UnidadesMedida.Estado.Activo && operativos.Contains(p.UnidadesMedida.Estado.Nombre)
            && p.TipoSeguimiento.Codigo == "NINGUNO" && p.TipoSeguimiento.ClaseCatalogo.Codigo == "TIPO_SEGUIMIENTO_PRODUCTO"
            && p.TipoSeguimiento.Estado.Activo && operativos.Contains(p.TipoSeguimiento.Estado.Nombre)
            && p.TipoSeguimiento.ClaseCatalogo.Estado.Activo && operativos.Contains(p.TipoSeguimiento.ClaseCatalogo.Estado.Nombre)
            && (p.TipoSeguimiento.EmpresaId == null || p.TipoSeguimiento.EmpresaId == empresa)
            && p.MetodoValuacion.Codigo == "PROMEDIO" && p.MetodoValuacion.ClaseCatalogo.Codigo == "METODO_VALUACION_INVENTARIO"
            && p.MetodoValuacion.Estado.Activo && operativos.Contains(p.MetodoValuacion.Estado.Nombre)
            && p.MetodoValuacion.ClaseCatalogo.Estado.Activo && operativos.Contains(p.MetodoValuacion.ClaseCatalogo.Estado.Nombre)
            && (p.MetodoValuacion.EmpresaId == null || p.MetodoValuacion.EmpresaId == empresa));

    internal async Task ValidarAsync(long? padre, IReadOnlyList<ComponenteVentaSolicitud> filas, int empresa, CancellationToken t)
    {
        if (filas.Count > 100 || filas.Any(x => x is null))
            throw new ValidacionFase4Exception("La composición admite hasta 100 componentes válidos.");
        if (filas.DistinctBy(x => (x.ProductoId, x.ProductoPresentacionId)).Count() != filas.Count)
            throw new ValidacionFase4Exception("No se puede repetir el mismo componente y presentación.");
        if (filas.Count > 0 && padre.HasValue && await db.CombosDetalle.AsNoTracking()
            .AnyAsync(x => x.EmpresaId == empresa && x.ProductoIncluidoId == padre, t))
            throw new ValidacionFase4Exception("El producto ya es componente de otra composición. No se admiten composiciones anidadas ni ciclos.");
        foreach (var fila in filas)
        {
            if (fila.ProductoId == padre) throw new ValidacionFase4Exception("Un producto no puede ser componente de sí mismo.");
            await ResolverAsync(fila.ProductoId, fila.ProductoPresentacionId, fila.Cantidad, empresa, t);
        }
    }

    internal async Task<ComponenteResuelto> ResolverAsync(long producto, long? presentacion, decimal cantidad, int empresa, CancellationToken t)
    {
        if (cantidad <= 0 || cantidad > 9999999999999.999999m || cantidad != decimal.Round(cantidad, 6))
            throw new ValidacionFase4Exception("La cantidad del componente debe ser positiva y admitir hasta seis decimales.");
        var p = await Terminales(db, empresa, operativos).Where(x => x.Id == producto)
            .Select(x => new { x.Id, x.Nombre, x.UnidadBaseId }).SingleOrDefaultAsync(t)
            ?? throw new ValidacionFase4Exception("El componente debe estar activo, pertenecer a la empresa y ser inventariable terminal, sin lotes/series/vencimiento y con costo promedio.");
        if (presentacion.HasValue && !await db.ProductoPresentaciones.AsNoTracking().AnyAsync(x =>
            x.Id == presentacion && x.ProductoId == producto && x.EmpresaId == empresa && x.FactorConversion > 0
            && x.Presentaciones.Estado.Activo && operativos.Contains(x.Presentaciones.Estado.Nombre), t))
            throw new ValidacionFase4Exception("La presentación del componente no existe o no está activa.");
        // No requiere PermiteVenta/PermiteCompra: también se consumen empaques no vendibles.
        var conversion = await inventario.ConvertirCantidadAsync(producto, presentacion, cantidad, empresa, t);
        return new(producto, p.Nombre, presentacion, cantidad, conversion.Factor, conversion.CantidadBase, p.UnidadBaseId);
    }

    internal static async Task<IReadOnlyList<ComponenteVentaConsultaDto>> ConsultarAsync(HeladeriaDbContext db, long padre, int empresa, CancellationToken t) =>
        await db.CombosDetalle.AsNoTracking().Where(x => x.EmpresaId == empresa && x.ComboProductoId == padre)
            .OrderBy(x => x.Id).Select(x => new ComponenteVentaConsultaDto(x.ProductoIncluidoId,
                x.ProductosNavigation.SKU, x.ProductosNavigation.Nombre, x.ProductoPresentacionId,
                x.ProductoPresentacion == null ? null : x.ProductoPresentacion.Presentaciones.Nombre,
                x.Cantidad, x.ProductoPresentacion == null ? 1 : x.ProductoPresentacion.FactorConversion,
                x.Cantidad * (x.ProductoPresentacion == null ? 1 : x.ProductoPresentacion.FactorConversion),
                x.ProductoPresentacion == null ? x.ProductosNavigation.UnidadesMedida.Nombre : x.ProductoPresentacion.UnidadesMedida.Nombre,
                x.ProductoPresentacion == null ? x.ProductosNavigation.UnidadesMedida.Abreviatura : x.ProductoPresentacion.UnidadesMedida.Abreviatura,
                x.ProductosNavigation.UnidadBaseId, x.ProductosNavigation.UnidadesMedida.Nombre,
                x.ProductosNavigation.UnidadesMedida.Abreviatura)).ToArrayAsync(t);
}

internal sealed record ComponenteResuelto(long ProductoId, string Nombre, long? PresentacionId,
    decimal Cantidad, decimal Factor, decimal CantidadBase, int UnidadBaseId);
