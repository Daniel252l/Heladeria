using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Heladeria.Aplicacion.Caja;
using Heladeria.Aplicacion.Seguridad.Autorizacion;
using Heladeria.Aplicacion.Ventas;
using Heladeria.Pruebas.Fase7;
using Heladeria.Pruebas.Fase9;
using Heladeria.Pruebas.Seguridad;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Heladeria.Pruebas.Fase8;

public sealed class AjusteFinalBPruebas
{
    [Theory]
    [InlineData(102)] [InlineData(98)] [InlineData(0)] [InlineData(100)]
    public async Task AperturaSugiereContadoFisicoNoEsperadoYPermiteCorreccion(decimal contado)
    {
        await using var e = await EscenarioCaja.CrearAsync();
        var cerrada = await e.CerrarAsync(await e.ArqueoAsync(await e.AbrirAsync(), contado));
        var refs = await e.Servicio.ReferenciasAsync(EscenarioCaja.Seguridad, default, cerrada.Encabezado.CajaId);
        Assert.Equal(contado, refs.SaldoUltimoCierre!.EfectivoContado);
        Assert.Equal(cerrada.Encabezado.Id, refs.SaldoUltimoCierre.SesionId);
        Assert.Equal(cerrada.Encabezado.NumeroCierre, refs.SaldoUltimoCierre.NumeroCierre);
        var abierta = await e.Servicio.AbrirAsync(new() { CajaId = cerrada.Encabezado.CajaId, MonedaId = 1, SaldoInicial = contado + 1 }, EscenarioCaja.Seguridad, default);
        Assert.Equal(contado + 1, abierta.Encabezado.SaldoInicial);
        Assert.Contains(await e.Contexto.Bitacoras.ToArrayAsync(), x => x.Accion == "CAJA_ABIERTA" && x.Comentario != null && x.Comentario.Contains("saldo inicial real"));
    }

    [Fact]
    public async Task SinCierreNoInventaSaldoNiUsaTurnoAbierto()
    {
        await using var e = await EscenarioCaja.CrearAsync(); var abierta = await e.AbrirAsync(345);
        Assert.Null((await e.Servicio.ReferenciasAsync(EscenarioCaja.Seguridad, default, abierta.Encabezado.CajaId)).SaldoUltimoCierre);
    }

    [Fact]
    public async Task EligeUltimoCerradoDeMismaCajaIgnorandoOtraCajaYTurnoAbierto()
    {
        await using var e = await EscenarioCaja.CrearAsync();
        var primera = await e.CerrarAsync(await e.ArqueoAsync(await e.AbrirAsync(), 101));
        var segunda = await e.Servicio.AbrirAsync(new() { CajaId = primera.Encabezado.CajaId, MonedaId = 1, SaldoInicial = 101 }, EscenarioCaja.Seguridad, default);
        segunda = await e.CerrarAsync(await e.ArqueoAsync(segunda, 99));
        await e.CerrarAsync(await e.ArqueoAsync(await e.AbrirAsync(500, "OTRA"), 501));
        await e.Servicio.AbrirAsync(new() { CajaId = primera.Encabezado.CajaId, MonedaId = 1, SaldoInicial = 700 }, EscenarioCaja.Seguridad, default);
        var saldo = (await e.Servicio.ReferenciasAsync(EscenarioCaja.Seguridad, default, primera.Encabezado.CajaId)).SaldoUltimoCierre!;
        Assert.Equal(segunda.Encabezado.Id, saldo.SesionId); Assert.Equal(99, saldo.EfectivoContado);
    }

    [Theory]
    [InlineData("historico")] [InlineData("arqueo")] [InlineData("moneda")]
    public async Task UltimoCierreIncompatibleNoRetrocedeAUnSaldoAnterior(string caso)
    {
        await using var e = await EscenarioCaja.CrearAsync();
        var cerrada = await e.CerrarAsync(await e.ArqueoAsync(await e.AbrirAsync(), 90));
        e.Contexto.ChangeTracker.Clear();
        var turno = await e.Contexto.TurnosCaja.SingleAsync();
        if (caso == "historico") turno.NumeroCierre = null;
        if (caso == "arqueo") turno.MontoFisico = 12;
        if (caso == "moneda") turno.MonedaId = 2;
        await e.Contexto.SaveChangesAsync(); e.Contexto.ChangeTracker.Clear();
        Assert.Null((await e.Servicio.ReferenciasAsync(EscenarioCaja.Seguridad, default, cerrada.Encabezado.CajaId)).SaldoUltimoCierre);
    }

