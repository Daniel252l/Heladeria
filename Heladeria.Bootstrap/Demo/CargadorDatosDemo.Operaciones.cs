using Heladeria.Aplicacion.Caja;
using Heladeria.Aplicacion.Compras;
using Heladeria.Aplicacion.Ventas;
using Microsoft.EntityFrameworkCore;

namespace Heladeria.Bootstrap.Demo;

public sealed partial class CargadorDatosDemo
{
    private async Task CrearOperacionesAsync(CancellationToken t)
    {
        var demoCaja = await caja.CrearCajaAsync(new() { Codigo = CodigoCaja, Nombre = "Caja de demostración" }, s, t);
        var formas = (await caja.ReferenciasAsync(s, t, demoCaja.Id)).FormasPago;
        var efectivo = formas.Single(x => x.Codigo == "EFECTIVO").Id;
        var tarjeta = formas.Single(x => x.Codigo == "TARJETA").Id;
        decimal apertura = 20000; // Fondo ficticio explícito, exclusivamente en la caja DEMO.
        var numeroVenta = 0;
        for (var dia = -13; dia <= 0; dia++)
        {
            var fecha = hoy.AddDays(dia);
            PonerFecha(fecha, 8);
            var sesion = await caja.AbrirAsync(new() { CajaId = demoCaja.Id, MonedaId = moneda, SaldoInicial = apertura }, s, t);
            var turno = sesion.Encabezado.Id; turnoIds.Add(turno);
            if (dia == -13)
            {
                // Todas las entradas iniciales provienen de recepciones; no se insertan saldos/Kardex a mano.
                var grupos = new[] { Articulos[..3],Articulos[3..6],Articulos[6..10],Articulos[10..14],
                    Articulos[14..18],Articulos[18..22],Articulos[22..26],Articulos[26..30] };
                for (var i = 0; i < grupos.Length; i++)
                {
                    PonerFecha(fecha, 9);
                    var cuenta = await ComprarAsync(grupos[i], i, i < 6, false, t);
                    if (cuenta.HasValue && i <= 3)
                    {
                        var c = await cartera.ObtenerAsync(true, cuenta.Value, s, t);
                        await AplicarCuentaAsync(true, cuenta.Value, turno, efectivo, i == 3 ? decimal.Round(c.Encabezado.ImporteOriginal / 2, 2) : c.Encabezado.ImporteOriginal, t);
                    }
                }
                progreso?.Invoke("COMPRAS_INICIALES");
            }
            if (dia == -6)
                await ComprarAsync([Articulos[0] with { Stock = 2000 }, Articulos[10] with { Stock = 12 }], 8, false, false, t);
            if (dia == -2)
                await ComprarAsync([Articulos[14] with { Stock = 40 }, Articulos[18] with { Stock = 100 }], 9, false, true, t);
            if (dia is -8 or -3)
            {
                sesion = await caja.ObtenerAsync(turno, s, t);
                await caja.MovimientoAsync(turno, new() { VersionFila = sesion.Encabezado.VersionFila,
                    Tipo = dia == -8 ? "INGRESO" : "EGRESO", Monto = dia == -8 ? 150 : 25,
                    Motivo = dia == -8 ? "DEMO: reposición ficticia de fondo" : "DEMO: retiro ficticio de fondo" }, s, t);
            }
            var cantidad = dia == 0 ? 10 : 2;
            for (var n = 0; n < cantidad; n++)
            {
                PonerFecha(fecha, 9 + n);
                await VenderAsync(numeroVenta++, turno, efectivo, tarjeta, false, t);
            }
            if (dia is -10 or 0)
                await VenderAsync(0, turno, efectivo, tarjeta, true, t);
            PonerFecha(fecha, 18);
            sesion = await caja.ObtenerAsync(turno, s, t);
            sesion = await caja.ArqueoAsync(turno, new() { VersionFila = sesion.Encabezado.VersionFila,
                EfectivoContado = sesion.EfectivoEsperado!.Value, Observaciones = "Conteo ficticio de demostración, conciliado sin diferencia." }, s, t);
            sesion = await caja.CerrarAsync(turno, new() { VersionFila = sesion.Encabezado.VersionFila,
                ArqueoId = sesion.Arqueos.Single(x => x.Vigente).Id }, s, t);
            apertura = sesion.Encabezado.EfectivoContadoCierre!.Value;
        }
        progreso?.Invoke("OPERACIONES");
    }
    private async Task<long?> ComprarAsync(Articulo[] filas, int indice, bool credito, bool parcial, CancellationToken t)
    {
        var fecha = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(reloj.GetUtcNow(), TimeZoneInfo.FindSystemTimeZoneById(zona)).DateTime);
        var orden = await compras.GuardarAsync(null, new() { SucursalId = s.SucursalId, ProveedorId = proveedorIds[indice % proveedorIds.Count],
            MonedaId = moneda, TipoCambio = 1, EsCredito = credito, FechaEmision = fecha.ToDateTime(TimeOnly.MinValue),
            FechaEntregaEsperada = fecha.ToDateTime(TimeOnly.MinValue), Detalles = filas.Select(a => new CompraLineaSolicitud {
                ProductoId = productoIds[a.Clave], ProductoPresentacionId = a.PresentacionCompra is null ? null : presentacionIds[a.Clave + "/" + a.PresentacionCompra],
                Cantidad = a.Stock / a.FactorCompra, PrecioUnitario = a.Costo * a.FactorCompra }).ToArray() }, s, t);
        compraIds.Add(orden.Encabezado.Id);
        orden = await compras.CambiarEstadoAsync(orden.Encabezado.Id, "CONFIRMADA", s, t);
        await compras.RecibirAsync(orden.Encabezado.Id, new() { ClaveOperacion = Guid.NewGuid(),
            BodegaId = bodega, UbicacionId = ubicacion, Observaciones = "Recepción ficticia HELADERIA-DEMO-V1",
            Detalles = orden.Detalles.Select(x => new RecepcionLineaSolicitud { OrdenCompraDetalleId = x.Id,
                Cantidad = parcial ? x.Cantidad / 2 : x.Cantidad }).ToArray() }, s, t);
        if (!credito) return null;
        orden = await compras.ObtenerAsync(orden.Encabezado.Id, s, t);
        var factura = await cartera.RegistrarFacturaAsync(new() {
            OrdenCompraId = orden.Encabezado.Id, VersionCompra = orden.Encabezado.VersionFila, ClaveOperacion = Guid.NewGuid(),
            Serie = "DEMO", NumeroDocumento = $"DEMO-FACT-{indice+1:000}", FechaDocumento = fecha,
            FechaVencimiento = fecha.AddDays(indice is 3 or 4 ? 7 : 30) }, s, t);
        cxpIds.Add(factura.Cuenta.Encabezado.Id);
        return factura.Cuenta.Encabezado.Id;
    }
    private async Task VenderAsync(int indice, long turno, int efectivo, int tarjeta, bool anular, CancellationToken t)
    {
        var selecciones = new[] { "ESP-VAC","HEL-CHO","BEB-COL","ESP-BAN","HEL-VAI","ESP-SUN","BEB-SPR",
            "ESP-MIL","HEL-FRE","ESP-BRO","BEB-AGU","HEL-COO","HEL-PIS","BEB-TE","HEL-MAN","HEL-LIM","HEL-CAR","HEL-CHI","HEL-CAF" };
        var clave = anular ? "ESP-VAC" : selecciones[indice % selecciones.Length];
        var credito = !anular && new[] { 2,8,14,20,28,34 }.Contains(indice);
        var fecha = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(reloj.GetUtcNow(), TimeZoneInfo.FindSystemTimeZoneById(zona)).DateTime);
        var lineas = new List<LineaVentaSolicitud> { new() { ProductoId = productoIds[clave],
            ProductoPresentacionId = clave.StartsWith("HEL-", StringComparison.Ordinal) ? presentacionIds[clave + "/V180"] : null,
            Cantidad = anular ? 1 : indice % 4 == 0 ? 2 : 1, PorcentajeDescuento = anular ? 100 : 0 } };
        if (!anular && indice % 5 == 0 && clave != "BEB-AGU") lineas.Add(new() { ProductoId = productoIds["BEB-AGU"], Cantidad = 1 });
        var v = await ventas.CrearAsync(new() { SucursalId = s.SucursalId, BodegaId = bodega, UbicacionId = ubicacion,
            MonedaId = moneda, ClienteId = credito ? clienteIds[6 + cxcIds.Count % 6] : indice % 2 == 0 ? clienteIds[indice % 6] : null,
            EsCredito = credito, FechaVencimientoCredito = credito ? fecha.AddDays(indice == 20 ? 3 : 15) : null,
            Detalles = lineas.ToArray() }, s, t);
        ventaIds.Add(v.Encabezado.Id);
        v = await ventas.ConfirmarAsync(v.Encabezado.Id, new() { VersionFila = v.Encabezado.VersionFila }, s, t);
        CobroAlCompletarSolicitud? cobro = null;
        if (!credito && !anular)
        {
            var sesion = await caja.ObtenerAsync(turno, s, t);
            var total = v.Encabezado.Total;
            FormaCobroSolicitud Pago(int forma, decimal monto) => new() { FormaPagoId = forma, MontoAplicado = monto,
                MontoRecibido = monto, Referencia = forma == tarjeta ? $"DEMO-TAR-{indice:000}" : null };
            var pagos = (indice % 10) switch {
                5 or 6 or 7 => new[] { Pago(tarjeta, total) },
                9 => new[] { Pago(efectivo, decimal.Round(total / 2, 2)), Pago(tarjeta, total - decimal.Round(total / 2, 2)) },
                _ => new[] { Pago(efectivo, total) } };
            cobro = new() { SesionCajaId = turno, VersionCaja = sesion.Encabezado.VersionFila, Pagos = pagos };
        }
        v = await ventas.CompletarAsync(v.Encabezado.Id, new() {
            VersionFila = v.Encabezado.VersionFila, ClaveOperacion = Guid.NewGuid(), Cobro = cobro }, s, t);
        if (credito)
        {
            var cuenta = await db.CuentasPorCobrar.AsNoTracking().SingleAsync(x => x.VentaId == v.Encabezado.Id, t);
            cxcIds.Add(cuenta.Id);
            if (cxcIds.Count <= 3)
                await AplicarCuentaAsync(false, cuenta.Id, turno, efectivo,
                    cxcIds.Count == 3 ? decimal.Round(cuenta.MontoOriginal / 2, 2) : cuenta.MontoOriginal, t);
        }
        if (anular)
            await ventas.AnularAsync(v.Encabezado.Id, new() { VersionFila = v.Encabezado.VersionFila,
                Motivo = "DEMO: cortesía sin cobro anulada; reversión de consumos originales." }, s, t);
    }
    private async Task AplicarCuentaAsync(bool pagar, long cuentaId, long turno, int forma, decimal monto, CancellationToken t)
    {
        var cuenta = await cartera.ObtenerAsync(pagar, cuentaId, s, t);
        var sesion = await caja.ObtenerAsync(turno, s, t);
        await cartera.AplicarAsync(pagar, cuentaId, new() { ClaveOperacion = Guid.NewGuid(), VersionFila = cuenta.Encabezado.VersionFila,
            SesionCajaId = turno, VersionCaja = sesion.Encabezado.VersionFila, FormaPagoId = forma, Monto = monto,
            Observacion = "Aplicación ficticia HELADERIA-DEMO-V1" }, s, t);
    }
    private async Task ValidarResultadoAsync(CancellationToken t)
    {
        var ids = productoIds.Values.ToArray();
        if (ids.Length != 35 || compraIds.Count != 10 || ventaIds.Count != 38 || turnoIds.Count != 14 || cxcIds.Count != 6 || cxpIds.Count != 6)
            throw new DemoRechazadaException("El conjunto demo no tiene las cantidades esperadas; se revierte.");
        if (await db.Existencias.AnyAsync(x => ids.Contains(x.ProductoId) && (x.Cantidad < 0 || x.CantidadReservada != 0 || x.CostoPromedio <= 0), t)
            || await db.TurnosCaja.AnyAsync(x => turnoIds.Contains(x.Id) && (x.FechaCierre == null || x.Diferencia != 0), t))
            throw new DemoRechazadaException("No concilian existencias o turnos demo; se revierte.");
        foreach (var pagar in new[] { false, true })
        foreach (var id in pagar ? cxpIds : cxcIds)
        {
            var c = await cartera.ObtenerAsync(pagar, id, s, t);
            if (!c.Encabezado.Operable || c.Encabezado.ImporteOriginal != c.Encabezado.SaldoPendiente + c.Historial.Sum(x => x.Monto))
                throw new DemoRechazadaException("Una cuenta demo no concilia; se revierte.");
        }
        foreach (var id in turnoIds) await caja.ObtenerAsync(id, s, t); // Incluye las conciliaciones del servicio.
    }
}
