using Heladeria.Aplicacion.Caja;
using Heladeria.Aplicacion.Configuracion;
using Heladeria.Aplicacion.Seguridad.Bootstrap;
using Heladeria.Infraestructura.Caja;
using Heladeria.Infraestructura.Fase4;
using Heladeria.Infraestructura.Inventario;
using Heladeria.Infraestructura.Persistencia;
using Heladeria.Infraestructura.Persistencia.Entidades;
using Heladeria.Infraestructura.Seguridad.Bootstrap;
using Heladeria.Pruebas.Fase5;
using Heladeria.Pruebas.Integracion.Persistencia;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Heladeria.Pruebas.Fase8;

public sealed class CajaSqlPruebas
{
    private static async Task<string> ScriptAsync()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "BaseDatos", "Heladeria.sql"))) dir = dir.Parent;
        return await File.ReadAllTextAsync(Path.Combine(dir!.FullName, "BaseDatos", "Reparaciones", "20261007_Fase8_Caja.sql"));
    }
    [Fact]
    public async Task SqlPrevioParcialRepetibleYRollbackAnteIncompatibilidadSinDatosFicticios()
    {
        await using var aislada = await BaseSqlServerAislada.CrearAsync(incluirFase8: false);
        await using var db = new HeladeriaDbContext(new DbContextOptionsBuilder<HeladeriaDbContext>().UseSqlServer(aislada.CadenaConexion).Options);
        var script = await ScriptAsync();
        // Simular preparación parcial e incompatible, exclusivamente dentro de la base desechable recién creada.
        await db.Database.ExecuteSqlRawAsync("ALTER TABLE dbo.TurnosCaja ADD MonedaId int NULL; ALTER TABLE dbo.PagoDetalles ADD MontoRecibido decimal(10,2) NULL;");
        var error = await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlRawAsync(script));
        Assert.Equal(51080, error.Number);
        Assert.Equal(0, await db.Database.SqlQuery<int>($"SELECT COUNT(*) AS Value FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.TurnosCaja') AND name=N'NumeroApertura'").SingleAsync());
        await db.Database.ExecuteSqlRawAsync("ALTER TABLE dbo.PagoDetalles ALTER COLUMN MontoRecibido decimal(12,2) NULL;");
        await db.Database.ExecuteSqlRawAsync(script); await db.Database.ExecuteSqlRawAsync(script);
        Assert.Empty(await db.TurnosCaja.ToArrayAsync()); Assert.False(await db.Pagos.AnyAsync()); // Esquema intencionalmente F8: no proyectar columnas agregadas en F9.
        Assert.Empty(await db.MovimientosCaja.ToArrayAsync()); Assert.Empty(await db.Cajas.ToArrayAsync());
        Assert.Empty(await db.ArqueosCaja.ToArrayAsync()); Assert.Empty(await db.PagoDetalles.ToArrayAsync());
        Assert.Empty(await db.FormasPago.ToArrayAsync());
        Assert.Equal(4, await db.Database.SqlQuery<int>($"SELECT COUNT(*) AS Value FROM sys.indexes WHERE name IN (N'UX_TurnosCaja_CajaAbierta',N'UX_Pagos_Venta',N'UX_Pagos_ClaveOperacion',N'UX_MovimientosCaja_PagoDetalle') AND is_unique=1 AND is_disabled=0").SingleAsync());
        await db.Database.ExecuteSqlRawAsync("DROP INDEX UX_Pagos_Venta ON dbo.Pagos; CREATE UNIQUE INDEX UX_Pagos_Venta ON dbo.Pagos(UsuarioId) WHERE VentaId IS NOT NULL;");
        Assert.Equal(51080, (await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlRawAsync(script))).Number);
    }
    [Fact]
    public async Task SqlRealAperturaCobroYCierreConcurrentesConRowVersionYBloqueosIndependientes()
    {
        // El helper crea una instancia LocalDB privada; no usa conexión ni configuración de la aplicación.
        await using var aislada = await BaseSqlServerAislada.CrearAsync();
        var opciones = new DbContextOptionsBuilder<HeladeriaDbContext>().UseSqlServer(aislada.CadenaConexion).Options;
        await using var db = new HeladeriaDbContext(opciones);
        var configuracion = EscenarioFase5.Opciones;
        var coordinador = new CoordinadorAjustesInventario();
        var bootstrap = new BootstrapAdministrador(db, new PasswordHasher<Usuarios>(), configuracion, TimeProvider.System);
        await bootstrap.EjecutarAsync(new SolicitudBootstrapAdministrador("admin", "admin@prueba.local", "Prueba-Aislada9!"), default);
        await new PreparadorFase4(db, configuracion, TimeProvider.System).EjecutarAsync(default);
        var inventario = new ServicioInventario(db, new AuditoriaFase4(db, TimeProvider.System), configuracion, TimeProvider.System, coordinador);
        await new PreparadorFase7(db, configuracion, TimeProvider.System, coordinador, inventario).EjecutarAsync(default);
        var asignacion = await db.UsuariosSucursales.Include(x => x.Sucursal).SingleAsync();
        var s = new ContextoConfiguracion(asignacion.UsuarioId, "ADMIN", asignacion.Sucursal.EmpresaId, asignacion.SucursalId, null, null);
        var activo = await db.Estados.Where(x => x.Nombre == "Activo").Select(x => x.Id).SingleAsync();
        var moneda = new Monedas { EmpresaId = s.EmpresaId, CodigoISO = "GTQ", NombreMoneda = "Quetzal", Simbolo = "Q", EsMonedaBase = true, Decimales = 2, EstadoId = activo };
        db.Monedas.Add(moneda); await db.SaveChangesAsync();
        await new PreparadorFase8(db, configuracion, TimeProvider.System, coordinador).EjecutarAsync(default);
        Assert.Empty((await new PreparadorFase8(db, configuracion, TimeProvider.System, coordinador).EjecutarAsync(default)).Registros);
        var servicio = new ServicioCaja(db, configuracion, TimeProvider.System, coordinador);
        var caja = await servicio.CrearCajaAsync(new() { Codigo = "AISLADA", Nombre = "Solo pruebas desechables" }, s, default);
        async Task<(T? Resultado, Exception? Error)> Independiente<T>(Func<ServicioCaja, Task<T>> operar)
        {
            await using var otro = new HeladeriaDbContext(opciones);
            // Coordinadores distintos: la exclusión depende de SQL Server, no de un semáforo compartido en memoria.
            var cajaServicio = new ServicioCaja(otro, configuracion, TimeProvider.System, new CoordinadorAjustesInventario());
            try { return (await operar(cajaServicio), null); } catch (Exception ex) { return (default, ex); }
        }
        var abrir = new AbrirCajaSolicitud { CajaId = caja.Id, MonedaId = moneda.Id, SaldoInicial = 100 };
        var aperturas = await Task.WhenAll(Independiente(x => x.AbrirAsync(abrir, s, default)), Independiente(x => x.AbrirAsync(abrir, s, default)));
        var sesion = Assert.Single(aperturas, x => x.Error is null).Resultado!;
        Assert.Equal("CAJA_YA_ABIERTA", Assert.IsType<ExcepcionCaja>(Assert.Single(aperturas, x => x.Error is not null).Error).Codigo);
        Assert.Equal(8, Convert.FromBase64String(sesion.Encabezado.VersionFila).Length);
        var estado = await db.EstadosFlujo.SingleAsync(x => x.FlujoEstado.Codigo == "VENTA_POS" && x.Codigo == "COMPLETADA");
        // Documento comercial de fixture. No inventario: esta prueba verifica únicamente transacción financiera.
        var venta = new Ventas { EmpresaId = s.EmpresaId, SucursalId = s.SucursalId, NumeroFactura = "PRUEBA-SQL-1",
            FechaVenta = DateTime.UtcNow, MonedaId = moneda.Id, TipoCambio = 1, SubTotal = 80, TotalVenta = 80,
            FlujoEstadoId = estado.FlujoEstadoId, EstadoId = estado.Id, UsuarioCreacionId = s.UsuarioId,
            BodegaId = await db.Bodegas.Select(x => x.Id).SingleAsync(), UbicacionId = await db.Ubicaciones.Select(x => x.Id).SingleAsync(), FechaCompletada = DateTime.UtcNow,
            CompletadoPorUsuarioId = s.UsuarioId, ClaveOperacion = Guid.NewGuid() };
        db.Ventas.Add(venta); await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        var efectivo = await db.FormasPago.Where(x => x.Codigo == "EFECTIVO").Select(x => x.Id).SingleAsync();
        var cobrar = new CobrarVentaSolicitud { VentaId = venta.Id, VersionVenta = Convert.ToBase64String(venta.VersionFila),
            VersionFila = sesion.Encabezado.VersionFila, ClaveOperacion = Guid.NewGuid(), Pagos = [new() { FormaPagoId = efectivo, MontoAplicado = 80, MontoRecibido = 100 }] };
        var cobros = await Task.WhenAll(Independiente(x => x.CobrarAsync(sesion.Encabezado.Id, cobrar, s, default)), Independiente(x => x.CobrarAsync(sesion.Encabezado.Id, cobrar, s, default)));
        Assert.All(cobros, x => Assert.Null(x.Error)); Assert.Single(cobros, x => x.Resultado!.Reintento);
        sesion = cobros[0].Resultado!.Sesion; Assert.Equal(180, sesion.EfectivoEsperado);
        Assert.NotEqual(abrir.SaldoInicial, sesion.EfectivoEsperado);
        Assert.Single(await db.Pagos.AsNoTracking().ToArrayAsync()); Assert.Single(await db.PagoDetalles.AsNoTracking().ToArrayAsync());
        Assert.Equal(2, await db.MovimientosCaja.CountAsync());
        var arqueo = await servicio.ArqueoAsync(sesion.Encabezado.Id, new() { VersionFila = sesion.Encabezado.VersionFila, EfectivoContado = 179 }, s, default);
        Assert.NotEqual(sesion.Encabezado.VersionFila, arqueo.Encabezado.VersionFila);
        var cerrar = new CerrarCajaSolicitud { VersionFila = arqueo.Encabezado.VersionFila, ArqueoId = arqueo.Arqueos.Single().Id };
        var cierres = await Task.WhenAll(Independiente(x => x.CerrarAsync(sesion.Encabezado.Id, cerrar, s, default)), Independiente(x => x.CerrarAsync(sesion.Encabezado.Id, cerrar, s, default)));
        var final = Assert.Single(cierres, x => x.Error is null).Resultado!;
        Assert.Equal("CAJA_NO_ABIERTA", Assert.IsType<ExcepcionCaja>(Assert.Single(cierres, x => x.Error is not null).Error).Codigo);
        Assert.Equal(-1, final.Encabezado.DiferenciaCierre); Assert.Equal("CERRADA", final.Encabezado.Estado);
        Assert.Equal(-1, (await db.TurnosCaja.AsNoTracking().SingleAsync()).Diferencia);
        Assert.Equal(-1, (await db.ArqueosCaja.AsNoTracking().SingleAsync()).Diferencia);
        await db.Database.ExecuteSqlRawAsync(await ScriptAsync()); // Repetición también con operaciones reales de prueba.
    }
}
