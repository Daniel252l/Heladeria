using System.Security.Cryptography;
using System.Text;
using Heladeria.Bootstrap.Demo;
using Heladeria.Aplicacion.Configuracion;
using Heladeria.Aplicacion.Seguridad.Bootstrap;
using Heladeria.Aplicacion.Seguridad.Configuracion;
using Heladeria.Infraestructura;
using Heladeria.Infraestructura.Caja;
using Heladeria.Infraestructura.Inventario;
using Heladeria.Infraestructura.Persistencia;
using Heladeria.Infraestructura.ReportesOperativos;
using Heladeria.Pruebas.Integracion.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Heladeria.Pruebas.Demo;

public sealed class DatosDemoPruebas
{
    private static readonly IOptions<OpcionesSeguridad> Config = Options.Create(new OpcionesSeguridad());
    private sealed class RelojFijo(DateTimeOffset valor) : TimeProvider { public override DateTimeOffset GetUtcNow() => valor; }
    private static readonly TimeProvider Reloj = new RelojFijo(new DateTimeOffset(2026, 11, 3, 6, 0, 1, TimeSpan.Zero));
    [Theory]
    [InlineData("Production", false, false)]
    [InlineData("Staging", false, false)]
    [InlineData("", false, false)]
    [InlineData("Development", false, true)]
    [InlineData("Production", true, true)]
    public void ProteccionDeAmbiente(string ambiente, bool confirmar, bool permitido)
    {
        if (permitido) CargadorDatosDemo.ValidarAmbiente(ambiente, confirmar);
        else Assert.Throws<DemoRechazadaException>(() => CargadorDatosDemo.ValidarAmbiente(ambiente, confirmar));
    }
    [Theory, InlineData(false), InlineData(true)]
    public async Task InstalacionLimpiaCompletaConservaSeguridadSesionExistenteYRechazaDuplicado(bool sesionExistente)
    {
        var reloj = sesionExistente ? new RelojFijo(new DateTimeOffset(2026,11,3,23,0,0,TimeSpan.Zero)) : Reloj;
        await using var aislada = await BaseSqlServerAislada.CrearAsync();
        await PrepararAsync(aislada.CadenaConexion);
        await using var lectura = Normal(aislada.CadenaConexion);
        var s = await ContextoAsync(lectura);
        var servicioCaja = new ServicioCaja(lectura, Config, reloj, new());
        string? antesSesion = null; long? sesion = null;
        if (sesionExistente)
        {
            var activo = await lectura.Estados.Where(x => x.Nombre == "Activo").Select(x => x.Id).SingleAsync();
            var gtq = new Heladeria.Infraestructura.Persistencia.Entidades.Monedas { EmpresaId = s.EmpresaId, CodigoISO = "GTQ",
                NombreMoneda = "Quetzal", Simbolo = "Q", EsMonedaBase = true, Decimales = 2, EstadoId = activo };
            lectura.Monedas.Add(gtq); await lectura.SaveChangesAsync(); lectura.ChangeTracker.Clear();
            var fisica = await servicioCaja.CrearCajaAsync(new() { Codigo = "PREEXISTENTE", Nombre = "Caja preexistente aislada" }, s, default);
            var abierta = await servicioCaja.AbrirAsync(new() { CajaId = fisica.Id, MonedaId = gtq.Id, SaldoInicial = 58 }, s, default);
            sesion = abierta.Encabezado.Id;
            antesSesion = System.Text.Json.JsonSerializer.Serialize(abierta);
        }
        var protegidas = new[] { "Usuarios","UsuariosSucursales","Roles","RolesPermisos","Permisos","SesionesUsuarios",
            "HistorialAccesos","Empresas","Sucursales","Tipos","ClaseTipos","Estados","ClaseEstados","FlujosEstados","EstadosFlujo","EstadoTransiciones","FormasPago" };
        var huellas = await HuellasAsync(lectura, protegidas);
        await using (var demo = CargadorDatosDemo.CrearContexto(aislada.CadenaConexion))
        {
            var r = await new CargadorDatosDemo(demo, Config, reloj).EjecutarAsync("Development", false);
            Assert.Equal(35, r.Productos); Assert.Equal(10, r.Compras); Assert.Equal(38, r.Ventas);
            Assert.Equal(new DateOnly(2026,11,3), r.Hasta); Assert.Equal(r.Hasta.AddDays(-13), r.Desde);
        }
        Assert.Equal(huellas, await HuellasAsync(lectura, protegidas));
        if (sesion.HasValue)
            Assert.Equal(antesSesion, System.Text.Json.JsonSerializer.Serialize(await servicioCaja.ObtenerAsync(sesion.Value, s, default)));
        Assert.Equal(35, await lectura.Productos.CountAsync());
        Assert.Equal(5, await lectura.Productos.CountAsync(x => x.EsCombo));
        Assert.Equal(6, await lectura.Categorias.CountAsync());
        Assert.Equal(63, await lectura.ProductoPresentaciones.CountAsync());
        Assert.Equal(5, await lectura.Proveedores.CountAsync(x => x.CodigoProveedor.StartsWith("DEMO-")));
        Assert.Equal(12, await lectura.Clientes.CountAsync());
        Assert.Equal(10, await lectura.OrdenesCompra.CountAsync());
        Assert.Equal(9, await lectura.OrdenesCompra.CountAsync(x => x.EstadosFlujo.Codigo == "RECIBIDA"));
        Assert.Equal(1, await lectura.OrdenesCompra.CountAsync(x => x.EstadosFlujo.Codigo == "PARCIALMENTE_RECIBIDA"));
        Assert.Equal(36, await lectura.Ventas.CountAsync(x => x.EstadosFlujo.Codigo == "COMPLETADA"));
        Assert.Equal(2, await lectura.Ventas.CountAsync(x => x.EstadosFlujo.Codigo == "ANULADA"));
        Assert.Equal(14, await lectura.TurnosCaja.CountAsync(x => x.Caja.Codigo == CargadorDatosDemo.CodigoCaja && x.FechaCierre != null && x.Diferencia == 0));
        Assert.Equal(sesionExistente ? 1 : 0, await lectura.TurnosCaja.CountAsync(x => x.FechaCierre == null));
        Assert.Equal(6, await lectura.CuentasPorCobrar.CountAsync());
        Assert.Equal(2, await lectura.CuentasPorCobrar.CountAsync(x => x.EstadosFlujo.Codigo == "PAGADA"));
        Assert.Equal(1, await lectura.CuentasPorCobrar.CountAsync(x => x.EstadosFlujo.Codigo == "PARCIAL"));
        Assert.Equal(3, await lectura.CuentasPorCobrar.CountAsync(x => x.EstadosFlujo.Codigo == "PENDIENTE"));
        Assert.Equal(3, await lectura.CuentasPorPagar.CountAsync(x => x.EstadosFlujo.Codigo == "PAGADA"));
        Assert.Equal(1, await lectura.CuentasPorPagar.CountAsync(x => x.EstadosFlujo.Codigo == "PARCIAL"));
        Assert.Equal(2, await lectura.CuentasPorPagar.CountAsync(x => x.EstadosFlujo.Codigo == "PENDIENTE"));
        Assert.Equal(6, await lectura.FacturasProveedores.CountAsync());
        var stock = await lectura.Existencias.AsNoTracking().ToArrayAsync();
        Assert.Equal(30, stock.Length); Assert.All(stock, x => { Assert.True(x.Cantidad >= 0); Assert.True(x.CostoPromedio > 0); });
        Assert.Equal(30, await lectura.ParametrosStockProductos.CountAsync());
        Assert.True(await lectura.Existencias.CountAsync(x => x.Productos.ParametrosStockProductos
            .Any(p => p.BodegaId == x.BodegaId && p.StockMinimo >= x.Cantidad)) >= 3);
        foreach (var e in stock)
        {
            var k = await lectura.Kardex.AsNoTracking().Where(x => x.ProductoId == e.ProductoId).OrderBy(x => x.FechaMovimiento).ThenBy(x => x.Id).ToArrayAsync();
            Assert.Equal(e.Cantidad, k.Sum(x => x.TipoAfectacion == "ENTRADA" ? x.Cantidad : -x.Cantidad));
            Assert.Equal(e.Cantidad, k.Last().SaldoCantidad);
        }
        Assert.True(await lectura.MovimientoDetalles.AnyAsync(x => x.VentaDetalleId != null && x.VentaDetalle!.EsCompuesto == true));
        Assert.All(await lectura.Ventas.Where(x => x.EstadosFlujo.Codigo == "COMPLETADA").Select(x => new { x.TotalVenta, Costo=x.VentasDetalle.Sum(d => d.CostoTotal) }).ToArrayAsync(),
            x => Assert.True(x.TotalVenta > x.Costo));
        foreach (var numeros in new[] {
            await lectura.Ventas.Select(x => x.NumeroFactura).ToArrayAsync(),
            await lectura.OrdenesCompra.Select(x => x.NumeroOrden).ToArrayAsync(),
            await lectura.MovimientosCaja.Select(x => x.NumeroDocumento!).ToArrayAsync() })
            Assert.Equal(numeros.Length, numeros.Distinct().Count());
        Assert.False(await lectura.Ventas.AnyAsync(x => x.FechaCompletada > reloj.GetUtcNow().UtcDateTime));
        var reporte = await new ServicioReportes(lectura, Config, reloj).DiarioAsync(new() { Fecha = new(2026,11,3) }, s, default);
        Assert.Equal(11, reporte.Ventas.TotalRegistros);
        Assert.Equal(2, reporte.CreditosGenerados.Sum(x => x.Cantidad));
        Assert.Single(await lectura.Bitacoras.Where(x => x.Accion == CargadorDatosDemo.Marca).ToArrayAsync());
        var todas = await TodasLasHuellasAsync(lectura);
        await using var segunda = CargadorDatosDemo.CrearContexto(aislada.CadenaConexion);
        Assert.Contains("ya fueron cargados", (await Assert.ThrowsAsync<DemoRechazadaException>(() =>
            new CargadorDatosDemo(segunda, Config, reloj).EjecutarAsync("Development", false))).Message);
        Assert.Equal(todas, await TodasLasHuellasAsync(lectura));
    }
    [Theory, InlineData("COMPRAS_INICIALES"), InlineData("VALIDADO")]
    public async Task FalloTardioRevierteTodoIncluidosCorrelativosAuditoriaYPermiteReintentar(string etapa)
    {
        await using var aislada = await BaseSqlServerAislada.CrearAsync();
        await PrepararAsync(aislada.CadenaConexion);
        await using var lectura = Normal(aislada.CadenaConexion);
        var antes = await TodasLasHuellasAsync(lectura);
        await using (var demo = CargadorDatosDemo.CrearContexto(aislada.CadenaConexion))
            await Assert.ThrowsAsync<OperationCanceledException>(() => new CargadorDatosDemo(demo, Config, Reloj, e => {
                if (e == etapa) throw new OperationCanceledException("Cancelación controlada exclusivamente en fixture.");
            }).EjecutarAsync("Development", false));
        Assert.Equal(antes, await TodasLasHuellasAsync(lectura));
        await using var reintento = CargadorDatosDemo.CrearContexto(aislada.CadenaConexion);
        Assert.Equal(38, (await new CargadorDatosDemo(reintento, Config, Reloj).EjecutarAsync("Development", false)).Ventas);
    }
    [Fact]
    public async Task SinBootstrapSeRechazaSinCrearSeguridadNiDatos()
    {
        await using var aislada = await BaseSqlServerAislada.CrearAsync();
        await using var demo = CargadorDatosDemo.CrearContexto(aislada.CadenaConexion);
        await Assert.ThrowsAsync<DemoRechazadaException>(() => new CargadorDatosDemo(demo, Config, Reloj).EjecutarAsync("Development", false));
        Assert.Empty(await demo.Usuarios.ToArrayAsync()); Assert.Empty(await demo.Productos.ToArrayAsync());
    }
    [Fact]
    public async Task ProductionSeRechazaAntesDeAbrirConexion()
    {
        await using var demo = CargadorDatosDemo.CrearContexto("Server=(localdb)\\InstanciaQueNoDebeAbrirse;Database=NoAbrir;Integrated Security=true");
        await Assert.ThrowsAsync<DemoRechazadaException>(() => new CargadorDatosDemo(demo, Config, Reloj).EjecutarAsync("Production", false));
        Assert.Equal(System.Data.ConnectionState.Closed, demo.Database.GetDbConnection().State);
    }
    [Fact]
    public async Task DosCargasConcurrentesNoDuplicanDatosNiMarcador()
    {
        await using var aislada = await BaseSqlServerAislada.CrearAsync();
        await PrepararAsync(aislada.CadenaConexion);
        var inicio = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<Exception?> Cargar()
        {
            await inicio.Task;
            await using var db = CargadorDatosDemo.CrearContexto(aislada.CadenaConexion);
            try { await new CargadorDatosDemo(db, Config, Reloj).EjecutarAsync("Development", false); return null; }
            catch (Exception e) { return e; }
        }
        var tareas = new[] { Cargar(), Cargar() }; inicio.SetResult();
        var resultados = await Task.WhenAll(tareas);
        Assert.Single(resultados, x => x is null);
        var rechazo = Assert.Single(resultados, x => x is not null);
        Assert.True(rechazo is DemoRechazadaException or Microsoft.Data.SqlClient.SqlException { Number: 51120 });
        await using var lectura = Normal(aislada.CadenaConexion);
        Assert.Equal(35, await lectura.Productos.CountAsync());
        Assert.Equal(38, await lectura.Ventas.CountAsync());
        Assert.Equal(1, await lectura.Bitacoras.CountAsync(x => x.Accion == CargadorDatosDemo.Marca));
    }
    private static HeladeriaDbContext Normal(string cadena) => new(new DbContextOptionsBuilder<HeladeriaDbContext>().UseSqlServer(cadena).Options);
    private static async Task<ContextoConfiguracion> ContextoAsync(HeladeriaDbContext db)
    {
        var a = await db.UsuariosSucursales.Include(x => x.Sucursal).SingleAsync();
        return new(a.UsuarioId, "ADMIN", a.Sucursal.EmpresaId, a.SucursalId, null, null);
    }
    private static async Task PrepararAsync(string cadena)
    {
        var servicios = new ServiceCollection().AddLogging();
        servicios.AddSingleton(Config);
        servicios.AgregarInfraestructura(new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["ConnectionStrings:Heladeria"] = cadena }).Build(), habilitarReintentosSql: false);
        await using var proveedor = servicios.BuildServiceProvider();
        async Task Hacer<T>(Func<T, Task> accion) where T : notnull
        {
            await using var scope = proveedor.CreateAsyncScope();
            await accion(scope.ServiceProvider.GetRequiredService<T>());
        }
        var secretoFixture = "Aislada-" + Convert.ToHexString(RandomNumberGenerator.GetBytes(16)) + "a1!";
        await Hacer<IBootstrapAdministrador>(x => x.EjecutarAsync(new("admin", "admin@fixture.invalid", secretoFixture), default));
        await Hacer<IPreparadorFase3>(x => x.EjecutarAsync(default));
        await Hacer<IPreparadorFase4>(x => x.EjecutarAsync(default));
        await Hacer<IPreparadorFase5>(x => x.EjecutarAsync(default));
        await Hacer<IPreparadorFase6>(x => x.EjecutarAsync(default));
        await Hacer<IPreparadorFase7>(x => x.EjecutarAsync(default));
        await Hacer<IPreparadorFase8>(x => x.EjecutarAsync(default));
        await Hacer<IPreparadorFase9>(x => x.EjecutarAsync(default));
        await Hacer<IPreparadorFase10>(x => x.EjecutarAsync(default));
    }
    private static async Task<string[]> TodasLasHuellasAsync(HeladeriaDbContext db)
    {
        var tablas = await db.Database.SqlQuery<string>($"SELECT name AS Value FROM sys.tables WHERE schema_id=SCHEMA_ID('dbo')").ToArrayAsync();
        return await HuellasAsync(db, tablas);
    }
    private static async Task<string[]> HuellasAsync(HeladeriaDbContext db, string[] tablas)
    {
        var resultado = new List<string>();
        foreach (var tabla in tablas.Order(StringComparer.Ordinal))
        {
            // Nombres de catálogo SQL/constantes de prueba; no datos de usuario. Solo se conserva SHA-256, nunca secretos en salida.
            var json = await db.Database.SqlQueryRaw<string>("SELECT CAST((SELECT * FROM dbo.[" + tabla.Replace("]", "]]") +
                "] FOR JSON PATH, INCLUDE_NULL_VALUES) AS nvarchar(max)) AS Value").SingleAsync();
            resultado.Add(tabla + ":" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json ?? "[]"))));
        }
        return resultado.ToArray();
    }
}
