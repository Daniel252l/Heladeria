using Heladeria.Aplicacion.Ventas;
using Heladeria.Infraestructura.Inventario;
using Heladeria.Infraestructura.Persistencia;
using Heladeria.Pruebas.Fase4;
using Heladeria.Pruebas.Fase5;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Heladeria.Pruebas.Fase7;

public sealed class VentasRollbackPruebas
{
    [Fact]
    public async Task FalloTrasPrimeraSalidaRevierteDocumentoCostosStockKardexCorrelativos()
    {
        await using var conexion = new SqliteConnection("Data Source=:memory:");
        await conexion.OpenAsync();
        var db = new ContextoSqlitePruebas(new DbContextOptionsBuilder<HeladeriaDbContext>().UseSqlite(conexion).Options);
        await db.Database.EnsureCreatedAsync();
        var coordinador = new CoordinadorAjustesInventario();
        await using var e = new EscenarioFase7(new EscenarioFase5(await EscenarioFase4.CrearConContextoAsync(db, coordinador), coordinador));
        await e.SemillarAsync();
        db.Existencias.Add(new() { EmpresaId = 1, ProductoId = 2, BodegaId = 1, UbicacionId = 1,
            Cantidad = 100, CostoPromedio = 1, VersionFila = [] });
        await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        var v = await e.ConfirmarAsync(new() { SucursalId = 1, BodegaId = 1, UbicacionId = 1, MonedaId = 1,
            Detalles = [new() { ProductoId = 2, Cantidad = 1 }, new() { ProductoId = 1, ProductoPresentacionId = 1, Cantidad = 2 }] });
        // Falla real de persistencia en el segundo producto, después de haber escrito la primera salida.
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TRIGGER FalloSegundaSalida BEFORE INSERT ON MovimientoDetalles
            WHEN NEW.ProductoId = 2 BEGIN SELECT RAISE(ABORT, 'Fallo deliberado de pruebas'); END;
            """);
        await Assert.ThrowsAsync<DbUpdateException>(() => e.CompletarAsync(v));
        Assert.Empty(db.Movimientos); Assert.Empty(db.MovimientoDetalles); Assert.Empty(db.Kardex);
        Assert.Empty(db.CostosProductos); Assert.Empty(db.HistoricoCostosProductos);
        Assert.Equal(5000, (await db.Existencias.AsNoTracking().SingleAsync(x => x.ProductoId == 1 && x.BodegaId == 1)).Cantidad);
        Assert.Equal(100, (await db.Existencias.AsNoTracking().SingleAsync(x => x.ProductoId == 2)).Cantidad);
        Assert.Equal(0, (await db.CorrelativosDocumentos.SingleAsync(x => x.SerieDocumento.Serie == "VS")).UltimoNumero);
        var actual = await e.Ventas.ObtenerAsync(v.Encabezado.Id, EscenarioFase7.Seguridad, default);
        Assert.Equal("CONFIRMADA", actual.Encabezado.Estado); Assert.Null(actual.Encabezado.ClaveOperacion);
        Assert.All(actual.Detalles, x => Assert.Null(x.CostoTotal));
        await db.Database.ExecuteSqlRawAsync("DROP TRIGGER FalloSegundaSalida;");
        var cerrada = await e.CompletarAsync(actual);
        Assert.Equal("COMPLETADA", cerrada.Encabezado.Estado); Assert.Equal(2, cerrada.Movimientos.Count);
    }

    [Fact]
    public async Task FalloEnReversionRevierteInclusoCostoPromedioHistoricosYAnulacion()
    {
        await using var conexion = new SqliteConnection("Data Source=:memory:"); await conexion.OpenAsync();
        var db = new ContextoSqlitePruebas(new DbContextOptionsBuilder<HeladeriaDbContext>().UseSqlite(conexion).Options);
        await db.Database.EnsureCreatedAsync();
        var coordinador = new CoordinadorAjustesInventario();
        await using var e = new EscenarioFase7(new EscenarioFase5(await EscenarioFase4.CrearConContextoAsync(db, coordinador), coordinador));
        await e.SemillarAsync();
        var v = await e.HistoricaSinCobroAsync(await e.ConfirmarAsync());
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TRIGGER FalloAuditoria BEFORE INSERT ON Bitacoras
            WHEN NEW.Accion = 'VENTA_ANULADA' BEGIN SELECT RAISE(ABORT, 'Fallo deliberado'); END;
            """);
        await Assert.ThrowsAsync<DbUpdateException>(() => e.AnularAsync(v));
        var actual = await e.Ventas.ObtenerAsync(v.Encabezado.Id, EscenarioFase7.Seguridad, default);
        Assert.Equal("COMPLETADA", actual.Encabezado.Estado); Assert.Single(actual.Movimientos);
        Assert.Equal(3000, (await db.Existencias.AsNoTracking().SingleAsync(x => x.Id == 1)).Cantidad);
        Assert.Single(db.Kardex); Assert.Empty(db.CostosProductos); Assert.Empty(db.HistoricoCostosProductos);
        Assert.Equal(0, (await db.CorrelativosDocumentos.SingleAsync(x => x.SerieDocumento.Serie == "VA")).UltimoNumero);
        await db.Database.ExecuteSqlRawAsync("DROP TRIGGER FalloAuditoria;");
        Assert.Equal("ANULADA", (await e.AnularAsync(actual)).Encabezado.Estado);
    }
}
