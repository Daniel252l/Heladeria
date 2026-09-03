namespace Heladeria.Aplicacion.Configuracion;

public abstract class ExcepcionConfiguracion(string mensaje, string codigo) : Exception(mensaje)
{
    public string Codigo { get; } = codigo;
}

public sealed class RecursoConfiguracionNoEncontradoException(string mensaje)
    : ExcepcionConfiguracion(mensaje, "RECURSO_NO_ENCONTRADO");

public sealed class ConflictoConfiguracionException(string mensaje)
    : ExcepcionConfiguracion(mensaje, "CONFLICTO_CONFIGURACION");

public sealed class ValidacionConfiguracionException(string mensaje)
    : ExcepcionConfiguracion(mensaje, "VALIDACION_CONFIGURACION");

