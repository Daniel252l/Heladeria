using System.ComponentModel.DataAnnotations;
using System.Data;
using Heladeria.Aplicacion.Compras;
using Heladeria.Aplicacion.Configuracion;
using Heladeria.Aplicacion.Seguridad.Configuracion;
using Heladeria.Infraestructura.Inventario;
using Heladeria.Infraestructura.Persistencia;
using Heladeria.Infraestructura.Persistencia.Entidades;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Heladeria.Infraestructura.Compras;

public abstract class ServicioComprasBase(HeladeriaDbContext contexto, IOptions<OpcionesSeguridad> opciones,
    TimeProvider reloj, CoordinadorAjustesInventario coordinador)
{
    protected HeladeriaDbContext Contexto { get; } = contexto;
    protected string[] Operativos { get; } = opciones.Value.EstadosActivos;
    protected DateTime Ahora => reloj.GetUtcNow().UtcDateTime;
    protected const decimal MaximoMonetario = 9999999999.99m;
    protected const decimal MaximoCantidad = 9999999999999.999999m;
    protected static string? Opcional(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
    protected static string Codigo(string texto) => texto.Trim().ToUpperInvariant();
    private static string? Truncar(string? texto, int longitud) => Opcional(texto) is { } valor ? valor[..Math.Min(valor.Length, longitud)] : null;
    protected static decimal Dinero(decimal valor) => decimal.Round(valor, 2, MidpointRounding.AwayFromZero);

    protected static void Validar(object solicitud)
    {
        var errores = new List<ValidationResult>();
        if (!Validator.TryValidateObject(solicitud, new(solicitud), errores, true))
            throw new ValidacionComprasException(string.Join(" ", errores.Select(item => item.ErrorMessage)));
    }

    protected static void Precision(decimal valor, int decimales, decimal maximo, string nombre, bool positivo = false)
    {
        if (valor < 0 || positivo && valor == 0 || valor > maximo || valor != decimal.Round(valor, decimales))
            throw new ValidacionComprasException($"{nombre} no tiene un valor o precisión válidos.");
    }

    protected async Task ValidarEstadoAsync(int id, CancellationToken token)
    {
        if (!await Contexto.Estados.AsNoTracking().AnyAsync(item => item.Id == id && item.Activo, token))
            throw new ValidacionComprasException("El estado no existe o no está disponible.");
    }

    protected async Task ValidarSucursalAsync(ContextoConfiguracion seguridad, CancellationToken token)
    {
        if (!await Contexto.UsuariosSucursales.AsNoTracking().AnyAsync(item =>
            item.UsuarioId == seguridad.UsuarioId && item.SucursalId == seguridad.SucursalId
            && item.Sucursal.EmpresaId == seguridad.EmpresaId && item.Estado.Activo
            && Operativos.Contains(item.Estado.Nombre) && item.Sucursal.Estado.Activo
            && Operativos.Contains(item.Sucursal.Estado.Nombre) && item.Sucursal.Empresa.Estado.Activo
            && Operativos.Contains(item.Sucursal.Empresa.Estado.Nombre), token))
            throw new RecursoComprasNoEncontradoException("La sucursal no existe dentro del contexto autorizado.");
    }

    protected void Auditar(ContextoConfiguracion seguridad, string entidad, object id, string accion, string comentario,
        string modulo = "COMPRAS") => Contexto.Bitacoras.Add(new Bitacoras
        {
            EmpresaId = seguridad.EmpresaId, SucursalId = seguridad.SucursalId, UsuarioId = seguridad.UsuarioId,
            ModuloId = modulo, Entidad = entidad, EntidadId = Convert.ToString(id, System.Globalization.CultureInfo.InvariantCulture),
            Accion = accion, Fecha = Ahora, DireccionIp = Truncar(seguridad.DireccionIp, 45), Equipo = Truncar(seguridad.Equipo, 200),
            Comentario = Truncar(comentario, 500)
        });

    // La clave es idéntica a Fase 4: recepciones y ajustes no pueden cruzar saldos.
    protected async Task<T> EscribirAsync<T>(ContextoConfiguracion seguridad, Func<Task<T>> operacion, CancellationToken token)
    {
        var clave = seguridad.EmpresaId.ToString(System.Globalization.CultureInfo.InvariantCulture);
        await using var local = await coordinador.AdquirirAsync(clave, token);
        if (Contexto.ChangeTracker.HasChanges())
            throw new ConflictoComprasException("El contexto contiene cambios pendientes ajenos a esta operación.");
        Contexto.ChangeTracker.Clear();
        await using var transaccion = Contexto.Database.IsRelational()
            ? await Contexto.Database.BeginTransactionAsync(IsolationLevel.Serializable, token) : null;
        try
        {
            if (Contexto.Database.IsSqlServer())
                await Contexto.Database.ExecuteSqlInterpolatedAsync($"""
                    DECLARE @resultado int;
                    EXEC @resultado = sys.sp_getapplock @Resource = {"Heladeria.Inventario." + clave},
                        @LockMode = N'Exclusive', @LockOwner = N'Transaction', @LockTimeout = 10000;
                    IF @resultado < 0 THROW 51002, 'Reserva de inventario no disponible.', 1;
                    """, token);
            await ValidarSucursalAsync(seguridad, token);
            var resultado = await operacion();
            await Contexto.SaveChangesAsync(token);
            if (transaccion is not null) await transaccion.CommitAsync(token);
            return resultado;
        }
        catch (Exception error)
        {
            if (transaccion is not null) await transaccion.RollbackAsync(CancellationToken.None);
            Contexto.ChangeTracker.Clear();
            if (error is DbUpdateConcurrencyException || error is SqlException { Number: 51002 or 1205 or 1222 })
                throw new ConflictoComprasException("La operación entró en conflicto con otra escritura. Inténtelo nuevamente.");
            throw;
        }
    }

    protected IQueryable<T> Paginar<T>(IQueryable<T> consulta, FiltroPaginadoBase filtro)
    {
        Validar(filtro);
        return consulta.Skip(filtro.CalcularDesplazamiento()).Take(filtro.TamanioPagina);
    }
}
