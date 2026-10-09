using System.ComponentModel.DataAnnotations;
using System.Data;
using Heladeria.Aplicacion.Caja;
using Heladeria.Aplicacion.Configuracion;
using Heladeria.Aplicacion.Seguridad.Configuracion;
using Heladeria.Infraestructura.Inventario;
using Heladeria.Infraestructura.Persistencia;
using Heladeria.Infraestructura.Persistencia.Entidades;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Heladeria.Infraestructura.Caja;

public abstract class ServicioCajaBase(HeladeriaDbContext contexto, IOptions<OpcionesSeguridad> opciones,
    TimeProvider reloj, CoordinadorAjustesInventario coordinador)
{
    protected HeladeriaDbContext Contexto { get; } = contexto;
    protected OpcionesSeguridad ConfiguracionTiempo { get; } = opciones.Value;
    protected string[] Operativos { get; } = opciones.Value.EstadosActivos;
    protected DateTime Ahora => reloj.GetUtcNow().UtcDateTime;
    public const string FlujoCaja = "CAJA_SESION";
    protected static string? Opcional(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
    protected static void Validar(object solicitud)
    {
        var errores = new List<ValidationResult>();
        if (!Validator.TryValidateObject(solicitud, new(solicitud), errores, true))
            throw new ValidacionCajaException(string.Join(" ", errores.Select(x => x.ErrorMessage)));
    }
    protected static void Importe(decimal valor, string nombre, bool cero = false)
    {
        if (valor < 0 || !cero && valor == 0 || valor > 9999999999.99m || valor != decimal.Round(valor, 2))
            throw new ValidacionCajaException($"{nombre} debe ser un importe válido con máximo dos decimales.");
    }
    protected static void Version(byte[] actual, string recibida)
    {
        byte[] bytes;
        try { bytes = Convert.FromBase64String(recibida); }
        catch (FormatException) { throw new ValidacionCajaException("VersionFila debe ser Base64."); }
        if (actual.Length == 0 || !actual.AsSpan().SequenceEqual(bytes))
            throw new ExcepcionCaja("El documento cambió. Recargue su versión actual.", "CONCURRENCIA_CAJA");
    }
    protected async Task ValidarSucursalAsync(ContextoConfiguracion s, CancellationToken t)
    {
        if (!await Contexto.UsuariosSucursales.AsNoTracking().AnyAsync(x => x.UsuarioId == s.UsuarioId
            && x.SucursalId == s.SucursalId && x.Sucursal.EmpresaId == s.EmpresaId
            && x.Usuario.Estado.Activo && Operativos.Contains(x.Usuario.Estado.Nombre)
            && x.Estado.Activo && Operativos.Contains(x.Estado.Nombre)
            && x.Sucursal.Estado.Activo && Operativos.Contains(x.Sucursal.Estado.Nombre)
            && x.Sucursal.Empresa.Estado.Activo && Operativos.Contains(x.Sucursal.Empresa.Estado.Nombre), t))
            throw new CajaNoEncontradaException("Sucursal no disponible en el contexto autorizado.");
    }
    protected async Task<Monedas> MonedaAsync(ContextoConfiguracion s, CancellationToken t)
    {
        var monedas = await Contexto.Monedas.AsNoTracking().Where(x => x.EmpresaId == s.EmpresaId && x.EsMonedaBase
            && x.Estado.Activo && Operativos.Contains(x.Estado.Nombre)).ToArrayAsync(t);
        if (monedas.Length != 1 || monedas[0].Decimales != 2)
            throw new ExcepcionCaja("Configure una única moneda base operativa con dos decimales.");
        return monedas[0];
    }
    protected async Task<int> ActivoAsync(CancellationToken t) =>
        await Contexto.Permisos.Where(x => x.Codigo == "CAJA.OPERAR" && x.Modulo.Activo
            && x.Estado.Activo && Operativos.Contains(x.Estado.Nombre))
            .Select(x => (int?)x.EstadoId).SingleOrDefaultAsync(t)
        ?? throw new ExcepcionCaja("Prepare explícitamente los catálogos de Fase 8.");
    protected async Task<EstadosFlujo> EstadoAsync(string codigo, CancellationToken t) =>
        await Contexto.EstadosFlujo.AsNoTracking().SingleOrDefaultAsync(x => x.FlujoEstado.Codigo == FlujoCaja
            && x.Codigo == codigo && x.Estado.Activo && Operativos.Contains(x.Estado.Nombre)
            && x.FlujoEstado.Estado.Activo && Operativos.Contains(x.FlujoEstado.Estado.Nombre), t)
        ?? throw new ExcepcionCaja("Falta el flujo operativo de Caja. Prepare Fase 8 explícitamente.");
    protected async Task<int> TipoAsync(string clase, string nombre, CancellationToken t) =>
        await Contexto.Tipos.AsNoTracking().Where(x => x.ClaseTipo.Nombre == clase && x.Nombre == nombre
            && x.ModuloId == "CAJA" && x.Estado.Activo && Operativos.Contains(x.Estado.Nombre)
            && x.ClaseTipo.Estado.Activo && Operativos.Contains(x.ClaseTipo.Estado.Nombre))
            .Select(x => (int?)x.Id).SingleOrDefaultAsync(t)
        ?? throw new ExcepcionCaja($"Falta el concepto operativo de caja {nombre}.");
    protected async Task<FormasPago[]> FormasAsync(ContextoConfiguracion s, CancellationToken t) =>
        await Contexto.FormasPago.AsNoTracking().Where(x => x.EmpresaId == s.EmpresaId && !x.EsCredito
            && x.Estado.Activo && Operativos.Contains(x.Estado.Nombre)
            && (x.Codigo == "EFECTIVO" && x.AfectaCaja && x.PermiteCambio
                || x.Codigo != "EFECTIVO" && !x.AfectaCaja && !x.PermiteCambio)).OrderBy(x => x.Nombre).ToArrayAsync(t);
    protected async Task<FormasPago> EfectivoAsync(ContextoConfiguracion s, CancellationToken t) =>
        (await FormasAsync(s, t)).SingleOrDefault(x => x.Codigo == "EFECTIVO")
        ?? throw new ExcepcionCaja("Falta la forma EFECTIVO operativa y compatible.");
    protected async Task<string> DocumentoAsync(string serie, ContextoConfiguracion s, CancellationToken t)
    {
        var nombre = serie switch { "CA" => "Apertura caja", "MC" => "Movimiento caja", "CC" => "Cierre caja", _ => throw new ExcepcionCaja("Serie inválida.") };
        var doc = await Contexto.SeriesDocumentos.AsNoTracking().SingleOrDefaultAsync(x => x.EmpresaId == s.EmpresaId
            && x.SucursalId == null && x.Serie == serie && x.TipoDocumento.Nombre == nombre
            && x.TipoDocumento.ModuloId == "CAJA" && x.TipoDocumento.ClaseTipo.Nombre == "DOCUMENTOS_CAJA"
            && x.Estado.Activo && Operativos.Contains(x.Estado.Nombre)
            && x.TipoDocumento.Estado.Activo && Operativos.Contains(x.TipoDocumento.Estado.Nombre)
            && x.TipoDocumento.ClaseTipo.Estado.Activo && Operativos.Contains(x.TipoDocumento.ClaseTipo.Estado.Nombre), t)
            ?? throw new ExcepcionCaja($"Falta la serie operativa {serie} de Caja.");
        var correlativo = Contexto.CorrelativosDocumentos.Local.FirstOrDefault(x => x.SerieDocumentoId == doc.Id)
            ?? await Contexto.CorrelativosDocumentos.SingleOrDefaultAsync(x => x.SerieDocumentoId == doc.Id, t)
            ?? throw new ExcepcionCaja("La serie no tiene correlativo.");
        if (correlativo.UltimoNumero == long.MaxValue) throw new ExcepcionCaja("Correlativo agotado.");
        var numero = correlativo.UltimoNumero + 1;
        var digitos = numero.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var texto = (doc.Prefijo ?? doc.Serie + "-") + digitos.PadLeft(doc.Longitud, '0');
        if (digitos.Length > doc.Longitud || texto.Length > 30) throw new ExcepcionCaja("Correlativo agotado.");
        correlativo.UltimoNumero = numero;
        correlativo.FechaModificacion = Ahora;
        return texto;
    }
    protected void Auditar(ContextoConfiguracion s, object id, string accion, string comentario, string entidad = "TurnosCaja")
    {
        static string? Limitar(string? v, int n) => v is null ? null : v[..Math.Min(v.Length, n)];
        Contexto.Bitacoras.Add(new Bitacoras { EmpresaId = s.EmpresaId, SucursalId = s.SucursalId,
            UsuarioId = s.UsuarioId, ModuloId = "CAJA", Entidad = entidad, EntidadId = id.ToString(),
            Accion = accion, Fecha = Ahora, Comentario = Limitar(comentario, 500),
            DireccionIp = Limitar(s.DireccionIp, 45), Equipo = Limitar(s.Equipo, 200) });
    }
    protected async Task<T> EscribirAsync<T>(ContextoConfiguracion s, Func<Task<T>> operacion, CancellationToken t,
        string? rechazo = null)
    {
        var clave = s.EmpresaId.ToString(System.Globalization.CultureInfo.InvariantCulture);
        await using var bloqueo = await coordinador.AdquirirAsync(clave, t);
        if (Contexto.ChangeTracker.HasChanges()) throw new ExcepcionCaja("Hay cambios pendientes ajenos a Caja.");
        Contexto.ChangeTracker.Clear();
        await using var transaccion = Contexto.Database.IsRelational()
            ? await Contexto.Database.BeginTransactionAsync(IsolationLevel.Serializable, t) : null;
        var autorizado = false;
        try
        {
            // Compartido con ventas/inventario: serializa cobro versus anulación entre procesos.
            if (Contexto.Database.IsSqlServer())
                await Contexto.Database.ExecuteSqlInterpolatedAsync($"""
                    DECLARE @r int;
                    EXEC @r = sys.sp_getapplock @Resource = {"Heladeria.Inventario." + clave},
                        @LockMode=N'Exclusive', @LockOwner=N'Transaction', @LockTimeout=10000;
                    IF @r<0 THROW 51002, 'Reserva transaccional no disponible.', 1;
                    """, t);
            await ValidarSucursalAsync(s, t);
            autorizado = true;
            var resultado = await operacion();
            await Contexto.SaveChangesAsync(t);
            if (transaccion is not null) await transaccion.CommitAsync(t);
            return resultado;
        }
        catch (Exception e)
        {
            if (transaccion is not null) await transaccion.RollbackAsync(CancellationToken.None);
            Contexto.ChangeTracker.Clear();
            if (autorizado && rechazo is not null)
            {
                if (transaccion is not null) await transaccion.DisposeAsync();
                try
                {
                    if (await Contexto.Modulos.AnyAsync(x => x.Id == "CAJA", CancellationToken.None))
                    {
                        Auditar(s, "Solicitud", rechazo, "Operación revertida: " + (e is ExcepcionCaja c ? c.Codigo : "ERROR_TRANSACCIONAL"));
                        await Contexto.SaveChangesAsync(CancellationToken.None);
                    }
                }
                catch (Exception) { Contexto.ChangeTracker.Clear(); }
            }
            if (e is SqlException { Number: 51080 }) throw new ExcepcionCaja("Aplique primero el script controlado de Fase 8.");
            if (e is DbUpdateConcurrencyException || e is SqlException { Number: 51002 or 1205 or 1222 or 2601 or 2627 }
                || e is DbUpdateException { InnerException: SqlException { Number: 2601 or 2627 } })
                throw new ExcepcionCaja("Conflicto concurrente. Recargue; para cobros conserve la UUID original.", "CONCURRENCIA_CAJA");
            throw;
        }
    }
}
