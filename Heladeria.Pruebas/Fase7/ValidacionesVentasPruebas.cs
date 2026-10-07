using Heladeria.Aplicacion.Seguridad.Bootstrap;
using Heladeria.Aplicacion.Ventas;
using Heladeria.Infraestructura.Inventario;
using Microsoft.EntityFrameworkCore;

namespace Heladeria.Pruebas.Fase7;

public sealed class ValidacionesVentasPruebas
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1.0000001)]
    public async Task CantidadInvalida(decimal cantidad)
    {
        await using var e = await EscenarioFase7.CrearAsync();
        await Assert.ThrowsAsync<ValidacionVentasException>(() => e.CrearAsync(EscenarioFase7.Solicitud(cantidad)));
        Assert.Empty(e.Contexto.Ventas);
    }
    [Fact]
    public async Task DetallesDuplicadosRechazados()
    {
        await using var e = await EscenarioFase7.CrearAsync();
        await Assert.ThrowsAsync<ValidacionVentasException>(() => e.CrearAsync(new() {
            SucursalId = 1, BodegaId = 1, UbicacionId = 1, MonedaId = 1,
            Detalles = [new() { ProductoId = 1, Cantidad = 1 }, new() { ProductoId = 1, Cantidad = 2 }] }));
    }
    [Fact]
    public async Task ClienteExistenteOperativoYMonedaBaseNoInventada()
    {
        await using var e = await EscenarioFase7.CrearAsync();
        e.Contexto.Clientes.Add(new() { Id = 1, EmpresaId = 1, CodigoCliente = "C-1", NombreCliente = "Cliente prueba", EstadoId = 1 });
        e.Contexto.Clientes.Add(new() { Id = 2, EmpresaId = 2, CodigoCliente = "C-2", NombreCliente = "Cliente ajeno", EstadoId = 1 });
        await e.Contexto.SaveChangesAsync(); e.Contexto.ChangeTracker.Clear();
        var v = await e.CrearAsync(EscenarioFase7.Solicitud(cliente: 1));
        Assert.Equal(1, v.Encabezado.ClienteId); Assert.Equal("Cliente prueba", v.Encabezado.Cliente);
        await Assert.ThrowsAsync<VentaNoEncontradaException>(() => e.CrearAsync(EscenarioFase7.Solicitud(cliente: 2)));
        await Assert.ThrowsAsync<VentaNoEncontradaException>(() => e.CrearAsync(EscenarioFase7.Solicitud(cliente: 99)));
        await Assert.ThrowsAsync<ValidacionVentasException>(() => e.CrearAsync(EscenarioFase7.Solicitud(moneda: 2)));
        (await e.Contexto.Monedas.SingleAsync(x => x.Id == 1)).EstadoId = 2;
        await e.Contexto.SaveChangesAsync(); e.Contexto.ChangeTracker.Clear();
        await Assert.ThrowsAsync<ConflictoVentasException>(() => e.CrearAsync(EscenarioFase7.Solicitud()));
    }
    [Theory]
    [InlineData("precio")]
    [InlineData("factor")]
    [InlineData("unidad")]
    [InlineData("tipo")]
    public async Task ConfiguracionCambiadaTrasConfirmarNoCierraSilenciosamente(string campo)
    {
        await using var e = await EscenarioFase7.CrearAsync(); var v = await e.ConfirmarAsync();
        var p = await e.Contexto.ProductoPresentaciones.SingleAsync(x => x.Id == 1);
        if (campo == "precio") p.PrecioVentaEstandar = 30;
        if (campo == "factor") p.FactorConversion = 500;
        if (campo == "unidad") p.UnidadMedidaId = 1;
        if (campo == "tipo") (await e.Contexto.TiposProductos.SingleAsync(x => x.Id == 1)).ManejaExistencia = false;
        await e.Contexto.SaveChangesAsync(); e.Contexto.ChangeTracker.Clear();
        await Assert.ThrowsAsync<ConflictoVentasException>(() => e.CompletarAsync(v));
        Assert.Empty(e.Contexto.Movimientos); Assert.Single(e.Contexto.Bitacoras.Where(x => x.Accion == "VENTA_RECHAZADA"));
    }
    [Fact]
    public async Task HistoricoSinSnapshotsSoloConsulta()
    {
        await using var e = await EscenarioFase7.CrearAsync(); var v = await e.CrearAsync(EscenarioFase7.Solicitud());
        (await e.Contexto.VentasDetalle.SingleAsync()).CantidadBase = null;
        await e.Contexto.SaveChangesAsync(); e.Contexto.ChangeTracker.Clear();
        Assert.Null((await e.Ventas.ObtenerAsync(v.Encabezado.Id, EscenarioFase7.Seguridad, default)).Detalles[0].CantidadBase);
        await Assert.ThrowsAsync<ConflictoVentasException>(() => e.AnularAsync(v));
    }
    [Fact]
    public async Task ImpuestoDeOtraEmpresaNoSeOmiteSilenciosamente()
    {
        await using var e = await EscenarioFase7.CrearAsync();
        e.Contexto.Impuestos.Add(new() { Id = 1, EmpresaId = 2, Codigo = "AJENO", Nombre = "Ajeno", Porcentaje = 12,
            FechaVigenciaDesde = new(2020, 1, 1), EstadoId = 1 });
        e.Contexto.ProductoImpuestos.Add(new() { ProductoId = 1, ImpuestoId = 1, EstadoId = 1 });
        await e.Contexto.SaveChangesAsync(); e.Contexto.ChangeTracker.Clear();
        await Assert.ThrowsAsync<ConflictoVentasException>(() => e.CrearAsync(EscenarioFase7.Solicitud()));
    }
    [Fact]
    public async Task PreparacionIncompatibleNoSobrescribe()
    {
        await using var e = await EscenarioFase7.CrearAsync();
        (await e.Contexto.ConceptosMovimientos.SingleAsync(x => x.Codigo == "VENTA-SALIDA")).AfectaCosto = true;
        await e.Contexto.SaveChangesAsync(); e.Contexto.ChangeTracker.Clear();
        await Assert.ThrowsAsync<BootstrapAdministradorRechazadoException>(() => e.Preparador.EjecutarAsync(default));
        Assert.True((await e.Contexto.ConceptosMovimientos.AsNoTracking().SingleAsync(x => x.Codigo == "VENTA-SALIDA")).AfectaCosto);
    }
    [Fact]
    public async Task ReintentosConcurrentesMismaClaveDescuentanUnaSolaVez()
    {
        var nombre = "reintento-" + Guid.NewGuid().ToString("N"); var coordinador = new CoordinadorAjustesInventario();
        await using var e = await EscenarioFase7.CrearAsync(nombre, true, coordinador);
        var venta = await e.ConfirmarAsync(); var clave = Guid.NewGuid();
        await using var otro = await EscenarioFase7.CrearAsync(nombre, false, coordinador);
        var resultados = await Task.WhenAll(Task.Run(() => e.CompletarAsync(venta, clave)), Task.Run(() => otro.CompletarAsync(venta, clave)));
        Assert.All(resultados, x => Assert.Equal("COMPLETADA", x.Encabezado.Estado));
        Assert.Single(e.Contexto.Movimientos); Assert.Single(e.Contexto.Kardex);
        Assert.Equal(3000, (await e.Contexto.Existencias.AsNoTracking().SingleAsync(x => x.Id == 1)).Cantidad);
    }
}
