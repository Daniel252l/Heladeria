using System.Threading.RateLimiting;
using Heladeria.Aplicacion.Seguridad.Configuracion;
using Microsoft.AspNetCore.RateLimiting;

namespace Heladeria.Api.Configuracion;

public static class ConfiguracionRateLimiting
{
    public const string Login = "autenticacion-login";
    public const string Renovacion = "autenticacion-renovacion";
    public const string CambioContrasena = "autenticacion-cambio-contrasena";

    public static IServiceCollection AgregarLimitesSolicitudes(
        this IServiceCollection servicios,
        IConfiguration configuracion)
    {
        var opciones = configuracion
            .GetRequiredSection(OpcionesSeguridad.Seccion)
            .Get<OpcionesSeguridad>() ?? new OpcionesSeguridad();

        servicios.AddRateLimiter(configuracionLimites =>
        {
            AgregarPolitica(
                configuracionLimites,
                Login,
                opciones.RateLimiting.IntentosLogin,
                opciones.RateLimiting.VentanaSegundos);
            AgregarPolitica(
                configuracionLimites,
                Renovacion,
                opciones.RateLimiting.Renovaciones,
                opciones.RateLimiting.VentanaSegundos);
            AgregarPolitica(
                configuracionLimites,
                CambioContrasena,
                opciones.RateLimiting.CambiosContrasena,
                opciones.RateLimiting.VentanaSegundos);

            configuracionLimites.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            configuracionLimites.OnRejected = async (contexto, cancellationToken) =>
            {
                if (!contexto.HttpContext.Response.HasStarted)
                {
                    await contexto.HttpContext.Response.WriteAsJsonAsync(new
                    {
                        exito = false,
                        mensaje = "Se excedió el límite de solicitudes. Intente más tarde.",
                        codigo = "LIMITE_SOLICITUDES",
                        traceId = contexto.HttpContext.TraceIdentifier
                    }, cancellationToken);
                }
            };
        });

        return servicios;
    }

    private static void AgregarPolitica(
        RateLimiterOptions opciones,
        string nombre,
        int limite,
        int ventanaSegundos)
    {
        opciones.AddPolicy(nombre, contexto =>
            RateLimitPartition.GetFixedWindowLimiter(
                contexto.Connection.RemoteIpAddress?.ToString() ?? "sin-ip",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = limite,
                    QueueLimit = 0,
                    Window = TimeSpan.FromSeconds(ventanaSegundos),
                    AutoReplenishment = true
                }));
    }
}

