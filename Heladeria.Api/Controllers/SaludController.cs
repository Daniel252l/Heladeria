using Heladeria.Api.Salud;
using Heladeria.Api.Seguridad.Contrasenas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Heladeria.Api.Controllers;

// Las acciones MVC publican el contrato en ApiExplorer/Swagger. La comprobación sigue siendo la estándar de ASP.NET Core.
[ApiController, AllowAnonymous, PermitirContrasenaTemporal, Route("health")]
[Produces("application/json")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class SaludController(HealthCheckService comprobaciones) : ControllerBase
{
    [HttpGet("live")]
    [ProducesResponseType(typeof(EstadoSaludRespuesta), StatusCodes.Status200OK)]
    public Task<ActionResult<EstadoSaludRespuesta>> LiveAsync(CancellationToken cancellationToken) =>
        ResponderAsync(_ => false, cancellationToken);

    [HttpGet("ready")]
    [ProducesResponseType(typeof(EstadoSaludRespuesta), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(EstadoSaludRespuesta), StatusCodes.Status503ServiceUnavailable)]
    public Task<ActionResult<EstadoSaludRespuesta>> ReadyAsync(CancellationToken cancellationToken) =>
        ResponderAsync(registro => registro.Tags.Contains("ready"), cancellationToken);

    private async Task<ActionResult<EstadoSaludRespuesta>> ResponderAsync(
        Func<HealthCheckRegistration, bool> filtro, CancellationToken cancellationToken)
    {
        var reporte = await comprobaciones.CheckHealthAsync(filtro, cancellationToken);
        var codigo = reporte.Status == HealthStatus.Unhealthy
            ? StatusCodes.Status503ServiceUnavailable : StatusCodes.Status200OK;
        return StatusCode(codigo, RespuestaSalud.Crear(reporte));
    }
}
