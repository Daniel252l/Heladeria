using System.Text.Json;
using System.Text.Json.Serialization;

namespace Heladeria.Api.Configuracion;

/// <summary>No convertir offsets de entrada a la zona de la máquina. Las fechas civiles permanecen civiles.</summary>
public sealed class ConvertidorFechaApi : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type tipo, JsonSerializerOptions opciones)
    {
        if (!reader.TryGetDateTime(out var valor)) throw new JsonException("Fecha ISO inválida.");
        return valor.Kind == DateTimeKind.Unspecified ? valor : reader.GetDateTimeOffset().UtcDateTime;
    }

    public override void Write(Utf8JsonWriter writer, DateTime valor, JsonSerializerOptions opciones)
    {
        if (valor.Kind == DateTimeKind.Local) throw new JsonException("La API no debe emitir fechas en la zona del servidor.");
        writer.WriteStringValue(valor);
    }
}
