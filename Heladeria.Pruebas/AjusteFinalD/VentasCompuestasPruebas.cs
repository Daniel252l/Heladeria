using Heladeria.Aplicacion.Caja;
using Heladeria.Aplicacion.Ventas;
using Heladeria.Pruebas.Fase7;
using Microsoft.EntityFrameworkCore;

namespace Heladeria.Pruebas.AjusteFinalD;

public sealed class VentasCompuestasPruebas
{
    [Theory, InlineData(1), InlineData(2), InlineData(3)]
    public async Task ComponentesConPresentacionYConsumiblesCostoKardexYTicket(int cantidad)
    {
        await using var e = await EscenarioComposicion.CrearAsync();
        var v = await e.ConfirmarAsync(cantidad);
        Assert.Empty(v.Movimientos); Assert.Equal(5000, await e.StockAsync(1));
        var cerrada = await e.Ventas.CompletarAsync(v);
        Assert.Equal("COMPLETADA", cerrada.Encabezado.Estado);
        var linea = Assert.Single(cerrada.Detalles); Assert.Equal(e.Padre, linea.ProductoId);
        Assert.Equal(7.65m * cantidad, linea.CostoTotal); Assert.Equal(7.65m, linea.CostoUnitarioBase);
        Assert.Equal(5000 - 120 * cantidad, await e.StockAsync(1));
        foreach (var id in e.Insumos) Assert.Equal(10 - cantidad, await e.StockAsync(id));
        Assert.Equal(5, cerrada.Movimientos.Count); Assert.Equal(5, await e.Db.Kardex.CountAsync());
        var movimientos = await e.Db.MovimientoDetalles.AsNoTracking().Include(x => x.Movimientos).ToArrayAsync();
        Assert.All(movimientos, x => { Assert.Equal(linea.Id, x.VentaDetalleId); Assert.NotNull(x.UnidadBaseId);
            Assert.Equal(v.Encabezado.Id, x.Movimientos.DocumentoOrigenId); Assert.Equal("VS", x.Movimientos.SerieDocumento);
            Assert.Equal(v.Encabezado.NumeroVenta, x.Movimientos.ReferenciaExterna); Assert.Equal(1, x.Movimientos.AplicadoPorUsuarioId); });
        Assert.DoesNotContain(movimientos, x => x.ProductoId == e.Padre);
        Assert.NotNull(cerrada.Comprobante); Assert.Single(cerrada.Detalles);
        // Los agregados decimales del reporte se verifican en ComposicionSqlPruebas con SQL Server real.
    }

    [Fact]
    public async Task UnComponenteYPresentacionDelPadreEscalaPorUnidadBase()
    {
        await using var e = await EscenarioComposicion.CrearAsync();
        await e.EditarAsync([new() { ProductoId = 1, ProductoPresentacionId = e.Bola, Cantidad = 2 }]);
        var pp = await e.Productos.CrearPresentacionAsync(e.Padre, new() { PresentacionId = 3, UnidadMedidaId = e.Unidad,
            FactorConversion = 3, PermiteVenta = true, PrecioVentaEstandar = 70, EstadoId = 1 }, EscenarioComposicion.S, default);
        var v = await e.Ventas.ConfirmarAsync(EscenarioFase7.Solicitud(2, e.Padre, pp.Id));
        var final = await e.Ventas.CompletarAsync(v);
        Assert.Single(final.Movimientos); Assert.Equal(4280, await e.StockAsync(1)); Assert.Equal(18, final.Detalles[0].CostoTotal);
    }

