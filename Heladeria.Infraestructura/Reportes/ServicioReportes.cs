using System.ComponentModel.DataAnnotations;
using Heladeria.Aplicacion.Configuracion;
using Heladeria.Aplicacion.Reportes;
using Heladeria.Aplicacion.Seguridad.Configuracion;
using Heladeria.Aplicacion.Tiempo;
using Heladeria.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Heladeria.Infraestructura.ReportesOperativos;

// Consultas exclusivamente de lectura. No utiliza los coordinadores ni SaveChanges de operaciones financieras.
public sealed partial class ServicioReportes(HeladeriaDbContext db, IOptions<OpcionesSeguridad> opciones,
    TimeProvider reloj) : IServicioReportes
{
    private readonly string[] activos = opciones.Value.EstadosActivos;
    private static void Validar(FiltroPaginadoBase f)
    {
        var errores = new List<ValidationResult>();
        if (!Validator.TryValidateObject(f, new(f), errores, true)
            || (long)(f.Pagina - 1) * f.TamanioPagina > int.MaxValue)
            throw new ExcepcionReporte("Filtros o paginación inválidos. Tamaño máximo: 100.");
    }
    private async Task<long> AlcanceAsync(FiltroPaginadoBase f, long? sucursal, ContextoConfiguracion s, CancellationToken t)
    {
        Validar(f);
        var id = sucursal ?? s.SucursalId;
        if (!await db.UsuariosSucursales.AsNoTracking().AnyAsync(x => x.UsuarioId == s.UsuarioId
            && x.SucursalId == id && x.Sucursal.EmpresaId == s.EmpresaId
            && x.Usuario.Estado.Activo && activos.Contains(x.Usuario.Estado.Nombre)
            && x.Estado.Activo && activos.Contains(x.Estado.Nombre)
            && x.Sucursal.Estado.Activo && activos.Contains(x.Sucursal.Estado.Nombre)
            && x.Sucursal.Empresa.Estado.Activo && activos.Contains(x.Sucursal.Empresa.Estado.Nombre), t))
            throw new ExcepcionReporte("Sucursal fuera del alcance autorizado.", "ALCANCE_REPORTE_DENEGADO", 403);
        return id;
    }
    private DateOnly Hoy(ContextoConfiguracion s, long sucursal) => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(
        reloj.GetUtcNow(), TimeZoneInfo.FindSystemTimeZoneById(TiempoEmpresarial.Zona(opciones.Value, s.EmpresaId, sucursal))).DateTime);
    private PeriodoReporte Periodo(FiltroReporte f, ContextoConfiguracion s, long sucursal)
    {
        var hasta = f.FechaHasta ?? Hoy(s, sucursal);
        if (hasta.Year < 1900 || hasta.Year > 9998)
            throw new ExcepcionReporte("El rango debe estar entre los años 1900 y 9998.");
        var desde = f.FechaDesde ?? hasta.AddDays(-29);
        if (desde > hasta || hasta.DayNumber - desde.DayNumber > 365 || hasta == DateOnly.MaxValue
            || desde.Year < 1900 || hasta.Year > 9998)
            throw new ExcepcionReporte("El rango debe ser válido y no exceder 366 días (años 1900–9998).");
        return new(desde, hasta, TiempoEmpresarial.Zona(opciones.Value, s.EmpresaId, sucursal), sucursal);
    }
    private static (DateTime Desde, DateTime Hasta) Utc(PeriodoReporte p)
    {
        try { return (TiempoEmpresarial.AUTC(p.FechaDesde.ToDateTime(TimeOnly.MinValue), p.ZonaHoraria),
            TiempoEmpresarial.AUTC(p.FechaHasta.AddDays(1).ToDateTime(TimeOnly.MinValue), p.ZonaHoraria)); }
        catch (ArgumentException) { throw new ExcepcionReporte("El rango incluye una medianoche inválida o ambigua en la zona de la sucursal."); }
    }
    private static string? Texto(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
    private static async Task<RespuestaPaginada<T>> PaginaAsync<T>(IQueryable<T> q, FiltroPaginadoBase f, CancellationToken t, int? totalConocido = null)
    {
        var total = totalConocido ?? await q.CountAsync(t);
        var filas = await q.Skip(checked((f.Pagina - 1) * f.TamanioPagina)).Take(f.TamanioPagina).ToArrayAsync(t);
        return new(filas, f.Pagina, f.TamanioPagina, total);
    }
    private static async Task<T[]> ResumenAsync<T>(IQueryable<T> q, CancellationToken t)
    {
        var filas = await q.Take(101).ToArrayAsync(t);
        if (filas.Length > 100) throw new ExcepcionReporte("Demasiados grupos de resumen; reduzca el alcance de los filtros.");
        return filas;
    }
}
