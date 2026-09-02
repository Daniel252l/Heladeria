using Heladeria.Aplicacion.Seguridad.Autorizacion;
using Microsoft.AspNetCore.Authorization;

namespace Heladeria.Api.Seguridad.Autorizacion;

public sealed class ManejadorPermiso(
    IServicioPermisos servicioPermisos,
    IServicioAuditoriaSeguridad servicioAuditoria,
    IHttpContextAccessor accesoHttpContext) : AuthorizationHandler<RequisitoPermiso>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext contexto,
        RequisitoPermiso requisito)
    {
        if (!contexto.User.TryObtenerContextoSeguridad(
                out var usuarioId,
                out _,
                out _,
                out var rolId))
        {
            return;
        }

        var cancellationToken = accesoHttpContext.HttpContext?.RequestAborted
            ?? CancellationToken.None;

        if (await servicioPermisos.TienePermisoAsync(
                usuarioId,
                rolId,
                requisito.Codigo,
                cancellationToken))
        {
            contexto.Succeed(requisito);
            return;
        }

        var httpContext = accesoHttpContext.HttpContext;
        _ = long.TryParse(
            contexto.User.FindFirst(ClaimsSeguridad.SucursalId)?.Value,
            out var sucursalId);

        await servicioAuditoria.RegistrarAccesoDenegadoAsync(
            usuarioId,
            sucursalId == 0 ? null : sucursalId,
            requisito.Codigo,
            httpContext?.Connection.RemoteIpAddress?.ToString(),
            cancellationToken);
    }
}

