using System.Data;
using System.Security.Cryptography;
using Heladeria.Aplicacion.Seguridad.Autenticacion;
using Heladeria.Aplicacion.Seguridad.Autorizacion;
using Heladeria.Aplicacion.Seguridad.Configuracion;
using Heladeria.Aplicacion.Seguridad.Excepciones;
using Heladeria.Infraestructura.Persistencia;
using Heladeria.Infraestructura.Persistencia.Entidades;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Heladeria.Infraestructura.Seguridad.Autenticacion;

public sealed class ServicioAutenticacion : IServicioAutenticacion
{
    private const string MotivoCredencialesInvalidas = "CREDENCIALES_INVALIDAS";
    private const string MotivoBloqueoAplicado = "BLOQUEO_APLICADO";
    private const string MotivoBloqueoVigente = "BLOQUEO_VIGENTE";

    private readonly HeladeriaDbContext _contexto;
    private readonly IPasswordHasher<Usuarios> _passwordHasher;
    private readonly IGeneradorTokensJwt _generadorTokens;
    private readonly OpcionesSeguridad _opciones;
    private readonly EvaluadorSeguridadUsuario _evaluador;
    private readonly IServicioPermisos _servicioPermisos;
    private readonly ILogger<ServicioAutenticacion> _logger;
    private readonly string _hashFalso;

    public ServicioAutenticacion(
        HeladeriaDbContext contexto,
        IPasswordHasher<Usuarios> passwordHasher,
        IGeneradorTokensJwt generadorTokens,
        IOptions<OpcionesSeguridad> opciones,
        TimeProvider timeProvider,
        IServicioPermisos servicioPermisos,
        ILogger<ServicioAutenticacion> logger)
    {
        _contexto = contexto;
        _passwordHasher = passwordHasher;
        _generadorTokens = generadorTokens;
        _opciones = opciones.Value;
        _evaluador = new EvaluadorSeguridadUsuario(opciones.Value, timeProvider);
        _servicioPermisos = servicioPermisos;
        _logger = logger;
        _hashFalso = passwordHasher.HashPassword(
            new Usuarios(),
            "Verificacion-constante-no-valida-2026!");
    }

