namespace Heladeria.Api.Configuracion;

// CORS no impide que el navegador envíe una petición simple con cookies; validar el origen antes de rotarlas.
public sealed class OrigenAutenticacionMiddleware(RequestDelegate siguiente, IConfiguration configuracion)
{
    public async Task InvokeAsync(HttpContext contexto)
    {
        if (HttpMethods.IsPost(contexto.Request.Method) && contexto.Request.Path.StartsWithSegments("/api/autenticacion"))
        {
            var origen = contexto.Request.Headers.Origin.ToString();
            var propio = $"{contexto.Request.Scheme}://{contexto.Request.Host}";
            var permitidos = configuracion.GetSection("Cors:OrigenesPermitidos").Get<string[]>() ?? [];
            if (origen.Length > 0 && origen != propio && !permitidos.Contains(origen, StringComparer.OrdinalIgnoreCase)
                || origen.Length == 0 && contexto.Request.Headers["Sec-Fetch-Site"] == "cross-site")
            {
                contexto.Response.StatusCode = StatusCodes.Status403Forbidden;
                await contexto.Response.WriteAsJsonAsync(new { exito = false, mensaje = "Origen de autenticación no autorizado.",
                    codigo = "ORIGEN_NO_PERMITIDO", traceId = contexto.TraceIdentifier });
                return;
            }
        }
        await siguiente(contexto);
    }
}