    [Theory]
    [InlineData(1, 2)] [InlineData(2, 3)]
    public async Task NoHeredaSaldosDeOtraSucursalOEmpresa(int empresa, long sucursal)
    {
        await using var e = await EscenarioCaja.CrearAsync(); var cerrada = await e.CerrarAsync(await e.ArqueoAsync(await e.AbrirAsync(), 555));
        var caja = await e.Contexto.Cajas.SingleAsync(); caja.EmpresaId = empresa; caja.SucursalId = sucursal;
        await e.Contexto.SaveChangesAsync(); e.Contexto.ChangeTracker.Clear();
        await Assert.ThrowsAsync<CajaNoEncontradaException>(() => e.Servicio.ReferenciasAsync(EscenarioCaja.Seguridad, default, cerrada.Encabezado.CajaId));
    }

    [Theory]
    [InlineData(50, 0, 100, 150)] [InlineData(0, 50, 0, 100)] [InlineData(20, 30, 50, 120)]
    public async Task CompletarContadoRegistraInventarioPagoYMovimientosEnUnaOperacion(decimal efectivo, decimal tarjeta, decimal recibido, decimal esperado)
    {
        await using var e = await EscenarioCaja.CrearAsync(); var sesion = await e.AbrirAsync();
        var venta = await e.Ventas.ConfirmarAsync();
        var solicitud = await Solicitud(e, sesion, venta, efectivo, tarjeta, recibido);
        var resultado = await e.Ventas.Ventas.CompletarAsync(venta.Encabezado.Id, solicitud, EscenarioCaja.Seguridad, default);
        var caja = await e.Servicio.ObtenerAsync(sesion.Encabezado.Id, EscenarioCaja.Seguridad, default);
        Assert.Equal("COMPLETADA", resultado.Encabezado.Estado);
        Assert.Equal(150, caja.SaldoTeorico); Assert.Equal(esperado, caja.EfectivoEsperado);
        Assert.Equal(3000, (await e.Contexto.Existencias.AsNoTracking().SingleAsync(x => x.Id == 1)).Cantidad);
        Assert.Single(e.Contexto.Pagos); Assert.Single(e.Contexto.Kardex); Assert.Empty(e.Contexto.CuentasPorCobrar);
        Assert.Equal(solicitud.Cobro!.Pagos.Length, await e.Contexto.MovimientosCaja.CountAsync(x => x.CodigoConcepto == "COBRO_VENTA"));
        Assert.Equal(solicitud.ClaveOperacion, resultado.Cobro!.ClaveOperacion);
        Assert.Empty((await e.Servicio.VentasPendientesAsync(new FiltroSesionesCaja(), EscenarioCaja.Seguridad, default)).Elementos);
        Assert.Contains(e.Contexto.Bitacoras, x => x.Accion == "VENTA_COBRADA");
        Assert.Contains(e.Contexto.Bitacoras, x => x.Accion == "VENTA_COMPLETADA");
        await Assert.ThrowsAsync<ConflictoVentasException>(() => e.Ventas.AnularAsync(resultado));
    }

