namespace Heladeria.Api.Configuracion;

public static class ConfiguracionCors
{
    public const string NombrePolitica = "FrontendHeladeria";

    public static IServiceCollection AgregarCorsSeguro(
        this IServiceCollection servicios,
        IConfiguration configuracion)
    {
        var origenes = configuracion
            .GetSection("Cors:OrigenesPermitidos")
            .Get<string[]>() ?? [];

        if (origenes.Any(x => !Uri.TryCreate(x, UriKind.Absolute, out var uri)
            || uri.Scheme is not ("http" or "https") || uri.GetLeftPart(UriPartial.Authority) != x
            || x.Contains('*') || uri.UserInfo.Length > 0))
            throw new InvalidOperationException("CORS requiere orígenes HTTP(S) explícitos sin rutas, credenciales ni comodines.");

        servicios.AddCors(opciones =>
        {
            opciones.AddPolicy(NombrePolitica, politica =>
            {
                if (origenes.Length > 0)
                {
                    politica
                        .WithOrigins(origenes)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                }
            });
        });

        return servicios;
    }
}
