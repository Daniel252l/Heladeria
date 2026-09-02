namespace Heladeria.Aplicacion.Seguridad.Autenticacion;

public interface IServicioAutenticacion
{
    Task<ResultadoAutenticacion> IniciarSesionAsync(
        IniciarSesionSolicitud solicitud,
        ContextoAcceso contextoAcceso,
        CancellationToken cancellationToken);

    Task<ResultadoAutenticacion> RenovarAsync(
        string refreshToken,
        ContextoAcceso contextoAcceso,
        CancellationToken cancellationToken);

    Task<ResultadoAutenticacion> SeleccionarSucursalAsync(
        int usuarioId,
        long sesionId,
        long sucursalActualId,
        string refreshToken,
        SeleccionarSucursalSolicitud solicitud,
        ContextoAcceso contextoAcceso,
        CancellationToken cancellationToken);

    Task CerrarSesionAsync(
        int usuarioId,
        long sesionId,
        long sucursalId,
        ContextoAcceso contextoAcceso,
        CancellationToken cancellationToken);

    Task CambiarContrasenaAsync(
        int usuarioId,
        long sucursalId,
        CambiarContrasenaSolicitud solicitud,
        ContextoAcceso contextoAcceso,
        CancellationToken cancellationToken);

    Task<SesionActualRespuesta> ObtenerSesionAsync(
        int usuarioId,
        long sucursalId,
        CancellationToken cancellationToken);
}