    [Theory, InlineData(false), InlineData(true)]
    public async Task StockExactoOInsuficienteSinCambiosFinancieros(bool insuficiente)
    {
        await using var e = await EscenarioComposicion.CrearAsync();
        var servilletas = await e.Db.Existencias.SingleAsync(x => x.ProductoId == e.Insumos.Last());
        servilletas.Cantidad = insuficiente ? 1 : 2; await e.Db.SaveChangesAsync(); e.Db.ChangeTracker.Clear();
        var v = await e.ConfirmarAsync(); var cajaAntes = await e.Db.MovimientosCaja.CountAsync();
        if (!insuficiente) { await e.Ventas.CompletarAsync(v); Assert.Equal(0, await e.StockAsync(e.Insumos.Last())); return; }
        var error = await Assert.ThrowsAsync<StockVentaInsuficienteException>(() => e.Ventas.CompletarAsync(v));
        Assert.Equal("EXISTENCIA_INSUFICIENTE", error.Codigo); Assert.Contains("SERVILLETA", error.Message);
        await SinCambios(e, v, cajaAntes);
    }

    [Fact]
    public async Task AcumulaDemandaCompartidaEntreCompuestoYLineaSimple()
    {
        await using var e = await EscenarioComposicion.CrearAsync();
        var v = await e.Ventas.ConfirmarAsync(new() { SucursalId = 1, BodegaId = 1, UbicacionId = 1, MonedaId = 1,
            Detalles = [new() { ProductoId = e.Padre, Cantidad = 1 }, new() { ProductoId = 1, Cantidad = 4900 }] });
        await Assert.ThrowsAsync<StockVentaInsuficienteException>(() => e.Ventas.CompletarAsync(v));
        Assert.Empty(await e.Db.Movimientos.ToArrayAsync()); Assert.Equal(5000, await e.StockAsync(1));
    }

    [Theory, InlineData("movimiento"), InlineData("pago"), InlineData("cxc")]
    public async Task FalloTardioRevierteTodosLosComponentesCajaCostosYCorrelativos(string fallo)
    {
        await using var e = await EscenarioComposicion.CrearAsync();
        var v = fallo == "cxc" ? await Credito(e) : await e.ConfirmarAsync();
        var caja = await e.Db.MovimientosCaja.CountAsync();
        var sql = fallo switch {
            "movimiento" => $"CREATE TRIGGER FalloD BEFORE INSERT ON MovimientoDetalles WHEN NEW.ProductoId={e.Insumos.Last()} BEGIN SELECT RAISE(ABORT,'Fallo deliberado'); END;",
            "pago" => "CREATE TRIGGER FalloD BEFORE INSERT ON Pagos BEGIN SELECT RAISE(ABORT,'Fallo deliberado'); END;",
            _ => "CREATE TRIGGER FalloD BEFORE INSERT ON CuentasPorCobrar BEGIN SELECT RAISE(ABORT,'Fallo deliberado'); END;" };
        await e.Db.Database.ExecuteSqlRawAsync(sql);
        await Assert.ThrowsAsync<DbUpdateException>(() => e.Ventas.CompletarAsync(v));
        await SinCambios(e, v, caja); Assert.Empty(await e.Db.CuentasPorCobrar.ToArrayAsync());
        await e.Db.Database.ExecuteSqlRawAsync("DROP TRIGGER FalloD;");
        Assert.Equal("COMPLETADA", (await e.Ventas.CompletarAsync(v)).Encabezado.Estado);
    }

