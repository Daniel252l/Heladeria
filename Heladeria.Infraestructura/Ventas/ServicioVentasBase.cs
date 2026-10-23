using System.ComponentModel.DataAnnotations;
using System.Data;
using Heladeria.Aplicacion.Configuracion;
using Heladeria.Aplicacion.Seguridad.Configuracion;
using Heladeria.Aplicacion.Ventas;
using Heladeria.Aplicacion.Productos;
using Heladeria.Infraestructura.Inventario;
using Heladeria.Infraestructura.Persistencia;
using Heladeria.Infraestructura.Persistencia.Entidades;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Heladeria.Infraestructura.VentasPos;

public abstract class ServicioVentasBase(HeladeriaDbContext contexto, IOptions<OpcionesSeguridad> opciones,
    TimeProvider reloj, CoordinadorAjustesInventario coordinador, ServicioInventario inventario)
{
    protected HeladeriaDbContext Contexto { get; } = contexto;
    protected ServicioInventario Inventario { get; } = inventario;
    protected string[] Operativos { get; } = opciones.Value.EstadosActivos;
    protected OpcionesSeguridad ConfiguracionTiempo { get; } = opciones.Value;
    protected DateTime Ahora => reloj.GetUtcNow().UtcDateTime;
    public const string FlujoVenta = "VENTA_POS";
    protected virtual bool AuditarRechazos => false;
    protected static void Validar(object solicitud)
    {
        var errores = new List<ValidationResult>();
        if (!Validator.TryValidateObject(solicitud, new(solicitud), errores, true))
            throw new ValidacionVentasException(string.Join(" ", errores.Select(e => e.ErrorMessage)));
    }
    protected static string? Opcional(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
    protected static void Precision(decimal valor, string nombre, bool positivo = true)
    {
        if (valor < 0 || positivo && valor == 0 || valor > 9999999999999.999999m || valor != decimal.Round(valor, 6))
            throw new ValidacionVentasException($"{nombre} no cabe en decimal(19,6) o tiene precisión inválida.");
    }
    protected static void Version(byte[] actual, string recibida)
    {
        byte[] bytes;
        try { bytes = Convert.FromBase64String(recibida); }
        catch (FormatException) { throw new ValidacionVentasException("VersionFila debe ser Base64."); }
        if (!actual.AsSpan().SequenceEqual(bytes))
            throw new ConflictoVentasException("La venta fue modificada. Recargue su versión actual.");
    }
    protected async Task ValidarSucursalAsync(ContextoConfiguracion s, CancellationToken t)
    {
        if (!await Contexto.UsuariosSucursales.AsNoTracking().AnyAsync(x => x.UsuarioId == s.UsuarioId
            && x.SucursalId == s.SucursalId && x.Sucursal.EmpresaId == s.EmpresaId
            && x.Estado.Activo && Operativos.Contains(x.Estado.Nombre)
            && x.Sucursal.Estado.Activo && Operativos.Contains(x.Sucursal.Estado.Nombre)
            && x.Sucursal.Empresa.Estado.Activo && Operativos.Contains(x.Sucursal.Empresa.Estado.Nombre), t))
            throw new VentaNoEncontradaException("Sucursal no disponible en el contexto autorizado.");
    }
    protected async Task ValidarAlmacenAsync(long bodega, long? ubicacion, ContextoConfiguracion s, CancellationToken t)
    {
        if (!await Contexto.Bodegas.AsNoTracking().AnyAsync(x => x.Id == bodega && x.EmpresaId == s.EmpresaId
            && x.SucursalId == s.SucursalId && x.Estado.Activo && Operativos.Contains(x.Estado.Nombre), t))
            throw new VentaNoEncontradaException("Bodega no disponible en la sucursal activa.");
        if (ubicacion.HasValue && !await Contexto.Ubicaciones.AsNoTracking().AnyAsync(x => x.Id == ubicacion
            && x.BodegaId == bodega && x.EmpresaId == s.EmpresaId
            && x.Estado.Activo && Operativos.Contains(x.Estado.Nombre), t))
            throw new VentaNoEncontradaException("Ubicación no disponible en la bodega indicada.");
    }
    protected async Task<Monedas> MonedaBaseAsync(ContextoConfiguracion s, CancellationToken t)
    {
        var monedas = await Contexto.Monedas.AsNoTracking().Where(x => x.EmpresaId == s.EmpresaId && x.EsMonedaBase
            && x.Estado.Activo && Operativos.Contains(x.Estado.Nombre)).ToArrayAsync(t);
        if (monedas.Length != 1 || monedas[0].Decimales != 2)
            throw new ConflictoVentasException("Configure una única moneda base operativa con dos decimales para ventas.");
        return monedas[0];
    }
    protected async Task<EstadosFlujo> EstadoAsync(string codigo, CancellationToken t) =>
        await Contexto.EstadosFlujo.AsNoTracking().SingleOrDefaultAsync(x => x.FlujoEstado.Codigo == FlujoVenta
            && x.Codigo == codigo && x.Estado.Activo && Operativos.Contains(x.Estado.Nombre)
            && x.FlujoEstado.Estado.Activo && Operativos.Contains(x.FlujoEstado.Estado.Nombre), t)
        ?? throw new ConflictoVentasException("Falta el flujo operativo de ventas. Ejecute la preparación explícita Fase 7.");

    protected async Task TransicionarAsync(Persistencia.Entidades.Ventas venta, string destino, string permiso, CancellationToken t)
    {
        var estado = await EstadoAsync(destino, t);
        if (!await Contexto.EstadoTransiciones.AsNoTracking().AnyAsync(x => x.FlujoEstadoId == venta.FlujoEstadoId
            && x.EstadoOrigenId == venta.EstadoId && x.EstadoDestinoId == estado.Id && x.PermisoCodigo == permiso
            && x.Estado.Activo && Operativos.Contains(x.Estado.Nombre), t))
            throw new ConflictoVentasException("La transición de venta no está permitida o configurada.");
        venta.EstadoId = estado.Id;
    }
    protected async Task<DocumentoVenta> DocumentoAsync(string serie, ContextoConfiguracion s, CancellationToken t)
    {
        var nombre = serie switch { "VT" => "Venta POS", "VS" => "Salida por venta", "VA" => "Anulación de venta", _ => throw new ConflictoVentasException("Serie no soportada.") };
        var doc = await Contexto.SeriesDocumentos.AsNoTracking().SingleOrDefaultAsync(x => x.EmpresaId == s.EmpresaId
            && x.SucursalId == null && x.Serie == serie && x.TipoDocumento.Nombre == nombre
            && x.TipoDocumento.ModuloId == "VENTAS" && x.TipoDocumento.ClaseTipo.Nombre == "DOCUMENTOS_VENTAS"
            && x.Estado.Activo && Operativos.Contains(x.Estado.Nombre)
            && x.TipoDocumento.Estado.Activo && Operativos.Contains(x.TipoDocumento.Estado.Nombre)
            && x.TipoDocumento.ClaseTipo.Estado.Activo && Operativos.Contains(x.TipoDocumento.ClaseTipo.Estado.Nombre), t)
            ?? throw new ConflictoVentasException($"Falta la serie operativa {serie} de Fase 7.");
        var correlativo = Contexto.CorrelativosDocumentos.Local.FirstOrDefault(x => x.SerieDocumentoId == doc.Id)
            ?? await Contexto.CorrelativosDocumentos.SingleOrDefaultAsync(x => x.SerieDocumentoId == doc.Id, t)
            ?? throw new ConflictoVentasException("La serie no tiene correlativo.");
        if (correlativo.UltimoNumero == long.MaxValue) throw new ConflictoVentasException("Correlativo agotado.");
        var numero = correlativo.UltimoNumero + 1;
        var digitos = numero.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var texto = (doc.Prefijo ?? doc.Serie + "-") + digitos.PadLeft(doc.Longitud, '0');
        if (digitos.Length > doc.Longitud || texto.Length > 30) throw new ConflictoVentasException("Longitud de correlativo agotada.");
        correlativo.UltimoNumero = numero;
        correlativo.FechaModificacion = Ahora;
        return new(doc.Serie, numero, texto);
    }
    protected void Auditar(ContextoConfiguracion s, object id, string accion, string comentario)
    {
        static string? Limitar(string? v, int n) => v is null ? null : v[..Math.Min(v.Length, n)];
        Contexto.Bitacoras.Add(new Bitacoras { EmpresaId = s.EmpresaId, SucursalId = s.SucursalId,
            UsuarioId = s.UsuarioId, ModuloId = "VENTAS", Entidad = "Ventas", EntidadId = id.ToString(),
            Accion = accion, Fecha = Ahora, Comentario = Limitar(comentario, 500),
            DireccionIp = Limitar(s.DireccionIp, 45), Equipo = Limitar(s.Equipo, 200) });
    }
    protected async Task<T> EscribirAsync<T>(ContextoConfiguracion s, Func<Task<T>> operacion, CancellationToken t)
    {
        var clave = s.EmpresaId.ToString(System.Globalization.CultureInfo.InvariantCulture);
        await using var bloqueo = await coordinador.AdquirirAsync(clave, t);
        if (Contexto.ChangeTracker.HasChanges()) throw new ConflictoVentasException("Hay cambios pendientes ajenos a la venta.");
        Contexto.ChangeTracker.Clear();
        await using var transaccion = Contexto.Database.IsRelational()
            ? await Contexto.Database.BeginTransactionAsync(IsolationLevel.Serializable, t) : null;
        var alcanceValidado = false;
        try
        {
            // Misma reserva que Inventario/Compras/Producción: nunca un lock independiente.
            if (Contexto.Database.IsSqlServer())
                await Contexto.Database.ExecuteSqlInterpolatedAsync($"""
                    DECLARE @r int;
                    EXEC @r = sys.sp_getapplock @Resource = {"Heladeria.Inventario." + clave},
                        @LockMode = N'Exclusive', @LockOwner = N'Transaction', @LockTimeout = 10000;
                    IF @r < 0 THROW 51002, 'Reserva de inventario no disponible.', 1;
                    """, t);
            await ValidarSucursalAsync(s, t);
            alcanceValidado = true;
            var resultado = await operacion();
            await Contexto.SaveChangesAsync(t);
            if (transaccion is not null) await transaccion.CommitAsync(t);
            return resultado;
        }
        catch (Exception e)
        {
            if (transaccion is not null) await transaccion.RollbackAsync(CancellationToken.None);
            Contexto.ChangeTracker.Clear();
            if (AuditarRechazos && alcanceValidado && e is ExcepcionVentas or Heladeria.Aplicacion.Caja.ExcepcionCaja or Heladeria.Aplicacion.Cartera.ExcepcionCartera or ExcepcionFase4 or DbUpdateConcurrencyException)
            {
                // Registro separado DESPUÉS del rollback; jamás conserva cambios de negocio fallidos.
                // Preparadores no entran aquí: una preparación rechazada debe permanecer sin cambios.
                if (transaccion is not null) await transaccion.DisposeAsync();
                try
                {
                    if (await Contexto.Modulos.AsNoTracking().AnyAsync(x => x.Id == "VENTAS", CancellationToken.None))
                    {
                        var codigo = e is ExcepcionVentas venta ? venta.Codigo : e is Heladeria.Aplicacion.Cartera.ExcepcionCartera cartera ? cartera.Codigo : "CONFLICTO_INVENTARIO";
                        Auditar(s, "Solicitud", "VENTA_RECHAZADA", $"Operación rechazada y revertida: {codigo}.");
                        await Contexto.SaveChangesAsync(CancellationToken.None);
                    }
                }
                catch (Exception)
                {
                    // La auditoría no debe reemplazar el error original si la conexión también falló.
                    // El manejador HTTP registra siempre el rechazo con TraceId.
                    Contexto.ChangeTracker.Clear();
                }
            }
            if (e is SqlException { Number: 51070 }) throw new ConflictoVentasException("Aplique primero el script controlado de esquema Fase 7.");
            if (e is DbUpdateConcurrencyException || e is SqlException { Number: 51002 or 1205 or 1222 or 2601 or 2627 }
                || e is DbUpdateException { InnerException: SqlException { Number: 2601 or 2627 } })
                throw new ConflictoVentasException("Conflicto de concurrencia. Recargue y reintente con la misma clave de operación.");
            throw;
        }
    }
    protected sealed record DocumentoVenta(string Serie, long Numero, string Texto);
}
