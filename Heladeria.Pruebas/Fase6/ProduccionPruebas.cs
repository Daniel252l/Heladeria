using Heladeria.Aplicacion.Produccion;
using Heladeria.Infraestructura.Inventario;
using Microsoft.EntityFrameworkCore;

namespace Heladeria.Pruebas.Fase6;

public sealed class ProduccionPruebas
{
    [Fact]
    public async Task CrearBorradorEscalaIngredientesYReservaCorrelativo()
    {
        await using var e = await EscenarioFase6.CrearAsync();
        var receta = await e.CrearRecetaAsync();

        var orden = await e.CrearOrdenAsync(receta.Encabezado.Id);

        Assert.Equal("OP-0000000001", orden.Encabezado.NumeroOrden);
        Assert.Equal("BORRADOR", orden.Encabezado.Estado);
        Assert.Equal(1000m, orden.Encabezado.CantidadPlanificadaBase);
        Assert.Equal("ml", orden.Encabezado.AbreviaturaUnidadBase);
        Assert.Null(orden.Encabezado.Presentacion);
        var detalle = Assert.Single(orden.Detalles);
        Assert.Equal(0.5m, detalle.CantidadPlanificada);
        Assert.Equal(1000m, detalle.FactorConversion);
        Assert.Equal(500m, detalle.CantidadPlanificadaBase);
        Assert.Equal("ml", detalle.AbreviaturaUnidadBase);
        Assert.Empty(orden.MovimientoIds);
        Assert.Equal(5000m, (await e.Contexto.Existencias.SingleAsync(item => item.Id == 1)).Cantidad);
    }

    [Fact]
    public async Task ConfirmarNoAfectaInventarioYAnularEsValido()
    {
        await using var e = await EscenarioFase6.CrearAsync();
        var receta = await e.CrearRecetaAsync();
        var orden = await e.CrearOrdenAsync(receta.Encabezado.Id);

        var confirmada = await e.ConfirmarAsync(orden.Encabezado.Id);
        Assert.Equal("CONFIRMADA", confirmada.Encabezado.Estado);
        Assert.Empty(confirmada.MovimientoIds);
        var anulada = await e.Produccion.AnularAsync(orden.Encabezado.Id,
            new CambiarEstadoProduccionSolicitud { VersionFila = confirmada.Encabezado.VersionFila },
            EscenarioFase6.Seguridad, default);

        Assert.Equal("ANULADA", anulada.Encabezado.Estado);
        Assert.Empty(anulada.MovimientoIds);
        Assert.Equal(5000m, (await e.Contexto.Existencias.SingleAsync(item => item.Id == 1)).Cantidad);
        Assert.Equal(100m, (await e.Contexto.Existencias.SingleAsync(item => item.Id == 10)).Cantidad);
    }

    [Fact]
    public async Task FinalizarConsumeIngresaGeneraKardexYCostoPromedioAtomico()
    {
        await using var e = await EscenarioFase6.CrearAsync();
        var receta = await e.CrearRecetaAsync();
        var orden = await e.CrearOrdenAsync(receta.Encabezado.Id);
        var confirmada = await e.ConfirmarAsync(orden.Encabezado.Id);

        var final = await e.Produccion.FinalizarAsync(orden.Encabezado.Id,
            Finalizacion(confirmada), EscenarioFase6.Seguridad, default);

        Assert.Equal("FINALIZADA", final.Encabezado.Estado);
        Assert.Equal(1000m, final.Encabezado.CantidadProducida);
        Assert.Equal(2, final.MovimientoIds.Count);
        Assert.Single(final.Consumos);
        Assert.Equal(500m, final.Consumos[0].CantidadConsumida);
        Assert.Equal(0.01m, final.Consumos[0].CostoUnitario);
        Assert.Equal(5m, final.Consumos[0].CostoTotal);
        Assert.NotNull(final.ProduccionTerminada);
        Assert.Equal(1000m, final.ProduccionTerminada!.CantidadProducida);
        Assert.Equal(5m, final.ProduccionTerminada.CostoMateriaPrima);
        Assert.Equal(0.005m, final.ProduccionTerminada.CostoUnitarioBase);
        Assert.Equal(4500m, (await e.Contexto.Existencias.AsNoTracking()
            .SingleAsync(item => item.Id == 1)).Cantidad);
        var terminado = await e.Contexto.Existencias.AsNoTracking().SingleAsync(item => item.Id == 10);
        Assert.Equal(1100m, terminado.Cantidad);
        Assert.Equal(0.006364m, terminado.CostoPromedio);
        Assert.Equal(2, await e.Contexto.Kardex.CountAsync());
        Assert.Equal(2, await e.Contexto.Movimientos.CountAsync(item => item.ModuloOrigen == "PRODUCCION"));
        Assert.Single(await e.Contexto.HistoricoCostosProductos
            .Where(item => item.ProductoId == 2 && item.OrigenCambio == "PRODUCCION")
            .ToArrayAsync());
        Assert.Equal(0.006364m, (await e.Contexto.CostosProductos
            .SingleAsync(item => item.ProductoId == 2)).CostoActual);
    }

