using Heladeria.Aplicacion.Seguridad.Autenticacion;
using Heladeria.Aplicacion.Seguridad.Bootstrap;
using Heladeria.Aplicacion.Seguridad.Configuracion;
using Heladeria.Aplicacion.Seguridad.Excepciones;
using Heladeria.Infraestructura.Persistencia;
using Heladeria.Infraestructura.Persistencia.Entidades;
using Heladeria.Infraestructura.Seguridad.Autenticacion;
using Heladeria.Infraestructura.Seguridad.Autorizacion;
using Heladeria.Infraestructura.Seguridad.Bootstrap;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Heladeria.Pruebas.Seguridad;

public sealed class BootstrapAdministradorPruebas
{
    [Fact]
    public async Task PreparacionFase3_CreaPermisoAdminUnaVez()
    {
        await using var contexto = CrearContexto();
        var (bootstrap, _, _, _) = CrearBootstrap(contexto);
        await bootstrap.EjecutarAsync(CrearSolicitud(CrearContrasenaValida()), CancellationToken.None);

        var resultado = await bootstrap.PrepararFase3Async(CancellationToken.None);

        Assert.Equal(4, resultado.Registros.Count);
        var permiso = await contexto.Permisos.SingleAsync(item => item.Codigo == BootstrapAdministrador.CodigoPermisoConfiguracionAdministrar);
        Assert.Contains(contexto.RolesPermisos, item => item.RoleId == BootstrapAdministrador.RolAdministradorId && item.PermisoId == permiso.Id && item.Permitido);
        Assert.Contains(contexto.Bitacoras, item => item.Accion == "FASE3_CONFIGURADA");
        await Assert.ThrowsAsync<BootstrapAdministradorRechazadoException>(() => bootstrap.PrepararFase3Async(CancellationToken.None));
        Assert.Single(contexto.Permisos, item => item.Codigo == BootstrapAdministrador.CodigoPermisoConfiguracionAdministrar);
    }

    [Fact]
    public async Task PreparacionFase4_CreaPermisosReferenciasYAlmacenUnaVez()
    {
        await using var contexto = CrearContexto();
        var (bootstrap, _, opciones, tiempo) = CrearBootstrap(contexto);
        await bootstrap.EjecutarAsync(CrearSolicitud(CrearContrasenaValida()), CancellationToken.None);
        var preparador = new PreparadorFase4(contexto, opciones, tiempo);

        var resultado = await preparador.EjecutarAsync(CancellationToken.None);

        Assert.NotEmpty(resultado.Registros);
        Assert.Single(contexto.Modulos, item => item.Id == PreparadorFase4.ModuloId);
        Assert.Equal(4, contexto.Permisos.Count(item => new[]
        {
            "PRODUCTOS.CONSULTAR",
            "PRODUCTOS.ADMINISTRAR",
            "INVENTARIO.CONSULTAR",
            "INVENTARIO.ADMINISTRAR"
        }.Contains(item.Codigo)));
        Assert.Equal(4, contexto.RolesPermisos.Count(item => item.RoleId == "ADMIN" && item.Permiso.ModuloId == PreparadorFase4.ModuloId));
        Assert.Equal(3, contexto.ClasesCatalogos.Count(item => item.Codigo == "TIPO_SEGUIMIENTO_PRODUCTO" || item.Codigo == "METODO_VALUACION_INVENTARIO" || item.Codigo == "TIPO_UBICACION"));
        Assert.Equal(2, contexto.ConceptosMovimientos.Count(item => item.Codigo.StartsWith("AJUSTE-")));
        Assert.Single(contexto.FlujosEstados, item => item.Codigo == PreparadorFase4.FlujoMovimientosCodigo);
        Assert.Single(contexto.EstadosFlujo, item => item.Codigo == "APLICADO");
        Assert.Single(contexto.Bodegas, item => item.Codigo == PreparadorFase4.BodegaInicialCodigo);
        Assert.Single(contexto.Ubicaciones, item => item.Codigo == PreparadorFase4.UbicacionInicialCodigo);
        Assert.Single(contexto.Bitacoras, item => item.Accion == PreparadorFase4.AccionMarcador);

        await Assert.ThrowsAsync<BootstrapAdministradorRechazadoException>(() =>
            preparador.EjecutarAsync(CancellationToken.None));
        Assert.Equal(4, contexto.Permisos.Count(item => item.ModuloId == PreparadorFase4.ModuloId));
    }

