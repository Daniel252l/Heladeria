using Heladeria.Aplicacion.Productos;
using Heladeria.Aplicacion.Ventas;
using Heladeria.Infraestructura.Fase4;
using Heladeria.Infraestructura.Inventario;
using Heladeria.Infraestructura.Persistencia;
using Heladeria.Infraestructura.Persistencia.Entidades;
using Heladeria.Pruebas.Fase5;
using Heladeria.Pruebas.Fase7;
using Heladeria.Pruebas.Fase9;
using Microsoft.EntityFrameworkCore;

namespace Heladeria.Pruebas.AjusteFinalD;

internal sealed class EscenarioComposicion(EscenarioCartera cartera, ServicioProductos productos) : IAsyncDisposable
{
    internal EscenarioCartera Cartera { get; } = cartera;
    internal HeladeriaDbContext Db => Cartera.Db;
    internal EscenarioFase7 Ventas => Cartera.Caja.Ventas;
    internal ServicioProductos Productos { get; } = productos;
    internal long Padre { get; private set; }
    internal int Unidad { get; private set; }
    internal long Bola { get; private set; }
    internal long[] Insumos { get; private set; } = [];
    internal static Heladeria.Aplicacion.Configuracion.ContextoConfiguracion S => EscenarioCartera.S;
    internal static async Task<EscenarioComposicion> CrearAsync(bool componentes = true)
    {
        var cartera = await EscenarioCartera.CrearAsync();
        var db = cartera.Db;
        var productos = new ServicioProductos(db, new AuditoriaFase4(db, TimeProvider.System), EscenarioFase5.Opciones,
            TimeProvider.System, new CoordinadorAjustesInventario());
        var e = new EscenarioComposicion(cartera, productos);
        var unidad = new UnidadesMedida { EmpresaId = 1, Codigo = "UN-D", Nombre = "Unidad", Abreviatura = "un",
            EstadoId = 1, FechaCreacion = DateTime.UtcNow, CreadoPorUsuarioId = 1 };
        var presentacion = new Presentaciones { EmpresaId = 1, Codigo = "BOLA-D", Nombre = "Bola", EstadoId = 1,
            FechaCreacion = DateTime.UtcNow, CreadoPorUsuarioId = 1 };
        db.AddRange(unidad, presentacion); await db.SaveChangesAsync(); e.Unidad = unidad.Id;
        var bola = new ProductoPresentaciones { EmpresaId = 1, ProductoId = 1, PresentacionId = presentacion.Id,
            UnidadMedidaId = unidad.Id, FactorConversion = 60, PermiteCompra = false, PermiteVenta = false,
            EstadoId = 1, FechaCreacion = DateTime.UtcNow, CreadoPorUsuarioId = 1 };
        db.Add(bola); (await db.Existencias.SingleAsync(x => x.Id == 1)).CostoPromedio = .025m;
        await db.SaveChangesAsync(); e.Bola = bola.Id; db.ChangeTracker.Clear();
        var ids = new List<long>();
        foreach (var (nombre, costo) in new[] { ("COCA-LATA", 4m), ("VASO-12", .5m), ("CUCHARA", .1m), ("SERVILLETA", .05m) })
        {
            var p = await productos.CrearAsync(e.Solicitud(nombre, null, vendible: false), S, default);
            ids.Add(p.Id);
            db.Existencias.Add(new() { EmpresaId = 1, ProductoId = p.Id, BodegaId = 1, UbicacionId = 1, Cantidad = 10, CostoPromedio = costo });
            await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        }
        e.Insumos = ids.ToArray();
        e.Padre = (await productos.CrearAsync(e.Solicitud("VACA-LOCA", componentes ? e.Componentes() : null), S, default)).Id;
        await cartera.Caja.AbrirAsync(0); db.ChangeTracker.Clear();
        return e;
    }
    internal ComponenteVentaSolicitud[] Componentes() => [new() { ProductoId = 1, ProductoPresentacionId = Bola, Cantidad = 2 },
        .. Insumos.Select(id => new ComponenteVentaSolicitud { ProductoId = id, Cantidad = 1 })];
    internal CrearProductoSolicitud Solicitud(string nombre, ComponenteVentaSolicitud[]? componentes, bool vendible = true) => new() {
        SKU = nombre, Nombre = nombre, CategoriaId = 1, TipoProductoId = 1, UnidadBaseId = Unidad,
        TipoSeguimientoId = 1, MetodoValuacionId = 2, EsVendible = vendible, PrecioVentaReferencial = vendible ? 25 : 999,
        PermiteDescuento = true, EstadoId = 1, Componentes = componentes };
    internal async Task<ProductoConsultaDto> EditarAsync(ComponenteVentaSolicitud[]? componentes, long? id = null, string? version = null)
    {
        var p = await Productos.ObtenerAsync(id ?? Padre, S, default);
        return await Productos.ActualizarAsync(p.Id, new() { VersionFila = version ?? p.VersionFila, SKU = p.SKU, Nombre = p.Nombre,
            CategoriaId = p.CategoriaId, TipoProductoId = p.TipoProductoId, UnidadBaseId = p.UnidadBaseId,
            TipoSeguimientoId = p.TipoSeguimientoId, MetodoValuacionId = p.MetodoValuacionId, EsVendible = p.EsVendible,
            PrecioVentaReferencial = p.PrecioVentaReferencial, PermiteDescuento = true, EstadoId = p.EstadoId,
            Componentes = componentes }, S, default);
    }
    internal Task<VentaConsultaDto> ConfirmarAsync(decimal cantidad = 2, bool gratis = false) => Ventas.ConfirmarAsync(
        EscenarioFase7.Solicitud(cantidad, Padre, null, gratis ? 100 : 0));
    internal async Task<decimal> StockAsync(long producto) => await Db.Existencias.AsNoTracking()
        .Where(x => x.ProductoId == producto && x.BodegaId == 1).Select(x => x.Cantidad).SingleAsync();
    public ValueTask DisposeAsync() => Cartera.DisposeAsync();
}