    [Fact]
    public async Task StockInsuficienteRevierteTodaLaFinalizacion()
    {
        await using var e = await EscenarioFase6.CrearAsync();
        var receta = await e.CrearRecetaAsync();
        var orden = await e.CrearOrdenAsync(receta.Encabezado.Id);
        var confirmada = await e.ConfirmarAsync(orden.Encabezado.Id);
        var ingrediente = await e.Contexto.Existencias.SingleAsync(item => item.Id == 1);
        ingrediente.Cantidad = 499m;
        await e.Contexto.SaveChangesAsync();
        e.Contexto.ChangeTracker.Clear();

        await Assert.ThrowsAsync<ExistenciaProduccionInsuficienteException>(() =>
            e.Produccion.FinalizarAsync(orden.Encabezado.Id, Finalizacion(confirmada),
                EscenarioFase6.Seguridad, default));

        Assert.Equal(499m, (await e.Contexto.Existencias.AsNoTracking()
            .SingleAsync(item => item.Id == 1)).Cantidad);
        Assert.Equal(100m, (await e.Contexto.Existencias.AsNoTracking()
            .SingleAsync(item => item.Id == 10)).Cantidad);
        Assert.Empty(e.Contexto.Movimientos.Where(item => item.ModuloOrigen == "PRODUCCION"));
        Assert.Empty(e.Contexto.ConsumosProduccion);
        Assert.Empty(e.Contexto.ProduccionTerminada);
        Assert.Equal("CONFIRMADA", (await e.Produccion.ObtenerAsync(
            orden.Encabezado.Id, EscenarioFase6.Seguridad, default)).Encabezado.Estado);
    }

    [Fact]
    public async Task DobleFinalizacionNoDuplicaInventarioNiCostos()
    {
        await using var e = await EscenarioFase6.CrearAsync();
        var receta = await e.CrearRecetaAsync();
        var orden = await e.CrearOrdenAsync(receta.Encabezado.Id);
        var confirmada = await e.ConfirmarAsync(orden.Encabezado.Id);
        var solicitud = Finalizacion(confirmada);
        var final = await e.Produccion.FinalizarAsync(
            orden.Encabezado.Id, solicitud, EscenarioFase6.Seguridad, default);

        await Assert.ThrowsAsync<ConflictoProduccionException>(() =>
            e.Produccion.FinalizarAsync(orden.Encabezado.Id,
                new FinalizarProduccionSolicitud
                {
                    ClaveOperacion = solicitud.ClaveOperacion,
                    UbicacionId = 1,
                    CantidadProducida = 1000m,
                    VersionFila = final.Encabezado.VersionFila
                }, EscenarioFase6.Seguridad, default));

        Assert.Equal(2, await e.Contexto.Movimientos.CountAsync(item => item.ModuloOrigen == "PRODUCCION"));
        Assert.Single(e.Contexto.ProduccionTerminada);
        Assert.Single(e.Contexto.ConsumosProduccion);
        Assert.Equal(4500m, (await e.Contexto.Existencias.SingleAsync(item => item.Id == 1)).Cantidad);
        Assert.Equal(1100m, (await e.Contexto.Existencias.SingleAsync(item => item.Id == 10)).Cantidad);
    }

