namespace Heladeria.Aplicacion.Seguridad.Bootstrap;

public interface IPreparadorFase8
{
    Task<ResultadoBootstrapAdministrador> EjecutarAsync(CancellationToken token);
}
