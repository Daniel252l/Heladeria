using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Heladeria.Api.Salud;

public static class RespuestaSalud
{
    private static readonly JsonSerializerOptions OpcionesJson = new(JsonSerializerDefaults.Web);

    public static EstadoSaludRespuesta Crear(HealthReport reporte) => new(
        reporte.Status == HealthStatus.Healthy ? "saludable" : "no_saludable",
        reporte.Entries.Select(entrada => new ComprobacionSaludRespuesta(
            entrada.Key, entrada.Value.Status.ToString().ToLowerInvariant())).ToArray());

    public static Task EscribirAsync(HttpContext contexto, HealthReport reporte)
    {
        contexto.Response.ContentType = "application/json; charset=utf-8";

        return contexto.Response.WriteAsync(JsonSerializer.Serialize(Crear(reporte), OpcionesJson));
    }
}

public sealed record EstadoSaludRespuesta(string Estado, IReadOnlyList<ComprobacionSaludRespuesta> Comprobaciones);
public sealed record ComprobacionSaludRespuesta(string Nombre, string Estado);
