namespace Heladeria.Api.Configuracion;

public sealed class EncabezadosSeguridadMiddleware(RequestDelegate siguiente)
{
    public async Task InvokeAsync(HttpContext contexto)
    {
        contexto.Response.OnStarting(() =>
        {
            contexto.Response.Headers.XContentTypeOptions = "nosniff";
            contexto.Response.Headers.XFrameOptions = "DENY";
            contexto.Response.Headers.Append("Referrer-Policy", "no-referrer");
            if (contexto.Request.Path.StartsWithSegments("/api"))
                contexto.Response.Headers.CacheControl = "no-store";
            return Task.CompletedTask;
        });

        await siguiente(contexto);
    }
}

public static class EncabezadosSeguridadExtensiones
{
    public static IApplicationBuilder UseEncabezadosSeguridad(this IApplicationBuilder aplicacion) =>
        aplicacion.UseMiddleware<EncabezadosSeguridadMiddleware>();
}