    [Fact]
    public async Task ContadoConDescuentoTotalConservaCierreSinCobroNiTurnoYReintentoExacto()
    {
        await using var e = await EscenarioCaja.CrearAsync();
        var venta = await e.Ventas.ConfirmarAsync(EscenarioFase7.Solicitud(descuento: 100));
        Assert.Equal(0, venta.Encabezado.Total);
        var solicitud = new CompletarVentaSolicitud { VersionFila = venta.Encabezado.VersionFila, ClaveOperacion = Guid.NewGuid() };
        var resultado = await e.Ventas.Ventas.CompletarAsync(venta.Encabezado.Id, solicitud, EscenarioCaja.Seguridad, default);
        Assert.Equal("COMPLETADA", resultado.Encabezado.Estado); Assert.Null(resultado.Cobro);
        Assert.Single(e.Contexto.Kardex); Assert.Single(e.Contexto.Movimientos);
        Assert.Equal(3000, (await e.Contexto.Existencias.AsNoTracking().SingleAsync(x => x.Id == 1)).Cantidad);
        Assert.Empty(e.Contexto.Pagos); Assert.Empty(e.Contexto.MovimientosCaja); Assert.Empty(e.Contexto.TurnosCaja);
        var antes = await Huella(e);
        await e.Ventas.Ventas.CompletarAsync(venta.Encabezado.Id, solicitud, EscenarioCaja.Seguridad, default);
        Assert.Equal(antes, await Huella(e));
    }

    [Fact]
    public async Task ContadoSinImporteNoAdmitePagoInventadoNiCambiaInventario()
    {
        await using var e = await EscenarioCaja.CrearAsync(); var sesion = await e.AbrirAsync();
        var venta = await e.Ventas.ConfirmarAsync(EscenarioFase7.Solicitud(descuento: 100));
        var solicitud = await Solicitud(e, sesion, venta);
        var antes = await Huella(e);
        await Assert.ThrowsAsync<ValidacionVentasException>(() => e.Ventas.Ventas.CompletarAsync(venta.Encabezado.Id, solicitud, EscenarioCaja.Seguridad, default));
        Assert.Equal(antes, await Huella(e));
    }

    [Fact]
    public async Task CreditoGeneraSoloCxcSinTurnoNiIngresoYReintentoNoDuplica()
    {
        await using var e = await EscenarioCartera.CrearAsync();
        var venta = await e.VentaCreditoAsync();
        await e.Caja.Ventas.CompletarAsync(venta, venta.Encabezado.ClaveOperacion);
        Assert.Single(e.Db.CuentasPorCobrar); Assert.Empty(e.Db.Pagos); Assert.Empty(e.Db.MovimientosCaja); Assert.Empty(e.Db.TurnosCaja);
    }

    [Theory]
    [InlineData("ausente", "COBRO_REQUERIDO")]
    [InlineData("cerrada", "CAJA_NO_ABIERTA")]
    [InlineData("version", "CONCURRENCIA_CAJA")]
    [InlineData("monto", "COBRO_INVALIDO")]
    public async Task FalloDeCobroDejaVentaConfirmadaSinEfectos(string caso, string codigo)
    {
        await using var e = await EscenarioCaja.CrearAsync(); var sesion = await e.AbrirAsync();
        var venta = await e.Ventas.ConfirmarAsync();
        if (caso == "cerrada") sesion = await e.CerrarAsync(await e.ArqueoAsync(sesion));
        var solicitud = await Solicitud(e, sesion, venta, caso == "monto" ? 49 : 50);
        solicitud = new() { VersionFila = solicitud.VersionFila, ClaveOperacion = solicitud.ClaveOperacion,
            Cobro = caso == "ausente" ? null : new() { SesionCajaId = sesion.Encabezado.Id,
                VersionCaja = caso == "version" ? "Ag==" : sesion.Encabezado.VersionFila, Pagos = solicitud.Cobro!.Pagos } };
        var antes = await Huella(e);
        var error = await Assert.ThrowsAsync<ExcepcionCaja>(() => e.Ventas.Ventas.CompletarAsync(venta.Encabezado.Id, solicitud, EscenarioCaja.Seguridad, default));
        Assert.Equal(codigo, error.Codigo); Assert.Equal(antes, await Huella(e));
        Assert.Equal("CONFIRMADA", (await e.Ventas.Ventas.ObtenerAsync(venta.Encabezado.Id, EscenarioCaja.Seguridad, default)).Encabezado.Estado);
    }