    public async Task<ResultadoAutenticacion> IniciarSesionAsync(
        IniciarSesionSolicitud solicitud,
        ContextoAcceso contextoAcceso,
        CancellationToken cancellationToken)
    {
        await using var transaccion = await IniciarTransaccionAsync(cancellationToken);
        var identificador = solicitud.Identificador.Trim();
        var consultaUsuario = _contexto.Database.IsSqlServer()
            ? _contexto.Usuarios.FromSqlInterpolated(
                $"SELECT * FROM [dbo].[Usuarios] WITH (UPDLOCK, HOLDLOCK) WHERE [UserName] = {identificador} OR [Email] = {identificador}")
            : _contexto.Usuarios.Where(
                item => item.UserName == identificador || item.Email == identificador);
        var usuario = await ConsultaUsuariosCompleta(consultaUsuario)
            .SingleOrDefaultAsync(
                cancellationToken);

        if (usuario is null)
        {
            _ = VerificarHashFalso(solicitud.Contrasena);
            RegistrarHistorial(null, identificador, false, MotivoCredencialesInvalidas, contextoAcceso);
            await GuardarYConfirmarAsync(transaccion, cancellationToken);
            throw new CredencialesInvalidasException();
        }

        if (!_evaluador.EsEstadoActivo(usuario.Estado)
            || !_evaluador.EstaEnHorarioPermitido(usuario))
        {
            _ = VerificarHashFalso(solicitud.Contrasena);
            RegistrarHistorial(usuario.Id, usuario.UserName, false, "USUARIO_NO_HABILITADO", contextoAcceso);
            await GuardarYConfirmarAsync(transaccion, cancellationToken);
            throw new CredencialesInvalidasException();
        }

        if (await TieneBloqueoVigenteAsync(usuario, cancellationToken))
        {
            RegistrarHistorial(usuario.Id, usuario.UserName, false, MotivoBloqueoVigente, contextoAcceso);
            await GuardarYConfirmarAsync(transaccion, cancellationToken);
            throw new CredencialesInvalidasException();
        }

        var resultadoPassword = VerificarPassword(usuario, solicitud.Contrasena);
        if (resultadoPassword == PasswordVerificationResult.Failed)
        {
            usuario.AccessFailedCount = usuario.AccessFailedCount == int.MaxValue
                ? int.MaxValue
                : usuario.AccessFailedCount + 1;

            var motivo = usuario.AccessFailedCount >= _opciones.Bloqueo.IntentosMaximos
                ? MotivoBloqueoAplicado
                : MotivoCredencialesInvalidas;

            RegistrarHistorial(usuario.Id, usuario.UserName, false, motivo, contextoAcceso);
            await GuardarYConfirmarAsync(transaccion, cancellationToken);

            if (motivo == MotivoBloqueoAplicado)
            {
                _logger.LogWarning("Usuario bloqueado temporalmente. UsuarioId: {UsuarioId}", usuario.Id);
            }

            throw new CredencialesInvalidasException();
        }

        var sucursales = ObtenerAsignacionesActivas(usuario);
        var asignacion = solicitud.SucursalId is long sucursalSolicitada
            ? sucursales.SingleOrDefault(item => item.SucursalId == sucursalSolicitada)
            : sucursales.FirstOrDefault(item => item.EsPrincipal) ?? sucursales.FirstOrDefault();

        if (asignacion is null)
        {
            RegistrarHistorial(usuario.Id, usuario.UserName, false, "SUCURSAL_NO_AUTORIZADA", contextoAcceso);
            await GuardarYConfirmarAsync(transaccion, cancellationToken);
            throw new CredencialesInvalidasException();
        }

        var filtroSesionVigente = ReglasSesionPersistida.CrearFiltroVigente(
            ahoraUtc: _evaluador.AhoraUtc,
            _opciones.Jwt.DiasRefreshToken);
        var tieneSesionActiva = await _contexto.SesionesUsuarios
            .Where(filtroSesionVigente)
            .AnyAsync(item => item.UsuarioId == usuario.Id, cancellationToken);

        if (tieneSesionActiva)
        {
            _logger.LogInformation(
                "Inicio de sesión rechazado porque el usuario ya tiene una sesión activa. UsuarioId: {UsuarioId}",
                usuario.Id);
            throw new SesionActivaException();
        }

        if (resultadoPassword == PasswordVerificationResult.SuccessRehashNeeded)
        {
            usuario.PasswordHash = _passwordHasher.HashPassword(usuario, solicitud.Contrasena);
        }

        usuario.AccessFailedCount = 0;
        var ahora = _evaluador.AhoraUtc;
        var sesion = new SesionesUsuarios
        {
            UsuarioId = usuario.Id,
            FechaInicio = ahora.UtcDateTime,
            DireccionIp = Recortar(contextoAcceso.DireccionIp, 45),
            Dispositivo = Recortar(contextoAcceso.Dispositivo, 200),
            Cerrada = false
        };

        _contexto.SesionesUsuarios.Add(sesion);
        RegistrarHistorial(usuario.Id, usuario.UserName, true, "LOGIN_EXITOSO", contextoAcceso);
        await _contexto.SaveChangesAsync(cancellationToken);

        var refreshToken = _generadorTokens.CrearRefreshToken(sesion.Id, asignacion.SucursalId);
        sesion.TokenIdentificador = _generadorTokens.CalcularHash(refreshToken);
        await _contexto.SaveChangesAsync(cancellationToken);

        var resultado = ConstruirResultado(
            usuario,
            asignacion.Sucursal,
            sesion.Id,
            refreshToken,
            ahora.AddDays(_opciones.Jwt.DiasRefreshToken));

        await ConfirmarAsync(transaccion, cancellationToken);
        _logger.LogInformation("Inicio de sesión exitoso. UsuarioId: {UsuarioId}, SesionId: {SesionId}", usuario.Id, sesion.Id);
        return resultado;
    }