    [Fact]
    public async Task FinalizadaNoPuedeAnularseYOrdenFueraDeSucursalEs404()
    {
        await using var e = await EscenarioFase6.CrearAsync();
        var receta = await e.CrearRecetaAsync();
        var orden = await e.CrearOrdenAsync(receta.Encabezado.Id);
        var confirmada = await e.ConfirmarAsync(orden.Encabezado.Id);
        var final = await e.Produccion.FinalizarAsync(orden.Encabezado.Id,
            Finalizacion(confirmada), EscenarioFase6.Seguridad, default);

        await Assert.ThrowsAsync<ConflictoProduccionException>(() =>
            e.Produccion.AnularAsync(orden.Encabezado.Id,
                new CambiarEstadoProduccionSolicitud { VersionFila = final.Encabezado.VersionFila },
                EscenarioFase6.Seguridad, default));
        var otraSucursal = EscenarioFase6.Seguridad with { SucursalId = 2 };
        await Assert.ThrowsAsync<RecursoProduccionNoEncontradoException>(() =>
            e.Produccion.ObtenerAsync(orden.Encabezado.Id, otraSucursal, default));
        Assert.Equal("FINALIZADA", (await e.Produccion.ObtenerAsync(
            orden.Encabezado.Id, EscenarioFase6.Seguridad, default)).Encabezado.Estado);
    }

    [Fact]
    public async Task UbicacionAjenaYVersionObsoletaNoProducenMovimientos()
    {
        await using var e = await EscenarioFase6.CrearAsync();
        var receta = await e.CrearRecetaAsync();
        var orden = await e.CrearOrdenAsync(receta.Encabezado.Id);
        var confirmada = await e.ConfirmarAsync(orden.Encabezado.Id);
        await Assert.ThrowsAsync<ConflictoProduccionException>(() => e.Produccion.FinalizarAsync(
            orden.Encabezado.Id,
            new FinalizarProduccionSolicitud
            {
                ClaveOperacion = Guid.NewGuid(), UbicacionId = 1,
                CantidadProducida = 1000m, VersionFila = Convert.ToBase64String([99])
            }, EscenarioFase6.Seguridad, default));
        await Assert.ThrowsAsync<RecursoProduccionNoEncontradoException>(() => e.Produccion.FinalizarAsync(
            orden.Encabezado.Id,
            new FinalizarProduccionSolicitud
            {
                ClaveOperacion = Guid.NewGuid(), UbicacionId = 2,
                CantidadProducida = 1000m, VersionFila = confirmada.Encabezado.VersionFila
            }, EscenarioFase6.Seguridad, default));
        Assert.Empty(e.Contexto.Movimientos.Where(item => item.ModuloOrigen == "PRODUCCION"));
    }

    [Fact]
    public async Task DosFinalizacionesConcurrentesSoloAplicanUnaVez()
    {
        var nombre = $"fase6-concurrente-{Guid.NewGuid():N}";
        var coordinador = new CoordinadorAjustesInventario();
        long ordenId;
        string version;
        await using (var semilla = await EscenarioFase6.CrearAsync(nombre, true, coordinador))
        {
            var receta = await semilla.CrearRecetaAsync();
            var orden = await semilla.CrearOrdenAsync(receta.Encabezado.Id);
            var confirmada = await semilla.ConfirmarAsync(orden.Encabezado.Id);
            ordenId = orden.Encabezado.Id;
            version = confirmada.Encabezado.VersionFila;
        }
        await using var a = await EscenarioFase6.CrearAsync(nombre, false, coordinador);
        await using var b = await EscenarioFase6.CrearAsync(nombre, false, coordinador);
        async Task<bool> Intentar(EscenarioFase6 escenario)
        {
            try
            {
                await escenario.Produccion.FinalizarAsync(ordenId,
                    new FinalizarProduccionSolicitud
                    {
                        ClaveOperacion = Guid.NewGuid(), UbicacionId = 1,
                        CantidadProducida = 1000m, VersionFila = version
                    }, EscenarioFase6.Seguridad, default);
                return true;
            }
            catch (ConflictoProduccionException) { return false; }
        }

        var resultados = await Task.WhenAll(
            Task.Run(() => Intentar(a)), Task.Run(() => Intentar(b)));

        Assert.Single(resultados, item => item);
        Assert.Single(resultados, item => !item);
        Assert.Equal(2, await a.Contexto.Movimientos.CountAsync(item => item.ModuloOrigen == "PRODUCCION"));
        Assert.Single(a.Contexto.ProduccionTerminada);
        Assert.Equal(4500m, (await a.Contexto.Existencias.SingleAsync(item => item.Id == 1)).Cantidad);
        Assert.Equal(1100m, (await a.Contexto.Existencias.SingleAsync(item => item.Id == 10)).Cantidad);
    }

    private static FinalizarProduccionSolicitud Finalizacion(OrdenProduccionConsultaDto orden) => new()
    {
        ClaveOperacion = Guid.NewGuid(),
        UbicacionId = 1,
        CantidadProducida = 1000m,
        VersionFila = orden.Encabezado.VersionFila
    };
}
