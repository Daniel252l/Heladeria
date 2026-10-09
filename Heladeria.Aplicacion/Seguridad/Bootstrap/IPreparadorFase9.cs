namespace Heladeria.Aplicacion.Seguridad.Bootstrap;

public interface IPreparadorFase9
{
    Task<ResultadoBootstrapAdministrador> EjecutarAsync(CancellationToken token);
}