    public async Task<ResultadoAutenticacion> RenovarAsync(
        string refreshToken,
        ContextoAcceso contextoAcceso,
        CancellationToken cancellationToken)
    {
        await using var transaccion = await IniciarTransaccionAsync(cancellationToken);
        var validacion = await ValidarRefreshTokenAsync(
            refreshToken,
            transaccion,
            cancellationToken);
        var nuevoRefresh = _generadorTokens.CrearRefreshToken(
            validacion.Sesion.Id,
            validacion.Sucursal.Id);

        validacion.Sesion.TokenIdentificador = _generadorTokens.CalcularHash(nuevoRefresh);
        RegistrarBitacora(
            validacion.Usuario,
            validacion.Sucursal,
            validacion.Sesion.Id,
            "TOKEN_RENOVADO",
            contextoAcceso);

        await GuardarYConfirmarAsync(transaccion, cancellationToken);

        return ConstruirResultado(
            validacion.Usuario,
            validacion.Sucursal,
            validacion.Sesion.Id,
            nuevoRefresh,
            FechaInicioUtc(validacion.Sesion).AddDays(_opciones.Jwt.DiasRefreshToken));
    }

    public async Task<ResultadoAutenticacion> SeleccionarSucursalAsync(
        int usuarioId,
        long sesionId,
        long sucursalActualId,
        string refreshToken,
        SeleccionarSucursalSolicitud solicitud,
        ContextoAcceso contextoAcceso,
        CancellationToken cancellationToken)
    {
        await using var transaccion = await IniciarTransaccionAsync(cancellationToken);
        var validacion = await ValidarRefreshTokenAsync(
            refreshToken,
            transaccion,
            cancellationToken);

        if (validacion.Usuario.Id != usuarioId
            || validacion.Sesion.Id != sesionId
            || validacion.Sucursal.Id != sucursalActualId)
        {
            throw new TokenRenovacionInvalidoException();
        }

        var nuevaAsignacion = ObtenerAsignacionesActivas(validacion.Usuario)
            .SingleOrDefault(item => item.SucursalId == solicitud.SucursalId);

        if (nuevaAsignacion is null)
        {
            RegistrarBitacora(
                validacion.Usuario,
                validacion.Sucursal,
                validacion.Sesion.Id,
                "SUCURSAL_DENEGADA",
                contextoAcceso,
                FormattableString.Invariant($"Sucursal solicitada: {solicitud.SucursalId}"));
            await GuardarYConfirmarAsync(transaccion, cancellationToken);
            throw new AccesoDenegadoException("No tiene acceso a la sucursal solicitada.");
        }

        var nuevoRefresh = _generadorTokens.CrearRefreshToken(
            validacion.Sesion.Id,
            nuevaAsignacion.SucursalId);
        validacion.Sesion.TokenIdentificador = _generadorTokens.CalcularHash(nuevoRefresh);

        RegistrarBitacora(
            validacion.Usuario,
            nuevaAsignacion.Sucursal,
            validacion.Sesion.Id,
            "SUCURSAL_SELECCIONADA",
            contextoAcceso);

        await GuardarYConfirmarAsync(transaccion, cancellationToken);

        return ConstruirResultado(
            validacion.Usuario,
            nuevaAsignacion.Sucursal,
            validacion.Sesion.Id,
            nuevoRefresh,
            FechaInicioUtc(validacion.Sesion).AddDays(_opciones.Jwt.DiasRefreshToken));
    }

    public async Task CerrarSesionAsync(
        int usuarioId,
        long sesionId,
        long sucursalId,
        ContextoAcceso contextoAcceso,
        CancellationToken cancellationToken)
    {
        var sesion = await _contexto.SesionesUsuarios
            .SingleOrDefaultAsync(
                item => item.Id == sesionId && item.UsuarioId == usuarioId,
                cancellationToken);

        if (sesion is null || sesion.Cerrada)
        {
            return;
        }

        var usuario = await ConsultaUsuariosCompleta()
            .SingleAsync(item => item.Id == usuarioId, cancellationToken);
        var sucursal = ObtenerAsignacionesActivas(usuario)
            .SingleOrDefault(item => item.SucursalId == sucursalId)
            ?.Sucursal;

        sesion.Cerrada = true;
        sesion.FechaFin = _evaluador.AhoraUtc.UtcDateTime;
        sesion.TokenIdentificador = null;

        RegistrarBitacora(usuario, sucursal, sesion.Id, "LOGOUT", contextoAcceso);
        await _contexto.SaveChangesAsync(cancellationToken);
    }