    [Fact]
    public async Task MismaUuidMismoPagoInclusoTrasCerrarCajaNoDuplicaYDistintoPagoRechaza()
    {
        await using var e = await EscenarioCaja.CrearAsync(); var sesion = await e.AbrirAsync(); var v = await e.Ventas.ConfirmarAsync();
        var solicitud = await Solicitud(e, sesion, v);
        await e.Ventas.Ventas.CompletarAsync(v.Encabezado.Id, solicitud, EscenarioCaja.Seguridad, default);
        sesion = await e.Servicio.ObtenerAsync(sesion.Encabezado.Id, EscenarioCaja.Seguridad, default);
        await e.CerrarAsync(await e.ArqueoAsync(sesion));
        var antes = await Huella(e);
        var repetida = await e.Ventas.Ventas.CompletarAsync(v.Encabezado.Id, solicitud, EscenarioCaja.Seguridad, default);
        Assert.Equal(solicitud.ClaveOperacion, repetida.Cobro!.ClaveOperacion); Assert.Equal(antes, await Huella(e));
        var distinta = new CompletarVentaSolicitud { VersionFila = v.Encabezado.VersionFila, ClaveOperacion = solicitud.ClaveOperacion,
            Cobro = new() { SesionCajaId = sesion.Encabezado.Id, VersionCaja = sesion.Encabezado.VersionFila,
                Pagos = [new() { FormaPagoId = solicitud.Cobro!.Pagos[0].FormaPagoId, MontoAplicado = 50, MontoRecibido = 60 }] } };
        Assert.Equal("COBRO_INVALIDO", (await Assert.ThrowsAsync<ExcepcionCaja>(() =>
            e.Ventas.Ventas.CompletarAsync(v.Encabezado.Id, distinta, EscenarioCaja.Seguridad, default))).Codigo);
        await Assert.ThrowsAsync<ConflictoVentasException>(() => e.Ventas.Ventas.CompletarAsync(v.Encabezado.Id,
            new() { VersionFila = v.Encabezado.VersionFila, ClaveOperacion = Guid.NewGuid(), Cobro = solicitud.Cobro }, EscenarioCaja.Seguridad, default));
        Assert.Equal(antes, await Huella(e));
    }

    [Theory]
    [InlineData("VENTA_COBRADA")] [InlineData("VENTA_COMPLETADA")]
    public async Task FalloTardioRevierteVentaInventarioPagoCajaCorrelativosYPermiteReintento(string accion)
    {
        await using var e = await EscenarioCaja.CrearAsync(); var s = await e.AbrirAsync(); var v = await e.Ventas.ConfirmarAsync();
        var solicitud = await Solicitud(e, s, v); var antes = await Huella(e);
        Assert.Contains(accion, new[] { "VENTA_COBRADA", "VENTA_COMPLETADA" });
        await e.Contexto.Database.ExecuteSqlRawAsync("CREATE TRIGGER FalloB BEFORE INSERT ON Bitacoras WHEN NEW.Accion='" + accion + "' BEGIN SELECT RAISE(ABORT,'Fallo aislado'); END;");
        await Assert.ThrowsAsync<DbUpdateException>(() => e.Ventas.Ventas.CompletarAsync(v.Encabezado.Id, solicitud, EscenarioCaja.Seguridad, default));
        Assert.Equal(antes, await Huella(e));
        await e.Contexto.Database.ExecuteSqlRawAsync("DROP TRIGGER FalloB;");
        Assert.Equal("COMPLETADA", (await e.Ventas.Ventas.CompletarAsync(v.Encabezado.Id, solicitud, EscenarioCaja.Seguridad, default)).Encabezado.Estado);
    }

