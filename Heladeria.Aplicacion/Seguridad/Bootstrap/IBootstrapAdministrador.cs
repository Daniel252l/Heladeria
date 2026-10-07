namespace Heladeria.Aplicacion.Seguridad.Bootstrap;

public interface IBootstrapAdministrador
{
    Task<ResultadoBootstrapAdministrador> EjecutarAsync(
        SolicitudBootstrapAdministrador solicitud,
        CancellationToken cancellationToken);
}

public interface IProvisionadorUsuarioConsulta
{
    Task<ResultadoBootstrapAdministrador> EjecutarAsync(
        string contrasena,
        CancellationToken cancellationToken);
}

public interface IPreparadorFase3
{
    Task<ResultadoBootstrapAdministrador> EjecutarAsync(CancellationToken cancellationToken);
}

public interface IPreparadorFase4
{
    Task<ResultadoBootstrapAdministrador> EjecutarAsync(CancellationToken cancellationToken);
}

public interface IPreparadorFase5
{
    Task<ResultadoBootstrapAdministrador> EjecutarAsync(CancellationToken cancellationToken);
}

public interface IPreparadorFase7
{
    Task<ResultadoBootstrapAdministrador> EjecutarAsync(CancellationToken cancellationToken);
}

public interface IPreparadorFase6
{
    Task<ResultadoBootstrapAdministrador> EjecutarAsync(CancellationToken cancellationToken);
}

public sealed class SolicitudBootstrapAdministrador(
    string nombreUsuario,
    string correo,
    string contrasena)
{
    public string NombreUsuario { get; } = nombreUsuario;

    public string Correo { get; } = correo;

    public string Contrasena { get; } = contrasena;
}

public sealed record RegistroBootstrapCreado(
    string Tabla,
    string Identificador,
    string Descripcion);

public sealed record ResultadoBootstrapAdministrador(
    IReadOnlyList<RegistroBootstrapCreado> Registros);

public sealed class BootstrapAdministradorRechazadoException(string mensaje)
    : Exception(mensaje)
{
}
