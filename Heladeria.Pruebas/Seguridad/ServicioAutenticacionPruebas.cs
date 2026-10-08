using Heladeria.Aplicacion.Seguridad.Autenticacion;
using Heladeria.Aplicacion.Seguridad.Excepciones;
using Heladeria.Infraestructura.Persistencia.Entidades;
using Heladeria.Infraestructura.Seguridad.Autorizacion;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Heladeria.Pruebas.Seguridad;

public sealed class ServicioAutenticacionPruebas
{
    private static readonly ContextoAcceso ContextoAcceso = new("127.0.0.1", "xUnit");

    [Fact]
    public async Task LoginYSesionComunicanLaZonaEfectivaDeCadaSucursal()
    {
        await using var escenario = await EscenarioSeguridad.CrearAsync(incluirSegundaSucursal: true);
        var asignaciones = await escenario.Contexto.UsuariosSucursales.Include(x => x.Sucursal).OrderBy(x => x.SucursalId).ToArrayAsync();
        escenario.Opciones.ZonasHorariasEmpresas[asignaciones[0].Sucursal.EmpresaId] = "Europe/Madrid";
        escenario.Opciones.ZonasHorariasSucursales[asignaciones[0].SucursalId] = "America/Guatemala";
        var servicio = escenario.CrearServicio();
        var login = await IniciarSesionAsync(escenario, servicio);
        Assert.Equal("America/Guatemala", login.Respuesta.Usuario.Sucursales.Single(x => x.Id == asignaciones[0].SucursalId).ZonaHoraria);
        var sesion = await servicio.ObtenerSesionAsync(escenario.Usuario.Id, login.Respuesta.Usuario.SucursalActiva.Id, default);
        Assert.Equal(login.Respuesta.Usuario.SucursalActiva.ZonaHoraria, sesion.Usuario.SucursalActiva.ZonaHoraria);
    }

    [Fact]
    public async Task LoginCorrecto_CreaSesionHashYAuditoria()
    {
        await using var escenario = await EscenarioSeguridad.CrearAsync();

        var resultado = await IniciarSesionAsync(escenario);
        var sesion = await escenario.Contexto.SesionesUsuarios.SingleAsync();

        Assert.True(resultado.Respuesta.Exito);
        Assert.False(string.IsNullOrWhiteSpace(resultado.Respuesta.AccessToken));
        Assert.NotEqual(resultado.RefreshToken, sesion.TokenIdentificador);
        Assert.Equal(64, sesion.TokenIdentificador?.Length);
        Assert.Contains(
            escenario.Contexto.HistorialAccesos,
            acceso => acceso.Exitoso && acceso.Motivo == "LOGIN_EXITOSO");
    }

    [Fact]
    public async Task SegundoLoginValido_ConSesionVigenteDevuelveConflictoSinCrearOtraSesion()
    {
        await using var escenario = await EscenarioSeguridad.CrearAsync();
        var servicio = escenario.CrearServicio();
        var primerLogin = await IniciarSesionAsync(escenario, servicio);
        var hashOriginal = (await escenario.Contexto.SesionesUsuarios.SingleAsync())
            .TokenIdentificador;

        var error = await Assert.ThrowsAsync<SesionActivaException>(() =>
            IniciarSesionAsync(escenario, servicio));

        Assert.Equal("SESION_ACTIVA", error.Codigo);
        Assert.Single(escenario.Contexto.SesionesUsuarios);
        Assert.Equal(
            hashOriginal,
            (await escenario.Contexto.SesionesUsuarios.SingleAsync()).TokenIdentificador);
        Assert.False(string.IsNullOrWhiteSpace(primerLogin.Respuesta.AccessToken));
    }

    [Fact]
    public async Task Logout_PermiteNuevoLoginYLaSesionAnteriorPermaneceCerrada()
    {
        await using var escenario = await EscenarioSeguridad.CrearAsync();
        var servicio = escenario.CrearServicio();
        var primerLogin = await IniciarSesionAsync(escenario, servicio);
        var primeraSesion = await escenario.Contexto.SesionesUsuarios.SingleAsync();

        await servicio.CerrarSesionAsync(
            primerLogin.Respuesta.Usuario.Id,
            primeraSesion.Id,
            primerLogin.Respuesta.Usuario.SucursalActiva.Id,
            ContextoAcceso,
            CancellationToken.None);
        var segundoLogin = await IniciarSesionAsync(escenario, servicio);
        var sesiones = await escenario.Contexto.SesionesUsuarios
            .OrderBy(item => item.Id)
            .ToListAsync();

        Assert.Equal(2, sesiones.Count);
        Assert.True(sesiones[0].Cerrada);
        Assert.NotNull(sesiones[0].FechaFin);
        Assert.Null(sesiones[0].TokenIdentificador);
        Assert.False(sesiones[1].Cerrada);
        Assert.NotEqual(primerLogin.Respuesta.AccessToken, segundoLogin.Respuesta.AccessToken);
    }

