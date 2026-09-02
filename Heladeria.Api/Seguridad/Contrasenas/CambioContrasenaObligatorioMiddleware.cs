using Heladeria.Aplicacion.Seguridad.Autorizacion;

namespace Heladeria.Api.Seguridad.Contrasenas;

public sealed class CambioContrasenaObligatorioMiddleware(RequestDelegate siguiente)
{
    public async Task InvokeAsync(HttpContext contexto)
    {
        var requiereCambio = string.Equals(
            contexto.User.FindFirst(ClaimsSeguridad.RequiereCambioContrasena)?.Value,
            bool.TrueString,
            StringComparison.OrdinalIgnoreCase);
        var permitido = contexto.GetEndpoint()?
            .Metadata
            .GetMetadata<PermitirContrasenaTemporalAttribute>() is not null;

        if (contexto.User.Identity?.IsAuthenticated == true && requiereCambio && !permitido)
        {
            contexto.Response.StatusCode = StatusCodes.Status403Forbidden;
            await contexto.Response.WriteAsJsonAsync(new
            {
                exito = false,
                mensaje = "Debe cambiar su contraseña antes de continuar.",
                codigo = "CAMBIO_CONTRASENA_REQUERIDO",
                traceId = contexto.TraceIdentifier
            });
            return;
        }

        await siguiente(contexto);
    }
}

