using System.Linq.Expressions;
using Heladeria.Infraestructura.Persistencia.Entidades;

namespace Heladeria.Infraestructura.Seguridad.Autenticacion;

internal static class ReglasSesionPersistida
{
    public static Expression<Func<SesionesUsuarios, bool>> CrearFiltroVigente(
        DateTimeOffset ahoraUtc,
        int diasRefreshToken)
    {
        var fechaMinima = ahoraUtc.AddDays(-diasRefreshToken).UtcDateTime;
        return sesion =>
            !sesion.Cerrada
            && sesion.FechaFin == null
            && sesion.TokenIdentificador != null
            && sesion.TokenIdentificador != string.Empty
            && sesion.FechaInicio > fechaMinima;
    }

    public static bool EsVigente(
        SesionesUsuarios sesion,
        DateTimeOffset ahoraUtc,
        int diasRefreshToken) =>
        CrearFiltroVigente(ahoraUtc, diasRefreshToken).Compile()(sesion);
}
