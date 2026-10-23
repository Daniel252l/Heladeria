using Heladeria.Aplicacion.Configuracion;
using Heladeria.Aplicacion.Ventas;
using Heladeria.Aplicacion.Caja;
using Heladeria.Infraestructura.Caja;
using Heladeria.Infraestructura.Fase4;
using Heladeria.Infraestructura.Inventario;
using Heladeria.Infraestructura.Persistencia;
using Heladeria.Infraestructura.Seguridad.Bootstrap;
using Heladeria.Infraestructura.VentasPos;
using Heladeria.Pruebas.Fase4;
using Heladeria.Pruebas.Fase5;
using Microsoft.EntityFrameworkCore;

namespace Heladeria.Pruebas.Fase7;

internal sealed class EscenarioFase7 : IAsyncDisposable
{
    private readonly EscenarioFase5 _fase5;
    internal EscenarioFase7(EscenarioFase5 fase5)
    {
        _fase5 = fase5;
        Contexto = fase5.Contexto;
        if (!Contexto.Database.IsRelational())
            Contexto.SavingChanges += (_, _) => {
                // InMemory no genera rowversion. Solo se inicializa el token ausente de filas nuevas de la fixture.
                foreach (var fila in Contexto.ChangeTracker.Entries().Where(x => x.State == EntityState.Added))
                    foreach (var propiedad in fila.Properties.Where(x => x.Metadata.Name == "VersionFila" && x.CurrentValue is null))
                        propiedad.CurrentValue = new byte[] { 1 };
            };
        Inventario = new ServicioInventario(Contexto, new AuditoriaFase4(Contexto, TimeProvider.System),
            EscenarioFase5.Opciones, TimeProvider.System, fase5.Coordinador);
        Ventas = new ServicioVentas(Contexto, EscenarioFase5.Opciones, TimeProvider.System, fase5.Coordinador, Inventario);
        Preparador = new PreparadorFase7(Contexto, EscenarioFase5.Opciones, TimeProvider.System, fase5.Coordinador, Inventario);
    }
    internal static ContextoConfiguracion Seguridad => EscenarioFase5.Seguridad;
    internal HeladeriaDbContext Contexto { get; }
    internal ServicioInventario Inventario { get; }
    internal ServicioVentas Ventas { get; }
    internal PreparadorFase7 Preparador { get; }
    internal static async Task<EscenarioFase7> CrearAsync(string? nombre = null, bool semillar = true, CoordinadorAjustesInventario? coordinador = null)
    {
        coordinador ??= new();
        var fase4 = await EscenarioFase4.CrearAsync(nombre ?? $"fase7-{Guid.NewGuid():N}", EscenarioFase4.RaizCompartida, semillar, coordinador);
        var e = new EscenarioFase7(new EscenarioFase5(fase4, coordinador));
        if (semillar) await e.SemillarAsync();
        return e;
    }
    internal async Task SemillarAsync(bool prepararCaja = true)
    {
        await _fase5.SemillarAsync(preparar: false);
        var pp = await Contexto.ProductoPresentaciones.SingleAsync(x => x.Id == 1);
        pp.PrecioVentaEstandar = 25m;
        var vaso = await Contexto.ProductoPresentaciones.SingleAsync(x => x.Id == 2);
        vaso.PrecioVentaEstandar = 8m;
        await Contexto.SaveChangesAsync();
        Contexto.ChangeTracker.Clear();
        await Preparador.EjecutarAsync(default);
        Contexto.ChangeTracker.Clear();
        if (prepararCaja)
        {
            await new PreparadorFase8(Contexto, EscenarioFase5.Opciones, TimeProvider.System, _fase5.Coordinador).EjecutarAsync(default);
            var servicio = new ServicioCaja(Contexto, EscenarioFase5.Opciones, TimeProvider.System, _fase5.Coordinador);
            var caja = await servicio.CrearCajaAsync(new() { Codigo = "CAJA-VENTAS", Nombre = "Caja de pruebas de ventas" }, Seguridad, default);
            var sesion = await servicio.AbrirAsync(new() { CajaId = caja.Id, MonedaId = 1, SaldoInicial = 0 }, Seguridad, default);
            if (!Contexto.Database.IsRelational())
            {
                var turno = await Contexto.TurnosCaja.SingleAsync(x => x.Id == sesion.Encabezado.Id);
                turno.VersionFila = [1]; await Contexto.SaveChangesAsync();
            }
            Contexto.ChangeTracker.Clear();
        }
    }
    internal static CrearVentaSolicitud Solicitud(decimal cantidad = 2, long producto = 1,
        long? presentacion = 1, decimal descuento = 0, long bodega = 1, long ubicacion = 1,
        long sucursal = 1, int moneda = 1, int? cliente = null) => new() {
        SucursalId = sucursal, BodegaId = bodega, UbicacionId = ubicacion, MonedaId = moneda, ClienteId = cliente,
        Detalles = [new() { ProductoId = producto, ProductoPresentacionId = presentacion,
            Cantidad = cantidad, PorcentajeDescuento = descuento }] };
    internal async Task<VentaConsultaDto> CrearAsync(CrearVentaSolicitud solicitud)
    {
        var v = await Ventas.CrearAsync(solicitud, Seguridad, default);
        if (!Contexto.Database.IsRelational())
        {
            var entidad = await Contexto.Ventas.SingleAsync(x => x.Id == v.Encabezado.Id);
            entidad.VersionFila = [1];
            await Contexto.SaveChangesAsync();
            Contexto.ChangeTracker.Clear();
            v = await Ventas.ObtenerAsync(v.Encabezado.Id, Seguridad, default);
        }
        return v;
    }
    internal async Task<VentaConsultaDto> ConfirmarAsync(CrearVentaSolicitud? solicitud = null)
    {
        var v = await CrearAsync(solicitud ?? Solicitud());
        return await Ventas.ConfirmarAsync(v.Encabezado.Id, new() { VersionFila = v.Encabezado.VersionFila }, Seguridad, default);
    }
    internal async Task<CobroAlCompletarSolicitud?> CobroAsync(VentaConsultaDto v)
    {
        if (v.Encabezado.EsCredito || v.Encabezado.Total == 0) return null;
        var turno = await Contexto.TurnosCaja.AsNoTracking().Where(x => x.Caja.EmpresaId == Seguridad.EmpresaId
            && x.Caja.SucursalId == Seguridad.SucursalId && x.FechaCierre == null).FirstAsync();
        return new() { SesionCajaId = turno.Id, VersionCaja = Convert.ToBase64String(turno.VersionFila),
            Pagos = [new FormaCobroSolicitud { FormaPagoId = await Contexto.FormasPago.Where(x => x.EmpresaId == 1 && x.Codigo == "EFECTIVO").Select(x => x.Id).SingleAsync(),
                MontoAplicado = v.Encabezado.Total, MontoRecibido = v.Encabezado.Total }] };
    }
    internal async Task<VentaConsultaDto> CompletarAsync(VentaConsultaDto v, Guid? clave = null) =>
        await Ventas.CompletarAsync(v.Encabezado.Id, new() { VersionFila = v.Encabezado.VersionFila,
            ClaveOperacion = clave ?? Guid.NewGuid(), Cobro = await CobroAsync(v) }, Seguridad, default);

