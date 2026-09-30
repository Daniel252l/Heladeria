using Heladeria.Aplicacion.Compras;
using Heladeria.Aplicacion.Seguridad.Bootstrap;
using Heladeria.Infraestructura.Inventario;
using Heladeria.Infraestructura.Persistencia;
using Heladeria.Infraestructura.Persistencia.Entidades;
using Heladeria.Pruebas.Fase4;
using Microsoft.EntityFrameworkCore;
using static Heladeria.Pruebas.Fase5.EscenarioFase5;

namespace Heladeria.Pruebas.Fase5;

public sealed class ConcurrenciaYPreparacionPruebas
{
    private static Heladeria.Aplicacion.Configuracion.ContextoConfiguracion Seguridad => EscenarioFase5.Seguridad;
    [Fact]
    public async Task PreparacionIdempotenteNoDuplicaNiReiniciaCorrelativos()
    {
        await using var e = await CrearAsync(); await e.ConfirmarAsync();
        var permisos = await e.Contexto.Permisos.CountAsync(); var estados = await e.Contexto.EstadosFlujo.CountAsync();
        var segunda = await e.Preparador.EjecutarAsync(default);
        Assert.Empty(segunda.Registros); Assert.Equal(permisos, await e.Contexto.Permisos.CountAsync());
        Assert.Equal(estados, await e.Contexto.EstadosFlujo.CountAsync());
        Assert.Equal(1, (await e.Contexto.CorrelativosDocumentos.SingleAsync(item => item.SerieDocumento.Serie == "OC")).UltimoNumero);
        Assert.Equal(1, await e.Contexto.Bitacoras.CountAsync(item => item.Accion == "FASE5_CONFIGURADA"));
        Assert.Equal(5, await e.Contexto.RolesPermisos.CountAsync());
    }
    [Fact]
    public async Task ReferenciaIncompatibleNoSeSobrescribe()
    {
        await using var e = await CrearAsync();
        (await e.Contexto.ConceptosMovimientos.SingleAsync(item => item.Codigo == "RECEPCION-COMPRA")).AfectaCosto = false;
        await e.Contexto.SaveChangesAsync();
        await Assert.ThrowsAsync<BootstrapAdministradorRechazadoException>(() => e.Preparador.EjecutarAsync(default));
        Assert.False((await e.Contexto.ConceptosMovimientos.SingleAsync(item => item.Codigo == "RECEPCION-COMPRA")).AfectaCosto);
    }
    [Fact]
    public async Task ContextoReutilizadoNoCruzaOrdenesNiRecepciones()
    {
        await using var e = await CrearAsync();
        var a = await e.ConfirmarAsync(Orden(3, 1000)); var b = await e.ConfirmarAsync(Orden(4, 2000));
        await e.Compras.RecibirAsync(b.Encabezado.Id, Recepcion(b.Detalles[0].Id, 4), Seguridad, default);
        var intacta = await e.Compras.ObtenerAsync(a.Encabezado.Id, Seguridad, default);
        Assert.Equal("CONFIRMADA", intacta.Encabezado.Estado); Assert.Equal(3m, intacta.Detalles[0].CantidadPendiente);
        await e.Compras.RecibirAsync(a.Encabezado.Id, Recepcion(a.Detalles[0].Id, 3), Seguridad, default);
        Assert.Equal("RECIBIDA", (await e.Compras.ObtenerAsync(b.Encabezado.Id, Seguridad, default)).Encabezado.Estado);
        Assert.Equal(12000m, (await e.Contexto.Existencias.AsNoTracking().SingleAsync(item => item.Id == 1)).Cantidad);
    }
    [Fact]
    public async Task DosRecepcionesSimultaneasMismoProductoMantienenStockCostoYPendiente()
    {
        var nombre = $"fase5-concurrente-{Guid.NewGuid():N}";
        var coordinador = new CoordinadorAjustesInventario(); long ordenId; long linea;
        await using (var e = new EscenarioFase5(await EscenarioFase4.CrearAsync(nombre, EscenarioFase4.RaizCompartida, true, coordinador), coordinador))
        {
            await e.SemillarAsync(); var stock = await e.Contexto.Existencias.SingleAsync(item => item.Id == 1);
            stock.Cantidad = 2500; stock.CostoPromedio = 0.02m; await e.Contexto.SaveChangesAsync();
            var orden = await e.ConfirmarAsync(Orden(3, 25)); ordenId = orden.Encabezado.Id; linea = orden.Detalles[0].Id;
        }
        await using var a = new EscenarioFase5(await EscenarioFase4.CrearAsync(nombre, EscenarioFase4.RaizCompartida, false, coordinador), coordinador);
        await using var b = new EscenarioFase5(await EscenarioFase4.CrearAsync(nombre, EscenarioFase4.RaizCompartida, false, coordinador), coordinador);
        var resultados = await Task.WhenAll(Task.Run(() => a.Compras.RecibirAsync(ordenId, Recepcion(linea, 1), Seguridad, default)),
            Task.Run(() => b.Compras.RecibirAsync(ordenId, Recepcion(linea, 2), Seguridad, default)));
        Assert.Equal(2, resultados.Length);
        var stockFinal = await a.Contexto.Existencias.AsNoTracking().SingleAsync(item => item.Id == 1);
        Assert.Equal(5500m, stockFinal.Cantidad);
        // Cada recepción guarda un promedio a seis decimales, como exige SQL.
        Assert.InRange(stockFinal.CostoPromedio, 0.022726m, 0.022728m);
        Assert.Equal("RECIBIDA", (await a.Compras.ObtenerAsync(ordenId, Seguridad, default)).Encabezado.Estado);
        Assert.Equal(2, await a.Contexto.Kardex.CountAsync());
        Assert.Equal(2, (await a.Contexto.RecepcionesCompra.Select(item => item.NumeroRecepcion).Distinct().ToArrayAsync()).Length);
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DosRecepcionesEnConflictoSoloUnaSeAplica(bool mismaClave)
    {
        var nombre = $"fase5-exceso-{Guid.NewGuid():N}"; var coordinador = new CoordinadorAjustesInventario(); long id; long linea;
        await using (var e = new EscenarioFase5(await EscenarioFase4.CrearAsync(nombre, EscenarioFase4.RaizCompartida, true, coordinador), coordinador))
        {
            await e.SemillarAsync(); var orden = await e.ConfirmarAsync(Orden(10, 1000)); id = orden.Encabezado.Id; linea = orden.Detalles[0].Id;
        }
        await using var a = new EscenarioFase5(await EscenarioFase4.CrearAsync(nombre, EscenarioFase4.RaizCompartida, false, coordinador), coordinador);
        await using var b = new EscenarioFase5(await EscenarioFase4.CrearAsync(nombre, EscenarioFase4.RaizCompartida, false, coordinador), coordinador);
        var operacion = Guid.NewGuid();
        async Task<bool> Intentar(EscenarioFase5 e)
        {
            try { await e.Compras.RecibirAsync(id, Recepcion(linea, mismaClave ? 1 : 6, mismaClave ? operacion : null), Seguridad, default); return true; }
            catch (ConflictoComprasException) { return false; }
        }
        var resultados = await Task.WhenAll(Task.Run(() => Intentar(a)), Task.Run(() => Intentar(b)));
        Assert.Single(resultados, item => item); Assert.Single(resultados, item => !item);
        Assert.Equal(mismaClave ? 6000m : 11000m, (await a.Contexto.Existencias.AsNoTracking().SingleAsync(item => item.Id == 1)).Cantidad);
        Assert.Single(a.Contexto.RecepcionesCompra);
    }
}