    [Fact]
    public async Task PreparacionFase4_ParcialEsRechazadaSinCompletarla()
    {
        await using var contexto = CrearContexto();
        var (bootstrap, _, opciones, tiempo) = CrearBootstrap(contexto);
        await bootstrap.EjecutarAsync(CrearSolicitud(CrearContrasenaValida()), CancellationToken.None);
        contexto.Modulos.Add(new Modulos { Id = PreparadorFase4.ModuloId, Nombre = "Inventario parcial", Icono = "x", Activo = true });
        await contexto.SaveChangesAsync();

        var preparador = new PreparadorFase4(contexto, opciones, tiempo);
        await Assert.ThrowsAsync<BootstrapAdministradorRechazadoException>(() =>
            preparador.EjecutarAsync(CancellationToken.None));

        Assert.Empty(contexto.Permisos.Where(item => item.ModuloId == PreparadorFase4.ModuloId));
        Assert.DoesNotContain(contexto.Bitacoras, item => item.Accion == PreparadorFase4.AccionMarcador);
    }

    [Fact]
    public async Task EntornoAdministrativo_CreaUsuarioConsultaSinPermisoYSinContrasenaTemporal()
    {
        await using var contexto = CrearContexto();
        var (bootstrap, hasher, opciones, tiempo) = CrearBootstrap(contexto);
        await bootstrap.EjecutarAsync(
            CrearSolicitud(CrearContrasenaValida()),
            CancellationToken.None);
        var contrasenaConsulta = CrearContrasenaValida();

        var resultado = await bootstrap.CrearUsuarioConsultaAsync(
            contrasenaConsulta,
            CancellationToken.None);

        Assert.Equal(3, resultado.Registros.Count);
        Assert.Equal(2, await contexto.Roles.CountAsync());
        Assert.Equal(2, await contexto.Usuarios.CountAsync());
        Assert.Equal(2, await contexto.UsuariosSucursales.CountAsync());
        Assert.Single(contexto.Empresas);
        Assert.Single(contexto.Sucursales);

        var rolAdmin = await contexto.Roles.SingleAsync(
            item => item.Id == BootstrapAdministrador.RolAdministradorId);
        var rolConsulta = await contexto.Roles.SingleAsync(
            item => item.Id == BootstrapAdministrador.RolConsultaId);
        Assert.Equal("Consulta", rolConsulta.Nombre);
        Assert.Equal(rolAdmin.NivelDeAccesoId, rolConsulta.NivelDeAccesoId);
        Assert.Equal(rolAdmin.TipoId, rolConsulta.TipoId);
        Assert.False(rolConsulta.AplicarConfiguracionesPorRol);
        Assert.DoesNotContain(
            contexto.RolesPermisos,
            item => item.RoleId == BootstrapAdministrador.RolConsultaId);

        var usuario = await contexto.Usuarios.SingleAsync(
            item => item.UserName == BootstrapAdministrador.UsuarioConsulta);
        var asignacion = await contexto.UsuariosSucursales.SingleAsync(
            item => item.UsuarioId == usuario.Id);
        var asignacionAdmin = await contexto.UsuariosSucursales.SingleAsync(
            item => item.Usuario.UserName == "admin");
        Assert.Equal(BootstrapAdministrador.CorreoUsuarioConsulta, usuario.Email);
        Assert.Equal(BootstrapAdministrador.RolConsultaId, usuario.RoleId);
        Assert.False(usuario.ContraseniaTemporal);
        Assert.NotNull(usuario.UltimoCambioDeContrasenia);
        Assert.Null(usuario.PasswordTemporal);
        Assert.Equal(usuario.HorarioInicio, usuario.HorarioFinal);
        Assert.Equal(asignacionAdmin.SucursalId, asignacion.SucursalId);
        Assert.True(asignacion.EsPrincipal);
        Assert.NotEqual(contrasenaConsulta, usuario.PasswordHash);
        Assert.NotEqual(
            PasswordVerificationResult.Failed,
            hasher.VerifyHashedPassword(usuario, usuario.PasswordHash, contrasenaConsulta));

        var permisos = new ServicioPermisos(contexto, opciones);
        Assert.False(await permisos.TienePermisoAsync(
            usuario.Id,
            usuario.RoleId,
            BootstrapAdministrador.CodigoPermisoAdministrar,
            CancellationToken.None));

        contexto.ChangeTracker.Clear();
        var servicioAutenticacion = new ServicioAutenticacion(
            contexto,
            hasher,
            new GeneradorTokensJwt(opciones, tiempo),
            opciones,
            tiempo,
            permisos,
            NullLogger<ServicioAutenticacion>.Instance);
        var login = await servicioAutenticacion.IniciarSesionAsync(
            new IniciarSesionSolicitud
            {
                Identificador = BootstrapAdministrador.UsuarioConsulta,
                Contrasena = contrasenaConsulta
            },
            new ContextoAcceso("127.0.0.1", "xUnit"),
            CancellationToken.None);

        Assert.False(login.Respuesta.RequiereCambioContrasena);
        Assert.Equal(BootstrapAdministrador.RolConsultaId, login.Respuesta.Usuario.RolId);
    }

