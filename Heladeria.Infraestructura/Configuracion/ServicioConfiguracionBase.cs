using System.Data;
using Heladeria.Aplicacion.Configuracion;
using Heladeria.Infraestructura.Persistencia;
using Heladeria.Infraestructura.Persistencia.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Heladeria.Infraestructura.Configuracion;

public abstract class ServicioConfiguracionBase(
    HeladeriaDbContext contexto,
    IAuditoriaConfiguracion auditoria)
{
    protected HeladeriaDbContext Contexto { get; } = contexto;

    protected IAuditoriaConfiguracion Auditoria { get; } = auditoria;

    protected static string NormalizarTexto(string valor)
    {
        var normalizado = valor.Trim();
        if (normalizado.Length == 0)
        {
            throw new ValidacionConfiguracionException(
                "Los textos obligatorios no pueden contener únicamente espacios.");
        }

        return normalizado;
    }

    protected static string? NormalizarOpcional(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    protected static string NormalizarCodigo(string valor) =>
        NormalizarTexto(valor).ToUpperInvariant();

    protected static IQueryable<T> Paginar<T>(
        IQueryable<T> consulta,
        FiltroPaginadoBase filtro) => consulta
            .Skip(filtro.CalcularDesplazamiento())
            .Take(filtro.TamanioPagina);

    protected async Task ValidarEstadoDisponibleAsync(
        int estadoId,
        CancellationToken cancellationToken)
    {
        var existe = await Contexto.Estados
            .AsNoTracking()
            .AnyAsync(item => item.Id == estadoId && item.Activo, cancellationToken);
        if (!existe)
        {
            throw new ValidacionConfiguracionException(
                "El estado indicado no existe o no está disponible.");
        }
    }

    protected async Task<IDbContextTransaction?> IniciarTransaccionSerializableAsync(
        string recurso,
        CancellationToken cancellationToken)
    {
        if (!Contexto.Database.IsRelational())
        {
            return null;
        }

        var transaccion = await Contexto.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        if (Contexto.Database.IsSqlServer())
        {
            await Contexto.Database.ExecuteSqlInterpolatedAsync(
                $"""
                DECLARE @resultado int;
                EXEC @resultado = sys.sp_getapplock
                    @Resource = {"Heladeria.Configuracion." + recurso},
                    @LockMode = N'Exclusive',
                    @LockOwner = N'Transaction',
                    @LockTimeout = 5000;
                IF @resultado < 0
                    THROW 51001, 'No fue posible reservar el catálogo solicitado.', 1;
                """,
                cancellationToken);
        }

        return transaccion;
    }

    protected static Task ConfirmarAsync(
        IDbContextTransaction? transaccion,
        CancellationToken cancellationToken) => transaccion is null
            ? Task.CompletedTask
            : transaccion.CommitAsync(cancellationToken);
}

public sealed class AuditoriaConfiguracion(
    HeladeriaDbContext contexto,
    TimeProvider timeProvider) : IAuditoriaConfiguracion
{
    public void Registrar(
        ContextoConfiguracion contextoSeguridad,
        string entidad,
        string? entidadId,
        string accion,
        string comentario)
    {
        contexto.Bitacoras.Add(new Bitacoras
        {
            EmpresaId = contextoSeguridad.EmpresaId,
            SucursalId = contextoSeguridad.SucursalId,
            ModuloId = "CONFIGURACION",
            Entidad = entidad,
            EntidadId = entidadId,
            Accion = accion,
            Fecha = timeProvider.GetUtcNow().UtcDateTime,
            UsuarioId = contextoSeguridad.UsuarioId,
            DireccionIp = Truncar(contextoSeguridad.DireccionIp, 45),
            Equipo = Truncar(contextoSeguridad.Equipo, 200),
            Comentario = Truncar(comentario, 500)
        });
    }

    private static string? Truncar(string? valor, int longitud) =>
        string.IsNullOrWhiteSpace(valor)
            ? null
            : valor.Trim()[..Math.Min(valor.Trim().Length, longitud)];
}
