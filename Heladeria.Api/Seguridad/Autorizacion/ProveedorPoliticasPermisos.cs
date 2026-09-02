using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Heladeria.Api.Seguridad.Autorizacion;

public sealed record RequisitoPermiso(string Codigo) : IAuthorizationRequirement;

public sealed class ProveedorPoliticasPermisos(
    IOptions<AuthorizationOptions> opciones) : DefaultAuthorizationPolicyProvider(opciones)
{
    private const string Prefijo = "Permiso:";

    public static string CrearNombrePolitica(string permiso) => $"{Prefijo}{permiso}";

    public override Task<AuthorizationPolicy?> GetPolicyAsync(string nombrePolitica)
    {
        if (!nombrePolitica.StartsWith(Prefijo, StringComparison.Ordinal))
        {
            return base.GetPolicyAsync(nombrePolitica);
        }

        var permiso = nombrePolitica[Prefijo.Length..];
        var politica = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new RequisitoPermiso(permiso))
            .Build();

        return Task.FromResult<AuthorizationPolicy?>(politica);
    }
}

