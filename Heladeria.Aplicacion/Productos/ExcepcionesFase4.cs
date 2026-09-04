namespace Heladeria.Aplicacion.Productos;

public abstract class ExcepcionFase4(string mensaje, string codigo) : Exception(mensaje)
{
    public string Codigo { get; } = codigo;
}

public sealed class RecursoFase4NoEncontradoException(string mensaje)
    : ExcepcionFase4(mensaje, "RECURSO_NO_ENCONTRADO");

public sealed class ValidacionFase4Exception(string mensaje)
    : ExcepcionFase4(mensaje, "VALIDACION_FASE4");

public sealed class ConflictoFase4Exception(string mensaje)
    : ExcepcionFase4(mensaje, "CONFLICTO_FASE4");

public sealed class ExistenciaInsuficienteException(string mensaje)
    : ExcepcionFase4(mensaje, "EXISTENCIA_INSUFICIENTE");

public sealed class ConcurrenciaInventarioException(string mensaje)
    : ExcepcionFase4(mensaje, "CONFLICTO_CONCURRENCIA");
