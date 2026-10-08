using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Heladeria.Aplicacion.Configuracion;
using Heladeria.Aplicacion.Seguridad.Bootstrap;
using Heladeria.Aplicacion.Seguridad.Configuracion;
using Heladeria.Aplicacion.Ventas;
using Heladeria.Infraestructura.Fase4;
using Heladeria.Infraestructura.Inventario;
using Heladeria.Infraestructura.Persistencia;
using Heladeria.Infraestructura.Persistencia.Entidades;
using Heladeria.Infraestructura.Seguridad.Autenticacion;
using Heladeria.Infraestructura.Seguridad.Bootstrap;
using Heladeria.Pruebas.Fase5;
using Heladeria.Pruebas.Integracion.Persistencia;
using Heladeria.Pruebas.Seguridad;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Heladeria.Pruebas.Tiempo;

public sealed class VentasConcurrentesSqlPruebas
{
    internal sealed class RelojFijo : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 10, 8, 20, 42, 15, 376, TimeSpan.Zero);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(5)]
    public async Task PostVentasSimultaneasConContextosYCoordinadoresIndependientes(int cantidad)
    {
        // Instancia Y base privadas desechables. Nunca se consulta la conexión de la aplicación.
        await using var aislada = await BaseSqlServerAislada.CrearAsync();
        var opciones = new DbContextOptionsBuilder<HeladeriaDbContext>().UseSqlServer(aislada.CadenaConexion).Options;
        await using var db = new HeladeriaDbContext(opciones);
        var reloj = new RelojFijo();
        var configuracion = EscenarioFase5.Opciones;
        await new BootstrapAdministrador(db, new PasswordHasher<Usuarios>(), configuracion, reloj)
            .EjecutarAsync(new SolicitudBootstrapAdministrador("admin", "admin@prueba.local", "Prueba-Aislada9!"), default);
        await new PreparadorFase4(db, configuracion, reloj).EjecutarAsync(default);
        var coordinador = new CoordinadorAjustesInventario();
        var inventario = new ServicioInventario(db, new AuditoriaFase4(db, reloj), configuracion, reloj, coordinador);
        await new PreparadorFase7(db, configuracion, reloj, coordinador, inventario).EjecutarAsync(default);
        var asignacion = await db.UsuariosSucursales.Include(x => x.Sucursal).SingleAsync();
        var empresa = asignacion.Sucursal.EmpresaId;
        var usuario = asignacion.UsuarioId;
        var activo = await db.Estados.Where(x => x.Nombre == "Activo").Select(x => x.Id).SingleAsync();
        var moneda = new Monedas { EmpresaId = empresa, CodigoISO = "GTQ", NombreMoneda = "Quetzal", Simbolo = "Q", EsMonedaBase = true, Decimales = 2, EstadoId = activo };
        var categoria = new Categorias { EmpresaId = empresa, Codigo = "PRUEBA", Nombre = "Prueba", Nivel = 1, EstadoId = activo, FechaCreacion = reloj.GetUtcNow().UtcDateTime, CreadoPorUsuarioId = usuario };
        var tipo = new TiposProductos { EmpresaId = empresa, Codigo = "SERVICIO", Nombre = "Servicio", ManejaExistencia = false, EstadoId = activo, FechaCreacion = reloj.GetUtcNow().UtcDateTime, CreadoPorUsuarioId = usuario };
        var unidad = new UnidadesMedida { EmpresaId = empresa, Codigo = "UN", Nombre = "Unidad", Abreviatura = "un", EstadoId = activo, FechaCreacion = reloj.GetUtcNow().UtcDateTime, CreadoPorUsuarioId = usuario };
        db.AddRange(moneda, categoria, tipo, unidad); await db.SaveChangesAsync();
        var producto = new Productos { EmpresaId = empresa, CategoriaId = categoria.Id, TipoProductoId = tipo.Id, UnidadBaseId = unidad.Id,
            TipoSeguimientoId = await db.Catalogos.Where(x => x.Codigo == "NINGUNO").Select(x => x.Id).SingleAsync(),
            MetodoValuacionId = await db.Catalogos.Where(x => x.Codigo == "PROMEDIO").Select(x => x.Id).SingleAsync(),
            SKU = "CONCURRENCIA", Nombre = "Prueba de orden temporal", EsVendible = true, PrecioVentaReferencial = 25,
            EstadoId = activo, FechaCreacion = reloj.GetUtcNow().UtcDateTime, CreadoPorUsuarioId = usuario };
        db.Productos.Add(producto); await db.SaveChangesAsync();
        var solicitud = new CrearVentaSolicitud { SucursalId = asignacion.SucursalId,
            BodegaId = await db.Bodegas.Select(x => x.Id).SingleAsync(), UbicacionId = await db.Ubicaciones.Select(x => x.Id).SingleAsync(),
            MonedaId = moneda.Id, Detalles = [new() { ProductoId = producto.Id, Cantidad = 1 }] };

        await using var fabrica = new FabricaApiSeguridad(true, servicios => {
            servicios.RemoveAll<HeladeriaDbContext>(); servicios.RemoveAll<DbContextOptions<HeladeriaDbContext>>();
            servicios.AddScoped(_ => new HeladeriaDbContext(opciones));
            servicios.RemoveAll<TimeProvider>(); servicios.AddSingleton<TimeProvider>(reloj);
            // Cada petición tiene coordinador propio: la exclusión se prueba en SQL, no en memoria.
            servicios.RemoveAll<CoordinadorAjustesInventario>(); servicios.AddScoped<CoordinadorAjustesInventario>();
        });
        using var cliente = fabrica.CrearCliente();
        var jwt = new GeneradorTokensJwt(fabrica.Services.GetRequiredService<IOptions<OpcionesSeguridad>>(), TimeProvider.System)
            .CrearAccessToken(new Usuarios { Id = usuario, UserName = "admin", RoleId = "ADMIN" }, asignacion.Sucursal, 1, false);
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt.Valor);
        var empezar = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<VentaConsultaDto> Crear()
        {
            await empezar.Task;
            using var r = await cliente.PostAsJsonAsync("/api/ventas", solicitud);
            Assert.True(r.StatusCode == HttpStatusCode.Created, await r.Content.ReadAsStringAsync());
            using var json = JsonDocument.Parse(await r.Content.ReadAsStringAsync());
            Assert.Equal("2026-10-08T20:42:15.376Z", json.RootElement.GetProperty("encabezado").GetProperty("fechaVenta").GetString());
            return (await r.Content.ReadFromJsonAsync<VentaConsultaDto>())!;
        }
        var tareas = Enumerable.Range(0, cantidad).Select(_ => Crear()).ToArray();
        empezar.SetResult();
        var ventas = await Task.WhenAll(tareas);
        Assert.Equal(cantidad, ventas.Select(x => x.Encabezado.Id).Distinct().Count());
        Assert.Equal(cantidad, ventas.Select(x => x.Encabezado.NumeroVenta).Distinct().Count());
        var numeros = ventas.Select(x => long.Parse(x.Encabezado.NumeroVenta[3..])).Order().ToArray();
        Assert.Equal(Enumerable.Range(1, cantidad).Select(x => (long)x), numeros);
        Assert.All(ventas, x => Assert.Equal(reloj.GetUtcNow().UtcDateTime, x.Encabezado.FechaVenta));
        Assert.Equal(cantidad, await db.Ventas.CountAsync());
        Assert.Equal(cantidad, await db.CorrelativosDocumentos.Where(x => x.SerieDocumento.Serie == "VT").Select(x => x.UltimoNumero).SingleAsync());

        // Prueba la pérdida de Kind del proveedor SQL y su restauración por EF sin cambiar los ticks.
        await using var sql = new SqlConnection(aislada.CadenaConexion); await sql.OpenAsync();
        await using var comando = new SqlCommand("SELECT TOP(1) FechaVenta FROM dbo.Ventas ORDER BY Id", sql);
        var crudo = (DateTime)(await comando.ExecuteScalarAsync())!;
        Assert.Equal(DateTimeKind.Unspecified, crudo.Kind);
        Assert.Equal(reloj.GetUtcNow().UtcDateTime.Ticks, crudo.Ticks);
        var leida = await db.Ventas.AsNoTracking().FirstAsync();
        Assert.Equal(DateTimeKind.Utc, leida.FechaVenta.Kind);
        var auditorias = await db.Bitacoras.AsNoTracking().Where(x => x.Entidad == "Ventas").ToArrayAsync();
        Assert.NotEmpty(auditorias);
        Assert.All(auditorias, x => { Assert.Equal(DateTimeKind.Utc, x.Fecha.Kind); Assert.Equal(reloj.GetUtcNow().UtcDateTime, x.Fecha); });
        var texto = await cliente.GetStringAsync($"/api/ventas/{leida.Id}");
        using var consulta = JsonDocument.Parse(texto);
        Assert.Equal("2026-10-08T20:42:15.376Z", consulta.RootElement.GetProperty("encabezado").GetProperty("fechaVenta").GetString());
        var ordenEsperado = ventas.OrderByDescending(x => x.Encabezado.NumeroVenta).Select(x => x.Encabezado.Id).ToArray();
        for (var vez = 0; vez < 3; vez++)
        {
            var lista = (await cliente.GetFromJsonAsync<RespuestaPaginada<VentaResumenDto>>("/api/ventas"))!;
            Assert.Equal(ordenEsperado, lista.Elementos.Select(x => x.Id));
        }
        // El día/rango civil se interpreta en Guatemala: 14:42 local incluye el UTC 20:42.
        var filtrada = (await cliente.GetFromJsonAsync<RespuestaPaginada<VentaResumenDto>>(
            "/api/ventas?fechaDesde=2026-10-08T14:42:00&fechaHasta=2026-10-08T14:43:00"))!;
        Assert.Equal(cantidad, filtrada.Elementos.Count);
    }
}
