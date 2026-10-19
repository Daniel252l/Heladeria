using Heladeria.Aplicacion.Configuracion;
using Heladeria.Aplicacion.Productos;
using Heladeria.Infraestructura.Persistencia;
using Heladeria.Infraestructura.Persistencia.Entidades;
using Heladeria.Aplicacion.Seguridad.Configuracion;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Heladeria.Infraestructura.Fase4;

public abstract class ServicioFase4Base(
    HeladeriaDbContext contexto,
    IAuditoriaFase4 auditoria,
    IOptions<OpcionesSeguridad> opciones,
    TimeProvider timeProvider)
{
    protected HeladeriaDbContext Contexto { get; } = contexto;
    protected IAuditoriaFase4 Auditoria { get; } = auditoria;
    protected TimeProvider Reloj { get; } = timeProvider;
    protected string[] EstadosOperativos { get; } = opciones.Value.EstadosActivos;

    protected DateTime AhoraUtc => Reloj.GetUtcNow().UtcDateTime;

    protected static string NormalizarTexto(string valor)
    {
        var normalizado = valor.Trim();
        if (normalizado.Length == 0)
        {
            throw new ValidacionFase4Exception(
                "Los textos obligatorios no pueden contener únicamente espacios.");
        }

        return normalizado;
    }

    protected static string NormalizarCodigo(string valor) =>
        NormalizarTexto(valor).ToUpperInvariant();

    protected static string? NormalizarOpcional(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    protected static IQueryable<T> Paginar<T>(IQueryable<T> consulta, FiltroPaginadoBase filtro) =>
        consulta.Skip(filtro.CalcularDesplazamiento()).Take(filtro.TamanioPagina);

    protected async Task ValidarEstadoAsync(int estadoId, CancellationToken token)
    {
        if (!await Contexto.Estados.AsNoTracking().AnyAsync(
                item => item.Id == estadoId && item.Activo,
                token))
        {
            throw new ValidacionFase4Exception(
                "El estado indicado no existe o no está disponible.");
        }
    }

    protected Task<bool> EsEstadoOperativoAsync(int estadoId, CancellationToken token) =>
        Contexto.Estados.AsNoTracking().AnyAsync(
            item => item.Id == estadoId
                && item.Activo
                && EstadosOperativos.Contains(item.Nombre),
            token);

    protected static string CodificarVersion(byte[] versionFila) =>
        Convert.ToBase64String(versionFila ?? []);

    protected static void ValidarVersion(byte[] versionActual, string versionSolicitada)
    {
        byte[] version;
        try
        {
            version = Convert.FromBase64String(versionSolicitada);
        }
        catch (FormatException)
        {
            throw new ValidacionFase4Exception("VersionFila no tiene un formato Base64 válido.");
        }

        if (!versionActual.AsSpan().SequenceEqual(version))
        {
            throw new ConflictoFase4Exception(
                "El registro fue modificado por otro usuario. Recargue los datos antes de reintentar.");
        }
    }

    protected async Task GuardarCambiosAsync(CancellationToken token)
    {
        try
        {
            await Contexto.SaveChangesAsync(token);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictoFase4Exception(
                "El registro fue modificado concurrentemente. Recargue los datos antes de reintentar.");
        }
    }
}

public sealed class AuditoriaFase4(
    HeladeriaDbContext contexto,
    TimeProvider timeProvider) : IAuditoriaFase4
{
    public void Registrar(
        ContextoConfiguracion seguridad,
        string entidad,
        string? entidadId,
        string accion,
        string comentario)
    {
        contexto.Bitacoras.Add(new Bitacoras
        {
            EmpresaId = seguridad.EmpresaId,
            SucursalId = seguridad.SucursalId,
            ModuloId = "INVENTARIO",
            Entidad = entidad,
            EntidadId = entidadId,
            Accion = accion,
            Fecha = timeProvider.GetUtcNow().UtcDateTime,
            UsuarioId = seguridad.UsuarioId,
            DireccionIp = Truncar(seguridad.DireccionIp, 45),
            Equipo = Truncar(seguridad.Equipo, 200),
            Comentario = Truncar(comentario, 500)
        });
    }

    private static string? Truncar(string? valor, int longitud)
    {
        var normalizado = string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
        return normalizado is null
            ? null
            : normalizado[..Math.Min(normalizado.Length, longitud)];
    }
}
