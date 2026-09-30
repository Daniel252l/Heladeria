using Heladeria.Aplicacion.Compras;
using Microsoft.EntityFrameworkCore;
using static Heladeria.Pruebas.Fase5.EscenarioFase5;

namespace Heladeria.Pruebas.Fase5;

public sealed class RecepcionesPruebas
{
    private static Heladeria.Aplicacion.Configuracion.ContextoConfiguracion Seguridad => EscenarioFase5.Seguridad;
    [Fact]
    public async Task CostoPositivoNoRepresentableNoSeConvierteSilenciosamenteEnCero()
    {
        await using var e = await CrearAsync();
        (await e.Contexto.ProductoPresentaciones.SingleAsync(item => item.Id == 1)).FactorConversion = 100000000m;
        await e.Contexto.SaveChangesAsync(); var orden = await e.ConfirmarAsync(Orden(1, 0.01m));
        await Assert.ThrowsAsync<ValidacionComprasException>(() => e.Compras.RecibirAsync(orden.Encabezado.Id, Recepcion(orden.Detalles[0].Id, 1), Seguridad, default));
        Assert.Empty(e.Contexto.RecepcionesCompra); Assert.Empty(e.Contexto.Movimientos);
    }
    [Fact]
    public async Task ParcialLuegoCompletaAplicaInventarioKardexCostosAuditoriaYTraza()
    {
        await using var e = await CrearAsync(); var orden = await e.ConfirmarAsync(); var id = orden.Encabezado.Id; var linea = orden.Detalles[0].Id;
        var parcial = await e.Compras.RecibirAsync(id, Recepcion(linea, 6), Seguridad, default);
        Assert.Equal("APLICADA", parcial.Estado); Assert.Single(parcial.Detalles); Assert.Single(parcial.MovimientoIds);
        var pendiente = await e.Compras.ObtenerAsync(id, Seguridad, default);
        Assert.Equal("PARCIALMENTE_RECIBIDA", pendiente.Encabezado.Estado);
        Assert.Equal(6m, pendiente.Detalles[0].CantidadRecibida); Assert.Equal(4m, pendiente.Detalles[0].CantidadPendiente);
        Assert.Equal(11000m, (await e.Contexto.Existencias.AsNoTracking().SingleAsync(item => item.Id == 1)).Cantidad);
        await Assert.ThrowsAsync<ConflictoComprasException>(() => e.Compras.CambiarEstadoAsync(id, "ANULADA", Seguridad, default));
        await e.Compras.RecibirAsync(id, Recepcion(linea, 4), Seguridad, default);
        var completa = await e.Compras.ObtenerAsync(id, Seguridad, default);
        Assert.Equal("RECIBIDA", completa.Encabezado.Estado); Assert.Equal(0m, completa.Detalles[0].CantidadPendiente);
        Assert.Equal(15000m, (await e.Contexto.Existencias.AsNoTracking().SingleAsync(item => item.Id == 1)).Cantidad);
        Assert.Equal(2, await e.Contexto.Movimientos.CountAsync()); Assert.Equal(2, await e.Contexto.MovimientoDetalles.CountAsync());
        Assert.Equal(2, await e.Contexto.Kardex.CountAsync()); Assert.Equal(2, await e.Contexto.HistoricoCostosProductos.CountAsync());
        Assert.Equal(2, await e.Contexto.HistoricoPreciosCompra.CountAsync()); Assert.Equal(2, await e.Contexto.Bitacoras.CountAsync(item => item.Accion == "RECEPCION_APLICADA"));
        Assert.All(e.Contexto.Movimientos, item => { Assert.Equal("COMPRAS", item.ModuloOrigen); Assert.NotNull(item.DocumentoOrigenId); Assert.Equal("IC", item.SerieDocumento); });
        Assert.All(e.Contexto.MovimientoDetalles, item => { Assert.Equal(1000m, item.FactorConversion); Assert.Equal(25m, item.CostoUnitario); });
        Assert.Empty(e.Contexto.CuentasPorPagar); Assert.Empty(e.Contexto.FacturasProveedores);
    }

    [Fact]
    public async Task Recepcion_ConvierteCostoPorLitroACostoPorMililitroSinDobleConversion()
    {
        await using var e = await CrearAsync();
        var orden = await e.ConfirmarAsync(Orden(3, 25));

        var recepcion = await e.Compras.RecibirAsync(
            orden.Encabezado.Id,
            Recepcion(orden.Detalles[0].Id, 3),
            Seguridad,
            default);

        Assert.Equal(0.025m, Assert.Single(recepcion.Detalles).CostoUnitarioBase);
        var detalle = await e.Contexto.MovimientoDetalles.SingleAsync();
        Assert.Equal(1000m, detalle.FactorConversion);
        Assert.Equal(3000m, detalle.CantidadBase);
        Assert.Equal(0.025m, detalle.CostoUnitario);
        Assert.Equal(75m, detalle.CantidadBase * detalle.CostoUnitario);
    }

