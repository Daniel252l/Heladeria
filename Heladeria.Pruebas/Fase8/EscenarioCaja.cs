using Heladeria.Aplicacion.Caja;
using Heladeria.Aplicacion.Configuracion;
using Heladeria.Aplicacion.Ventas;
using Heladeria.Infraestructura.Caja;
using Heladeria.Infraestructura.Inventario;
using Heladeria.Infraestructura.Persistencia;
using Heladeria.Infraestructura.Seguridad.Bootstrap;
using Heladeria.Pruebas.Fase4;
using Heladeria.Pruebas.Fase5;
using Heladeria.Pruebas.Fase7;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Heladeria.Pruebas.Fase8;

internal sealed class EscenarioCaja : IAsyncDisposable
{
    private readonly SqliteConnection _conexion;
    private EscenarioCaja(SqliteConnection conexion, EscenarioFase7 ventas, CoordinadorAjustesInventario coordinador)
    {
        _conexion = conexion; Ventas = ventas; Contexto = ventas.Contexto;
        Servicio = new ServicioCaja(Contexto, EscenarioFase5.Opciones, TimeProvider.System, coordinador);
        Preparador = new PreparadorFase8(Contexto, EscenarioFase5.Opciones, TimeProvider.System, coordinador);
    }
    internal static ContextoConfiguracion Seguridad => EscenarioFase7.Seguridad;
    internal HeladeriaDbContext Contexto { get; }
    internal EscenarioFase7 Ventas { get; }
    internal ServicioCaja Servicio { get; }
    internal PreparadorFase8 Preparador { get; }
    internal static async Task<EscenarioCaja> CrearAsync(bool preparar = true)
    {
        var conexion = new SqliteConnection("Data Source=:memory:"); await conexion.OpenAsync();
        var db = new ContextoSqlitePruebas(new DbContextOptionsBuilder<HeladeriaDbContext>().UseSqlite(conexion).Options);
        await db.Database.EnsureCreatedAsync();
        // Emulación exclusiva de pruebas. Las pruebas LocalDB verifican rowversion nativo y applock.
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TRIGGER VersionCaja AFTER UPDATE ON TurnosCaja
            WHEN NEW.VersionFila=OLD.VersionFila
            BEGIN UPDATE TurnosCaja SET VersionFila=randomblob(8) WHERE Id=NEW.Id; END;
            """);
        var coordinador = new CoordinadorAjustesInventario();
        var ventas = new EscenarioFase7(new EscenarioFase5(await EscenarioFase4.CrearConContextoAsync(db, coordinador), coordinador));
        await ventas.SemillarAsync(prepararCaja: false);
        var e = new EscenarioCaja(conexion, ventas, coordinador);
        if (preparar) await e.Preparador.EjecutarAsync(default);
        db.ChangeTracker.Clear(); return e;
    }
    internal async Task<SesionCajaDto> AbrirAsync(decimal monto = 100, string codigo = "PRUEBA")
    {
        var caja = await Servicio.CrearCajaAsync(new() { Codigo = codigo, Nombre = "Caja de pruebas aisladas" }, Seguridad, default);
        return await Servicio.AbrirAsync(new() { CajaId = caja.Id, MonedaId = 1, SaldoInicial = monto }, Seguridad, default);
    }
    // La cobranza separada ahora se ejercita sobre ventas históricas ya completadas sin pago.
    internal async Task<VentaConsultaDto> VentaAsync() => await Ventas.HistoricaSinCobroAsync(await Ventas.ConfirmarAsync());
    internal async Task<CobrarVentaSolicitud> SolicitudCobroAsync(SesionCajaDto sesion, VentaConsultaDto venta,
        decimal efectivo = 50, decimal tarjeta = 0, decimal? recibido = null, Guid? clave = null)
    {
        var formas = (await Servicio.ReferenciasAsync(Seguridad, default)).FormasPago;
        var pagos = new List<FormaCobroSolicitud>();
        if (efectivo > 0) pagos.Add(new() { FormaPagoId = formas.Single(x => x.Codigo == "EFECTIVO").Id, MontoAplicado = efectivo, MontoRecibido = recibido ?? efectivo });
        if (tarjeta > 0) pagos.Add(new() { FormaPagoId = formas.Single(x => x.Codigo == "TARJETA").Id, MontoAplicado = tarjeta, MontoRecibido = tarjeta, Referencia = "AUT-TEST" });
        return new() { VentaId = venta.Encabezado.Id, VersionVenta = venta.Encabezado.VersionFila,
            VersionFila = sesion.Encabezado.VersionFila, ClaveOperacion = clave ?? Guid.NewGuid(), Pagos = pagos.ToArray() };
    }
    internal Task<SesionCajaDto> MovimientoAsync(SesionCajaDto sesion, string tipo = "INGRESO", decimal monto = 10) =>
        Servicio.MovimientoAsync(sesion.Encabezado.Id, new() { VersionFila = sesion.Encabezado.VersionFila,
            Tipo = tipo, Monto = monto, Motivo = "Movimiento de prueba" }, Seguridad, default);
    internal Task<SesionCajaDto> ArqueoAsync(SesionCajaDto sesion, decimal? contado = null) => Servicio.ArqueoAsync(sesion.Encabezado.Id,
        new() { VersionFila = sesion.Encabezado.VersionFila, EfectivoContado = contado ?? sesion.EfectivoEsperado!.Value }, Seguridad, default);
    internal Task<SesionCajaDto> CerrarAsync(SesionCajaDto sesion) => Servicio.CerrarAsync(sesion.Encabezado.Id,
        new() { VersionFila = sesion.Encabezado.VersionFila, ArqueoId = sesion.Arqueos.Last().Id }, Seguridad, default);
    public async ValueTask DisposeAsync() { await Ventas.DisposeAsync(); await _conexion.DisposeAsync(); }
}
