using Heladeria.Aplicacion.Caja;
using Heladeria.Aplicacion.Cartera;
using Heladeria.Aplicacion.Configuracion;
using Heladeria.Aplicacion.Seguridad.Configuracion;
using Heladeria.Infraestructura.Caja;
using Heladeria.Infraestructura.Inventario;
using Heladeria.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Heladeria.Infraestructura.Cartera;

public sealed partial class ServicioCartera(HeladeriaDbContext contexto, IOptions<OpcionesSeguridad> opciones,
    TimeProvider reloj, CoordinadorAjustesInventario coordinador, ServicioCaja caja)
    : ServicioCajaBase(contexto, opciones, reloj, coordinador), IServicioCartera
{
    private DateOnly Hoy(ContextoConfiguracion s) => ReglasCartera.Hoy(Ahora, ConfiguracionTiempo, s);
    private sealed class Fila
    {
        public long Id { get; init; }
        public int EmpresaId { get; init; }
        public long SucursalId { get; init; }
        public int TerceroId { get; init; }
        public string Tercero { get; init; } = "";
        public long OrigenId { get; init; }
        public string Numero { get; init; } = "";
        public long? CompraId { get; init; }
        public int MonedaId { get; init; }
        public string Moneda { get; init; } = "";
        public DateOnly Emision { get; init; }
        public DateOnly Vence { get; init; }
        public decimal Original { get; init; }
        public decimal Saldo { get; init; }
        public string Estado { get; init; } = "";
        public byte[] Version { get; init; } = [];
        public bool Operable { get; init; }
        public int? CreadoPor { get; init; }
        public DateTime? Creado { get; init; }
        public int? ModificadoPor { get; init; }
        public DateTime? Modificado { get; init; }
    }
    private IQueryable<Fila> Filas(bool pagar, ContextoConfiguracion s) => pagar
        ? Contexto.CuentasPorPagar.AsNoTracking().Where(x => x.EmpresaId == s.EmpresaId
            && (x.SucursalId ?? x.FacturaProveedor.OrdenCompra!.SucursalId) == s.SucursalId).Select(x => new Fila {
                Id = x.Id, EmpresaId = x.EmpresaId, SucursalId = x.SucursalId ?? x.FacturaProveedor.OrdenCompra!.SucursalId,
                TerceroId = x.ProveedorId, Tercero = x.Proveedor.NombreProveedor, OrigenId = x.FacturaProveedorId,
                Numero = (x.FacturaProveedor.Serie ?? "") + " " + x.FacturaProveedor.NumeroDocumento,
                CompraId = x.FacturaProveedor.OrdenCompraId, MonedaId = x.MonedaId ?? x.FacturaProveedor.MonedaId,
                Moneda = x.FacturaProveedor.Moneda.CodigoISO, Emision = x.FechaEmision, Vence = x.FechaVencimiento,
                Original = x.MontoOriginal, Saldo = x.SaldoPendiente, Estado = x.EstadosFlujo.Codigo,
                Version = x.VersionFila, Operable = x.FechaCreacion != null, CreadoPor = x.UsuarioCreacionId,
                Creado = x.FechaCreacion, ModificadoPor = x.UsuarioModificacionId, Modificado = x.FechaModificacion })
        : Contexto.CuentasPorCobrar.AsNoTracking().Where(x => x.EmpresaId == s.EmpresaId
            && (x.SucursalId ?? x.Venta.SucursalId) == s.SucursalId).Select(x => new Fila {
                Id = x.Id, EmpresaId = x.EmpresaId, SucursalId = x.SucursalId ?? x.Venta.SucursalId,
                TerceroId = x.ClienteId, Tercero = x.Cliente.NombreCliente, OrigenId = x.VentaId, Numero = x.Venta.NumeroFactura,
                MonedaId = x.MonedaId ?? x.Venta.MonedaId, Moneda = x.Venta.Moneda.CodigoISO,
                Emision = x.FechaEmision, Vence = x.FechaVencimiento, Original = x.MontoOriginal, Saldo = x.SaldoPendiente,
                Estado = x.EstadosFlujo.Codigo, Version = x.VersionFila, Operable = x.FechaCreacion != null,
                CreadoPor = x.UsuarioCreacionId, Creado = x.FechaCreacion,
                ModificadoPor = x.UsuarioModificacionId, Modificado = x.FechaModificacion });
    private IQueryable<Fila> Filtrar(bool pagar, FiltroCartera f, ContextoConfiguracion s)
    {
        Validar(f);
        if (f.FechaDesde > f.FechaHasta) throw new ExcepcionCartera("Rango de fechas inválido.", pagar ? "VALIDACION_CXP" : "VALIDACION_CXC", 400);
        var q = Filas(pagar, s);
        if (f.SucursalId.HasValue) q = q.Where(x => x.SucursalId == f.SucursalId);
        if (f.TerceroId.HasValue) q = q.Where(x => x.TerceroId == f.TerceroId);
        if (f.Estado is not null) q = q.Where(x => x.Estado == f.Estado);
        if (f.FechaDesde.HasValue) q = q.Where(x => x.Emision >= f.FechaDesde);
        if (f.FechaHasta.HasValue) q = q.Where(x => x.Emision <= f.FechaHasta);
        if (Opcional(f.Busqueda) is { } b) q = q.Where(x => x.Numero.Contains(b) || x.Tercero.Contains(b));
        var hoy = Hoy(s);
        if (f.Vencimiento == "PAGADA") q = q.Where(x => x.Estado == "PAGADA");
        if (f.Vencimiento == "VENCIDA") q = q.Where(x => x.Estado != "ANULADA" && x.Saldo > 0 && x.Vence < hoy);
        if (f.Vencimiento == "VIGENTE") q = q.Where(x => x.Estado != "ANULADA" && x.Saldo > 0 && x.Vence >= hoy);
        return q;
    }
    private static CuentaCarteraDto Dto(Fila x, bool pagar, DateOnly hoy) => new(x.Id, x.EmpresaId,
        x.SucursalId, x.TerceroId, x.Tercero, x.OrigenId, pagar ? "FACTURA_PROVEEDOR" : "VENTA", x.Numero.Trim(),
        x.CompraId, x.MonedaId, x.Moneda, x.Emision, x.Vence, x.Original, x.Original - x.Saldo, x.Saldo,
        x.Estado, x.Estado is "PAGADA" or "ANULADA" ? x.Estado : x.Vence < hoy ? "VENCIDA" : "VIGENTE",
        Convert.ToBase64String(x.Version), x.Operable, x.CreadoPor, x.Creado, x.ModificadoPor, x.Modificado);

    public Task<RespuestaPaginada<CuentaCarteraDto>> ListarAsync(bool porPagar, FiltroCartera filtro, ContextoConfiguracion s, CancellationToken t) =>
        EscribirAsync(s, async () => {
            var q = Filtrar(porPagar, filtro, s);
            var total = await q.CountAsync(t);
            var filas = await q.OrderByDescending(x => x.Id).Skip(filtro.CalcularDesplazamiento())
                .Take(filtro.TamanioPagina).ToArrayAsync(t);
            return new RespuestaPaginada<CuentaCarteraDto>(filas.Select(x => Dto(x, porPagar, Hoy(s))).ToArray(), filtro.Pagina, filtro.TamanioPagina, total);
        }, t);
    public Task<IReadOnlyList<ResumenCarteraDto>> ResumenAsync(bool porPagar, FiltroCartera filtro, ContextoConfiguracion s, CancellationToken t) =>
        EscribirAsync<IReadOnlyList<ResumenCarteraDto>>(s, async () => {
            var filas = await Filtrar(porPagar, filtro, s).Where(x => x.Saldo > 0 && x.Estado != "ANULADA").ToArrayAsync(t);
            var hoy = Hoy(s);
            return filas.GroupBy(x => new { x.MonedaId, x.Moneda }).Select(g => new ResumenCarteraDto(g.Key.MonedaId,
                g.Key.Moneda, g.Sum(x => x.Saldo), g.Where(x => x.Vence < hoy).Sum(x => x.Saldo),
                g.Where(x => x.Vence >= hoy).Sum(x => x.Saldo), g.Count(), g.Count(x => x.Vence < hoy))).ToArray();
        }, t);
    public async Task<ReferenciasCajaDto> ReferenciasAsync(bool porPagar, ContextoConfiguracion s, CancellationToken t)
    {
        var r = await caja.ReferenciasAsync(s, t);
        return r with { FormasPago = r.FormasPago.Where(x => x.Codigo == "EFECTIVO" || !porPagar && x.Codigo == "TARJETA").ToArray() };
    }
    public Task<DetalleCarteraDto> ObtenerAsync(bool porPagar, long id, ContextoConfiguracion s, CancellationToken t) =>
        EscribirAsync(s, () => LeerAsync(porPagar, id, s, t), t);
    private async Task<DetalleCarteraDto> LeerAsync(bool pagar, long id, ContextoConfiguracion s, CancellationToken t)
    {
        var fila = await Filas(pagar, s).SingleOrDefaultAsync(x => x.Id == id, t)
            ?? throw new ExcepcionCartera("Cuenta no encontrada en la sucursal autorizada.", pagar ? "CUENTA_CXP_NO_ENCONTRADA" : "CUENTA_CXC_NO_ENCONTRADA", 404);
        var aplicaciones = pagar
            ? await Contexto.AplicacionesCuentasPorPagar.AsNoTracking().Where(x => x.CuentaPorPagarId == id)
                .Select(x => new { x.Id, x.PagoId, x.FechaAplicacion, x.MontoAplicado, x.UsuarioId }).OrderBy(x => x.Id).ToArrayAsync(t)
            : await Contexto.AplicacionesCuentasPorCobrar.AsNoTracking().Where(x => x.CuentaPorCobrarId == id)
                .Select(x => new { x.Id, x.PagoId, x.FechaAplicacion, x.MontoAplicado, x.UsuarioId }).OrderBy(x => x.Id).ToArrayAsync(t);
        var ids = aplicaciones.Select(x => x.PagoId).ToArray();
        var pagos = await Contexto.Pagos.AsNoTracking().Include(x => x.Usuario).Include(x => x.Estado)
            .Include(x => x.PagoDetalles).ThenInclude(x => x.FormaPago).Where(x => ids.Contains(x.Id)).ToArrayAsync(t);
        var movimientos = await Contexto.MovimientosCaja.AsNoTracking().Where(x => x.PagoDetalleId != null && ids.Contains(x.PagoDetalle!.PagoId))
            .Select(x => new { x.Id, x.PagoDetalle!.PagoId }).ToArrayAsync(t);
        var historial = aplicaciones.Select(a => {
            var p = pagos.Single(x => x.Id == a.PagoId);
            var d = p.PagoDetalles.Count == 1 ? p.PagoDetalles.Single() : null;
            return new AplicacionCarteraDto(a.Id, p.Id, a.FechaAplicacion, a.MontoAplicado,
                d?.FormaPagoId, d?.FormaPago.Nombre ?? "Mixto", d?.Referencia, p.ClaveOperacion, p.Observacion,
                a.UsuarioId, p.Usuario.UserName, p.TurnoCajaId, movimientos.Where(m => m.PagoId == p.Id).Select(m => m.Id).ToArray(),
                p.Estado.Activo && Operativos.Contains(p.Estado.Nombre) ? "APLICADO" : "NO_VIGENTE") {
                    Detalles = p.PagoDetalles.OrderBy(x => x.Id).Select(x => new DetalleAplicacionCarteraDto(
                        x.Id, x.FormaPagoId, x.FormaPago.Nombre, x.Monto, x.Referencia)).ToArray() };
        }).ToArray();
        var aplicado = aplicaciones.Where(a => pagos.Any(p => p.Id == a.PagoId && p.Estado.Activo && Operativos.Contains(p.Estado.Nombre))).Sum(x => x.MontoAplicado);
        if (fila.Operable && (fila.Original - aplicado != fila.Saldo || fila.Saldo < 0
            || pagos.Any(p => p.PagoDetalles.Count != 1 || p.MontoTotal != aplicaciones.Single(a => a.PagoId == p.Id).MontoAplicado)))
            throw new ExcepcionCartera("El saldo y su historial requieren conciliación; no se corregirán automáticamente.");
        return new(Dto(fila, pagar, Hoy(s)), historial);
    }
}
