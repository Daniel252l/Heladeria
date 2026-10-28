using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using Heladeria.Aplicacion.Caja;
using Heladeria.Aplicacion.Configuracion;
using Heladeria.Aplicacion.Seguridad.Autenticacion;
using Heladeria.Aplicacion.Seguridad.Autorizacion;
using Heladeria.Aplicacion.Seguridad.Bootstrap;
using Heladeria.Aplicacion.Seguridad.Configuracion;
using Heladeria.Aplicacion.Seguridad.Excepciones;
using Heladeria.Aplicacion.Ventas;
using Heladeria.Bootstrap.Demo;
using Heladeria.Bootstrap.Seguridad;
using Heladeria.Infraestructura;
using Heladeria.Infraestructura.Persistencia;
using Heladeria.Infraestructura.Persistencia.Entidades;
using Heladeria.Infraestructura.Seguridad.Autenticacion;
using Heladeria.Infraestructura.Seguridad.Autorizacion;
using Heladeria.Pruebas.Integracion.Persistencia;
using Heladeria.Pruebas.Seguridad;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Heladeria.Pruebas.AjusteFinalE;

public sealed class UsuarioVentasPruebas
{
    private static string Secreto() => "Fixture-" + Convert.ToHexString(RandomNumberGenerator.GetBytes(20)) + "a9!";
    private static readonly IOptions<OpcionesSeguridad> Config = Options.Create(EscenarioSeguridad.CrearOpciones());

    [Fact]
    public async Task PreparacionIdempotenteHashTemporalAlcanceExactoYRegresionConsulta()
    {
        await using var aislada = await BaseSqlServerAislada.CrearAsync();
        await using var servicios = await PrepararAsync(aislada.CadenaConexion);
        await using var scope = servicios.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HeladeriaDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Usuarios>>();
        var preparador = ActivatorUtilities.CreateInstance<ProvisionadorUsuarioVentas>(scope.ServiceProvider);
        var consulta = scope.ServiceProvider.GetRequiredService<IProvisionadorUsuarioConsulta>();
        await consulta.EjecutarAsync(Secreto(), default);
        var antesAdmin = await db.Usuarios.AsNoTracking().SingleAsync(x => x.UserName == "admin");
        var antesConsulta = await db.Usuarios.AsNoTracking().SingleAsync(x => x.UserName == "consulta");
        var secreto = Secreto();
        Assert.NotEmpty((await preparador.EjecutarAsync(() => secreto, default)).Registros);
        db.ChangeTracker.Clear();
        var usuario = await db.Usuarios.Include(x => x.UsuariosSucursales).ThenInclude(x => x.Sucursal).SingleAsync(x => x.UserName == "ventas");
        Assert.Equal("VENDEDOR", usuario.RoleId);
        Assert.Equal(PasswordVerificationResult.Success, hasher.VerifyHashedPassword(usuario, usuario.PasswordHash, secreto));
        Assert.True(usuario.ContraseniaTemporal); Assert.Null(usuario.UltimoCambioDeContrasenia); Assert.Null(usuario.PasswordTemporal);
        Assert.Equal(64, usuario.SecurityStamp!.Length); Assert.Equal(usuario.HorarioInicio, usuario.HorarioFinal);
        var asignacion = Assert.Single(usuario.UsuariosSucursales);
        var adminSucursal = await db.UsuariosSucursales.AsNoTracking().Include(x => x.Sucursal).SingleAsync(x => x.UsuarioId == antesAdmin.Id);
        Assert.Equal(adminSucursal.SucursalId, asignacion.SucursalId); Assert.Equal(adminSucursal.Sucursal.EmpresaId, asignacion.Sucursal.EmpresaId);
        Assert.True(asignacion.EsPrincipal);
        var permisos = scope.ServiceProvider.GetRequiredService<IServicioPermisos>();
        Assert.Equal(ProvisionadorUsuarioVentas.PermisosNecesarios.Order(), (await permisos.ObtenerPermisosAsync(usuario.Id, default)).Order());
        Assert.Empty(await permisos.ObtenerPermisosAsync(antesConsulta.Id, default));
        var roles = await db.Roles.CountAsync(); var auditoria = await db.Bitacoras.CountAsync();
        Assert.Empty((await preparador.EjecutarAsync(() => throw new Exception("No debe leer/restablecer contraseña al repetir"), default)).Registros);
        Assert.Equal(roles, await db.Roles.CountAsync()); Assert.Equal(auditoria, await db.Bitacoras.CountAsync());
        Assert.Equal(usuario.PasswordHash, (await db.Usuarios.AsNoTracking().SingleAsync(x => x.UserName == "ventas")).PasswordHash);
        Assert.Equal(antesAdmin.PasswordHash, (await db.Usuarios.AsNoTracking().SingleAsync(x => x.UserName == "admin")).PasswordHash);
        Assert.Equal(antesConsulta.PasswordHash, (await db.Usuarios.AsNoTracking().SingleAsync(x => x.UserName == "consulta")).PasswordHash);
    }