    [Fact]
    public async Task SesionExpirada_NoImpideNuevoLogin()
    {
        await using var escenario = await EscenarioSeguridad.CrearAsync();
        escenario.Contexto.SesionesUsuarios.Add(new SesionesUsuarios
        {
            Id = 50,
            UsuarioId = escenario.Usuario.Id,
            FechaInicio = escenario.Tiempo.GetUtcNow()
                .AddDays(-escenario.Opciones.Jwt.DiasRefreshToken)
                .AddSeconds(-1)
                .UtcDateTime,
            TokenIdentificador = new string('A', 64),
            Cerrada = false
        });
        await escenario.Contexto.SaveChangesAsync();

        var resultado = await IniciarSesionAsync(escenario);

        Assert.True(resultado.Respuesta.Exito);
        Assert.Equal(2, await escenario.Contexto.SesionesUsuarios.CountAsync());
    }

    [Fact]
    public async Task ContrasenaIncorrecta_DevuelveErrorGenericoEIncrementaContador()
    {
        await using var escenario = await EscenarioSeguridad.CrearAsync();

        var error = await Assert.ThrowsAsync<CredencialesInvalidasException>(() =>
            escenario.CrearServicio().IniciarSesionAsync(
                new IniciarSesionSolicitud
                {
                    Identificador = "admin",
                    Contrasena = "incorrecta"
                },
                ContextoAcceso,
                CancellationToken.None));

        Assert.Equal("Credenciales inválidas.", error.Message);
        Assert.Equal(1, (await escenario.Contexto.Usuarios.SingleAsync()).AccessFailedCount);
        Assert.Contains(
            escenario.Contexto.HistorialAccesos,
            acceso => !acceso.Exitoso && acceso.Motivo == "CREDENCIALES_INVALIDAS");
    }

    [Fact]
    public async Task UsuarioInexistente_DevuelveMismoErrorGenerico()
    {
        await using var escenario = await EscenarioSeguridad.CrearAsync();

        var error = await Assert.ThrowsAsync<CredencialesInvalidasException>(() =>
            escenario.CrearServicio().IniciarSesionAsync(
                new IniciarSesionSolicitud
                {
                    Identificador = "nadie",
                    Contrasena = "incorrecta"
                },
                ContextoAcceso,
                CancellationToken.None));

        Assert.Equal("Credenciales inválidas.", error.Message);
        Assert.Contains(
            escenario.Contexto.HistorialAccesos,
            acceso => acceso.UsuarioId == null && !acceso.Exitoso);
    }

    [Fact]
    public async Task UsuarioInactivo_NoPuedeIniciarSesion()
    {
        await using var escenario = await EscenarioSeguridad.CrearAsync(usuarioActivo: false);

        await Assert.ThrowsAsync<CredencialesInvalidasException>(() =>
            IniciarSesionAsync(escenario));

        Assert.Empty(escenario.Contexto.SesionesUsuarios);
    }

    [Fact]
    public async Task IntentosFallidos_BloqueanTemporalmenteAunqueLuegoUseContrasenaCorrecta()
    {
        await using var escenario = await EscenarioSeguridad.CrearAsync();
        var servicio = escenario.CrearServicio();

        for (var intento = 0; intento < escenario.Opciones.Bloqueo.IntentosMaximos; intento++)
        {
            await Assert.ThrowsAsync<CredencialesInvalidasException>(() =>
                servicio.IniciarSesionAsync(
                    new IniciarSesionSolicitud
                    {
                        Identificador = "admin",
                        Contrasena = "incorrecta"
                    },
                    ContextoAcceso,
                    CancellationToken.None));
        }

        await Assert.ThrowsAsync<CredencialesInvalidasException>(() =>
            IniciarSesionAsync(escenario, servicio));
        Assert.Contains(
            escenario.Contexto.HistorialAccesos,
            acceso => acceso.Motivo == "BLOQUEO_APLICADO");
    }

    [Fact]
    public async Task RefreshValido_RotaTokenEInvalidaElAnterior()
    {
        await using var escenario = await EscenarioSeguridad.CrearAsync();
        var servicio = escenario.CrearServicio();
        var login = await IniciarSesionAsync(escenario, servicio);

        var renovado = await servicio.RenovarAsync(
            login.RefreshToken,
            ContextoAcceso,
            CancellationToken.None);

        Assert.NotEqual(login.RefreshToken, renovado.RefreshToken);
        Assert.NotEqual(login.Respuesta.AccessToken, renovado.Respuesta.AccessToken);
        Assert.Single(escenario.Contexto.SesionesUsuarios);
        await Assert.ThrowsAsync<TokenRenovacionInvalidoException>(() =>
            servicio.RenovarAsync(
                login.RefreshToken,
                ContextoAcceso,
                CancellationToken.None));
    }

    [Fact]
    public async Task RefreshRevocadoEnLogout_EsRechazado()
    {
        await using var escenario = await EscenarioSeguridad.CrearAsync();
        var servicio = escenario.CrearServicio();
        var login = await IniciarSesionAsync(escenario, servicio);
        var sesionId = (await escenario.Contexto.SesionesUsuarios.SingleAsync()).Id;

        await servicio.CerrarSesionAsync(
            login.Respuesta.Usuario.Id,
            sesionId,
            login.Respuesta.Usuario.SucursalActiva.Id,
            ContextoAcceso,
            CancellationToken.None);

        await Assert.ThrowsAsync<TokenRenovacionInvalidoException>(() =>
            servicio.RenovarAsync(
                login.RefreshToken,
                ContextoAcceso,
                CancellationToken.None));
        Assert.True((await escenario.Contexto.SesionesUsuarios.SingleAsync()).Cerrada);
    }

