using System.Text.Json;
using Heladeria.Aplicacion.Ventas;
using Heladeria.Pruebas.Fase7;
using Heladeria.Pruebas.Fase8;
using Heladeria.Pruebas.Fase9;
using Microsoft.EntityFrameworkCore;

namespace Heladeria.Pruebas.AjusteFinalC;

public sealed class ComprobantesPruebas
{
    [Theory, InlineData(50, 0, 60), InlineData(0, 50, 0), InlineData(20, 30, 25)]
    public async Task ContadoHistoricoEfectivoTarjetaMixtoSinEscrituras(decimal efectivo, decimal tarjeta, decimal recibido)
    {
        await using var e = await EscenarioCaja.CrearAsync();
        var s = EscenarioCaja.Seguridad;
        var caja = await e.AbrirAsync(); var venta = await e.Ventas.ConfirmarAsync();
        Assert.Null(venta.Comprobante);
        var solicitud = await e.SolicitudCobroAsync(caja, venta, efectivo, tarjeta, recibido);
        venta = await e.Ventas.Ventas.CompletarAsync(venta.Encabezado.Id, new() { VersionFila = venta.Encabezado.VersionFila,
            ClaveOperacion = solicitud.ClaveOperacion, Cobro = new() { SesionCajaId = caja.Encabezado.Id, VersionCaja = caja.Encabezado.VersionFila, Pagos = solicitud.Pagos } }, s, default);
        var c = Assert.IsType<DatosComprobanteDto>(venta.Comprobante);
        Assert.Null(venta.Encabezado.Cliente); Assert.Null(c.CodigoCliente); Assert.Equal(50, c.Pagos.Sum(x => x.Aplicado));
        if (efectivo > 0) { var p = Assert.Single(c.Pagos, x => x.EsEfectivo == true); Assert.Equal(recibido, p.Recibido); Assert.Equal(recibido - efectivo, p.Vuelto); }
        if (tarjeta > 0) { var p = Assert.Single(c.Pagos, x => x.EsEfectivo == false); Assert.Equal(tarjeta, p.Recibido); Assert.Equal(0, p.Vuelto); }
        e.Contexto.ChangeTracker.Clear();
        (await e.Contexto.ProductoPresentaciones.SingleAsync(x => x.Id == 1)).PrecioVentaEstandar = 999;
        await e.Contexto.SaveChangesAsync(); e.Contexto.ChangeTracker.Clear();
        var antes = await e.Contexto.Bitacoras.CountAsync(); var pagos = await e.Contexto.Pagos.CountAsync();
        var movimientos = await e.Contexto.MovimientosCaja.CountAsync(); var stock = await e.Contexto.Existencias.AsNoTracking().Select(x => x.Cantidad).ToArrayAsync();
        var consulta = await e.Ventas.Ventas.ObtenerAsync(venta.Encabezado.Id, s, default);
        Assert.Equal(JsonSerializer.Serialize(venta), JsonSerializer.Serialize(consulta));
        Assert.Equal(25, Assert.Single(consulta.Detalles).PrecioUnitario);
        Assert.Equal(antes, await e.Contexto.Bitacoras.CountAsync()); Assert.Equal(pagos, await e.Contexto.Pagos.CountAsync());
        Assert.Equal(movimientos, await e.Contexto.MovimientosCaja.CountAsync());
        Assert.Equal(stock, await e.Contexto.Existencias.AsNoTracking().Select(x => x.Cantidad).ToArrayAsync());
        Assert.Empty(e.Contexto.ChangeTracker.Entries());
    }
    [Fact]
    public async Task CreditoConClienteSinPagoFicticioYSaldoOriginalAunqueAbone()
    {
        await using var e = await EscenarioCartera.CrearAsync(); var venta = await e.VentaCreditoAsync();
        var c = Assert.IsType<DatosComprobanteDto>(venta.Comprobante);
        Assert.Equal("CLIENTE-PRUEBA", c.CodigoCliente); Assert.Equal(50, c.SaldoInicialCredito); Assert.Empty(c.Pagos);
        var cuenta = await e.Servicio.ObtenerAsync(false, c.CuentaPorCobrarId!.Value, EscenarioCartera.S, default);
        var sesion = await e.Caja.AbrirAsync();
        await e.Servicio.AplicarAsync(false, cuenta.Encabezado.Id, await e.SolicitudAsync(cuenta, sesion, 10), EscenarioCartera.S, default);
        var actual = await e.Caja.Ventas.Ventas.ObtenerAsync(venta.Encabezado.Id, EscenarioCartera.S, default);
        Assert.Equal(50, actual.Comprobante!.SaldoInicialCredito); Assert.Empty(actual.Comprobante.Pagos);
    }
    [Fact]
    public async Task AnuladaConservaDetalleYBorradorNoTieneTicket()
    {
        await using var e = await EscenarioFase7.CrearAsync(); var s = EscenarioFase7.Seguridad;
        var borrador = await e.Ventas.CrearAsync(EscenarioFase7.Solicitud(), s, default); Assert.Null(borrador.Comprobante);
        var venta = await e.HistoricaSinCobroAsync(await e.ConfirmarAsync());
        var anulada = await e.Ventas.AnularAsync(venta.Encabezado.Id, new() { VersionFila = venta.Encabezado.VersionFila, Motivo = "Anulación de prueba aislada" }, s, default);
        Assert.Equal("ANULADA", anulada.Encabezado.Estado); Assert.NotNull(anulada.Comprobante);
        Assert.Equal(JsonSerializer.Serialize(venta.Detalles), JsonSerializer.Serialize(anulada.Detalles));
    }
    [Fact]
    public async Task TicketNoPermiteCambiarSucursalEnUrl()
    {
        await using var e = await EscenarioFase7.CrearAsync(); var v = await e.CompletarAsync(await e.ConfirmarAsync());
        await Assert.ThrowsAsync<VentaNoEncontradaException>(() => e.Ventas.ObtenerAsync(v.Encabezado.Id, EscenarioFase7.Seguridad with { SucursalId = 2 }, default));
    }
}