    [Fact]
    public async Task RechazoDeContrasenaYCancelacionNoDejanRolUsuarioNiPermisosParciales()
    {
        await using var aislada = await BaseSqlServerAislada.CrearAsync();
        await using var servicios = await PrepararAsync(aislada.CadenaConexion);
        await using var scope = servicios.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HeladeriaDbContext>();
        var p = ActivatorUtilities.CreateInstance<ProvisionadorUsuarioVentas>(scope.ServiceProvider);
        var roles = await db.Roles.CountAsync(); var tipos = await db.Tipos.CountAsync(); var asignaciones = await db.RolesPermisos.CountAsync();
        await Assert.ThrowsAsync<ValidacionSeguridadException>(() => p.EjecutarAsync(() => "corta", default));
        await Assert.ThrowsAsync<OperationCanceledException>(() => p.EjecutarAsync(() => throw new OperationCanceledException(), default));
        Assert.Equal(roles, await db.Roles.CountAsync()); Assert.Equal(tipos, await db.Tipos.CountAsync()); Assert.Equal(asignaciones, await db.RolesPermisos.CountAsync());
        Assert.False(await db.Usuarios.AnyAsync(x => x.UserName == "ventas"));
        await p.EjecutarAsync(Secreto, default);
        var extra = await db.Permisos.SingleAsync(x => x.Codigo == "CONFIGURACION.ADMINISTRAR");
        db.RolesPermisos.Add(new() { RoleId = "VENDEDOR", PermisoId = extra.Id, Permitido = true }); await db.SaveChangesAsync();
        await Assert.ThrowsAsync<BootstrapAdministradorRechazadoException>(() => p.EjecutarAsync(Secreto, default));
        Assert.True(await db.RolesPermisos.AnyAsync(x => x.RoleId == "VENDEDOR" && x.PermisoId == extra.Id));
    }

    [Fact]
    public async Task ReutilizaRolOperativoEquivalenteSinDuplicarlo()
    {
        await using var aislada = await BaseSqlServerAislada.CrearAsync();
        await using var servicios = await PrepararAsync(aislada.CadenaConexion);
        await using var scope = servicios.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<HeladeriaDbContext>();
        var admin = await db.Roles.SingleAsync(x => x.Id == "ADMIN");
        var rol = new Roles { Id = "OPERADOR", Nombre = "Operador", TipoId = admin.TipoId, NivelDeAccesoId = admin.NivelDeAccesoId,
            CambioDeContrasenia = admin.CambioDeContrasenia, AplicarConfiguracionesPorRol = false };
        db.Roles.Add(rol);
        foreach (var codigo in ProvisionadorUsuarioVentas.PermisosNecesarios)
            db.RolesPermisos.Add(new() { Role = rol, PermisoId = await db.Permisos.Where(x => x.Codigo == codigo).Select(x => x.Id).SingleAsync(), Permitido = true });
        await db.SaveChangesAsync();
        var p = ActivatorUtilities.CreateInstance<ProvisionadorUsuarioVentas>(scope.ServiceProvider);
        await p.EjecutarAsync(Secreto, default); await p.EjecutarAsync(Secreto, default);
        Assert.False(await db.Roles.AnyAsync(x => x.Id == "VENDEDOR"));
        Assert.Equal("OPERADOR", (await db.Usuarios.SingleAsync(x => x.UserName == "ventas")).RoleId);
    }