    [Fact]
    public async Task CostoPromedioClaveExactoSegunDecimalSql()
    {
        await using var e = await CrearAsync();
        var stock = await e.Contexto.Existencias.SingleAsync(item => item.Id == 1); stock.Cantidad = 2500; stock.CostoPromedio = 0.02m;
        await e.Contexto.SaveChangesAsync();
        var orden = await e.ConfirmarAsync(Orden(3, 25));
        var recepcion = await e.Compras.RecibirAsync(
            orden.Encabezado.Id,
            Recepcion(orden.Detalles[0].Id, 3),
            Seguridad,
            default);
        var resultado = await e.Contexto.Existencias.AsNoTracking().SingleAsync(item => item.Id == 1);
        Assert.Equal(0.025m, Assert.Single(recepcion.Detalles).CostoUnitarioBase);
        Assert.Equal(5500m, resultado.Cantidad); Assert.Equal(0.022727m, resultado.CostoPromedio);
        Assert.Equal(0.022727m, (await e.Contexto.CostosProductos.AsNoTracking().SingleAsync()).CostoActual);
        Assert.Equal(0.022727m, (await e.Contexto.HistoricoCostosProductos.SingleAsync()).CostoNuevo);
        var detalle = await e.Contexto.MovimientoDetalles.SingleAsync();
        Assert.Equal(0.025m, detalle.CostoUnitario);
        Assert.Equal(75m, detalle.CantidadBase * detalle.CostoUnitario);
        var kardex = await e.Contexto.Kardex.SingleAsync();
        Assert.Equal(0.025m, kardex.CostoUnitario);
        Assert.Equal(124.9985m, kardex.SaldoCosto);
    }
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task CantidadesNoPositivasRechazadas(decimal cantidad)
    {
        await using var e = await CrearAsync(); var orden = await e.ConfirmarAsync();
        await Assert.ThrowsAsync<ValidacionComprasException>(() => e.Compras.RecibirAsync(orden.Encabezado.Id, Recepcion(orden.Detalles[0].Id, cantidad), Seguridad, default));
        Assert.Empty(e.Contexto.RecepcionesCompra); Assert.Empty(e.Contexto.Movimientos);
    }
    [Fact]
    public async Task ExcesoRecibidoRechazadoSinCambios()
    {
        await using var e = await CrearAsync(); var orden = await e.ConfirmarAsync();
        await Assert.ThrowsAsync<ConflictoComprasException>(() => e.Compras.RecibirAsync(orden.Encabezado.Id, Recepcion(orden.Detalles[0].Id, 11), Seguridad, default));
        Assert.Empty(e.Contexto.Movimientos); Assert.Empty(e.Contexto.RecepcionesCompra);
        Assert.Equal(5000m, (await e.Contexto.Existencias.AsNoTracking().SingleAsync(item => item.Id == 1)).Cantidad);
    }
    [Fact]
    public async Task ClaveDuplicadaNoAplicaDosVecesInclusoEnOtraOrden()
    {
        await using var e = await CrearAsync(); var orden = await e.ConfirmarAsync(); var clave = Guid.NewGuid();
        await e.Compras.RecibirAsync(orden.Encabezado.Id, Recepcion(orden.Detalles[0].Id, 1, clave), Seguridad, default);
        await Assert.ThrowsAsync<ConflictoComprasException>(() => e.Compras.RecibirAsync(orden.Encabezado.Id, Recepcion(orden.Detalles[0].Id, 1, clave), Seguridad, default));
        var otra = await e.ConfirmarAsync();
        await Assert.ThrowsAsync<ConflictoComprasException>(() => e.Compras.RecibirAsync(otra.Encabezado.Id, Recepcion(otra.Detalles[0].Id, 1, clave), Seguridad, default));
        Assert.Single(e.Contexto.RecepcionesCompra); Assert.Single(e.Contexto.Kardex);
    }
    [Fact]
    public async Task UbicacionDeOtraSucursalDevuelve404()
    {
        await using var e = await CrearAsync(); var orden = await e.ConfirmarAsync();
        await Assert.ThrowsAsync<RecursoComprasNoEncontradoException>(() => e.Compras.RecibirAsync(orden.Encabezado.Id,
            Recepcion(orden.Detalles[0].Id, 1, bodega: 2, ubicacion: 2), Seguridad, default));
        Assert.Empty(e.Contexto.Movimientos);
    }
    [Fact]
    public async Task DetalleInvalidoRepetidoOClaveVaciaNoInserta()
    {
        await using var e = await CrearAsync(); var orden = await e.ConfirmarAsync();
        await Assert.ThrowsAsync<ValidacionComprasException>(() => e.Compras.RecibirAsync(orden.Encabezado.Id, Recepcion(999, 1), Seguridad, default));
        await Assert.ThrowsAsync<ValidacionComprasException>(() => e.Compras.RecibirAsync(orden.Encabezado.Id, Recepcion(orden.Detalles[0].Id, 1, Guid.Empty), Seguridad, default));
        var repetida = new CrearRecepcionSolicitud { ClaveOperacion = Guid.NewGuid(), BodegaId = 1, UbicacionId = 1,
            Detalles = [new() { OrdenCompraDetalleId = orden.Detalles[0].Id, Cantidad = 1 }, new() { OrdenCompraDetalleId = orden.Detalles[0].Id, Cantidad = 1 }] };
        await Assert.ThrowsAsync<ValidacionComprasException>(() => e.Compras.RecibirAsync(orden.Encabezado.Id, repetida, Seguridad, default));
        Assert.Empty(e.Contexto.RecepcionesCompra);
    }
    [Fact]
    public async Task RecepcionCompletaNoSeAplicaNuevamenteYDetalleIdor()
    {
        await using var e = await CrearAsync(); var orden = await e.ConfirmarAsync();
        var recibo = await e.Compras.RecibirAsync(orden.Encabezado.Id, Recepcion(orden.Detalles[0].Id, 10), Seguridad, default);
        await Assert.ThrowsAsync<ConflictoComprasException>(() => e.Compras.RecibirAsync(orden.Encabezado.Id, Recepcion(orden.Detalles[0].Id, 1), Seguridad, default));
        await Assert.ThrowsAsync<RecursoComprasNoEncontradoException>(() => e.Compras.ObtenerRecepcionAsync(recibo.Id, Seguridad with { SucursalId = 2 }, default));
        await Assert.ThrowsAsync<RecursoComprasNoEncontradoException>(() => e.Compras.ObtenerRecepcionAsync(999, Seguridad, default));
    }
}
