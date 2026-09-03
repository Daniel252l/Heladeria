using Heladeria.Aplicacion.Seguridad.Autenticacion;
using Heladeria.Aplicacion.Seguridad.Configuracion;
using Heladeria.Infraestructura.Persistencia.Entidades;

namespace Heladeria.Infraestructura.Seguridad.Autenticacion;

internal sealed class EvaluadorSeguridadUsuario(
    OpcionesSeguridad opciones,
    TimeProvider timeProvider)
{
    private readonly TimeZoneInfo _zonaHoraria =
        TimeZoneInfo.FindSystemTimeZoneById(opciones.ZonaHoraria);

    public DateTimeOffset AhoraUtc => timeProvider.GetUtcNow();

    public DateOnly FechaLocal => DateOnly.FromDateTime(
        TimeZoneInfo.ConvertTime(AhoraUtc, _zonaHoraria).DateTime);

    public bool EsEstadoActivo(Estados estado) =>
        estado.Activo
        && opciones.EstadosActivos.Contains(
            estado.Nombre,
            StringComparer.OrdinalIgnoreCase);

    public bool EstaEnHorarioPermitido(Usuarios usuario)
    {
        var hora = TimeOnly.FromDateTime(
            TimeZoneInfo.ConvertTime(AhoraUtc, _zonaHoraria).DateTime);

        if (usuario.HorarioInicio == usuario.HorarioFinal)
        {
            return true;
        }

        return usuario.HorarioInicio < usuario.HorarioFinal
            ? hora >= usuario.HorarioInicio && hora <= usuario.HorarioFinal
            : hora >= usuario.HorarioInicio || hora <= usuario.HorarioFinal;
    }

    public bool RequiereCambioContrasena(Usuarios usuario)
    {
        if (usuario.ContraseniaTemporal || usuario.UltimoCambioDeContrasenia is null)
        {
            return true;
        }

        return opciones.Contrasenas.AplicarVencimientoPorRolEnMeses
            && usuario.UltimoCambioDeContrasenia.Value
                .AddMonths(usuario.Role.CambioDeContrasenia) <= FechaLocal;
    }

    public void ValidarNuevaContrasena(string nueva, string confirmacion)
        => ValidadorPoliticaContrasena.Validar(nueva, confirmacion, opciones.Contrasenas);
}