    [Fact]
    public async Task UsuarioConsulta_SegundaEjecucionEsRechazadaSinDuplicados()
    {
        await using var contexto = CrearContexto();
        var (bootstrap, _, _, _) = CrearBootstrap(contexto);
        await bootstrap.EjecutarAsync(
            CrearSolicitud(CrearContrasenaValida()),
            CancellationToken.None);
        await bootstrap.CrearUsuarioConsultaAsync(
            CrearContrasenaValida(),
            CancellationToken.None);

        await Assert.ThrowsAsync<BootstrapAdministradorRechazadoException>(() =>
            bootstrap.CrearUsuarioConsultaAsync(
                CrearContrasenaValida(),
                CancellationToken.None));

        Assert.Single(contexto.Roles, item => item.Id == BootstrapAdministrador.RolConsultaId);
        Assert.Single(contexto.Usuarios, item => item.UserName == BootstrapAdministrador.UsuarioConsulta);
    }

    [Fact]
    public async Task UsuarioConsulta_SinAdministradorActivoEsRechazadoSinCambios()
    {
        await using var contexto = CrearContexto();
        var (bootstrap, _, _, _) = CrearBootstrap(contexto);

        await Assert.ThrowsAsync<BootstrapAdministradorRechazadoException>(() =>
            bootstrap.CrearUsuarioConsultaAsync(
                CrearContrasenaValida(),
                CancellationToken.None));

        Assert.Empty(contexto.Roles);
        Assert.Empty(contexto.Usuarios);
        Assert.Empty(contexto.UsuariosSucursales);
    }

