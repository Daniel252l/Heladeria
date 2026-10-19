namespace Heladeria.Aplicacion.Seguridad.Bootstrap;

public interface IPreparadorFase10
{
    Task<ResultadoBootstrapAdministrador> EjecutarAsync(CancellationToken token);
}
