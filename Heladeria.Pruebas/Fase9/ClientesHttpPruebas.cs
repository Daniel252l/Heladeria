using System.Net;
using System.Net.Http.Json;
using Heladeria.Aplicacion.Clientes;
using Heladeria.Aplicacion.Configuracion;
using Heladeria.Aplicacion.Seguridad.Autorizacion;
using Heladeria.Aplicacion.Ventas;
using Heladeria.Infraestructura.Persistencia;
using Heladeria.Pruebas.Fase5;
using Heladeria.Pruebas.Fase7;
using Heladeria.Pruebas.Seguridad;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Heladeria.Pruebas.Fase9;

public sealed class ClientesHttpPruebas
{
    [Theory]
    [InlineData("GET", "/api/clientes")]
    [InlineData("GET", "/api/clientes/1")]
    [InlineData("POST", "/api/clientes")]
    public async Task ExigenAutenticacionYPermiso(string metodo, string ruta)
    {
        await using var f = new FabricaApiSeguridad(false);
        using var anonimo = f.CrearCliente(); using var sinPermiso = f.CrearClienteAutenticado();
        using var a = new HttpRequestMessage(new HttpMethod(metodo), ruta) { Content = JsonContent.Create(new { }) };
        using var b = new HttpRequestMessage(new HttpMethod(metodo), ruta) { Content = JsonContent.Create(new { }) };
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonimo.SendAsync(a)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await sinPermiso.SendAsync(b)).StatusCode);
    }

    [Theory]
    [InlineData("GET", "/api/clientes", PermisosSistema.VentasConsultar)]
    [InlineData("GET", "/api/clientes/1", PermisosSistema.VentasConsultar)]
    [InlineData("POST", "/api/clientes", PermisosSistema.VentasAdministrar)]
    public async Task ExigenExactamentePermisosExistentes(string metodo, string ruta, string denegado)
    {
        await using var f = new FabricaApiSeguridad(true, s => {
            s.RemoveAll<IServicioPermisos>(); s.AddSingleton<IServicioPermisos>(new PermisosSin(denegado));
        });
        using var cliente = f.CrearClienteAutenticado();
        using var solicitud = new HttpRequestMessage(new HttpMethod(metodo), ruta) { Content = JsonContent.Create(new { }) };
        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.SendAsync(solicitud)).StatusCode);
    }

    public static IEnumerable<object[]> SolicitudesInvalidas()
    {
        yield return [new { }];
        yield return [new { codigoCliente = "  ", nombreCliente = "Cliente" }];
        yield return [new { codigoCliente = "CLI", nombreCliente = "  " }];
        yield return [new { codigoCliente = new string('C', 21), nombreCliente = "Cliente" }];
        yield return [new { codigoCliente = "CLI", nombreCliente = new string('C', 151) }];
        yield return [new { codigoCliente = "CLI", nombreCliente = "Cliente", nit = new string('1', 21) }];
        yield return [new { codigoCliente = "CLI", nombreCliente = "Cliente", telefono = new string('1', 21) }];
        yield return [new { codigoCliente = "CLI", nombreCliente = "Cliente", correo = "no-es-correo" }];
        yield return [new { codigoCliente = "CLI", nombreCliente = "Cliente", limiteCredito = -1 }];
        yield return [new { codigoCliente = "CLI", nombreCliente = "Cliente", limiteCredito = 10000000000m }];
        yield return [new { codigoCliente = "CLI", nombreCliente = "Cliente", limiteCredito = 0.001m }];
        yield return [new { codigoCliente = "CLI", nombreCliente = "Cliente", diasCredito = -1 }];
        yield return [new { codigoCliente = "CLI", nombreCliente = "Cliente", diasCredito = 32768 }];
        yield return [new { codigoCliente = "CLI\n", nombreCliente = "Cliente" }];
    }

    [Theory, MemberData(nameof(SolicitudesInvalidas))]
    public async Task ValidacionesMvcDevuelven400SinLlegarAPersistencia(object solicitud)
    {
        await using var f = new FabricaApiSeguridad(true); using var cliente = f.CrearClienteAutenticado();
        Assert.Equal(HttpStatusCode.BadRequest, (await cliente.PostAsJsonAsync("/api/clientes", solicitud)).StatusCode);
    }

    [Fact]
    public async Task Alta201ListadoDetalleReferenciasYCxcPositivaConClienteCreadoPorApi()
    {
        await using var e = await EscenarioCartera.CrearAsync();
        var permisosAntes = await e.Db.RolesPermisos.CountAsync();
        var bitacorasAntes = await e.Db.Bitacoras.CountAsync();
        await using var f = Fabrica(e.Db); using var http = f.CrearClienteAutenticado();
        var respuesta = await http.PostAsJsonAsync("/api/clientes", new CrearClienteSolicitud {
            CodigoCliente = " cli-api ", NombreCliente = " Cliente registrado por API ", Nit = "123456-7",
            Correo = "cliente@example.test", Telefono = "55550000", LimiteCredito = 500, DiasCredito = 30 });
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var nuevo = (await respuesta.Content.ReadFromJsonAsync<ClienteConsultaDto>())!;
        Assert.True(nuevo.Id > 0); Assert.Equal(EscenarioCartera.S.EmpresaId, nuevo.EmpresaId);
        Assert.Equal("CLI-API", nuevo.CodigoCliente); Assert.Equal("Cliente registrado por API", nuevo.NombreCliente);
        Assert.Equal("Activo", nuevo.Estado); Assert.Equal(500, nuevo.LimiteCredito); Assert.Equal(30, nuevo.DiasCredito);
        Assert.EndsWith($"/api/clientes/{nuevo.Id}", respuesta.Headers.Location!.ToString());
        Assert.Equal(nuevo, await http.GetFromJsonAsync<ClienteConsultaDto>(respuesta.Headers.Location));
        var listado = (await http.GetFromJsonAsync<RespuestaPaginada<ClienteConsultaDto>>("/api/clientes?busqueda=cli-api&tamanioPagina=1"))!;
        Assert.Equal(nuevo, Assert.Single(listado.Elementos)); Assert.Equal(1, listado.TotalRegistros);
        var referencias = (await http.GetFromJsonAsync<ReferenciasVentasDto>("/api/ventas/referencias"))!;
        Assert.Contains(referencias.Clientes, x => x.Id == nuevo.Id && x.Codigo == nuevo.CodigoCliente && x.Nombre == nuevo.NombreCliente);
        var auditoria = await e.Db.Bitacoras.SingleAsync(x => x.Accion == "CLIENTE_CREADO");
        Assert.Equal("Clientes", auditoria.Entidad); Assert.Equal(nuevo.Id.ToString(), auditoria.EntidadId);
        Assert.Equal(EscenarioCartera.S.UsuarioId, auditoria.UsuarioId); Assert.Equal(EscenarioCartera.S.SucursalId, auditoria.SucursalId);
        Assert.Equal(bitacorasAntes + 1, await e.Db.Bitacoras.CountAsync());
        Assert.Equal(permisosAntes, await e.Db.RolesPermisos.CountAsync());
        Assert.Empty(e.Db.ContactosClientes); Assert.Empty(e.Db.DireccionesClientes);
        var entidad = await e.Db.Clientes.AsNoTracking().SingleAsync(x => x.Id == nuevo.Id);
        Assert.Null(entidad.ListaPrecioId); Assert.Null(entidad.EstadoCreditoId);

        // Regresión positiva Fase 9: mismo cliente persistido por HTTP, sin cambiar CxC ni Ventas.
        var datos = EscenarioFase7.Solicitud();
        var venta = await e.Caja.Ventas.ConfirmarAsync(new CrearVentaSolicitud {
            SucursalId = datos.SucursalId, BodegaId = datos.BodegaId, UbicacionId = datos.UbicacionId,
            MonedaId = datos.MonedaId, Detalles = datos.Detalles, ClienteId = nuevo.Id, EsCredito = true,
            FechaVencimientoCredito = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30) });
        venta = await e.Caja.Ventas.CompletarAsync(venta);
        var cuenta = await e.Db.CuentasPorCobrar.SingleAsync(x => x.VentaId == venta.Encabezado.Id);
        Assert.Equal(nuevo.Id, cuenta.ClienteId); Assert.True(cuenta.SaldoPendiente > 0);
    }

    [Fact]
    public async Task Duplicado409DetalleAjeno404Inexistente404PaginacionYEmpresaNoInyectable()
    {
        await using var e = await EscenarioCartera.CrearAsync();
        var ajeno = new Heladeria.Infraestructura.Persistencia.Entidades.Clientes {
            EmpresaId = 2, CodigoCliente = "OTRA", NombreCliente = "No visible", EstadoId = 1 };
        e.Db.Add(ajeno); await e.Db.SaveChangesAsync(); e.Db.ChangeTracker.Clear();
        await using var f = Fabrica(e.Db); using var http = f.CrearClienteAutenticado();
        var alta = await http.PostAsJsonAsync("/api/clientes", new {
            codigoCliente = "NUEVO", nombreCliente = "Cliente", empresaId = 2, estadoId = 2, id = 987654 });
        Assert.Equal(HttpStatusCode.Created, alta.StatusCode);
        var creado = (await alta.Content.ReadFromJsonAsync<ClienteConsultaDto>())!;
        Assert.Equal(1, creado.EmpresaId); Assert.Equal("Activo", creado.Estado); Assert.NotEqual(987654, creado.Id);
        var cantidad = await e.Db.Clientes.CountAsync(); var auditorias = await e.Db.Bitacoras.CountAsync();
        var duplicado = await http.PostAsJsonAsync("/api/clientes", new { codigoCliente = " nuevo ", nombreCliente = "Otro" });
        Assert.Equal(HttpStatusCode.Conflict, duplicado.StatusCode); Assert.Contains("CLIENTE_DUPLICADO", await duplicado.Content.ReadAsStringAsync());
        Assert.Equal(cantidad, await e.Db.Clientes.CountAsync()); Assert.Equal(auditorias, await e.Db.Bitacoras.CountAsync());
        Assert.Equal(HttpStatusCode.NotFound, (await http.GetAsync($"/api/clientes/{ajeno.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await http.GetAsync("/api/clientes/999999")).StatusCode);
        var todos = (await http.GetFromJsonAsync<RespuestaPaginada<ClienteConsultaDto>>("/api/clientes"))!;
        Assert.All(todos.Elementos, x => Assert.Equal(1, x.EmpresaId));
        Assert.Empty((await http.GetFromJsonAsync<RespuestaPaginada<ClienteConsultaDto>>("/api/clientes?pagina=2147483647&tamanioPagina=100"))!.Elementos);
        Assert.Equal(HttpStatusCode.BadRequest, (await http.GetAsync("/api/clientes?pagina=0")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await http.GetAsync("/api/clientes?tamanioPagina=101")).StatusCode);
    }

    private static FabricaApiSeguridad Fabrica(HeladeriaDbContext db) => new(true, servicios => {
        servicios.RemoveAll<HeladeriaDbContext>(); servicios.RemoveAll<DbContextOptions<HeladeriaDbContext>>();
        servicios.AddScoped<HeladeriaDbContext>(_ => new ContextoSqlitePruebas(new DbContextOptionsBuilder<HeladeriaDbContext>()
            .UseSqlite(db.Database.GetDbConnection()).Options));
    });
    private sealed class PermisosSin(string denegado) : IServicioPermisos
    {
        public Task<bool> TienePermisoAsync(int usuarioId, string rolId, string codigoPermiso, CancellationToken t) => Task.FromResult(codigoPermiso != denegado);
        public Task<IReadOnlyList<string>> ObtenerPermisosAsync(int usuarioId, CancellationToken t) => Task.FromResult<IReadOnlyList<string>>([]);
    }
}
