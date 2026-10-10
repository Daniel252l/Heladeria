using Heladeria.Aplicacion.Cartera;
using Heladeria.Aplicacion.Caja;
using Heladeria.Aplicacion.Configuracion;
using Heladeria.Aplicacion.Ventas;
using Heladeria.Infraestructura.Cartera;
using Heladeria.Infraestructura.Compras;
using Heladeria.Infraestructura.Inventario;
using Heladeria.Infraestructura.Persistencia;
using Heladeria.Infraestructura.Persistencia.Entidades;
using Heladeria.Infraestructura.Seguridad.Bootstrap;
using Heladeria.Pruebas.Fase5;
using Heladeria.Pruebas.Fase7;
using Heladeria.Pruebas.Fase8;
using Microsoft.EntityFrameworkCore;

namespace Heladeria.Pruebas.Fase9;

internal sealed class EscenarioCartera(EscenarioCaja caja, ServicioCartera servicio, ServicioCompras compras,
    PreparadorFase9 preparador) : IAsyncDisposable
{
    internal EscenarioCaja Caja { get; } = caja;
    internal ServicioCartera Servicio { get; } = servicio;
    internal ServicioCompras Compras { get; } = compras;
    internal PreparadorFase9 Preparador { get; } = preparador;
    internal HeladeriaDbContext Db => Caja.Contexto;
    internal static ContextoConfiguracion S => EscenarioCaja.Seguridad;
    internal static async Task<EscenarioCartera> CrearAsync()
    {
        var caja = await EscenarioCaja.CrearAsync();
        var db = caja.Contexto;
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TRIGGER VersionCxc AFTER UPDATE ON CuentasPorCobrar WHEN NEW.VersionFila=OLD.VersionFila
            BEGIN UPDATE CuentasPorCobrar SET VersionFila=randomblob(8) WHERE Id=NEW.Id; END;
            CREATE TRIGGER VersionCxp AFTER UPDATE ON CuentasPorPagar WHEN NEW.VersionFila=OLD.VersionFila
            BEGIN UPDATE CuentasPorPagar SET VersionFila=randomblob(8) WHERE Id=NEW.Id; END;
            CREATE TRIGGER VersionOrden AFTER UPDATE ON OrdenesCompra WHEN NEW.VersionFila=OLD.VersionFila
            BEGIN UPDATE OrdenesCompra SET VersionFila=randomblob(8) WHERE Id=NEW.Id; END;
            """);
        var coordinador = new CoordinadorAjustesInventario();
        await new PreparadorFase5(db, EscenarioFase5.Opciones, TimeProvider.System, coordinador).EjecutarAsync(default);
        var prep = new PreparadorFase9(db, EscenarioFase5.Opciones, TimeProvider.System, coordinador);
        await prep.EjecutarAsync(default);
        db.Clientes.Add(new Clientes { EmpresaId = S.EmpresaId, CodigoCliente = "CLIENTE-PRUEBA", NombreCliente = "Cliente nominativo", EstadoId = 1 });
        await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        return new(caja, new ServicioCartera(db, EscenarioFase5.Opciones, TimeProvider.System, coordinador, caja.Servicio),
            new ServicioCompras(db, EscenarioFase5.Opciones, TimeProvider.System, coordinador, caja.Ventas.Inventario), prep);
    }
    internal async Task<VentaConsultaDto> VentaCreditoAsync(bool cliente = true, bool completar = true)
    {
        var solicitud = EscenarioFase7.Solicitud();
        var v = await Caja.Ventas.ConfirmarAsync(new CrearVentaSolicitud { EsCredito = true,
            FechaVencimientoCredito = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(10), ClienteId = cliente ? await Db.Clientes.Select(x => x.Id).SingleAsync() : null,
            SucursalId = solicitud.SucursalId, BodegaId = solicitud.BodegaId, UbicacionId = solicitud.UbicacionId,
            MonedaId = solicitud.MonedaId, Detalles = solicitud.Detalles });
        return completar ? await Caja.Ventas.CompletarAsync(v) : v;
    }
    internal async Task<RegistrarFacturaCreditoSolicitud> CompraCreditoAsync(bool credito = true)
    {
        var compra = await Compras.GuardarAsync(null, new() { EsCredito = credito, SucursalId = S.SucursalId,
            ProveedorId = 1, MonedaId = 1, FechaEmision = new(2026, 9, 1),
            Detalles = [new() { ProductoId = 1, ProductoPresentacionId = 1, Cantidad = 2, PrecioUnitario = 25 }] }, S, default);
        await Compras.CambiarEstadoAsync(compra.Encabezado.Id, "CONFIRMADA", S, default);
        await Compras.RecibirAsync(compra.Encabezado.Id, EscenarioFase5.Recepcion(compra.Detalles.Single().Id, 2), S, default);
        compra = await Compras.ObtenerAsync(compra.Encabezado.Id, S, default);
        return new() { OrdenCompraId = compra.Encabezado.Id, VersionCompra = compra.Encabezado.VersionFila,
            ClaveOperacion = Guid.NewGuid(), NumeroDocumento = "FACT-REAL-PRUEBA", Serie = "A",
            FechaDocumento = new(2026, 9, 1), FechaVencimiento = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(10) };
    }
    internal async Task<DetalleCarteraDto> CuentaAsync(bool pagar)
    {
        if (pagar) return (await Servicio.RegistrarFacturaAsync(await CompraCreditoAsync(), S, default)).Cuenta;
        var venta = await VentaCreditoAsync();
        return await Servicio.ObtenerAsync(false, await Db.CuentasPorCobrar.Where(x => x.VentaId == venta.Encabezado.Id).Select(x => x.Id).SingleAsync(), S, default);
    }
    internal async Task<AplicarPagoCarteraSolicitud> SolicitudAsync(DetalleCarteraDto cuenta, SesionCajaDto caja,
        decimal monto = 50, Guid? clave = null, string forma = "EFECTIVO") => new() {
            ClaveOperacion = clave ?? Guid.NewGuid(), VersionFila = cuenta.Encabezado.VersionFila,
            SesionCajaId = caja.Encabezado.Id, VersionCaja = caja.Encabezado.VersionFila, Monto = monto,
            FormaPagoId = await Db.FormasPago.Where(x => x.Codigo == forma).Select(x => x.Id).SingleAsync(), Referencia = "AUT-PRUEBA" };
    public ValueTask DisposeAsync() => Caja.DisposeAsync();
}