    [Theory, InlineData("EFECTIVO"), InlineData("TARJETA"), InlineData("CREDITO")]
    public async Task CierreFinancieroYReintentoNoDuplicaConsumos(string forma)
    {
        await using var e = await EscenarioComposicion.CrearAsync();
        var v = forma == "CREDITO" ? await Credito(e) : await e.ConfirmarAsync();
        var cobro = await e.Ventas.CobroAsync(v);
        if (forma == "TARJETA") cobro = new() { SesionCajaId = cobro!.SesionCajaId, VersionCaja = cobro.VersionCaja,
            Pagos = [new() { FormaPagoId = await e.Db.FormasPago.Where(x => x.Codigo == forma).Select(x => x.Id).SingleAsync(),
                MontoAplicado = v.Encabezado.Total, MontoRecibido = v.Encabezado.Total, Referencia = "AUTORIZACION-PRUEBA" }] };
        var solicitud = new CompletarVentaSolicitud { VersionFila = v.Encabezado.VersionFila, ClaveOperacion = Guid.NewGuid(), Cobro = cobro };
        var final = await e.Ventas.Ventas.CompletarAsync(v.Encabezado.Id, solicitud, EscenarioComposicion.S, default);
        await e.EditarAsync([new() { ProductoId = 1, Cantidad = 180 }]); // No cambia el reintento ya aplicado.
        var repetida = await e.Ventas.Ventas.CompletarAsync(v.Encabezado.Id, solicitud, EscenarioComposicion.S, default);
        Assert.Equal(final.Movimientos, repetida.Movimientos); Assert.Equal(5, await e.Db.Kardex.CountAsync());
        Assert.Equal(4760, await e.StockAsync(1)); Assert.Equal(15.3m, repetida.Detalles[0].CostoTotal);
        Assert.Equal(forma == "CREDITO" ? 1 : 0, await e.Db.CuentasPorCobrar.CountAsync());
        Assert.Equal(forma == "CREDITO" ? 0 : 1, await e.Db.Pagos.CountAsync());
        await Assert.ThrowsAsync<ConflictoVentasException>(() => e.Ventas.CompletarAsync(final, Guid.NewGuid()));
    }

    [Fact]
    public async Task AnulacionUsaConsumoOriginalAunqueSeQuiteLaComposicion()
    {
        await using var e = await EscenarioComposicion.CrearAsync();
        var v = await e.Ventas.CompletarAsync(await e.ConfirmarAsync(gratis: true));
        await e.EditarAsync([]);
        var anulada = await e.Ventas.AnularAsync(v);
        Assert.Equal("ANULADA", anulada.Encabezado.Estado); Assert.Equal(10, anulada.Movimientos.Count);
        Assert.Equal(5000, await e.StockAsync(1)); foreach (var id in e.Insumos) Assert.Equal(10, await e.StockAsync(id));
        var filas = await e.Db.MovimientoDetalles.AsNoTracking().Include(x => x.Movimientos).ToArrayAsync();
        foreach (var salida in filas.Where(x => x.Movimientos.SerieDocumento == "VS")) {
            var entrada = Assert.Single(filas, x => x.Movimientos.SerieDocumento == "VA" && x.ProductoId == salida.ProductoId);
            Assert.Equal(salida.CantidadBase, entrada.CantidadBase); Assert.Equal(salida.CostoUnitario, entrada.CostoUnitario);
        }
        await Assert.ThrowsAsync<ConflictoVentasException>(() => e.Ventas.AnularAsync(anulada)); Assert.Equal(10, await e.Db.Kardex.CountAsync());
    }

    private static async Task<VentaConsultaDto> Credito(EscenarioComposicion e) => await e.Ventas.ConfirmarAsync(new() {
        SucursalId = 1, BodegaId = 1, UbicacionId = 1, MonedaId = 1, EsCredito = true,
        ClienteId = await e.Db.Clientes.Select(x => x.Id).SingleAsync(), FechaVencimientoCredito = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(10),
        Detalles = [new() { ProductoId = e.Padre, Cantidad = 2 }] });
    private static async Task SinCambios(EscenarioComposicion e, VentaConsultaDto v, int caja)
    {
        Assert.Empty(await e.Db.Movimientos.ToArrayAsync()); Assert.Empty(await e.Db.Kardex.ToArrayAsync());
        Assert.Empty(await e.Db.Pagos.ToArrayAsync()); Assert.Equal(caja, await e.Db.MovimientosCaja.CountAsync());
        Assert.Equal(5000, await e.StockAsync(1));
        Assert.Equal(0, await e.Db.CorrelativosDocumentos.Where(x => x.SerieDocumento.Serie == "VS").Select(x => x.UltimoNumero).SingleAsync());
        var actual = await e.Ventas.Ventas.ObtenerAsync(v.Encabezado.Id, EscenarioComposicion.S, default);
        Assert.Equal("CONFIRMADA", actual.Encabezado.Estado); Assert.Null(actual.Encabezado.ClaveOperacion);
        Assert.All(actual.Detalles, x => Assert.Null(x.CostoTotal));
    }
}