    [Fact]
    public async Task AccessTokenDeSesionRevocada_EsRechazadoPorValidacionPersistida()
    {
        await using var escenario = await EscenarioSeguridad.CrearAsync();
        var servicio = escenario.CrearServicio();
        var login = await IniciarSesionAsync(escenario, servicio);
        var sesion = await escenario.Contexto.SesionesUsuarios.SingleAsync();
        var validador = new ServicioValidacionSesion(
            escenario.Contexto,
            Options.Create(escenario.Opciones),
            escenario.Tiempo);

        Assert.True(await validador.EsValidaAsync(
            login.Respuesta.Usuario.Id,
            sesion.Id,
            login.Respuesta.Usuario.SucursalActiva.Id,
            CancellationToken.None));

        await servicio.CerrarSesionAsync(
            login.Respuesta.Usuario.Id,
            sesion.Id,
            login.Respuesta.Usuario.SucursalActiva.Id,
            ContextoAcceso,
            CancellationToken.None);

        Assert.False(await validador.EsValidaAsync(
            login.Respuesta.Usuario.Id,
            sesion.Id,
            login.Respuesta.Usuario.SucursalActiva.Id,
            CancellationToken.None));
    }

    [Fact]
    public async Task CambioContrasenaExitoso_ActualizaHashYRevocaSesiones()
    {
        const string nuevaContrasena = "Paleta#Nueva2026";
        await using var escenario = await EscenarioSeguridad.CrearAsync(contrasenaTemporal: true);
        var servicio = escenario.CrearServicio();
        var login = await IniciarSesionAsync(escenario, servicio);

        await servicio.CambiarContrasenaAsync(
            login.Respuesta.Usuario.Id,
            login.Respuesta.Usuario.SucursalActiva.Id,
            new CambiarContrasenaSolicitud
            {
                ContrasenaActual = EscenarioSeguridad.ContrasenaValida,
                NuevaContrasena = nuevaContrasena,
                Confirmacion = nuevaContrasena
            },
            ContextoAcceso,
            CancellationToken.None);

        var usuario = await escenario.Contexto.Usuarios.SingleAsync();
        Assert.True(escenario.VerificarContrasena(usuario, nuevaContrasena));
        Assert.False(usuario.ContraseniaTemporal);
        Assert.Null(usuario.PasswordTemporal);
        Assert.NotNull(usuario.UltimoCambioDeContrasenia);
        Assert.All(
            escenario.Contexto.SesionesUsuarios,
            sesion => Assert.True(sesion.Cerrada));
    }

    [Fact]
    public async Task ContrasenaTemporal_MarcaCambioObligatorio()
    {
        await using var escenario = await EscenarioSeguridad.CrearAsync(contrasenaTemporal: true);

        var resultado = await IniciarSesionAsync(escenario);

        Assert.True(resultado.Respuesta.RequiereCambioContrasena);
    }

    [Fact]
    public async Task SesionActual_DevuelvePermisosEfectivosDelUsuario()
    {
        await using var escenario = await EscenarioSeguridad.CrearAsync(incluirPermiso: true);
        var servicio = escenario.CrearServicio();
        var login = await IniciarSesionAsync(escenario, servicio);

        var sesion = await servicio.ObtenerSesionAsync(
            login.Respuesta.Usuario.Id,
            login.Respuesta.Usuario.SucursalActiva.Id,
            CancellationToken.None);

        Assert.Equal(login.Respuesta.Usuario.Id, sesion.Usuario.Id);
        Assert.Contains("SEGURIDAD.ADMINISTRAR", sesion.Permisos);
    }

    [Fact]
    public async Task SucursalNoAutorizada_EsRechazada()
    {
        await using var escenario = await EscenarioSeguridad.CrearAsync();

        await Assert.ThrowsAsync<CredencialesInvalidasException>(() =>
            escenario.CrearServicio().IniciarSesionAsync(
                new IniciarSesionSolicitud
                {
                    Identificador = "admin",
                    Contrasena = EscenarioSeguridad.ContrasenaValida,
                    SucursalId = 999
                },
                ContextoAcceso,
                CancellationToken.None));

        Assert.Empty(escenario.Contexto.SesionesUsuarios);
    }

    private static Task<ResultadoAutenticacion> IniciarSesionAsync(
        EscenarioSeguridad escenario,
        IServicioAutenticacion? servicio = null) =>
        (servicio ?? escenario.CrearServicio()).IniciarSesionAsync(
            new IniciarSesionSolicitud
            {
                Identificador = "admin",
                Contrasena = EscenarioSeguridad.ContrasenaValida
            },
            ContextoAcceso,
            CancellationToken.None);
}
