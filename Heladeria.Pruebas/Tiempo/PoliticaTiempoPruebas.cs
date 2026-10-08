using System.Text.Json;
using Heladeria.Api.Configuracion;
using Heladeria.Aplicacion.Seguridad.Configuracion;
using Heladeria.Aplicacion.Tiempo;
using Heladeria.Infraestructura.Persistencia;
using Heladeria.Infraestructura.Persistencia.Entidades;
using Microsoft.EntityFrameworkCore;
using Heladeria.Pruebas.Fase6;
using Heladeria.Aplicacion.Produccion;

namespace Heladeria.Pruebas.Tiempo;

public sealed class PoliticaTiempoPruebas
{
    [Fact]
    public async Task ProduccionNormalizaPlanificacionYFiltraPorZonaSinTomarlaComoRelojOperativo()
    {
        await using var e = await EscenarioFase6.CrearAsync();
        var receta = await e.CrearRecetaAsync();
        var orden = await e.Produccion.CrearAsync(new CrearOrdenProduccionSolicitud {
            SucursalId = EscenarioFase6.Seguridad.SucursalId, BodegaProduccionId = 1,
            RecetaId = receta.Encabezado.Id, CantidadPlanificada = 1000,
            FechaInicio = new DateTime(2050, 10, 8, 14, 42, 0)
        }, EscenarioFase6.Seguridad, default);
        Assert.Equal(new DateTime(2050, 10, 8, 20, 42, 0, DateTimeKind.Utc), orden.Encabezado.FechaInicio);
        Assert.Equal(DateTimeKind.Utc, orden.Encabezado.FechaInicio.Kind);
        var lista = await e.Produccion.ListarAsync(new FiltroProduccionSolicitud {
            FechaDesde = new DateTime(2050, 10, 8, 14, 40, 0), FechaHasta = new DateTime(2050, 10, 8, 14, 45, 0)
        }, EscenarioFase6.Seguridad, default);
        Assert.Equal(orden.Encabezado.Id, Assert.Single(lista.Elementos).Id);
        var auditoria = await e.Contexto.Bitacoras.SingleAsync(x => x.Accion == "ORDEN_PRODUCCION_CREADA");
        Assert.NotEqual(orden.Encabezado.FechaInicio, auditoria.Fecha);
        Assert.Equal(DateTimeKind.Utc, auditoria.Fecha.Kind);
        Assert.Null(orden.Encabezado.FechaFin);
    }

    [Theory]
    [InlineData(typeof(Ventas), "FechaVenta")]
    [InlineData(typeof(Ventas), "FechaCompletada")]
    [InlineData(typeof(RecepcionesCompra), "FechaRecepcion")]
    [InlineData(typeof(Existencias), "UltimoMovimiento")]
    [InlineData(typeof(Kardex), "FechaMovimiento")]
    [InlineData(typeof(Movimientos), "FechaAplicacion")]
    [InlineData(typeof(OrdenesProduccion), "FechaInicio")]
    [InlineData(typeof(OrdenesProduccion), "FechaFin")]
    [InlineData(typeof(ConsumosProduccion), "FechaConsumo")]
    [InlineData(typeof(ProduccionTerminada), "FechaFinalizacion")]
    [InlineData(typeof(TurnosCaja), "FechaApertura")]
    [InlineData(typeof(TurnosCaja), "FechaCierre")]
    [InlineData(typeof(TurnosCaja), "FechaModificacion")]
    [InlineData(typeof(Pagos), "Fecha")]
    [InlineData(typeof(MovimientosCaja), "Fecha")]
    [InlineData(typeof(ArqueosCaja), "Fecha")]
    [InlineData(typeof(Bitacoras), "Fecha")]
    [InlineData(typeof(HistorialAccesos), "Fecha")]
    public void TimestampsRestauranKindUtcSinDesplazarValorPersistidoYEmitenZ(Type entidad, string campo)
    {
        using var db = new HeladeriaDbContext(new DbContextOptionsBuilder<HeladeriaDbContext>().UseSqlServer().Options);
        var conversion = db.Model.FindEntityType(entidad)!.FindProperty(campo)!.GetValueConverter();
        Assert.NotNull(conversion);
        var sql = new DateTime(2026, 10, 8, 20, 42, 15, 376, DateTimeKind.Unspecified);
        var materializada = Assert.IsType<DateTime>(conversion.ConvertFromProvider(sql));
        Assert.Equal(DateTimeKind.Utc, materializada.Kind);
        Assert.Equal(sql.Ticks, materializada.Ticks);
        Assert.Equal(sql.Ticks, Assert.IsType<DateTime>(conversion.ConvertToProvider(materializada)).Ticks);
        Assert.Null(conversion.ConvertFromProvider(null));
        var opciones = new JsonSerializerOptions { Converters = { new ConvertidorFechaApi() } };
        Assert.Equal("\"2026-10-08T20:42:15.376Z\"", JsonSerializer.Serialize(materializada, opciones));
    }