    public async Task CambiarContrasenaAsync(
        int usuarioId,
        long sucursalId,
        CambiarContrasenaSolicitud solicitud,
        ContextoAcceso contextoAcceso,
        CancellationToken cancellationToken)
    {
        await using var transaccion = await IniciarTransaccionAsync(cancellationToken);
        var usuario = await ConsultaUsuariosCompleta()
            .SingleOrDefaultAsync(item => item.Id == usuarioId, cancellationToken)
            ?? throw new AccesoDenegadoException();
        var sucursal = ObtenerAsignacionesActivas(usuario)
            .SingleOrDefault(item => item.SucursalId == sucursalId)
            ?.Sucursal;

        if (VerificarPassword(usuario, solicitud.ContrasenaActual) == PasswordVerificationResult.Failed)
        {
            RegistrarBitacora(
                usuario,
                sucursal,
                null,
                "CAMBIO_CONTRASENA_FALLIDO",
                contextoAcceso,
                entidad: "Usuarios");
            await GuardarYConfirmarAsync(transaccion, cancellationToken);
            throw new ValidacionSeguridadException(
                "La contraseña actual no es válida.",
                "CONTRASENA_ACTUAL_INVALIDA");
        }

        _evaluador.ValidarNuevaContrasena(
            solicitud.NuevaContrasena,
            solicitud.Confirmacion);

        if (VerificarPassword(usuario, solicitud.NuevaContrasena) != PasswordVerificationResult.Failed)
        {
            throw new ValidacionSeguridadException(
                "La nueva contraseña debe ser diferente de la actual.",
                "CONTRASENA_SIN_CAMBIOS");
        }

        usuario.PasswordHash = _passwordHasher.HashPassword(usuario, solicitud.NuevaContrasena);
        usuario.SecurityStamp = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        usuario.UltimoCambioDeContrasenia = _evaluador.FechaLocal;
        usuario.ContraseniaTemporal = false;
        usuario.PasswordTemporal = null;
        usuario.AccessFailedCount = 0;

        var sesionesAbiertas = await _contexto.SesionesUsuarios
            .Where(item => item.UsuarioId == usuarioId && !item.Cerrada)
            .ToListAsync(cancellationToken);

        foreach (var sesion in sesionesAbiertas)
        {
            sesion.Cerrada = true;
            sesion.FechaFin = _evaluador.AhoraUtc.UtcDateTime;
            sesion.TokenIdentificador = null;
        }

        RegistrarBitacora(
            usuario,
            sucursal,
            null,
            "CONTRASENA_CAMBIADA",
            contextoAcceso,
            entidad: "Usuarios");
        await GuardarYConfirmarAsync(transaccion, cancellationToken);
        _logger.LogInformation("Contraseña cambiada y sesiones revocadas. UsuarioId: {UsuarioId}", usuarioId);
    }

    public async Task<SesionActualRespuesta> ObtenerSesionAsync(
        int usuarioId,
        long sucursalId,
        CancellationToken cancellationToken)
    {
        var usuario = await ConsultaUsuariosCompleta()
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == usuarioId, cancellationToken)
            ?? throw new AccesoDenegadoException();
        var asignacion = ObtenerAsignacionesActivas(usuario)
            .SingleOrDefault(item => item.SucursalId == sucursalId)
            ?? throw new AccesoDenegadoException();

        var permisos = await _servicioPermisos.ObtenerPermisosAsync(
            usuarioId,
            cancellationToken);