    [Fact]
    public async Task BaseVacia_CreaEntornoCompletoYAdministradorTemporal()
    {
        await using var contexto = CrearContexto();
        var (bootstrap, hasher, opciones, tiempo) = CrearBootstrap(contexto);
        var contrasenaInicial = CrearContrasenaValida();

        var resultado = await bootstrap.EjecutarAsync(
            CrearSolicitud(contrasenaInicial),
            CancellationToken.None);

        Assert.Equal(17, resultado.Registros.Count);
        Assert.Equal(1, await contexto.ClaseEstados.CountAsync());
        Assert.Equal(1, await contexto.Estados.CountAsync());
        Assert.Equal(1, await contexto.Modulos.CountAsync());
        Assert.Equal(1, await contexto.ClaseTipos.CountAsync());
        Assert.Equal(2, await contexto.Tipos.CountAsync());
        Assert.Equal(1, await contexto.NivelesDeAccesos.CountAsync());
        Assert.Equal(1, await contexto.Roles.CountAsync());
        Assert.Equal(1, await contexto.AccionesSistema.CountAsync());
        Assert.Equal(1, await contexto.Permisos.CountAsync());
        Assert.Equal(1, await contexto.RolesPermisos.CountAsync());
        Assert.Equal(1, await contexto.Empresas.CountAsync());
        Assert.Equal(1, await contexto.Proveedores.CountAsync());
        Assert.Equal(1, await contexto.Sucursales.CountAsync());
        Assert.Equal(1, await contexto.Usuarios.CountAsync());
        Assert.Equal(1, await contexto.UsuariosSucursales.CountAsync());
        Assert.Equal(1, await contexto.Bitacoras.CountAsync());

        var empresa = await contexto.Empresas.SingleAsync();
        var proveedor = await contexto.Proveedores.SingleAsync();
        var sucursal = await contexto.Sucursales.SingleAsync();
        Assert.Equal(empresa.Id, proveedor.EmpresaId);
        Assert.Equal(BootstrapAdministrador.CodigoProveedorInicial, proveedor.CodigoProveedor);
        Assert.Equal("Proveedor inicial", proveedor.NombreProveedor);
        Assert.Equal((short)0, proveedor.DiasCredito);
        Assert.Null(proveedor.Nit);
        Assert.Null(proveedor.Telefono);
        Assert.Null(proveedor.Correo);
        Assert.Null(proveedor.Direccion);
        Assert.Null(proveedor.LimiteCredito);
        Assert.Null(proveedor.MonedaId);
        Assert.Equal(proveedor.Id, sucursal.ProveedorId);
        Assert.Equal(empresa.Id, sucursal.EmpresaId);
        Assert.Equal(proveedor.EstadoId, sucursal.EstadoId);
        Assert.Equal("Proveedores", resultado.Registros[12].Tabla);
        Assert.Equal(proveedor.Id.ToString(), resultado.Registros[12].Identificador);

        var usuario = await contexto.Usuarios.SingleAsync();
        Assert.Equal("admin", usuario.UserName);
        Assert.Equal("admin@heladeria.local", usuario.Email);
        Assert.Equal(BootstrapAdministrador.RolAdministradorId, usuario.RoleId);
        Assert.NotEqual(contrasenaInicial, usuario.PasswordHash);
        Assert.NotNull(usuario.SecurityStamp);
        Assert.Equal(64, usuario.SecurityStamp.Length);
        Assert.True(usuario.ContraseniaTemporal);
        Assert.Null(usuario.UltimoCambioDeContrasenia);
        Assert.Null(usuario.PasswordTemporal);
        Assert.Equal(usuario.HorarioInicio, usuario.HorarioFinal);
        Assert.NotEqual(
            PasswordVerificationResult.Failed,
            hasher.VerifyHashedPassword(usuario, usuario.PasswordHash, contrasenaInicial));

        contexto.ChangeTracker.Clear();
        var servicioAutenticacion = new ServicioAutenticacion(
            contexto,
            hasher,
            new GeneradorTokensJwt(opciones, tiempo),
            opciones,
            tiempo,
            new ServicioPermisos(contexto, opciones),
            NullLogger<ServicioAutenticacion>.Instance);
        var login = await servicioAutenticacion.IniciarSesionAsync(
            new IniciarSesionSolicitud
            {
                Identificador = "admin",
                Contrasena = contrasenaInicial
            },
            new ContextoAcceso("127.0.0.1", "xUnit"),
            CancellationToken.None);

        Assert.True(login.Respuesta.RequiereCambioContrasena);
    }

    [Fact]
    public async Task SegundaEjecucion_EsRechazadaSinCrearMasRegistros()
    {
        await using var contexto = CrearContexto();
        var (bootstrap, _, _, _) = CrearBootstrap(contexto);
        var solicitud = CrearSolicitud(CrearContrasenaValida());
        await bootstrap.EjecutarAsync(solicitud, CancellationToken.None);

        await Assert.ThrowsAsync<BootstrapAdministradorRechazadoException>(() =>
            bootstrap.EjecutarAsync(solicitud, CancellationToken.None));

        Assert.Equal(1, await contexto.Usuarios.CountAsync());
        Assert.Equal(1, await contexto.Roles.CountAsync());
        Assert.Equal(1, await contexto.Bitacoras.CountAsync());
    }

