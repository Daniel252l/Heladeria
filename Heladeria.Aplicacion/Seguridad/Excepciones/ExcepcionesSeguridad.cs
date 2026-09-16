namespace Heladeria.Aplicacion.Seguridad.Excepciones;

public abstract class ExcepcionSeguridad(string mensaje, string codigo) : Exception(mensaje)
{
    public string Codigo { get; } = codigo;
}

public sealed class CredencialesInvalidasException()
    : ExcepcionSeguridad("Credenciales inválidas.", "CREDENCIALES_INVALIDAS")
{
}

public sealed class TokenRenovacionInvalidoException()
    : ExcepcionSeguridad("La sesión no es válida o expiró.", "SESION_INVALIDA")
{
}

public sealed class SesionActivaException()
    : ExcepcionSeguridad(
        "El usuario ya tiene una sesión activa. Cierre la sesión anterior antes de volver a ingresar.",
        "SESION_ACTIVA")
{
}

public sealed class AccesoDenegadoException(string? mensaje = null)
    : ExcepcionSeguridad(
        mensaje ?? "No tiene acceso al recurso solicitado.",
        "ACCESO_DENEGADO")
{
}

public sealed class ValidacionSeguridadException(string mensaje, string codigo)
    : ExcepcionSeguridad(mensaje, codigo)
{
}
