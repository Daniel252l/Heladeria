using Heladeria.Aplicacion.Seguridad.Configuracion;
using Heladeria.Aplicacion.Seguridad.Excepciones;

namespace Heladeria.Aplicacion.Seguridad.Autenticacion;

public static class ValidadorPoliticaContrasena
{
    public static void Validar(
        string nueva,
        string confirmacion,
        OpcionesContrasenas politica)
    {
        if (!string.Equals(nueva, confirmacion, StringComparison.Ordinal))
        {
            throw new ValidacionSeguridadException(
                "La confirmación de contraseña no coincide.",
                "CONFIRMACION_NO_COINCIDE");
        }

        if (nueva.Length < politica.LongitudMinima)
        {
            throw new ValidacionSeguridadException(
                $"La contraseña debe tener al menos {politica.LongitudMinima} caracteres.",
                "CONTRASENA_DEBIL");
        }

        if (politica.RequiereMayuscula && !nueva.Any(char.IsUpper)
            || politica.RequiereMinuscula && !nueva.Any(char.IsLower)
            || politica.RequiereNumero && !nueva.Any(char.IsDigit)
            || politica.RequiereCaracterEspecial && !nueva.Any(c => !char.IsLetterOrDigit(c)))
        {
            throw new ValidacionSeguridadException(
                "La contraseña no cumple la política configurada.",
                "CONTRASENA_DEBIL");
        }
    }
}

