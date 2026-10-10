using Heladeria.Aplicacion.Caja;
using Heladeria.Aplicacion.Cartera;
using Heladeria.Aplicacion.Seguridad.Bootstrap;
using Microsoft.EntityFrameworkCore;

namespace Heladeria.Pruebas.Fase9;

public sealed class CarteraPruebas
{
    [Fact]
    public async Task EdicionClienteAnteriorNoReclasificaModalidadDeVentaNiCompra()
    {
        await using var e = await EscenarioCartera.CrearAsync();
        var baseVenta = Fase7.EscenarioFase7.Solicitud(cliente: await e.Db.Clientes.Select(x => x.Id).SingleAsync());
        var vence = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(10);
        var v = await e.Caja.Ventas.CrearAsync(new Heladeria.Aplicacion.Ventas.CrearVentaSolicitud {
            SucursalId = baseVenta.SucursalId, BodegaId = baseVenta.BodegaId, UbicacionId = baseVenta.UbicacionId,
            MonedaId = baseVenta.MonedaId, ClienteId = baseVenta.ClienteId, Detalles = baseVenta.Detalles, EsCredito = true, FechaVencimientoCredito = vence });
        v = await e.Caja.Ventas.Ventas.ActualizarAsync(v.Encabezado.Id, new() {
            VersionFila = v.Encabezado.VersionFila, SucursalId = baseVenta.SucursalId, BodegaId = baseVenta.BodegaId,
            UbicacionId = baseVenta.UbicacionId, MonedaId = baseVenta.MonedaId, ClienteId = baseVenta.ClienteId, Detalles = baseVenta.Detalles }, EscenarioCartera.S, default);
        Assert.True(v.Encabezado.EsCredito); Assert.Equal(vence, v.Encabezado.FechaVencimientoCredito);
        var solicitud = Fase5.EscenarioFase5.Orden(cantidad: 2, precio: 25);
        var compra = await e.Compras.GuardarAsync(null, solicitud, EscenarioCartera.S, default);
        var historica = await e.Db.OrdenesCompra.SingleAsync(x => x.Id == compra.Encabezado.Id);
        historica.EsCredito = null; await e.Db.SaveChangesAsync(); e.Db.ChangeTracker.Clear();
        compra = await e.Compras.GuardarAsync(compra.Encabezado.Id, solicitud, EscenarioCartera.S, default);
        Assert.Null(compra.Encabezado.EsCredito);
        historica = await e.Db.OrdenesCompra.SingleAsync(x => x.Id == compra.Encabezado.Id);
        historica.EsCredito = true; await e.Db.SaveChangesAsync(); e.Db.ChangeTracker.Clear();
        compra = await e.Compras.GuardarAsync(compra.Encabezado.Id, solicitud, EscenarioCartera.S, default);
        Assert.True(compra.Encabezado.EsCredito);
    }
    [Fact]
    public async Task FallaGeneracionCreditoRevierteCierreInventarioKardexYCorrelativos()
    {
        await using var e = await EscenarioCartera.CrearAsync(); var venta = await e.VentaCreditoAsync(completar: false);
        var movimientos = await e.Db.Movimientos.CountAsync(); var kardex = await e.Db.Kardex.CountAsync();
        var existencias = await e.Db.Existencias.AsNoTracking().OrderBy(x => x.Id).Select(x => new { x.Id, x.Cantidad, x.CostoPromedio }).ToArrayAsync();
        var numeros = await e.Db.CorrelativosDocumentos.AsNoTracking().OrderBy(x => x.Id).Select(x => x.UltimoNumero).ToArrayAsync();
        await e.Db.Database.ExecuteSqlRawAsync("CREATE TRIGGER FalloCuenta BEFORE INSERT ON CuentasPorCobrar BEGIN SELECT RAISE(ABORT, 'Fallo cuenta'); END;");
        await Assert.ThrowsAsync<DbUpdateException>(() => e.Caja.Ventas.CompletarAsync(venta));
        var final = await e.Caja.Ventas.Ventas.ObtenerAsync(venta.Encabezado.Id, EscenarioCartera.S, default);
        Assert.Equal("CONFIRMADA", final.Encabezado.Estado); Assert.Null(final.Encabezado.ClaveOperacion);
        Assert.Empty(final.Movimientos); Assert.Empty(await e.Db.CuentasPorCobrar.ToArrayAsync());
        Assert.Equal(movimientos, await e.Db.Movimientos.CountAsync()); Assert.Equal(kardex, await e.Db.Kardex.CountAsync());
        Assert.Equal(existencias, await e.Db.Existencias.AsNoTracking().OrderBy(x => x.Id).Select(x => new { x.Id, x.Cantidad, x.CostoPromedio }).ToArrayAsync());
        Assert.Equal(numeros, await e.Db.CorrelativosDocumentos.AsNoTracking().OrderBy(x => x.Id).Select(x => x.UltimoNumero).ToArrayAsync());
    }
    [Theory, InlineData(false), InlineData(true)]
    public async Task CuentaHistoricaSoloConsultaSinInferirMetadata(bool pagar)
    {
        await using var e = await EscenarioCartera.CrearAsync(); var c = await e.CuentaAsync(pagar);
        if (pagar) { var x = await e.Db.CuentasPorPagar.SingleAsync(); x.FechaCreacion = null; x.UsuarioCreacionId = null; x.MonedaId = null; x.SucursalId = null; }
        else { var x = await e.Db.CuentasPorCobrar.SingleAsync(); x.FechaCreacion = null; x.UsuarioCreacionId = null; x.MonedaId = null; x.SucursalId = null; }
        await e.Db.SaveChangesAsync(); e.Db.ChangeTracker.Clear();
        var historica = await e.Servicio.ObtenerAsync(pagar, c.Encabezado.Id, EscenarioCartera.S, default);
        Assert.False(historica.Encabezado.Operable); Assert.Equal(c.Encabezado.SucursalId, historica.Encabezado.SucursalId);
        var solicitud = await e.SolicitudAsync(historica, await e.Caja.AbrirAsync());
        await Assert.ThrowsAsync<ExcepcionCartera>(() => e.Servicio.AplicarAsync(pagar, c.Encabezado.Id, solicitud, EscenarioCartera.S, default));
        Assert.Empty(await e.Db.Pagos.ToArrayAsync());
    }
    [Theory, InlineData(false), InlineData(true)]
    public async Task SaldoManipuladoRequiereConciliacionNoSeSobrescribe(bool pagar)
    {
        await using var e = await EscenarioCartera.CrearAsync(); var c = await e.CuentaAsync(pagar);
        if (pagar) (await e.Db.CuentasPorPagar.SingleAsync()).SaldoPendiente = 1;
        else (await e.Db.CuentasPorCobrar.SingleAsync()).SaldoPendiente = 1;
        await e.Db.SaveChangesAsync(); e.Db.ChangeTracker.Clear();
        await Assert.ThrowsAsync<ExcepcionCartera>(() => e.Servicio.ObtenerAsync(pagar, c.Encabezado.Id, EscenarioCartera.S, default));
        Assert.Empty(await e.Db.Pagos.ToArrayAsync());
    }
    [Theory, InlineData(false), InlineData(true)]
    public async Task VencimientoUsaFechaCivilDeSucursalNoDiaUtc(bool pagar)
    {
        await using var e = await EscenarioCartera.CrearAsync(); var c = await e.CuentaAsync(pagar);
        if (pagar) { var x = await e.Db.CuentasPorPagar.SingleAsync(); x.FechaEmision = new(2026, 10, 8); x.FechaVencimiento = new(2026, 10, 8); }
        else { var x = await e.Db.CuentasPorCobrar.SingleAsync(); x.FechaEmision = new(2026, 10, 8); x.FechaVencimiento = new(2026, 10, 8); }
        await e.Db.SaveChangesAsync(); e.Db.ChangeTracker.Clear();
        var reloj = new RelojFijo(new DateTimeOffset(2026, 10, 9, 3, 0, 0, TimeSpan.Zero));
        var servicio = new Heladeria.Infraestructura.Cartera.ServicioCartera(e.Db, Fase5.EscenarioFase5.Opciones,
            reloj, new Heladeria.Infraestructura.Inventario.CoordinadorAjustesInventario(), e.Caja.Servicio);
        Assert.Equal("VIGENTE", (await servicio.ObtenerAsync(pagar, c.Encabezado.Id, EscenarioCartera.S, default)).Encabezado.Vencimiento);
        reloj.Instante = reloj.Instante.AddHours(4);
        Assert.Equal("VENCIDA", (await servicio.ObtenerAsync(pagar, c.Encabezado.Id, EscenarioCartera.S, default)).Encabezado.Vencimiento);
    }
    private sealed class RelojFijo(DateTimeOffset instante) : TimeProvider
    {
        internal DateTimeOffset Instante { get; set; } = instante;
        public override DateTimeOffset GetUtcNow() => Instante;
    }
    [Theory, InlineData(false), InlineData(true)]
    public async Task GeneraDesdeOrigenSaldoNominativoAuditoriaUtc(bool pagar)
    {
        await using var e = await EscenarioCartera.CrearAsync(); var c = await e.CuentaAsync(pagar);
        Assert.Equal(50, c.Encabezado.ImporteOriginal); Assert.Equal(50, c.Encabezado.SaldoPendiente);
        Assert.Equal(0, c.Encabezado.TotalAplicado); Assert.Equal("PENDIENTE", c.Encabezado.Estado);
        Assert.Equal("VIGENTE", c.Encabezado.Vencimiento); Assert.True(c.Encabezado.Operable);
        Assert.Equal(DateTimeKind.Utc, c.Encabezado.FechaCreacion!.Value.Kind); Assert.Empty(c.Historial);
        Assert.Single(await e.Db.Bitacoras.Where(x => x.ModuloId == (pagar ? "CXP" : "CXC") && x.Accion == "CUENTA_GENERADA").ToArrayAsync());
    }
    [Theory, InlineData(false), InlineData(true)]
    public async Task DosAplicacionesParcialesLiquidacionCajaArqueoYCierre(bool pagar)
    {
        await using var e = await EscenarioCartera.CrearAsync(); var cuenta = await e.CuentaAsync(pagar);
        var sesion = await e.Caja.ArqueoAsync(await e.Caja.AbrirAsync());
        var primero = await e.Servicio.AplicarAsync(pagar, cuenta.Encabezado.Id, await e.SolicitudAsync(cuenta, sesion, 20), EscenarioCartera.S, default);
        Assert.Equal("PARCIAL", primero.Cuenta.Encabezado.Estado); Assert.Equal(30, primero.Cuenta.Encabezado.SaldoPendiente);
        Assert.Equal(20, primero.Cuenta.Encabezado.TotalAplicado); Assert.NotEqual(cuenta.Encabezado.VersionFila, primero.Cuenta.Encabezado.VersionFila);
        sesion = await e.Caja.Servicio.ObtenerAsync(sesion.Encabezado.Id, EscenarioCartera.S, default);
        Assert.False(sesion.Arqueos.Single().Vigente); Assert.Equal(pagar ? 80 : 120, sesion.EfectivoEsperado);
        var final = await e.Servicio.AplicarAsync(pagar, cuenta.Encabezado.Id, await e.SolicitudAsync(primero.Cuenta, sesion, 30), EscenarioCartera.S, default);
        Assert.Equal("PAGADA", final.Cuenta.Encabezado.Estado); Assert.Equal("PAGADA", final.Cuenta.Encabezado.Vencimiento);
        Assert.Equal(0, final.Cuenta.Encabezado.SaldoPendiente); Assert.Equal(50, final.Cuenta.Encabezado.TotalAplicado);
        Assert.Equal(2, final.Cuenta.Historial.Count); Assert.All(final.Cuenta.Historial, h => {
            Assert.Equal(DateTimeKind.Utc, h.Fecha.Kind); Assert.Single(h.MovimientoIds); Assert.NotEmpty(h.Usuario); });
        sesion = await e.Caja.Servicio.ObtenerAsync(sesion.Encabezado.Id, EscenarioCartera.S, default);
        Assert.Equal(pagar ? 50 : 150, sesion.EfectivoEsperado);
        Assert.Contains(sesion.TotalesPorTipo, x => x.Tipo == (pagar ? "PAGO_CUENTA_POR_PAGAR" : "COBRO_CUENTA_POR_COBRAR") && x.Total == 50);
        var cierre = await e.Caja.CerrarAsync(await e.Caja.ArqueoAsync(sesion));
        Assert.Equal(pagar ? 50 : 0, cierre.Encabezado.TotalEgresosCierre); Assert.Equal(pagar ? 0 : 50, cierre.Encabezado.TotalIngresosCierre);
    }
    [Theory, InlineData(false), InlineData(true)]
    public async Task IdempotenciaMismaClaveInclusoTrasCerrarCajaYRechazoClaveDistinta(bool pagar)
    {
        await using var e = await EscenarioCartera.CrearAsync(); var c = await e.CuentaAsync(pagar); var sesion = await e.Caja.AbrirAsync();
        var solicitud = await e.SolicitudAsync(c, sesion);
        var uno = await e.Servicio.AplicarAsync(pagar, c.Encabezado.Id, solicitud, EscenarioCartera.S, default);
        sesion = await e.Caja.Servicio.ObtenerAsync(sesion.Encabezado.Id, EscenarioCartera.S, default);
        await e.Caja.CerrarAsync(await e.Caja.ArqueoAsync(sesion));
        var dos = await e.Servicio.AplicarAsync(pagar, c.Encabezado.Id, solicitud, EscenarioCartera.S, default);
        Assert.True(dos.Reintento); Assert.Equal(uno.PagoId, dos.PagoId); Assert.Single(await e.Db.Pagos.ToArrayAsync());
        var distinta = await e.SolicitudAsync(uno.Cuenta, sesion);
        Assert.Equal("CUENTA_YA_PAGADA", (await Assert.ThrowsAsync<ExcepcionCartera>(() => e.Servicio.AplicarAsync(pagar, c.Encabezado.Id, distinta, EscenarioCartera.S, default))).Codigo);
        var conflictiva = await e.SolicitudAsync(c, sesion, 49, solicitud.ClaveOperacion);
        Assert.Equal("CLAVE_OPERACION_CONFLICTIVA", (await Assert.ThrowsAsync<ExcepcionCartera>(() => e.Servicio.AplicarAsync(pagar, c.Encabezado.Id, conflictiva, EscenarioCartera.S, default))).Codigo);
    }
    [Theory, InlineData(false), InlineData(true)]
    public async Task ExcesoNoDejaPagoMovimientoSaldoNiCorrelativoParcial(bool pagar)
    {
        await using var e = await EscenarioCartera.CrearAsync(); var c = await e.CuentaAsync(pagar); var sesion = await e.Caja.AbrirAsync();
        var contador = await e.Db.CorrelativosDocumentos.AsNoTracking().Where(x => x.SerieDocumento.Serie == "MC").Select(x => x.UltimoNumero).SingleAsync();
        var solicitud = await e.SolicitudAsync(c, sesion, 51);
        Assert.Equal(pagar ? "PAGO_SUPERA_SALDO" : "ABONO_SUPERA_SALDO", (await Assert.ThrowsAsync<ExcepcionCartera>(() => e.Servicio.AplicarAsync(pagar, c.Encabezado.Id, solicitud, EscenarioCartera.S, default))).Codigo);
        Assert.Empty(await e.Db.Pagos.ToArrayAsync()); Assert.Single(await e.Db.MovimientosCaja.ToArrayAsync());
        Assert.Equal(contador, await e.Db.CorrelativosDocumentos.AsNoTracking().Where(x => x.SerieDocumento.Serie == "MC").Select(x => x.UltimoNumero).SingleAsync());
        Assert.Equal(c.Encabezado, (await e.Servicio.ObtenerAsync(pagar, c.Encabezado.Id, EscenarioCartera.S, default)).Encabezado);
    }
    [Theory, InlineData(false), InlineData(true)]
    public async Task FalloDespuesDeInsertarPagoRevierteTodo(bool pagar)
    {
        await using var e = await EscenarioCartera.CrearAsync(); var c = await e.CuentaAsync(pagar); var sesion = await e.Caja.AbrirAsync();
        await e.Db.Database.ExecuteSqlRawAsync("CREATE TRIGGER FalloFinanciero BEFORE INSERT ON MovimientosCaja WHEN NEW.PagoDetalleId IS NOT NULL BEGIN SELECT RAISE(ABORT, 'Fallo inyectado'); END;");
        var solicitud = await e.SolicitudAsync(c, sesion, 20);
        await Assert.ThrowsAsync<DbUpdateException>(() => e.Servicio.AplicarAsync(pagar, c.Encabezado.Id, solicitud, EscenarioCartera.S, default));
        Assert.Empty(await e.Db.Pagos.ToArrayAsync()); Assert.Empty(await e.Db.PagoDetalles.ToArrayAsync());
        Assert.Empty(await e.Db.AplicacionesCuentasPorCobrar.ToArrayAsync()); Assert.Empty(await e.Db.AplicacionesCuentasPorPagar.ToArrayAsync());
        Assert.Equal(c.Encabezado, (await e.Servicio.ObtenerAsync(pagar, c.Encabezado.Id, EscenarioCartera.S, default)).Encabezado);
        Assert.Equal(sesion.Encabezado.VersionFila, (await e.Caja.Servicio.ObtenerAsync(sesion.Encabezado.Id, EscenarioCartera.S, default)).Encabezado.VersionFila);
    }
    [Theory, InlineData(false), InlineData(true)]
    public async Task CajaCerradaYAlcanceAjenoRechazados(bool pagar)
    {
        await using var e = await EscenarioCartera.CrearAsync(); var c = await e.CuentaAsync(pagar); var sesion = await e.Caja.AbrirAsync();
        var solicitud = await e.SolicitudAsync(c, sesion);
        await e.Caja.CerrarAsync(await e.Caja.ArqueoAsync(sesion));
        Assert.Equal("CAJA_NO_ABIERTA", (await Assert.ThrowsAsync<ExcepcionCaja>(() => e.Servicio.AplicarAsync(pagar, c.Encabezado.Id, solicitud, EscenarioCartera.S, default))).Codigo);
        await Assert.ThrowsAnyAsync<ExcepcionCaja>(() => e.Servicio.ObtenerAsync(pagar, c.Encabezado.Id, EscenarioCartera.S with { EmpresaId = 2 }, default));
        Assert.Empty(await e.Db.Pagos.ToArrayAsync());
    }
    [Theory, InlineData(false), InlineData(true)]
    public async Task VersionObsoletaNoPermiteAbono(bool pagar)
    {
        await using var e = await EscenarioCartera.CrearAsync(); var c = await e.CuentaAsync(pagar); var sesion = await e.Caja.AbrirAsync();
        await e.Servicio.AplicarAsync(pagar, c.Encabezado.Id, await e.SolicitudAsync(c, sesion, 10), EscenarioCartera.S, default);
        sesion = await e.Caja.Servicio.ObtenerAsync(sesion.Encabezado.Id, EscenarioCartera.S, default);
        var solicitud = await e.SolicitudAsync(c, sesion, 10);
        Assert.Equal(pagar ? "CONCURRENCIA_CXP" : "CONCURRENCIA_CXC", (await Assert.ThrowsAsync<ExcepcionCartera>(() => e.Servicio.AplicarAsync(pagar, c.Encabezado.Id, solicitud, EscenarioCartera.S, default))).Codigo);
        Assert.Single(await e.Db.Pagos.ToArrayAsync());
    }
    [Fact]
    public async Task ContadoNoGeneraCuentasNiObligaciones()
    {
        await using var e = await EscenarioCartera.CrearAsync(); await e.Caja.VentaAsync();
        var solicitud = await e.CompraCreditoAsync(false);
        await Assert.ThrowsAsync<ExcepcionCartera>(() => e.Servicio.RegistrarFacturaAsync(solicitud, EscenarioCartera.S, default));
        Assert.Empty(await e.Db.CuentasPorCobrar.ToArrayAsync()); Assert.Empty(await e.Db.CuentasPorPagar.ToArrayAsync());
    }
    [Fact]
    public async Task CreditoAnonimoRechazadoSinCrearVenta()
    {
        await using var e = await EscenarioCartera.CrearAsync();
        Assert.Equal("CLIENTE_REQUERIDO_PARA_CREDITO", (await Assert.ThrowsAsync<ExcepcionCartera>(() => e.VentaCreditoAsync(false))).Codigo);
        Assert.Empty(await e.Db.Ventas.ToArrayAsync());
    }
    [Fact]
    public async Task FacturaPosteriorRepetibleYNoDuplicable()
    {
        await using var e = await EscenarioCartera.CrearAsync(); var solicitud = await e.CompraCreditoAsync();
        Assert.Empty(await e.Db.CuentasPorPagar.ToArrayAsync());
        var uno = await e.Servicio.RegistrarFacturaAsync(solicitud, EscenarioCartera.S, default);
        var dos = await e.Servicio.RegistrarFacturaAsync(solicitud, EscenarioCartera.S, default);
        Assert.True(dos.Reintento); Assert.Equal(uno.FacturaId, dos.FacturaId); Assert.Single(await e.Db.FacturasProveedores.ToArrayAsync());
        var nueva = new RegistrarFacturaCreditoSolicitud { ClaveOperacion = Guid.NewGuid(), OrdenCompraId = solicitud.OrdenCompraId,
            VersionCompra = solicitud.VersionCompra, NumeroDocumento = "OTRA", FechaDocumento = solicitud.FechaDocumento, FechaVencimiento = solicitud.FechaVencimiento };
        Assert.Equal("DOCUMENTO_FINANCIERO_DUPLICADO", (await Assert.ThrowsAsync<ExcepcionCartera>(() => e.Servicio.RegistrarFacturaAsync(nueva, EscenarioCartera.S, default))).Codigo);
    }
    [Fact]
    public async Task CxpNoAdmiteTarjetaNiEfectivoInsuficiente()
    {
        await using var e = await EscenarioCartera.CrearAsync(); var c = await e.CuentaAsync(true); var sesion = await e.Caja.AbrirAsync(10);
        var grande = await e.SolicitudAsync(c, sesion, 50);
        Assert.Equal("SALDO_INSUFICIENTE", (await Assert.ThrowsAsync<ExcepcionCartera>(() => e.Servicio.AplicarAsync(true, c.Encabezado.Id, grande, EscenarioCartera.S, default))).Codigo);
        var tarjeta = await e.SolicitudAsync(c, sesion, 5, forma: "TARJETA");
        Assert.Equal("FORMA_PAGO_INVALIDA", (await Assert.ThrowsAsync<ExcepcionCartera>(() => e.Servicio.AplicarAsync(true, c.Encabezado.Id, tarjeta, EscenarioCartera.S, default))).Codigo);
    }
    [Fact]
    public async Task CobroTarjetaAumentaTotalNoEfectivo()
    {
        await using var e = await EscenarioCartera.CrearAsync(); var c = await e.CuentaAsync(false); var sesion = await e.Caja.AbrirAsync(0);
        await e.Servicio.AplicarAsync(false, c.Encabezado.Id, await e.SolicitudAsync(c, sesion, forma: "TARJETA"), EscenarioCartera.S, default);
        sesion = await e.Caja.Servicio.ObtenerAsync(sesion.Encabezado.Id, EscenarioCartera.S, default);
        Assert.Equal(0, sesion.EfectivoEsperado); Assert.Equal(50, sesion.SaldoTeorico);
    }
    [Theory, InlineData(false), InlineData(true)]
    public async Task AnulacionConAplicacionesBloqueada(bool pagar)
    {
        await using var e = await EscenarioCartera.CrearAsync(); var c = await e.CuentaAsync(pagar); var sesion = await e.Caja.AbrirAsync();
        await e.Servicio.AplicarAsync(pagar, c.Encabezado.Id, await e.SolicitudAsync(c, sesion, 10), EscenarioCartera.S, default);
        var error = await Assert.ThrowsAsync<ExcepcionCartera>(async () => {
            if (pagar) await e.Compras.CambiarEstadoAsync(c.Encabezado.CompraId!.Value, "ANULADA", EscenarioCartera.S, default);
            else await e.Caja.Ventas.AnularAsync(await e.Caja.Ventas.Ventas.ObtenerAsync(c.Encabezado.DocumentoOrigenId, EscenarioCartera.S, default));
        });
        Assert.Equal("OPERACION_FINANCIERA_EXISTENTE", error.Codigo);
    }
    [Fact]
    public async Task PreparacionIdempotenteSoloAdminSinOperaciones()
    {
        await using var e = await EscenarioCartera.CrearAsync();
        Assert.Empty((await e.Preparador.EjecutarAsync(default)).Registros);
        Assert.Equal(6, await e.Db.Permisos.CountAsync(x => x.ModuloId == "CXC" || x.ModuloId == "CXP"));
        Assert.False(await e.Db.RolesPermisos.AnyAsync(x => x.RoleId != "ADMIN" && (x.Permiso.ModuloId == "CXC" || x.Permiso.ModuloId == "CXP")));
        Assert.Empty(await e.Db.Pagos.ToArrayAsync()); Assert.Empty(await e.Db.CuentasPorCobrar.ToArrayAsync()); Assert.Empty(await e.Db.CuentasPorPagar.ToArrayAsync());
        var flujo = await e.Db.EstadosFlujo.SingleAsync(x => x.FlujoEstado.Codigo == "CUENTAS_POR_COBRAR" && x.Codigo == "PAGADA");
        flujo.EsFinal = false; await e.Db.SaveChangesAsync();
        await Assert.ThrowsAsync<BootstrapAdministradorRechazadoException>(() => e.Preparador.EjecutarAsync(default));
    }
    [Theory, InlineData(false), InlineData(true)]
    public async Task ConsultasFiltrosResumenVencidasVigentesYPaginacion(bool pagar)
    {
        await using var e = await EscenarioCartera.CrearAsync(); var c = await e.CuentaAsync(pagar);
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        if (pagar) { var cuenta = await e.Db.CuentasPorPagar.SingleAsync(); cuenta.FechaEmision = hoy.AddDays(-3); cuenta.FechaVencimiento = hoy.AddDays(-2); }
        else { var cuenta = await e.Db.CuentasPorCobrar.SingleAsync(); cuenta.FechaEmision = hoy.AddDays(-3); cuenta.FechaVencimiento = hoy.AddDays(-2); }
        await e.Db.SaveChangesAsync(); e.Db.ChangeTracker.Clear();
        var detalle = await e.Servicio.ObtenerAsync(pagar, c.Encabezado.Id, EscenarioCartera.S, default);
        Assert.Equal("VENCIDA", detalle.Encabezado.Vencimiento);
        var resumen = Assert.Single(await e.Servicio.ResumenAsync(pagar, new(), EscenarioCartera.S, default));
        Assert.Equal(50, resumen.TotalVencido); Assert.Equal(0, resumen.TotalVigente); Assert.Equal("GTQ", resumen.Moneda);
        Assert.Equal(1, (await e.Servicio.ListarAsync(pagar, new() { Vencimiento = "VENCIDA", TerceroId = c.Encabezado.TerceroId }, EscenarioCartera.S, default)).TotalRegistros);
        Assert.Equal(0, (await e.Servicio.ListarAsync(pagar, new() { Vencimiento = "VIGENTE" }, EscenarioCartera.S, default)).TotalRegistros);
    }
}