    [Theory]
    [InlineData(PermisosSistema.CajaOperar)] [InlineData(PermisosSistema.CajaCobrar)]
    public async Task CierreConCobroNoEludePermisosCaja(string denegado)
    {
        await using var fabrica = new FabricaApiSeguridad(true, servicios => {
            servicios.RemoveAll<IServicioPermisos>(); servicios.AddSingleton<IServicioPermisos>(new PermisosSin(denegado));
        });
        using var cliente = fabrica.CrearClienteAutenticado();
        var respuesta = await cliente.PostAsJsonAsync("/api/ventas/1/completar", new CompletarVentaSolicitud {
            VersionFila = "AQ==", ClaveOperacion = Guid.NewGuid(), Cobro = new() { SesionCajaId = 1, VersionCaja = "AQ==",
                Pagos = [new() { FormaPagoId = 1, MontoAplicado = 50, MontoRecibido = 50 }] } });
        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }
    private sealed class PermisosSin(string denegado) : IServicioPermisos
    {
        public Task<bool> TienePermisoAsync(int usuarioId, string rolId, string codigoPermiso, CancellationToken t) => Task.FromResult(codigoPermiso != denegado);
        public Task<IReadOnlyList<string>> ObtenerPermisosAsync(int usuarioId, CancellationToken t) => Task.FromResult<IReadOnlyList<string>>([]);
    }
    [Theory]
    [InlineData(true)] [InlineData(false)]
    public async Task DosRequestsDeCierreNoDuplicanInventarioNiPago(bool mismaUuid)
    {
        await using var e = await EscenarioCaja.CrearAsync(); var s = await e.AbrirAsync(); var v = await e.Ventas.ConfirmarAsync();
        var solicitud = await Solicitud(e, s, v);
        var otra = new CompletarVentaSolicitud { VersionFila = solicitud.VersionFila,
            ClaveOperacion = mismaUuid ? solicitud.ClaveOperacion : Guid.NewGuid(), Cobro = solicitud.Cobro };
        async Task<bool> Intentar(CompletarVentaSolicitud peticion)
        {
            try { await e.Ventas.Ventas.CompletarAsync(v.Encabezado.Id, peticion, EscenarioCaja.Seguridad, default); return true; }
            catch (ConflictoVentasException) { return false; }
        }
        var resultados = await Task.WhenAll(Intentar(solicitud), Intentar(otra));
        Assert.Equal(mismaUuid ? 2 : 1, resultados.Count(x => x));
        Assert.Single(e.Contexto.Pagos); Assert.Single(e.Contexto.PagoDetalles);
        Assert.Single(e.Contexto.Kardex); Assert.Single(e.Contexto.Movimientos);
        Assert.Single(await e.Contexto.MovimientosCaja.Where(x => x.CodigoConcepto == "COBRO_VENTA").ToArrayAsync());
    }
    private static async Task<CompletarVentaSolicitud> Solicitud(EscenarioCaja e, SesionCajaDto s, VentaConsultaDto v, decimal efectivo = 50, decimal tarjeta = 0, decimal? recibido = null)
    {
        var cobro = await e.SolicitudCobroAsync(s, v, efectivo, tarjeta, recibido);
        return new() { VersionFila = v.Encabezado.VersionFila, ClaveOperacion = cobro.ClaveOperacion,
            Cobro = new() { SesionCajaId = s.Encabezado.Id, VersionCaja = s.Encabezado.VersionFila, Pagos = cobro.Pagos } };
    }
    private static async Task<string> Huella(EscenarioCaja e) => JsonSerializer.Serialize(new {
        Ventas = await e.Contexto.Ventas.AsNoTracking().OrderBy(x => x.Id).Select(x => new { x.Id, x.EstadoId, x.ClaveOperacion, x.FechaCompletada, x.VersionFila }).ToArrayAsync(),
        Stock = await e.Contexto.Existencias.AsNoTracking().OrderBy(x => x.Id).Select(x => new { x.Id, x.Cantidad, x.CostoPromedio, x.VersionFila }).ToArrayAsync(),
        Pagos = await e.Contexto.Pagos.CountAsync(), Detalles = await e.Contexto.PagoDetalles.CountAsync(), Kardex = await e.Contexto.Kardex.CountAsync(),
        Movimientos = await e.Contexto.Movimientos.CountAsync(), MovimientosCaja = await e.Contexto.MovimientosCaja.CountAsync(),
        Costos = await e.Contexto.CostosProductos.CountAsync(), Historicos = await e.Contexto.HistoricoCostosProductos.CountAsync(),
        Turnos = await e.Contexto.TurnosCaja.AsNoTracking().OrderBy(x => x.Id).Select(x => new { x.Id, x.VersionFila, x.MontoInicial, x.MontoFisico, x.FechaCierre }).ToArrayAsync(),
        Correlativos = await e.Contexto.CorrelativosDocumentos.AsNoTracking().OrderBy(x => x.Id).Select(x => new { x.Id, x.UltimoNumero }).ToArrayAsync()
    });
}
