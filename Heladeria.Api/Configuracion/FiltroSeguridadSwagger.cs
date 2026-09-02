using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Heladeria.Api.Configuracion;

public sealed class FiltroSeguridadSwagger : IOperationFilter
{
    public void Apply(OpenApiOperation operacion, OperationFilterContext contexto)
    {
        var metodo = contexto.MethodInfo;
        var permiteAnonimo = metodo.IsDefined(typeof(AllowAnonymousAttribute), true)
            || metodo.DeclaringType?.IsDefined(typeof(AllowAnonymousAttribute), true) == true;
        var requiereAutenticacion = metodo.IsDefined(typeof(AuthorizeAttribute), true)
            || metodo.DeclaringType?.IsDefined(typeof(AuthorizeAttribute), true) == true;

        if (permiteAnonimo || !requiereAutenticacion)
        {
            return;
        }

        operacion.Security =
        [
            new OpenApiSecurityRequirement
            {
                [new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                }] = []
            }
        ];
        operacion.Responses.TryAdd("401", new OpenApiResponse { Description = "No autenticado" });
        operacion.Responses.TryAdd("403", new OpenApiResponse { Description = "Acceso denegado" });
    }
}