    [Fact]
    public async Task RolAdminExistente_RechazaSinCambios()
    {
        await using var contexto = CrearContexto();
        contexto.Roles.Add(new Roles
        {
            Id = BootstrapAdministrador.RolAdministradorId,
            Nombre = "Administrador",
            NivelDeAccesoId = 1,
            CambioDeContrasenia = 12,
            TipoId = 1,
            AplicarConfiguracionesPorRol = true
        });
        await contexto.SaveChangesAsync();
        var (bootstrap, _, _, _) = CrearBootstrap(contexto);

        await Assert.ThrowsAsync<BootstrapAdministradorRechazadoException>(() =>
            bootstrap.EjecutarAsync(
                CrearSolicitud(CrearContrasenaValida()),
                CancellationToken.None));

        Assert.Empty(contexto.Usuarios);
        Assert.Empty(contexto.Empresas);
    }

    [Fact]
    public async Task MarcadorExistente_RechazaSinCambios()
    {
        await using var contexto = CrearContexto();
        contexto.Bitacoras.Add(new Bitacoras
        {
            Entidad = BootstrapAdministrador.EntidadMarcador,
            Accion = BootstrapAdministrador.AccionMarcador,
            Fecha = DateTime.UtcNow
        });
        await contexto.SaveChangesAsync();
        var (bootstrap, _, _, _) = CrearBootstrap(contexto);

        await Assert.ThrowsAsync<BootstrapAdministradorRechazadoException>(() =>
            bootstrap.EjecutarAsync(
                CrearSolicitud(CrearContrasenaValida()),
                CancellationToken.None));

        Assert.Empty(contexto.Usuarios);
        Assert.Empty(contexto.Roles);
        Assert.Single(contexto.Bitacoras);
    }

    [Fact]
    public async Task UsuarioAdministradorExistente_RechazaSinCambios()
    {
        await using var contexto = CrearContexto();
        contexto.Roles.Add(new Roles
        {
            Id = "OPERADOR",
            Nombre = "Operador",
            NivelDeAccesoId = 1,
            CambioDeContrasenia = 12,
            TipoId = 1,
            AplicarConfiguracionesPorRol = false
        });
        contexto.Usuarios.Add(new Usuarios
        {
            Email = "otro@heladeria.local",
            UserName = "admin",
            IdentityId = "admin-existente",
            PasswordHash = "no-utilizado",
            RoleId = "OPERADOR",
            HorarioInicio = new TimeOnly(0, 0),
            HorarioFinal = new TimeOnly(0, 0),
            Imagen = "sin-imagen",
            Soundex = "A353",
            EstadoId = 1,
            TipoInicioSesionId = 1
        });
        await contexto.SaveChangesAsync();
        var (bootstrap, _, _, _) = CrearBootstrap(contexto);

        await Assert.ThrowsAsync<BootstrapAdministradorRechazadoException>(() =>
            bootstrap.EjecutarAsync(
                CrearSolicitud(CrearContrasenaValida()),
                CancellationToken.None));

        Assert.Single(contexto.Usuarios);
        Assert.Empty(contexto.Empresas);
    }

    [Fact]
    public async Task ContrasenaDebil_RechazaAntesDeCrearRegistros()
    {
        await using var contexto = CrearContexto();
        var (bootstrap, _, _, _) = CrearBootstrap(contexto);

        await Assert.ThrowsAsync<ValidacionSeguridadException>(() =>
            bootstrap.EjecutarAsync(
                new SolicitudBootstrapAdministrador(
                    "admin",
                    "admin@heladeria.local",
                    "debil"),
                CancellationToken.None));

        Assert.Empty(contexto.Usuarios);
        Assert.Empty(contexto.Roles);
    }

