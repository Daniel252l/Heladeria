using Heladeria.Aplicacion.Seguridad.Configuracion;

namespace Heladeria.Aplicacion.Tiempo;

/// <summary>Zona de presentación/planificación; el reloj operativo sigue siendo TimeProvider UTC.</summary>
public static class TiempoEmpresarial
{
    public static string Zona(OpcionesSeguridad opciones, int empresaId, long sucursalId) =>
        opciones.ZonasHorariasSucursales.GetValueOrDefault(sucursalId)
        ?? opciones.ZonasHorariasEmpresas.GetValueOrDefault(empresaId) ?? opciones.ZonaHoraria;

    public static bool ZonaValida(string zona)
    {
        // IANA permite enviar exactamente la misma configuración a Intl en el navegador.
        try { return TimeZoneInfo.FindSystemTimeZoneById(zona).HasIanaId; }
        catch (TimeZoneNotFoundException) { return false; }
        catch (InvalidTimeZoneException) { return false; }
    }

    public static DateTime AUTC(DateTime valor, string zona)
    {
        if (valor.Kind == DateTimeKind.Utc) return valor;
        if (valor.Kind == DateTimeKind.Local)
            throw new ArgumentException("Se requiere UTC o una fecha civil sin zona; no la zona del servidor.");
        var tz = TimeZoneInfo.FindSystemTimeZoneById(zona);
        if (tz.IsInvalidTime(valor) || tz.IsAmbiguousTime(valor))
            throw new ArgumentException("La hora local no existe o es ambigua en la zona configurada; indique un offset explícito.");
        return TimeZoneInfo.ConvertTimeToUtc(valor, tz);
    }
}
