using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Heladeria.Api.ManejoErrores;
using Heladeria.Aplicacion.Ventas;
using Heladeria.Infraestructura.Inventario;
using Heladeria.Infraestructura.Persistencia;
using Heladeria.Pruebas.Fase4;
using Heladeria.Pruebas.Fase5;
using Heladeria.Pruebas.Seguridad;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Heladeria.Pruebas.Fase7;

public sealed class VentaConfirmadaStockInsuficientePruebas
{
    [Fact]
    public async Task ConfirmadaCon20LitrosY18500MlDevuelveStockInsuficienteSinModificarNegocio()
    {
        await using var conexion = new SqliteConnection("Data Source=:memory:");
        await conexion.OpenAsync();
        var opciones = new DbContextOptionsBuilder<HeladeriaDbContext>().UseSqlite(conexion).Options;
        var db = new ContextoSqlitePruebas(opciones);
        await db.Database.EnsureCreatedAsync();
        var coordinador = new CoordinadorAjustesInventario();
        await using var escenario = new EscenarioFase7(new EscenarioFase5(
            await EscenarioFase4.CrearConContextoAsync(db, coordinador), coordinador));
        await escenario.SemillarAsync();
        var existencia = await db.Existencias.SingleAsync(x => x.ProductoId == 1 && x.BodegaId == 1);
        existencia.Cantidad = 18500m;
        existencia.CantidadReservada = 0m;
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        await using var fabrica = new FabricaApiSeguridad(true, servicios =>
        {
            servicios.RemoveAll<HeladeriaDbContext>();
            servicios.RemoveAll<DbContextOptions<HeladeriaDbContext>>();
            servicios.AddScoped<HeladeriaDbContext>(_ => new ContextoSqlitePruebas(opciones));
        });
        using var cliente = fabrica.CrearClienteAutenticado();
        var creada = await cliente.PostAsJsonAsync("/api/ventas", EscenarioFase7.Solicitud(cantidad: 20));
        Assert.Equal(HttpStatusCode.Created, creada.StatusCode);
        var borrador = (await creada.Content.ReadFromJsonAsync<VentaConsultaDto>())!;
        Assert.Equal("BORRADOR", borrador.Encabezado.Estado);
        var ruta = $"/api/ventas/{borrador.Encabezado.Id}";
        var confirmar = await cliente.PostAsJsonAsync(ruta + "/confirmar",
            new ConfirmarVentaSolicitud { VersionFila = borrador.Encabezado.VersionFila });
        Assert.Equal(HttpStatusCode.OK, confirmar.StatusCode);

        var antes = (await cliente.GetFromJsonAsync<VentaConsultaDto>(ruta))!;
        Assert.Equal("CONFIRMADA", antes.Encabezado.Estado);
        Assert.Null(antes.Encabezado.ClaveOperacion);
        Assert.Empty(antes.Movimientos);
        var linea = Assert.Single(antes.Detalles);
        Assert.Equal(1, linea.ProductoId);
        Assert.Equal("Helado Chocolate", linea.Producto);
        Assert.Equal("Litro", linea.Presentacion);
        Assert.Equal(20m, linea.Cantidad);
        Assert.Equal(1000m, linea.FactorConversion);
        Assert.Equal(20000m, linea.CantidadBase);
        var persistida = await db.Ventas.AsNoTracking().Include(x => x.EstadosFlujo)
            .ThenInclude(x => x.FlujoEstado).SingleAsync(x => x.Id == antes.Encabezado.Id);
        Assert.Equal("VENTA_POS", persistida.EstadosFlujo.FlujoEstado.Codigo);
        Assert.Equal(persistida.FlujoEstadoId, persistida.EstadosFlujo.FlujoEstadoId);
        Assert.Equal(persistida.EstadoId, persistida.EstadosFlujo.Id);
        var saldosAntes = await SaldosAsync(db);
        var correlativosAntes = await CorrelativosAsync(db);

        var completar = await cliente.PostAsJsonAsync(ruta + "/completar", new CompletarVentaSolicitud {
            VersionFila = antes.Encabezado.VersionFila,
            ClaveOperacion = Guid.Parse("7e4cb0d2-b64f-4a59-a4e3-5d6c248b7202") });
        Assert.Equal(HttpStatusCode.Conflict, completar.StatusCode);
        var error = (await completar.Content.ReadFromJsonAsync<RespuestaError>())!;
        Assert.Equal("EXISTENCIA_INSUFICIENTE", error.Codigo);
        Assert.NotEqual("CONFLICTO_VENTAS", error.Codigo);
        Assert.DoesNotContain("La venta debe estar en CONFIRMADA", error.Mensaje);

        var despues = (await cliente.GetFromJsonAsync<VentaConsultaDto>(ruta))!;
        Assert.Equal(JsonSerializer.Serialize(antes), JsonSerializer.Serialize(despues));
        Assert.Equal("CONFIRMADA", despues.Encabezado.Estado);
        Assert.Null(despues.Encabezado.ClaveOperacion);
        Assert.Null(despues.Encabezado.FechaCompletada);
        Assert.Empty(despues.Movimientos);
        Assert.Equal(saldosAntes, await SaldosAsync(db));
        Assert.Equal(correlativosAntes, await CorrelativosAsync(db));
        Assert.Equal(18500m, (await db.Existencias.AsNoTracking().SingleAsync(x => x.Id == existencia.Id)).Cantidad);
        Assert.Empty(await db.Movimientos.AsNoTracking().ToArrayAsync());
        Assert.Empty(await db.MovimientoDetalles.AsNoTracking().ToArrayAsync());
        Assert.Empty(await db.Kardex.AsNoTracking().ToArrayAsync());
        Assert.Empty(await db.CostosProductos.AsNoTracking().ToArrayAsync());
        Assert.Empty(await db.HistoricoCostosProductos.AsNoTracking().ToArrayAsync());
        Assert.All(despues.Detalles, x => { Assert.Null(x.CostoUnitarioBase); Assert.Null(x.CostoTotal); });
    }

    private static async Task<string> SaldosAsync(HeladeriaDbContext db) => JsonSerializer.Serialize(
        await db.Existencias.AsNoTracking().OrderBy(x => x.Id).Select(x => new {
            x.Id, x.ProductoId, x.BodegaId, x.UbicacionId, x.Cantidad, x.CantidadReservada,
            x.CostoPromedio, x.UltimoMovimiento, x.VersionFila }).ToArrayAsync());

    private static async Task<string> CorrelativosAsync(HeladeriaDbContext db) => JsonSerializer.Serialize(
        await db.CorrelativosDocumentos.AsNoTracking().OrderBy(x => x.Id).Select(x => new {
            x.Id, x.SerieDocumentoId, x.UltimoNumero, x.FechaModificacion, x.VersionFila }).ToArrayAsync());
}
