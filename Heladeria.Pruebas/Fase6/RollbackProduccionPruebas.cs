using Heladeria.Aplicacion.Produccion;
using Heladeria.Infraestructura.Inventario;
using Heladeria.Infraestructura.Persistencia;
using Heladeria.Infraestructura.Persistencia.Entidades;
using Heladeria.Infraestructura.Produccion;
using Heladeria.Infraestructura.Seguridad.Bootstrap;
using Heladeria.Pruebas.Fase4;
using Heladeria.Pruebas.Fase5;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Heladeria.Pruebas.Fase6;

public sealed class RollbackProduccionPruebas
{
    [Fact]
    public async Task FalloDespuesDelConsumoRevierteStockMovimientosCostosYCorrelativos()
    {
        await using var conexion = new SqliteConnection("Data Source=:memory:");
        await conexion.OpenAsync();
        var contexto = new ContextoSqlitePruebas(
            new DbContextOptionsBuilder<HeladeriaDbContext>().UseSqlite(conexion).Options);
        await contexto.Database.EnsureCreatedAsync();
        var coordinador = new CoordinadorAjustesInventario();
        await using var fase5 = new EscenarioFase5(
            await EscenarioFase4.CrearConContextoAsync(contexto, coordinador), coordinador);
        await fase5.SemillarAsync();
        contexto.Existencias.Add(new Existencias
        {
            EmpresaId = 1, ProductoId = 2, BodegaId = 1, UbicacionId = 1,
            Cantidad = 100m, CantidadReservada = 0m, CostoPromedio = 0.02m,
            VersionFila = []
        });
        await contexto.SaveChangesAsync();
        var preparador = new PreparadorFase6(contexto, EscenarioFase5.Opciones,
            TimeProvider.System, coordinador, fase5.Fase4.Inventario);
        await preparador.EjecutarAsync(default);
        var recetas = new ServicioRecetas(contexto, EscenarioFase5.Opciones,
            TimeProvider.System, coordinador, fase5.Fase4.Inventario);
        var produccion = new ServicioProduccion(contexto, EscenarioFase5.Opciones,
            TimeProvider.System, coordinador, fase5.Fase4.Inventario);
        var receta = await recetas.CrearAsync(EscenarioFase6.SolicitudReceta(),
            EscenarioFase6.Seguridad, default);
        var orden = await produccion.CrearAsync(EscenarioFase6.SolicitudOrden(receta.Encabezado.Id),
            EscenarioFase6.Seguridad, default);
        orden = await produccion.ConfirmarAsync(orden.Encabezado.Id,
            new CambiarEstadoProduccionSolicitud { VersionFila = orden.Encabezado.VersionFila },
            EscenarioFase6.Seguridad, default);
        var bitacorasAntes = await contexto.Bitacoras.CountAsync();
        var conceptoEntrada = await contexto.ConceptosMovimientos
            .SingleAsync(item => item.Codigo == "PRODUCCION-ENTRADA");
        contexto.ConceptosMovimientos.Remove(conceptoEntrada);
        await contexto.SaveChangesAsync();
        contexto.ChangeTracker.Clear();

        await Assert.ThrowsAsync<ConflictoProduccionException>(() => produccion.FinalizarAsync(
            orden.Encabezado.Id,
            new FinalizarProduccionSolicitud
            {
                ClaveOperacion = Guid.NewGuid(), UbicacionId = 1,
                CantidadProducida = 1000m, VersionFila = orden.Encabezado.VersionFila
            }, EscenarioFase6.Seguridad, default));

        Assert.Equal(5000m, (await contexto.Existencias.AsNoTracking()
            .SingleAsync(item => item.ProductoId == 1 && item.BodegaId == 1)).Cantidad);
        Assert.Equal(100m, (await contexto.Existencias.AsNoTracking()
            .SingleAsync(item => item.ProductoId == 2 && item.BodegaId == 1)).Cantidad);
        Assert.Empty(contexto.Movimientos.Where(item => item.ModuloOrigen == "PRODUCCION"));
        Assert.Empty(contexto.MovimientoDetalles.Where(item => item.Movimientos.ModuloOrigen == "PRODUCCION"));
        Assert.Empty(contexto.Kardex);
        Assert.Empty(contexto.ConsumosProduccion);
        Assert.Empty(contexto.ProduccionTerminada);
        Assert.Empty(contexto.CostosProductos);
        Assert.Empty(contexto.HistoricoCostosProductos);
        Assert.Equal(bitacorasAntes, await contexto.Bitacoras.CountAsync());
        Assert.Equal(0, (await contexto.CorrelativosDocumentos
            .SingleAsync(item => item.SerieDocumento.Serie == "PC")).UltimoNumero);
        Assert.Equal(0, (await contexto.CorrelativosDocumentos
            .SingleAsync(item => item.SerieDocumento.Serie == "PT")).UltimoNumero);
        Assert.Equal("CONFIRMADA", (await produccion.ObtenerAsync(
            orden.Encabezado.Id, EscenarioFase6.Seguridad, default)).Encabezado.Estado);
    }
}