    // Fixture explícita de datos ANTERIORES al Ajuste B. No llama a completar sin pago ni elimina pagos.
    // Conserva pruebas de reversión de inventario para históricos no cobrados, todavía soportados.
    internal async Task<VentaConsultaDto> HistoricaSinCobroAsync(VentaConsultaDto v, bool inventario = true)
    {
        foreach (var l in v.Detalles)
        {
            decimal costo = 0;
            if (inventario && l.ManejaExistencia == true)
            {
                var salida = await Inventario.AjustarAsync(new() { SucursalId = v.Encabezado.SucursalId,
                    BodegaId = v.Encabezado.BodegaId!.Value, UbicacionId = v.Encabezado.UbicacionId!.Value,
                    ProductoId = l.ProductoId, ProductoPresentacionId = l.ProductoPresentacionId, Cantidad = l.Cantidad, Tipo = "SALIDA" }, Seguridad, default);
                costo = salida.CostoUnitario;
                var movimiento = await Contexto.Movimientos.SingleAsync(x => x.Id == salida.MovimientoId);
                movimiento.ModuloOrigen = "VENTAS"; movimiento.DocumentoOrigenId = v.Encabezado.Id;
                movimiento.ConceptoMovimientoId = await Contexto.ConceptosMovimientos.Where(x => x.Codigo == "VENTA-SALIDA").Select(x => x.Id).SingleAsync();
                await Contexto.SaveChangesAsync(); Contexto.ChangeTracker.Clear();
            }
            var detalle = await Contexto.VentasDetalle.SingleAsync(x => x.Id == l.Id);
            detalle.CostoUnitario = costo; detalle.CostoTotal = costo * l.CantidadBase;
            await Contexto.SaveChangesAsync(); Contexto.ChangeTracker.Clear();
        }
        var venta = await Contexto.Ventas.SingleAsync(x => x.Id == v.Encabezado.Id);
        venta.EstadoId = await Contexto.EstadosFlujo.Where(x => x.FlujoEstado.Codigo == "VENTA_POS" && x.Codigo == "COMPLETADA").Select(x => x.Id).SingleAsync();
        venta.FechaCompletada = DateTime.UtcNow; venta.CompletadoPorUsuarioId = Seguridad.UsuarioId; venta.ClaveOperacion = Guid.NewGuid();
        await Contexto.SaveChangesAsync(); Contexto.ChangeTracker.Clear();
        return await Ventas.ObtenerAsync(venta.Id, Seguridad, default);
    }
    internal Task<VentaConsultaDto> AnularAsync(VentaConsultaDto v) => Ventas.AnularAsync(v.Encabezado.Id,
        new() { VersionFila = v.Encabezado.VersionFila, Motivo = "Error de operación" }, Seguridad, default);
    public ValueTask DisposeAsync() => _fase5.DisposeAsync();
}
