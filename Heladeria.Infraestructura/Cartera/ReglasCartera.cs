using Heladeria.Aplicacion.Cartera;
using Heladeria.Aplicacion.Configuracion;
using Heladeria.Aplicacion.Seguridad.Configuracion;
using Heladeria.Aplicacion.Tiempo;
using Heladeria.Infraestructura.Persistencia;
using Heladeria.Infraestructura.Persistencia.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Heladeria.Infraestructura.Cartera;

internal static class ReglasCartera
{
    internal const string Cobro = "COBRO_CUENTA_POR_COBRAR";
    internal const string Pago = "PAGO_CUENTA_POR_PAGAR";
    internal static DateOnly Hoy(DateTime utc, OpcionesSeguridad opciones, ContextoConfiguracion s) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(utc, TimeZoneInfo.FindSystemTimeZoneById(
            TiempoEmpresarial.Zona(opciones, s.EmpresaId, s.SucursalId))));
    internal static async Task<EstadosFlujo> EstadoAsync(HeladeriaDbContext db, bool pagar, string codigo,
        string[] operativos, CancellationToken t) => await db.EstadosFlujo.AsNoTracking().SingleOrDefaultAsync(x =>
            x.FlujoEstado.Codigo == (pagar ? "CUENTAS_POR_PAGAR" : "CUENTAS_POR_COBRAR") && x.Codigo == codigo
            && x.Estado.Activo && operativos.Contains(x.Estado.Nombre) && x.FlujoEstado.Estado.Activo
            && operativos.Contains(x.FlujoEstado.Estado.Nombre), t)
        ?? throw new ExcepcionCartera("Prepare explícitamente los catálogos financieros de Fase 9.");

    internal static void Auditar(HeladeriaDbContext db, bool pagar, object id, string accion,
        string comentario, ContextoConfiguracion s, DateTime ahora)
    {
        static string? Limitar(string? v, int n) => v is null ? null : v[..Math.Min(n, v.Length)];
        db.Bitacoras.Add(new Bitacoras { EmpresaId = s.EmpresaId, SucursalId = s.SucursalId,
            UsuarioId = s.UsuarioId, ModuloId = pagar ? "CXP" : "CXC", Entidad = pagar ? "CuentasPorPagar" : "CuentasPorCobrar",
            EntidadId = id.ToString(), Accion = accion, Fecha = ahora, Comentario = Limitar(comentario, 500),
            DireccionIp = Limitar(s.DireccionIp, 45), Equipo = Limitar(s.Equipo, 200) });
    }

    // El llamador es CompletarAsync: misma transacción, reserva empresarial y DbContext de inventario/venta.
    internal static async Task GenerarDesdeVentaAsync(HeladeriaDbContext db, Persistencia.Entidades.Ventas venta,
        ContextoConfiguracion s, OpcionesSeguridad opciones, DateTime ahora, CancellationToken t)
    {
        if (!venta.EsCredito) return;
        if (!venta.ClienteId.HasValue) throw new ExcepcionCartera("El crédito requiere un cliente.", "CLIENTE_REQUERIDO_PARA_CREDITO", 400);
        if (!venta.FechaVencimientoCredito.HasValue || venta.FechaVencimientoCredito < Hoy(ahora, opciones, s))
            throw new ExcepcionCartera("Indique un vencimiento vigente para el crédito.", "VALIDACION_CXC", 400);
        if (venta.TotalVenta <= 0 || await db.CuentasPorCobrar.AnyAsync(x => x.VentaId == venta.Id, t)
            || await db.Pagos.AnyAsync(x => x.VentaId == venta.Id, t))
            throw new ExcepcionCartera("La venta ya tiene una operación financiera o su importe no es positivo.");
        var estado = await EstadoAsync(db, false, "PENDIENTE", opciones.EstadosActivos, t);
        var cuenta = new CuentasPorCobrar { EmpresaId = s.EmpresaId, SucursalId = s.SucursalId,
            ClienteId = venta.ClienteId.Value, VentaId = venta.Id, MonedaId = venta.MonedaId,
            FechaEmision = Hoy(ahora, opciones, s), FechaVencimiento = venta.FechaVencimientoCredito.Value,
            MontoOriginal = venta.TotalVenta, SaldoPendiente = venta.TotalVenta,
            FlujoEstadoId = estado.FlujoEstadoId, EstadoId = estado.Id,
            UsuarioCreacionId = s.UsuarioId, FechaCreacion = ahora };
        db.CuentasPorCobrar.Add(cuenta);
        await db.SaveChangesAsync(t);
        Auditar(db, false, cuenta.Id, "CUENTA_GENERADA", $"Origen venta {venta.Id}; saldo inicial {venta.TotalVenta}.", s, ahora);
    }
}