    [Fact]
    public async Task LoginTemporalCambioSesionPosCobroComprobanteY403RealesEnLocalDb()
    {
        await using var aislada = await BaseSqlServerAislada.CrearAsync();
        await using var servicios = await PrepararAsync(aislada.CadenaConexion);
        await using (var demo = CargadorDatosDemo.CrearContexto(aislada.CadenaConexion))
            await new CargadorDatosDemo(demo, Config, TimeProvider.System).EjecutarAsync("Development", false, default);
        var temporal = Secreto();
        await using (var scope = servicios.CreateAsyncScope())
            await ActivatorUtilities.CreateInstance<ProvisionadorUsuarioVentas>(scope.ServiceProvider).EjecutarAsync(() => temporal, default);
        await using var fabrica = new FabricaApiSeguridad(false, s => {
            s.RemoveAll<HeladeriaDbContext>(); s.RemoveAll<DbContextOptions<HeladeriaDbContext>>();
            s.AddDbContext<HeladeriaDbContext>(o => o.UseSqlServer(aislada.CadenaConexion));
            s.RemoveAll<IServicioValidacionSesion>(); s.AddScoped<IServicioValidacionSesion, ServicioValidacionSesion>();
            s.RemoveAll<IServicioPermisos>(); s.AddScoped<IServicioPermisos, ServicioPermisos>();
        });
        using var http = fabrica.CrearCliente();
        async Task<IniciarSesionRespuesta> Login(string contrasena)
        {
            var r = await http.PostAsJsonAsync("/api/autenticacion/iniciar-sesion", new { identificador = "ventas", contrasena });
            Assert.Equal(HttpStatusCode.OK, r.StatusCode);
            var login = (await r.Content.ReadFromJsonAsync<IniciarSesionRespuesta>())!;
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
            return login;
        }
        Assert.True((await Login(temporal)).RequiereCambioContrasena);
        Assert.Equal(HttpStatusCode.Forbidden, (await http.GetAsync("/api/ventas/referencias")).StatusCode);
        var definitiva = Secreto();
        Assert.Equal(HttpStatusCode.OK, (await http.PostAsJsonAsync("/api/autenticacion/cambiar-contrasena",
            new { contrasenaActual = temporal, nuevaContrasena = definitiva, confirmacion = definitiva })).StatusCode);
        var login = await Login(definitiva); Assert.False(login.RequiereCambioContrasena);
        var sesion = (await http.GetFromJsonAsync<SesionActualRespuesta>("/api/autenticacion/sesion"))!;
        Assert.Equal(ProvisionadorUsuarioVentas.PermisosNecesarios.Order(), sesion.Permisos.Order());
        Assert.Single(sesion.Usuario.Sucursales);
        Assert.Equal(HttpStatusCode.Forbidden, (await http.PostAsJsonAsync("/api/autenticacion/seleccionar-sucursal", new { sucursalId = long.MaxValue })).StatusCode);
        // La lectura de empresas es autenticada desde Fase 3; la administración sí exige su permiso.
        foreach (var ruta in new[] { "/api/auditoria", "/api/cuentas-por-pagar", "/api/inventario/existencias", "/api/reportes/ventas" })
            Assert.Equal(HttpStatusCode.Forbidden, (await http.GetAsync(ruta)).StatusCode);
        foreach (var ruta in new[] { "/api/configuracion/empresas", "/api/cuentas-por-pagar/desde-compra", "/api/cuentas-por-pagar/1/pagos", "/api/caja/sesiones/abrir", "/api/ventas/1/anular" })
            Assert.Equal(HttpStatusCode.Forbidden, (await http.PostAsJsonAsync(ruta, new { })).StatusCode);
        var refs = (await http.GetFromJsonAsync<ReferenciasVentasDto>("/api/ventas/referencias"))!;
        Assert.NotEmpty(refs.Clientes);
        var catalogo = (await http.GetFromJsonAsync<RespuestaPaginada<ProductoPosDto>>("/api/ventas/productos"))!;
        var producto = catalogo.Elementos.First(x => x.Presentaciones.Any(p => p.PrecioValido));
        Assert.Equal(HttpStatusCode.OK, (await http.GetAsync($"/api/ventas/productos/{producto.Id}/presentaciones")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await http.GetAsync("/api/clientes")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await http.GetAsync("/api/caja/referencias")).StatusCode);
        SesionCajaDto caja;
        await using (var scope = servicios.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HeladeriaDbContext>();
            var a = await db.UsuariosSucursales.Include(x => x.Usuario).Include(x => x.Sucursal).SingleAsync(x => x.Usuario.RoleId == "ADMIN");
            var admin = new ContextoConfiguracion(a.UsuarioId, "ADMIN", a.Sucursal.EmpresaId, a.SucursalId, null, null);
            var servicio = scope.ServiceProvider.GetRequiredService<IServicioCaja>();
            var creada = await servicio.CrearCajaAsync(new() { Codigo = "TEST-E", Nombre = "Caja prueba aislada" }, admin, default);
            caja = await servicio.AbrirAsync(new() { CajaId = creada.Id, MonedaId = refs.MonedaId, SaldoInicial = 100 }, admin, default);
        }
        var ubicacion = refs.Almacenes.First();
        var respuesta = await http.PostAsJsonAsync("/api/ventas", new CrearVentaSolicitud { EsCredito = false, SucursalId = refs.SucursalId,
            BodegaId = ubicacion.BodegaId, UbicacionId = ubicacion.UbicacionId, MonedaId = refs.MonedaId,
            Detalles = [new() { ProductoId = producto.Id, ProductoPresentacionId = producto.Presentaciones.First(p => p.PrecioValido).ProductoPresentacionId, Cantidad = 1 }] });
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var venta = (await respuesta.Content.ReadFromJsonAsync<VentaConsultaDto>())!;
        respuesta = await http.PostAsJsonAsync($"/api/ventas/{venta.Encabezado.Id}/confirmar", new { venta.Encabezado.VersionFila });
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode); venta = (await respuesta.Content.ReadFromJsonAsync<VentaConsultaDto>())!;
        var cajaRefs = (await http.GetFromJsonAsync<ReferenciasCajaDto>("/api/caja/referencias"))!;
        respuesta = await http.PostAsJsonAsync($"/api/ventas/{venta.Encabezado.Id}/completar", new CompletarVentaSolicitud {
            VersionFila = venta.Encabezado.VersionFila, ClaveOperacion = Guid.NewGuid(), Cobro = new() { SesionCajaId = caja.Encabezado.Id,
                VersionCaja = caja.Encabezado.VersionFila, Pagos = [new() { FormaPagoId = cajaRefs.FormasPago.First(x => x.EsEfectivo).Id,
                    MontoAplicado = venta.Encabezado.Total, MontoRecibido = venta.Encabezado.Total }] } });
        Assert.True(respuesta.StatusCode == HttpStatusCode.OK, await respuesta.Content.ReadAsStringAsync());
        venta = (await respuesta.Content.ReadFromJsonAsync<VentaConsultaDto>())!;
        Assert.Equal("COMPLETADA", venta.Encabezado.Estado); Assert.NotNull(venta.Cobro); Assert.NotNull(venta.Comprobante);
        Assert.Equal(HttpStatusCode.OK, (await http.GetAsync($"/api/ventas/{venta.Encabezado.Id}")).StatusCode);
    }

    private static async Task<ServiceProvider> PrepararAsync(string cadena)
    {
        var s = new ServiceCollection().AddLogging(); s.AddSingleton(Config);
        s.AgregarInfraestructura(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Heladeria"] = cadena }).Build(), false);
        var proveedor = s.BuildServiceProvider();
        async Task Preparar<T>(Func<T, Task> accion) where T : notnull
        { await using var scope = proveedor.CreateAsyncScope(); await accion(scope.ServiceProvider.GetRequiredService<T>()); }
        await Preparar<IBootstrapAdministrador>(x => x.EjecutarAsync(new("admin", "admin@fixture.invalid", Secreto()), default));
        await Preparar<IPreparadorFase3>(x => x.EjecutarAsync(default)); await Preparar<IPreparadorFase4>(x => x.EjecutarAsync(default));
        await Preparar<IPreparadorFase5>(x => x.EjecutarAsync(default)); await Preparar<IPreparadorFase6>(x => x.EjecutarAsync(default));
        await Preparar<IPreparadorFase7>(x => x.EjecutarAsync(default)); await Preparar<IPreparadorFase8>(x => x.EjecutarAsync(default));
        await Preparar<IPreparadorFase9>(x => x.EjecutarAsync(default)); await Preparar<IPreparadorFase10>(x => x.EjecutarAsync(default));
        return proveedor;
    }
}