    [Fact]
    public async Task CatalogosPrevios_NoAsumeIdsFijosNiIdentity()
    {
        await using var contexto = CrearContexto();
        contexto.ClaseEstados.Add(new ClaseEstados
        {
            Id = 10,
            Nombre = "Otro",
            Activo = true
        });
        contexto.Estados.Add(new Estados
        {
            Id = 20,
            Nombre = "Inactivo",
            Activo = false,
            ClaseEstadoId = 10
        });
        contexto.ClaseTipos.Add(new ClaseTipos
        {
            Id = 30,
            Nombre = "Otro",
            EstadoId = 20
        });
        contexto.Tipos.Add(new Tipos
        {
            Id = 40,
            Nombre = "Otro",
            EstadoId = 20,
            ClaseTipoId = 30
        });
        contexto.NivelesDeAccesos.Add(new NivelesDeAccesos
        {
            Id = 50,
            Nombre = "Otro"
        });
        contexto.AccionesSistema.Add(new AccionesSistema
        {
            Id = 80,
            Codigo = "CONSULTAR",
            Nombre = "Consultar",
            EstadoId = 20
        });
        contexto.Proveedores.Add(new Proveedores
        {
            Id = 90,
            EmpresaId = 900,
            CodigoProveedor = "OTRO",
            NombreProveedor = "Otro proveedor",
            DiasCredito = 0,
            EstadoId = 20
        });
        await contexto.SaveChangesAsync();
        var (bootstrap, _, _, _) = CrearBootstrap(contexto);

        await bootstrap.EjecutarAsync(
            CrearSolicitud(CrearContrasenaValida()),
            CancellationToken.None);

        Assert.Contains(contexto.ClaseEstados, item => item.Id == 11);
        Assert.Contains(contexto.Estados, item => item.Id == 21 && item.Nombre == "Activo");
        Assert.Contains(contexto.ClaseTipos, item => item.Id == 31);
        Assert.Contains(contexto.Tipos, item => item.Id == 41);
        Assert.Contains(contexto.Tipos, item => item.Id == 42);
        Assert.Contains(contexto.NivelesDeAccesos, item => item.Id == 51);
        Assert.Contains(
            contexto.AccionesSistema,
            item => item.Id > 80 && item.Codigo == "ADMINISTRAR");
        var proveedorInicial = await contexto.Proveedores.SingleAsync(
            item => item.CodigoProveedor == BootstrapAdministrador.CodigoProveedorInicial);
        var sucursalInicial = await contexto.Sucursales.SingleAsync();
        Assert.True(proveedorInicial.Id > 90);
        Assert.Equal(proveedorInicial.Id, sucursalInicial.ProveedorId);
    }

    private static HeladeriaDbContext CrearContexto()
    {
        var opciones = new DbContextOptionsBuilder<HeladeriaDbContext>()
            .UseInMemoryDatabase($"bootstrap-{Guid.NewGuid():N}")
            .Options;
        return new HeladeriaDbContext(opciones);
    }

    private static (
        BootstrapAdministrador Bootstrap,
        PasswordHasher<Usuarios> Hasher,
        IOptions<OpcionesSeguridad> Opciones,
        TiempoFijo Tiempo) CrearBootstrap(HeladeriaDbContext contexto)
    {
        var hasher = new PasswordHasher<Usuarios>();
        var opciones = Options.Create(EscenarioSeguridad.CrearOpciones());
        var tiempo = new TiempoFijo(
            new DateTimeOffset(2026, 9, 3, 18, 0, 0, TimeSpan.Zero));
        return (
            new BootstrapAdministrador(contexto, hasher, opciones, tiempo),
            hasher,
            opciones,
            tiempo);
    }

    private static SolicitudBootstrapAdministrador CrearSolicitud(string contrasena) => new(
        "admin",
        "admin@heladeria.local",
        contrasena);

    private static string CrearContrasenaValida() =>
        $"Bootstrap#{Guid.NewGuid():N}Aa1";
}