    [Fact]
    public void FechasCivilesYVigenciasNoSeConviertenEnInstantes()
    {
        using var db = new HeladeriaDbContext(new DbContextOptionsBuilder<HeladeriaDbContext>().UseSqlServer().Options);
        Assert.Null(db.Model.FindEntityType(typeof(OrdenesCompra))!.FindProperty("FechaEmision")!.GetValueConverter());
        Assert.Null(db.Model.FindEntityType(typeof(OrdenesCompra))!.FindProperty("FechaEntregaEsperada")!.GetValueConverter());
        Assert.Equal(typeof(DateOnly), db.Model.FindEntityType(typeof(Recetas))!.FindProperty("VigenteDesde")!.ClrType);
    }

    [Theory]
    [InlineData("2026-10-08T14:42:15.376-06:00", DateTimeKind.Utc, 20)]
    [InlineData("2026-10-08T20:42:15.376Z", DateTimeKind.Utc, 20)]
    [InlineData("2026-10-08T14:42:15.376", DateTimeKind.Unspecified, 14)]
    [InlineData("2026-10-08", DateTimeKind.Unspecified, 0)]
    public void ApiRespetaOffsetOFechaCivilSinUsarZonaDelHost(string iso, DateTimeKind kind, int hora)
    {
        var opciones = new JsonSerializerOptions { Converters = { new ConvertidorFechaApi() } };
        var valor = JsonSerializer.Deserialize<DateTime>($"\"{iso}\"", opciones);
        Assert.Equal(kind, valor.Kind); Assert.Equal(hora, valor.Hour);
    }

    [Fact]
    public void ZonaEfectivaPriorizaSucursalEmpresaInstalacionYSeValidaIana()
    {
        var opciones = new OpcionesSeguridad { ZonaHoraria = "America/Guatemala",
            ZonasHorariasEmpresas = new() { [9] = "Europe/Madrid" }, ZonasHorariasSucursales = new() { [14] = "America/New_York" } };
        Assert.Equal("America/New_York", TiempoEmpresarial.Zona(opciones, 9, 14));
        Assert.Equal("Europe/Madrid", TiempoEmpresarial.Zona(opciones, 9, 15));
        Assert.Equal("America/Guatemala", TiempoEmpresarial.Zona(opciones, 10, 16));
        Assert.True(TiempoEmpresarial.ZonaValida("America/Guatemala"));
        Assert.False(TiempoEmpresarial.ZonaValida("Zona/Inexistente"));
    }

    [Fact]
    public void HorasCivilesDeFiltrosYPlanificacionUsanZonaConfiguradaNoMaquina()
    {
        var civil = new DateTime(2026, 10, 8, 14, 42, 0);
        var utc = TiempoEmpresarial.AUTC(civil, "America/Guatemala");
        Assert.Equal(new DateTime(2026, 10, 8, 20, 42, 0, DateTimeKind.Utc), utc);
        Assert.Equal(utc, TiempoEmpresarial.AUTC(utc, "Europe/Madrid"));
        Assert.Throws<ArgumentException>(() => TiempoEmpresarial.AUTC(DateTime.SpecifyKind(civil, DateTimeKind.Local), "America/Guatemala"));
        Assert.Throws<ArgumentException>(() => TiempoEmpresarial.AUTC(new(2026, 3, 8, 2, 30, 0), "America/New_York"));
        Assert.Throws<ArgumentException>(() => TiempoEmpresarial.AUTC(new(2026, 11, 1, 1, 30, 0), "America/New_York"));
    }
}