        return new SesionActualRespuesta(
            ConstruirUsuario(usuario, asignacion.Sucursal),
            _evaluador.RequiereCambioContrasena(usuario),
            permisos);
    }

    private IQueryable<Usuarios> ConsultaUsuariosCompleta(
        IQueryable<Usuarios>? consulta = null) =>
        (consulta ?? _contexto.Usuarios)
            .Include(item => item.Estado)
            .Include(item => item.Role)
            .Include(item => item.UsuariosSucursales)
                .ThenInclude(item => item.Estado)
            .Include(item => item.UsuariosSucursales)
                .ThenInclude(item => item.Sucursal)
                    .ThenInclude(item => item.Estado)
            .Include(item => item.UsuariosSucursales)
                .ThenInclude(item => item.Sucursal)
                    .ThenInclude(item => item.Empresa)
                        .ThenInclude(item => item.Estado)
            .AsSplitQuery();

    private List<UsuariosSucursales> ObtenerAsignacionesActivas(Usuarios usuario) =>
        usuario.UsuariosSucursales
            .Where(item =>
                _evaluador.EsEstadoActivo(item.Estado)
                && _evaluador.EsEstadoActivo(item.Sucursal.Estado)
                && _evaluador.EsEstadoActivo(item.Sucursal.Empresa.Estado))
            .OrderByDescending(item => item.EsPrincipal)
            .ThenBy(item => item.Sucursal.Nombre)
            .ToList();

    private async Task<bool> TieneBloqueoVigenteAsync(
        Usuarios usuario,
        CancellationToken cancellationToken)
    {
        if (usuario.AccessFailedCount < _opciones.Bloqueo.IntentosMaximos)
        {
            return false;
        }

        var fechaBloqueo = await _contexto.HistorialAccesos
            .Where(item =>
                item.UsuarioId == usuario.Id
                && item.Motivo == MotivoBloqueoAplicado)
            .OrderByDescending(item => item.Fecha)
            .Select(item => (DateTime?)item.Fecha)
            .FirstOrDefaultAsync(cancellationToken);

        if (fechaBloqueo is not null
            && DateTime.SpecifyKind(fechaBloqueo.Value, DateTimeKind.Utc)
                .AddMinutes(_opciones.Bloqueo.MinutosBloqueo) > _evaluador.AhoraUtc.UtcDateTime)
        {
            return true;
        }

        usuario.AccessFailedCount = 0;
        return false;
    }

    private async Task<(SesionesUsuarios Sesion, Usuarios Usuario, Sucursales Sucursal)>
        ValidarRefreshTokenAsync(
            string refreshToken,
            IDbContextTransaction? transaccion,
            CancellationToken cancellationToken)
    {
        if (!_generadorTokens.IntentarLeerRefreshToken(
                refreshToken,
                out var sesionId,
                out var sucursalId))
        {
            throw new TokenRenovacionInvalidoException();
        }

        var sesion = await _contexto.SesionesUsuarios
            .SingleOrDefaultAsync(item => item.Id == sesionId, cancellationToken);

        if (sesion is null
            || !_generadorTokens.HashCoincide(refreshToken, sesion.TokenIdentificador))
        {
            throw new TokenRenovacionInvalidoException();
        }

        if (!ReglasSesionPersistida.EsVigente(
                sesion,
                _evaluador.AhoraUtc,
                _opciones.Jwt.DiasRefreshToken))
        {
            sesion.Cerrada = true;
            sesion.FechaFin = _evaluador.AhoraUtc.UtcDateTime;
            sesion.TokenIdentificador = null;
            await GuardarYConfirmarAsync(transaccion, cancellationToken);
            throw new TokenRenovacionInvalidoException();
        }

        var usuario = await ConsultaUsuariosCompleta()
            .SingleOrDefaultAsync(item => item.Id == sesion.UsuarioId, cancellationToken);
        var asignacion = usuario is null
            ? null
            : ObtenerAsignacionesActivas(usuario)
                .SingleOrDefault(item => item.SucursalId == sucursalId);

        if (usuario is null
            || !_evaluador.EsEstadoActivo(usuario.Estado)
            || !_evaluador.EstaEnHorarioPermitido(usuario)
            || asignacion is null)
        {
            sesion.Cerrada = true;
            sesion.FechaFin = _evaluador.AhoraUtc.UtcDateTime;
            sesion.TokenIdentificador = null;
            await GuardarYConfirmarAsync(transaccion, cancellationToken);
            throw new TokenRenovacionInvalidoException();
        }

        return (sesion, usuario, asignacion.Sucursal);
    }

    private ResultadoAutenticacion ConstruirResultado(
        Usuarios usuario,
        Sucursales sucursal,
        long sesionId,
        string refreshToken,
        DateTimeOffset expiraRefreshTokenUtc)
    {
        var requiereCambio = _evaluador.RequiereCambioContrasena(usuario);
        var accessToken = _generadorTokens.CrearAccessToken(
            usuario,
            sucursal,
            sesionId,
            requiereCambio);

        return new ResultadoAutenticacion(
            new IniciarSesionRespuesta(
                true,
                accessToken.Valor,
                accessToken.ExpiraUtc,
                ConstruirUsuario(usuario, sucursal),
                requiereCambio),
            refreshToken,
            expiraRefreshTokenUtc);
    }

    private UsuarioAutenticadoRespuesta ConstruirUsuario(
        Usuarios usuario,
        Sucursales sucursalActiva)
    {
        var sucursales = ObtenerAsignacionesActivas(usuario)
            .Select(item => new SucursalUsuarioRespuesta(
                item.Sucursal.Id,
                item.Sucursal.Nombre,
                item.Sucursal.EmpresaId,
                item.Sucursal.Empresa.NombreComercial ?? item.Sucursal.Empresa.RazonSocial,
                item.EsPrincipal)
            {
                ZonaHoraria = Heladeria.Aplicacion.Tiempo.TiempoEmpresarial.Zona(
                    _opciones, item.Sucursal.EmpresaId, item.SucursalId)
            })
            .ToArray();

        return new UsuarioAutenticadoRespuesta(
            usuario.Id,
            usuario.UserName,
            usuario.Email,
            usuario.RoleId,
            usuario.Role.Nombre,
            sucursales.Single(item => item.Id == sucursalActiva.Id),
            sucursales);
    }

    private PasswordVerificationResult VerificarPassword(Usuarios usuario, string password)
    {
        try
        {
            return _passwordHasher.VerifyHashedPassword(usuario, usuario.PasswordHash, password);
        }
        catch (FormatException)
        {
            return PasswordVerificationResult.Failed;
        }
    }

    private PasswordVerificationResult VerificarHashFalso(string password) =>
        _passwordHasher.VerifyHashedPassword(new Usuarios(), _hashFalso, password);

    private void RegistrarHistorial(
        int? usuarioId,
        string identificador,
        bool exitoso,
        string motivo,
        ContextoAcceso contextoAcceso)
    {
        _contexto.HistorialAccesos.Add(new HistorialAccesos
        {
            UsuarioId = usuarioId,
            UserName = Recortar(identificador, 50),
            Fecha = _evaluador.AhoraUtc.UtcDateTime,
            DireccionIp = Recortar(contextoAcceso.DireccionIp, 45),
            Exitoso = exitoso,
            Motivo = Recortar(motivo, 300)
        });
    }

    private void RegistrarBitacora(
        Usuarios usuario,
        Sucursales? sucursal,
        long? sesionId,
        string accion,
        ContextoAcceso contextoAcceso,
        string? comentario = null,
        string entidad = "SesionesUsuarios")
    {
        _contexto.Bitacoras.Add(new Bitacoras
        {
            EmpresaId = sucursal?.EmpresaId,
            SucursalId = sucursal?.Id,
            Entidad = entidad,
            EntidadId = entidad == "Usuarios"
                ? usuario.Id.ToString()
                : sesionId?.ToString(),
            Accion = accion,
            Fecha = _evaluador.AhoraUtc.UtcDateTime,
            UsuarioId = usuario.Id,
            DireccionIp = Recortar(contextoAcceso.DireccionIp, 45),
            Equipo = Recortar(contextoAcceso.Dispositivo, 200),
            Comentario = Recortar(comentario, 500)
        });
    }

    private async Task<IDbContextTransaction?> IniciarTransaccionAsync(
        CancellationToken cancellationToken) =>
        _contexto.Database.IsRelational()
            ? await _contexto.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken)
            : null;

    private async Task GuardarYConfirmarAsync(
        IDbContextTransaction? transaccion,
        CancellationToken cancellationToken)
    {
        await _contexto.SaveChangesAsync(cancellationToken);
        await ConfirmarAsync(transaccion, cancellationToken);
    }

    private static Task ConfirmarAsync(
        IDbContextTransaction? transaccion,
        CancellationToken cancellationToken) =>
        transaccion is null
            ? Task.CompletedTask
            : transaccion.CommitAsync(cancellationToken);

    private static DateTimeOffset FechaInicioUtc(SesionesUsuarios sesion) =>
        new(DateTime.SpecifyKind(sesion.FechaInicio, DateTimeKind.Utc));

    private static string? Recortar(string? valor, int longitudMaxima) =>
        string.IsNullOrWhiteSpace(valor)
            ? null
            : valor.Trim()[..Math.Min(valor.Trim().Length, longitudMaxima)];
}
