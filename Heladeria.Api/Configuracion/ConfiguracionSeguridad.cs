using System.Text;
using Heladeria.Api.Seguridad.Autorizacion;
using Heladeria.Aplicacion.Seguridad.Autorizacion;
using Heladeria.Aplicacion.Seguridad.Configuracion;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

namespace Heladeria.Api.Configuracion;

public static class ConfiguracionSeguridad
{
    public static IServiceCollection AgregarSeguridad(
        this IServiceCollection servicios,
        IConfiguration configuracion)
    {
        var seccion = configuracion.GetRequiredSection(OpcionesSeguridad.Seccion);
        servicios
            .AddOptions<OpcionesSeguridad>()
            .Bind(seccion)
            .Validate(
                opciones => Encoding.UTF8.GetByteCount(opciones.Jwt.ClaveFirma) >= 32,
                "Seguridad:Jwt:ClaveFirma debe contener al menos 32 bytes.")
            .Validate(opciones => opciones.Jwt.MinutosAccessToken is >= 5 and <= 30)
            .Validate(opciones => opciones.Jwt.DiasRefreshToken is >= 1 and <= 30)
            .Validate(opciones => !string.IsNullOrWhiteSpace(opciones.Jwt.Emisor) && !string.IsNullOrWhiteSpace(opciones.Jwt.Audiencia))
            .Validate(opciones => opciones.CookieRefresh.SameSite is "Strict" or "Lax" or "None", "SameSite debe ser Strict, Lax o None.")
            .Validate(opciones => opciones.Bloqueo.IntentosMaximos > 0)
            .Validate(opciones => opciones.Bloqueo.MinutosBloqueo > 0)
            .Validate(opciones => opciones.RateLimiting.IntentosLogin > 0)
            .Validate(opciones => opciones.RateLimiting.Renovaciones > 0)
            .Validate(opciones => opciones.RateLimiting.CambiosContrasena > 0)
            .Validate(opciones => opciones.RateLimiting.VentanaSegundos > 0)
            .Validate(opciones => opciones.EstadosActivos.Length > 0)
            .Validate(opciones => new[] { opciones.ZonaHoraria }
                .Concat(opciones.ZonasHorariasEmpresas.Values).Concat(opciones.ZonasHorariasSucursales.Values)
                .All(Heladeria.Aplicacion.Tiempo.TiempoEmpresarial.ZonaValida), "Las zonas horarias deben ser identificadores IANA válidos.")
            .ValidateOnStart();

        var opciones = seccion.Get<OpcionesSeguridad>() ?? new OpcionesSeguridad();
        if (Encoding.UTF8.GetByteCount(opciones.Jwt.ClaveFirma) < 32)
        {
            throw new InvalidOperationException(
                "Seguridad:Jwt:ClaveFirma debe configurarse fuera del repositorio y contener al menos 32 bytes.");
        }

        var clave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(opciones.Jwt.ClaveFirma));

        servicios
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(jwt =>
            {
                jwt.MapInboundClaims = false;
                jwt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = opciones.Jwt.Emisor,
                    ValidateAudience = true,
                    ValidAudience = opciones.Jwt.Audiencia,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = clave,
                    ValidateLifetime = true,
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    RequireSignedTokens = true,
                    RequireExpirationTime = true,
                    ClockSkew = TimeSpan.FromSeconds(15),
                    NameClaimType = ClaimsSeguridad.NombreUsuario
                };
                jwt.IncludeErrorDetails = false;
                jwt.Events = CrearEventosJwt();
            });

        servicios.AddAuthorization();
        servicios.AddHttpContextAccessor();
        servicios.AddSingleton<IAuthorizationPolicyProvider, ProveedorPoliticasPermisos>();
        servicios.AddScoped<IAuthorizationHandler, ManejadorPermiso>();

        return servicios;
    }

    private static JwtBearerEvents CrearEventosJwt() => new()
    {
        OnTokenValidated = async contexto =>
        {
            if (!contexto.Principal!.TryObtenerContextoSeguridad(
                    out var usuarioId,
                    out var sesionId,
                    out var sucursalId,
                    out _))
            {
                contexto.Fail("Contexto de sesión inválido.");
                return;
            }

            var validador = contexto.HttpContext.RequestServices
                .GetRequiredService<IServicioValidacionSesion>();
            if (!await validador.EsValidaAsync(
                    usuarioId,
                    sesionId,
                    sucursalId,
                    contexto.HttpContext.RequestAborted))
            {
                contexto.Fail("La sesión fue revocada o expiró.");
            }
        },
        OnChallenge = async contexto =>
        {
            contexto.HandleResponse();
            contexto.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await contexto.Response.WriteAsJsonAsync(new
            {
                exito = false,
                mensaje = "No se proporcionó una sesión válida.",
                codigo = "NO_AUTENTICADO",
                traceId = contexto.HttpContext.TraceIdentifier
            });
        },
        OnForbidden = async contexto =>
        {
            contexto.Response.StatusCode = StatusCodes.Status403Forbidden;
            await contexto.Response.WriteAsJsonAsync(new
            {
                exito = false,
                mensaje = "No tiene permiso para realizar esta operación.",
                codigo = "ACCESO_DENEGADO",
                traceId = contexto.HttpContext.TraceIdentifier
            });
        }
    };
}
