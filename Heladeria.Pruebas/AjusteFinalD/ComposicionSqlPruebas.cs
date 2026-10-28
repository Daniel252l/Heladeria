using Heladeria.Aplicacion.Configuracion;
using Heladeria.Aplicacion.Productos;
using Heladeria.Aplicacion.Ventas;
using Heladeria.Infraestructura.Fase4;
using Heladeria.Infraestructura.Inventario;
using Heladeria.Infraestructura.Persistencia;
using Heladeria.Infraestructura.Persistencia.Entidades;
using Heladeria.Infraestructura.VentasPos;
using Heladeria.Infraestructura.ReportesOperativos;
using Heladeria.Pruebas.Fase5;
using Heladeria.Pruebas.Fase9;
using Heladeria.Pruebas.Integracion.Persistencia;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Heladeria.Pruebas.AjusteFinalD;

public sealed class ComposicionSqlPruebas
{
    internal static async Task<string> ScriptAsync()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "BaseDatos", "Heladeria.sql"))) dir = dir.Parent;
        return await File.ReadAllTextAsync(Path.Combine(dir!.FullName, "BaseDatos", "Reparaciones", "20261027_AjusteD_ComposicionVenta.sql"));
    }

    [Theory, InlineData(false), InlineData(true)]
    public async Task EsquemaPrevioOParcialRepetibleYRollbackAnteIncompatibilidad(bool parcial)
    {
        await using var aislada = await BaseSqlServerAislada.CrearAsync(incluirAjusteD: false);
        await using var db = new HeladeriaDbContext(new DbContextOptionsBuilder<HeladeriaDbContext>().UseSqlServer(aislada.CadenaConexion).Options);
        var script = await ScriptAsync();
        if (parcial) await db.Database.ExecuteSqlRawAsync("ALTER TABLE CombosDetalle ADD ProductoPresentacionId bigint NULL; ALTER TABLE VentasDetalle ADD EsCompuesto int NULL;");
        if (parcial)
        {
            Assert.Equal(51110, (await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlRawAsync(script))).Number);
            Assert.Equal(0, await db.Database.SqlQuery<int>($"SELECT COUNT(*) AS Value FROM sys.columns WHERE object_id=OBJECT_ID('dbo.MovimientoDetalles') AND name='VentaDetalleId'").SingleAsync());
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE VentasDetalle ALTER COLUMN EsCompuesto bit NULL;");
        }
        await db.Database.ExecuteSqlRawAsync(script); await db.Database.ExecuteSqlRawAsync(script);
        Assert.Empty(await db.CombosDetalle.ToArrayAsync()); Assert.Empty(await db.VentasDetalle.ToArrayAsync()); Assert.Empty(await db.MovimientoDetalles.ToArrayAsync());
        Assert.Equal(6, await db.Database.SqlQuery<byte>($"SELECT scale AS Value FROM sys.columns WHERE object_id=OBJECT_ID('dbo.CombosDetalle') AND name='Cantidad'").SingleAsync());
        await db.Database.ExecuteSqlRawAsync("DROP INDEX UQ_CombosDetalle_Componente ON CombosDetalle; CREATE INDEX UQ_CombosDetalle_Componente ON CombosDetalle(ComboProductoId);");
        Assert.Equal(51110, (await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlRawAsync(script))).Number);
        await db.Database.ExecuteSqlRawAsync("DROP INDEX UQ_CombosDetalle_Componente ON CombosDetalle; ALTER TABLE CombosDetalle DROP CONSTRAINT FK_CombosDetalle_Presentacion; ALTER TABLE CombosDetalle ADD CONSTRAINT FK_CombosDetalle_Presentacion FOREIGN KEY(ProductoPresentacionId) REFERENCES Productos(Id);");
        Assert.Equal(51110, (await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlRawAsync(script))).Number);
    }

    [Theory, InlineData(false), InlineData(true)]
    public async Task CompuestoIntegralYConcurrenciaSqlConCoordinadoresIndependientes(bool mismaOperacion)
    {
        await using var aislada = await BaseSqlServerAislada.CrearAsync();
        var opciones = new DbContextOptionsBuilder<HeladeriaDbContext>().UseSqlServer(aislada.CadenaConexion).Options;
        await using var db = new HeladeriaDbContext(opciones);
        var (s, _, _, _) = await CarteraSqlPruebas.PrepararAsync(db, false);
        foreach (var previa in await db.Ventas.ToArrayAsync()) previa.FechaCompletada = DateTime.UtcNow.AddYears(-1);
        await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        var (servicio, productos, _) = Servicios(db);
        var activo = await db.Estados.Where(x => x.Nombre == "Activo").Select(x => x.Id).SingleAsync();
        var categoria = new Categorias { EmpresaId = s.EmpresaId, Codigo = "D", Nombre = "Prueba D", Nivel = 1, EstadoId = activo, FechaCreacion = DateTime.UtcNow, CreadoPorUsuarioId = s.UsuarioId };
        var tipo = new TiposProductos { EmpresaId = s.EmpresaId, Codigo = "INVENTARIO-D", Nombre = "Inventario", ManejaExistencia = true, EstadoId = activo, FechaCreacion = DateTime.UtcNow, CreadoPorUsuarioId = s.UsuarioId };
        var ml = new UnidadesMedida { EmpresaId = s.EmpresaId, Codigo = "ML-D", Nombre = "Mililitro", Abreviatura = "ml", PermiteDecimales = true, CantidadDecimales = 6, EstadoId = activo, FechaCreacion = DateTime.UtcNow, CreadoPorUsuarioId = s.UsuarioId };
        var un = new UnidadesMedida { EmpresaId = s.EmpresaId, Codigo = "UN-D", Nombre = "Unidad", Abreviatura = "un", EstadoId = activo, FechaCreacion = DateTime.UtcNow, CreadoPorUsuarioId = s.UsuarioId };
        var bola = new Presentaciones { EmpresaId = s.EmpresaId, Codigo = "BOLA-D", Nombre = "Bola", EstadoId = activo, FechaCreacion = DateTime.UtcNow, CreadoPorUsuarioId = s.UsuarioId };
        db.AddRange(categoria, tipo, ml, un, bola); await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        var seguimiento = await db.Catalogos.Where(x => x.Codigo == "NINGUNO").Select(x => x.Id).SingleAsync();
        var metodo = await db.Catalogos.Where(x => x.Codigo == "PROMEDIO").Select(x => x.Id).SingleAsync();
        CrearProductoSolicitud Solicitud(string nombre, int unidad, ComponenteVentaSolicitud[]? componentes = null) => new() {
            SKU = nombre, Nombre = nombre, CategoriaId = categoria.Id, TipoProductoId = tipo.Id, UnidadBaseId = unidad,
            TipoSeguimientoId = seguimiento, MetodoValuacionId = metodo, EstadoId = activo, EsVendible = componentes is not null,
            PermiteDescuento = true, PrecioVentaReferencial = 25, Componentes = componentes };
        var helado = await productos.CrearAsync(Solicitud("HEL-VAI", ml.Id), s, default);
        var pp = await productos.CrearPresentacionAsync(helado.Id, new() { PresentacionId = bola.Id, UnidadMedidaId = un.Id,
            FactorConversion = 60, EstadoId = activo }, s, default);
        var componentes = new List<ComponenteVentaSolicitud> { new() { ProductoId = helado.Id, ProductoPresentacionId = pp.Id, Cantidad = 2 } };
        var costos = new Dictionary<long, decimal> { [helado.Id] = .025m };
        foreach (var (nombre, costo) in new[] { ("COCA-LATA",4m),("VASO-12",.5m),("CUCHARA",.1m),("SERVILLETA",.05m) }) {
            var p = await productos.CrearAsync(Solicitud(nombre, un.Id), s, default);
            componentes.Add(new() { ProductoId = p.Id, Cantidad = 1 }); costos[p.Id] = costo;
        }
        var padre = await productos.CrearAsync(Solicitud("VACA-LOCA", un.Id, componentes.ToArray()), s, default);
        var bodega = await db.Bodegas.Select(x => x.Id).SingleAsync(); var ubicacion = await db.Ubicaciones.Select(x => x.Id).SingleAsync();
        foreach (var (id, costo) in costos) db.Existencias.Add(new() { EmpresaId = s.EmpresaId, ProductoId = id,
            BodegaId = bodega, UbicacionId = ubicacion, Cantidad = id == helado.Id ? 240 : 2, CostoPromedio = costo });
        await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        async Task<VentaConsultaDto> Confirmar() {
            var v = await servicio.CrearAsync(new() { SucursalId = s.SucursalId, BodegaId = bodega, UbicacionId = ubicacion,
                MonedaId = await db.Monedas.Select(x => x.Id).SingleAsync(), Detalles = [new() { ProductoId = padre.Id, Cantidad = 2, PorcentajeDescuento = 100 }] }, s, default);
            return await servicio.ConfirmarAsync(v.Encabezado.Id, new() { VersionFila = v.Encabezado.VersionFila }, s, default);
        }
        var a = await Confirmar(); var b = mismaOperacion ? a : await Confirmar(); var uuid = Guid.NewGuid();
        var empezar = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<(VentaConsultaDto? Venta, Exception? Error)> Completar(VentaConsultaDto v, Guid clave) {
            await empezar.Task;
            await using var otro = new HeladeriaDbContext(opciones); var (ventas, _, _) = Servicios(otro);
            try { return (await ventas.CompletarAsync(v.Encabezado.Id, new() { VersionFila = v.Encabezado.VersionFila, ClaveOperacion = clave }, s, default), null); }
            catch (Exception ex) { return (null, ex); }
        }
        var tareas = new[] { Completar(a, uuid), Completar(b, mismaOperacion ? uuid : Guid.NewGuid()) }; empezar.SetResult();
        var resultados = await Task.WhenAll(tareas);
        if (mismaOperacion) Assert.All(resultados, x => Assert.Null(x.Error));
        else Assert.IsType<StockVentaInsuficienteException>(Assert.Single(resultados, x => x.Error is not null).Error);
        var final = resultados.First(x => x.Venta is not null).Venta!;
        Assert.Equal(15.3m, Assert.Single(final.Detalles).CostoTotal); Assert.Equal(padre.Id, final.Detalles[0].ProductoId);
        Assert.All(await db.Existencias.AsNoTracking().ToArrayAsync(), x => Assert.Equal(0, x.Cantidad));
        Assert.Equal(5, await db.Kardex.CountAsync()); Assert.Equal(5, final.Movimientos.Count);
        var reportes = new ServicioReportes(db, EscenarioFase5.Opciones, TimeProvider.System);
        var reporte = await reportes.ProductosAsync(new() { ProductoId = padre.Id, Estado = "COMPLETADA" }, s, default);
        Assert.Equal(padre.Id, Assert.Single(reporte.Elementos).ProductoId); Assert.Equal(15.3m, reporte.Elementos[0].Costo);
        Assert.Equal(-15.3m, reporte.Elementos[0].MargenBruto); // Venta gratuita para ejercitar anulación sin pago/CxC.
        var diario = await reportes.DiarioAsync(new(), s, default);
        Assert.Equal(15.3m, diario.VentasTotales.Single(x => x.Estado == "COMPLETADA").Costo);
        // Modificar cantidades actuales no cambia ni la descarga aplicada ni su reversión.
        foreach (var c in await db.CombosDetalle.ToArrayAsync()) c.Cantidad *= 2;
        await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        await servicio.AnularAsync(final.Encabezado.Id, new() { VersionFila = final.Encabezado.VersionFila, Motivo = "Prueba aislada" }, s, default);
        foreach (var saldo in await db.Existencias.AsNoTracking().ToArrayAsync()) Assert.Equal(saldo.ProductoId == helado.Id ? 240 : 2, saldo.Cantidad);
        Assert.Equal(10, await db.Kardex.CountAsync());
        await db.Database.ExecuteSqlRawAsync(await ScriptAsync());
    }

    private static (ServicioVentas, ServicioProductos, ServicioInventario) Servicios(HeladeriaDbContext db)
    {
        var coord = new CoordinadorAjustesInventario(); var auditoria = new AuditoriaFase4(db, TimeProvider.System);
        var inventario = new ServicioInventario(db, auditoria, EscenarioFase5.Opciones, TimeProvider.System, coord);
        return (new(db, EscenarioFase5.Opciones, TimeProvider.System, coord, inventario),
            new(db, auditoria, EscenarioFase5.Opciones, TimeProvider.System, coord), inventario);
    }
}
