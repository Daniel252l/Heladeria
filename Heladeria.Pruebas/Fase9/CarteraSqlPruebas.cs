using Heladeria.Aplicacion.Caja;
using Heladeria.Aplicacion.Cartera;
using Heladeria.Aplicacion.Configuracion;
using Heladeria.Aplicacion.Seguridad.Bootstrap;
using Heladeria.Infraestructura.Caja;
using Heladeria.Infraestructura.Cartera;
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

namespace Heladeria.Pruebas.Fase9;

public sealed class CarteraSqlPruebas
{
    private static async Task<string> ScriptAsync()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "BaseDatos", "Heladeria.sql"))) dir = dir.Parent;
        return await File.ReadAllTextAsync(Path.Combine(dir!.FullName, "BaseDatos", "Reparaciones", "20261009_Fase9_CxC_CxP.sql"));
    }
    [Fact]
    public async Task EsquemaPrevioParcialRepetibleRollbackColumnasChecksIndicesYMaestro()
    {
        await using var aislada = await BaseSqlServerAislada.CrearAsync(incluirFase9: false);
        await using var db = new HeladeriaDbContext(new DbContextOptionsBuilder<HeladeriaDbContext>().UseSqlServer(aislada.CadenaConexion).Options);
        var sql = await ScriptAsync();
        await db.Database.ExecuteSqlRawAsync("ALTER TABLE dbo.OrdenesCompra ADD EsCredito bit NULL; ALTER TABLE dbo.Pagos ADD HuellaSolicitud varbinary(16) NULL;");
        Assert.Equal(51090, (await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlRawAsync(sql))).Number);
        Assert.Equal(0, await db.Database.SqlQuery<int>($"SELECT COUNT(*) AS Value FROM sys.columns WHERE object_id=OBJECT_ID(N'dbo.CuentasPorCobrar') AND name=N'FechaCreacion'").SingleAsync());
        await db.Database.ExecuteSqlRawAsync("ALTER TABLE dbo.Pagos ALTER COLUMN HuellaSolicitud varbinary(32) NULL;");
        await db.Database.ExecuteSqlRawAsync(sql); await db.Database.ExecuteSqlRawAsync(sql);
        Assert.Empty(await db.CuentasPorCobrar.ToArrayAsync()); Assert.Empty(await db.CuentasPorPagar.ToArrayAsync());
        Assert.Empty(await db.Pagos.ToArrayAsync()); Assert.Empty(await db.FacturasProveedores.ToArrayAsync());
        Assert.Empty(await db.Modulos.ToArrayAsync());
        Assert.Equal(2, await db.Database.SqlQuery<int>($"SELECT COUNT(*) AS Value FROM sys.indexes WHERE name IN ('UX_FacturasProveedores_ClaveOperacion','UX_FacturasProveedores_OrdenFase9') AND is_unique=1").SingleAsync());
        await db.Database.ExecuteSqlRawAsync("ALTER TABLE dbo.Pagos DROP CONSTRAINT CK_Pagos_Huella_Fase9; ALTER TABLE dbo.Pagos ADD CONSTRAINT CK_Pagos_Huella_Fase9 CHECK(MontoTotal>0);");
        Assert.Equal(51090, (await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlRawAsync(sql))).Number);
    }
    [Theory, InlineData(false, false), InlineData(false, true), InlineData(true, false), InlineData(true, true)]
    public async Task DosPagosSimultaneosContextosYCoordinadoresIndependientes(bool pagar, bool mismaClave)
    {
        await using var aislada = await BaseSqlServerAislada.CrearAsync();
        var opciones = new DbContextOptionsBuilder<HeladeriaDbContext>().UseSqlServer(aislada.CadenaConexion).Options;
        await using var db = new HeladeriaDbContext(opciones);
        var (s, cuenta, sesion, forma) = await PrepararAsync(db, pagar);
        var uuid = Guid.NewGuid();
        AplicarPagoCarteraSolicitud Solicitud(Guid clave) => new() { ClaveOperacion = clave, VersionFila = cuenta.Encabezado.VersionFila,
            SesionCajaId = sesion.Encabezado.Id, VersionCaja = sesion.Encabezado.VersionFila, FormaPagoId = forma, Monto = 100 };
        async Task<(ResultadoPagoCarteraDto? Resultado, Exception? Error)> Independiente(Guid clave)
        {
            await using var otro = new HeladeriaDbContext(opciones);
            var coord = new CoordinadorAjustesInventario();
            var caja = new ServicioCaja(otro, EscenarioFase5.Opciones, TimeProvider.System, coord);
            var cartera = new ServicioCartera(otro, EscenarioFase5.Opciones, TimeProvider.System, coord, caja);
            try { return (await cartera.AplicarAsync(pagar, cuenta.Encabezado.Id, Solicitud(clave), s, default), null); }
            catch (Exception ex) { return (null, ex); }
        }
        var resultados = await Task.WhenAll(Independiente(uuid), Independiente(mismaClave ? uuid : Guid.NewGuid()));
        Assert.Single(resultados, x => x.Resultado?.Reintento == false);
        if (mismaClave) { Assert.All(resultados, x => Assert.Null(x.Error)); Assert.Single(resultados, x => x.Resultado?.Reintento == true); }
        else Assert.Equal("CUENTA_YA_PAGADA", Assert.IsType<ExcepcionCartera>(Assert.Single(resultados, x => x.Error is not null).Error).Codigo);
        Assert.Single(await db.Pagos.AsNoTracking().ToArrayAsync()); Assert.Single(await db.PagoDetalles.ToArrayAsync());
        Assert.Equal(2, await db.MovimientosCaja.CountAsync());
        Assert.Equal(2, await db.CorrelativosDocumentos.Where(x => x.SerieDocumento.Serie == "MC").Select(x => x.UltimoNumero).SingleAsync());
        Assert.Equal(1, await db.Bitacoras.CountAsync(x => x.Accion == (pagar ? "PAGO_APLICADO" : "ABONO_APLICADO")));
        var (carteraFinal, cajaFinal) = Servicios(db);
        var final = await carteraFinal.ObtenerAsync(pagar, cuenta.Encabezado.Id, s, default);
        Assert.Equal(0, final.Encabezado.SaldoPendiente); Assert.Equal(100, final.Encabezado.TotalAplicado);
        Assert.Equal("PAGADA", final.Encabezado.Estado); Assert.Single(final.Historial);
        Assert.Equal(pagar ? 400 : 600, (await cajaFinal.ObtenerAsync(sesion.Encabezado.Id, s, default)).EfectivoEsperado);
        await db.Database.ExecuteSqlRawAsync(await ScriptAsync()); // Repetible también con operaciones de prueba.
    }
    [Theory, InlineData(false), InlineData(true)]
    public async Task CincoAbonosConcurrentesExigenVersionActualYReintentosExplicitosSinPerderSaldo(bool pagar)
    {
        await using var aislada = await BaseSqlServerAislada.CrearAsync();
        var opciones = new DbContextOptionsBuilder<HeladeriaDbContext>().UseSqlServer(aislada.CadenaConexion).Options;
        await using var db = new HeladeriaDbContext(opciones);
        var (s, cuenta, sesion, forma) = await PrepararAsync(db, pagar);
        async Task<bool> Aplicar(int i, bool recargar)
        {
            await using var otro = new HeladeriaDbContext(opciones);
            var (cartera, caja) = Servicios(otro);
            var c = recargar ? await cartera.ObtenerAsync(pagar, cuenta.Encabezado.Id, s, default) : cuenta;
            var ca = recargar ? await caja.ObtenerAsync(sesion.Encabezado.Id, s, default) : sesion;
            try
            {
                await cartera.AplicarAsync(pagar, cuenta.Encabezado.Id, new() { ClaveOperacion = new Guid(i, 0, 0, new byte[8]),
                    VersionFila = c.Encabezado.VersionFila, SesionCajaId = ca.Encabezado.Id, VersionCaja = ca.Encabezado.VersionFila,
                    Monto = 20, FormaPagoId = forma }, s, default); return true;
            }
            catch (ExcepcionCartera e) when (!recargar && e.Codigo == (pagar ? "CONCURRENCIA_CXP" : "CONCURRENCIA_CXC")) { return false; }
        }
        var resultado = await Task.WhenAll(Enumerable.Range(1, 5).Select(i => Aplicar(i, false)));
        Assert.Single(resultado, x => x);
        foreach (var i in Enumerable.Range(1, 5).Where(i => !resultado[i - 1])) Assert.True(await Aplicar(i, true));
        var (servicio, _) = Servicios(db); var final = await servicio.ObtenerAsync(pagar, cuenta.Encabezado.Id, s, default);
        Assert.Equal(0, final.Encabezado.SaldoPendiente); Assert.Equal(5, final.Historial.Count);
        Assert.Equal(5, await db.Pagos.CountAsync()); Assert.Equal(6, await db.MovimientosCaja.CountAsync());
        Assert.Equal(6, await db.CorrelativosDocumentos.Where(x => x.SerieDocumento.Serie == "MC").Select(x => x.UltimoNumero).SingleAsync());
        Assert.Equal(5, await db.Bitacoras.CountAsync(x => x.Accion == (pagar ? "PAGO_APLICADO" : "ABONO_APLICADO")));
    }
    private static (ServicioCartera, ServicioCaja) Servicios(HeladeriaDbContext db)
    {
        var coordinador = new CoordinadorAjustesInventario();
        var caja = new ServicioCaja(db, EscenarioFase5.Opciones, TimeProvider.System, coordinador);
        return (new ServicioCartera(db, EscenarioFase5.Opciones, TimeProvider.System, coordinador, caja), caja);
    }
    internal static async Task<(ContextoConfiguracion, DetalleCarteraDto, SesionCajaDto, int)> PrepararAsync(
        HeladeriaDbContext db, bool pagar, decimal importe = 100, decimal saldoInicial = 500)
    {
        // Todos los datos siguientes son fixtures en una instancia privada recién creada; nunca se leen appsettings.
        var config = EscenarioFase5.Opciones; var coord = new CoordinadorAjustesInventario();
        await new BootstrapAdministrador(db, new PasswordHasher<Usuarios>(), config, TimeProvider.System)
            .EjecutarAsync(new SolicitudBootstrapAdministrador("admin", "admin@prueba.local", "Prueba-Aislada9!"), default);
        await new PreparadorFase4(db, config, TimeProvider.System).EjecutarAsync(default);
        var inventario = new ServicioInventario(db, new AuditoriaFase4(db, TimeProvider.System), config, TimeProvider.System, coord);
        await new PreparadorFase5(db, config, TimeProvider.System, coord).EjecutarAsync(default);
        await new PreparadorFase7(db, config, TimeProvider.System, coord, inventario).EjecutarAsync(default);
        await new PreparadorFase8(db, config, TimeProvider.System, coord).EjecutarAsync(default);
        await new PreparadorFase9(db, config, TimeProvider.System, coord).EjecutarAsync(default);
        Assert.Empty((await new PreparadorFase9(db, config, TimeProvider.System, coord).EjecutarAsync(default)).Registros);
        var a = await db.UsuariosSucursales.Include(x => x.Sucursal).SingleAsync();
        var s = new ContextoConfiguracion(a.UsuarioId, "ADMIN", a.Sucursal.EmpresaId, a.SucursalId, null, null);
        var activo = await db.Estados.Where(x => x.Nombre == "Activo").Select(x => x.Id).SingleAsync();
        var moneda = new Monedas { EmpresaId = s.EmpresaId, CodigoISO = "GTQ", NombreMoneda = "Quetzal", Simbolo = "Q", EsMonedaBase = true, Decimales = 2, EstadoId = activo };
        var cliente = new Clientes { EmpresaId = s.EmpresaId, CodigoCliente = "CLIENTE", NombreCliente = "Cliente aislado", EstadoId = activo };
        db.AddRange(moneda, cliente); await db.SaveChangesAsync();
        var financiero = await db.EstadosFlujo.SingleAsync(x => x.FlujoEstado.Codigo == (pagar ? "CUENTAS_POR_PAGAR" : "CUENTAS_POR_COBRAR") && x.Codigo == "PENDIENTE");
        var hoy = new DateOnly(2026, 10, 9); long id;
        if (pagar)
        {
            var recibido = await db.EstadosFlujo.SingleAsync(x => x.FlujoEstado.Codigo == "ORDENES_COMPRA" && x.Codigo == "RECIBIDA");
            var proveedor = await db.Proveedores.Select(x => x.Id).SingleAsync();
            var orden = new OrdenesCompra { EmpresaId = s.EmpresaId, SucursalId = s.SucursalId, ProveedorId = proveedor,
                NumeroOrden = "OC-AISLADA", FechaEmision = hoy.ToDateTime(TimeOnly.MinValue), MonedaId = moneda.Id,
                TipoCambio = 1, SubTotal = importe, TotalEstimado = importe, EsCredito = true,
                FlujoEstadoId = recibido.FlujoEstadoId, EstadoId = recibido.Id, UsuarioCreacionId = s.UsuarioId };
            var factura = new FacturasProveedores { EmpresaId = s.EmpresaId, ProveedorId = proveedor, OrdenCompra = orden,
                NumeroDocumento = "FACTURA-AISLADA", FechaDocumento = hoy, FechaVencimiento = hoy.AddDays(30), MonedaId = moneda.Id,
                TipoCambio = 1, SubTotal = importe, Total = importe, EstadoId = activo };
            var cuenta = new CuentasPorPagar { EmpresaId = s.EmpresaId, SucursalId = s.SucursalId, ProveedorId = proveedor,
                FacturaProveedor = factura, MonedaId = moneda.Id, FechaEmision = hoy, FechaVencimiento = hoy.AddDays(30),
                MontoOriginal = importe, SaldoPendiente = importe, FlujoEstadoId = financiero.FlujoEstadoId, EstadoId = financiero.Id,
                UsuarioCreacionId = s.UsuarioId, FechaCreacion = TimeProvider.System.GetUtcNow().UtcDateTime };
            db.CuentasPorPagar.Add(cuenta); await db.SaveChangesAsync(); id = cuenta.Id;
        }
        else
        {
            var completada = await db.EstadosFlujo.SingleAsync(x => x.FlujoEstado.Codigo == "VENTA_POS" && x.Codigo == "COMPLETADA");
            var venta = new Ventas { EmpresaId = s.EmpresaId, SucursalId = s.SucursalId, ClienteId = cliente.Id,
                NumeroFactura = "VT-AISLADA", FechaVenta = TimeProvider.System.GetUtcNow().UtcDateTime, MonedaId = moneda.Id,
                TipoCambio = 1, SubTotal = importe, TotalVenta = importe, EsCredito = true, FechaVencimientoCredito = hoy.AddDays(30),
                FlujoEstadoId = completada.FlujoEstadoId, EstadoId = completada.Id, UsuarioCreacionId = s.UsuarioId,
                FechaCompletada = TimeProvider.System.GetUtcNow().UtcDateTime, CompletadoPorUsuarioId = s.UsuarioId, ClaveOperacion = Guid.NewGuid(),
                BodegaId = await db.Bodegas.Select(x => x.Id).SingleAsync(), UbicacionId = await db.Ubicaciones.Select(x => x.Id).SingleAsync() };
            var cuenta = new CuentasPorCobrar { EmpresaId = s.EmpresaId, SucursalId = s.SucursalId, ClienteId = cliente.Id,
                Venta = venta, MonedaId = moneda.Id, FechaEmision = hoy, FechaVencimiento = hoy.AddDays(30), MontoOriginal = importe,
                SaldoPendiente = importe, FlujoEstadoId = financiero.FlujoEstadoId, EstadoId = financiero.Id,
                UsuarioCreacionId = s.UsuarioId, FechaCreacion = TimeProvider.System.GetUtcNow().UtcDateTime };
            db.CuentasPorCobrar.Add(cuenta); await db.SaveChangesAsync(); id = cuenta.Id;
        }
        db.ChangeTracker.Clear(); var (cartera, caja) = Servicios(db);
        var cajaFisica = await caja.CrearCajaAsync(new() { Codigo = "AISLADA", Nombre = "Solo fixture desechable" }, s, default);
        var sesion = await caja.AbrirAsync(new() { CajaId = cajaFisica.Id, MonedaId = moneda.Id, SaldoInicial = saldoInicial }, s, default);
        var forma = await db.FormasPago.Where(x => x.Codigo == "EFECTIVO").Select(x => x.Id).SingleAsync();
        return (s, await cartera.ObtenerAsync(pagar, id, s, default), sesion, forma);
    }
}
