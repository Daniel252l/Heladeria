using System.Text.Json;
using Heladeria.Aplicacion.Caja;
using Heladeria.Aplicacion.Seguridad.Bootstrap;
using Heladeria.Aplicacion.Ventas;
using Heladeria.Infraestructura.Persistencia.Entidades;
using Heladeria.Pruebas.Fase7;
using Microsoft.EntityFrameworkCore;
using static Heladeria.Pruebas.Fase8.EscenarioCaja;

namespace Heladeria.Pruebas.Fase8;

public sealed class CajaPruebas
{
    private static Heladeria.Aplicacion.Configuracion.ContextoConfiguracion Seguridad => EscenarioCaja.Seguridad;
    [Fact]
    public async Task PreparacionIdempotenteSoloAdminSinOperacionesNiReinicioDeCorrelativos()
    {
        await using var e = await CrearAsync(false);
        var primera = await e.Preparador.EjecutarAsync(default);
        Assert.NotEmpty(primera.Registros);
        var segunda = await e.Preparador.EjecutarAsync(default); Assert.Empty(segunda.Registros);
        Assert.Empty(e.Contexto.Cajas); Assert.Empty(e.Contexto.TurnosCaja); Assert.Empty(e.Contexto.Pagos);
        Assert.Empty(e.Contexto.MovimientosCaja); Assert.Empty(e.Contexto.Ventas); Assert.Empty(e.Contexto.ArqueosCaja);
        Assert.Equal(7, await e.Contexto.Permisos.CountAsync(x => x.ModuloId == "CAJA"));
        Assert.All(await e.Contexto.RolesPermisos.Where(x => x.Permiso.ModuloId == "CAJA").ToArrayAsync(), x => { Assert.Equal("ADMIN", x.RoleId); Assert.True(x.Permitido); });
        var sesion = await e.AbrirAsync();
        await e.Preparador.EjecutarAsync(default);
        Assert.Equal(1, (await e.Contexto.CorrelativosDocumentos.SingleAsync(x => x.SerieDocumento.Serie == "CA")).UltimoNumero);
        Assert.Single(await e.Contexto.Bitacoras.Where(x => x.Accion == "FASE8_CONFIGURADA").ToArrayAsync());
        Assert.Equal("ABIERTA", sesion.Encabezado.Estado);
    }
    [Fact]
    public async Task PreparacionIncompatibleRevierteTodoYPermiteReintento()
    {
        await using var e = await CrearAsync(false);
        e.Contexto.FormasPago.Add(new() { EmpresaId = 1, Codigo = "EFECTIVO", Nombre = "No compatible", EstadoId = 1, EsCredito = true });
        await e.Contexto.SaveChangesAsync();
        await Assert.ThrowsAsync<BootstrapAdministradorRechazadoException>(() => e.Preparador.EjecutarAsync(default));
        Assert.False(await e.Contexto.Modulos.AnyAsync(x => x.Id == "CAJA"));
        Assert.False(await e.Contexto.Permisos.AnyAsync(x => x.ModuloId == "CAJA"));
        Assert.False(await e.Contexto.SeriesDocumentos.AnyAsync(x => x.Serie == "CA"));
        var forma = await e.Contexto.FormasPago.SingleAsync(); forma.EsCredito = false; forma.AfectaCaja = true; forma.PermiteCambio = true;
        await e.Contexto.SaveChangesAsync();
        Assert.NotEmpty((await e.Preparador.EjecutarAsync(default)).Registros);
    }
    [Theory]
    [InlineData(0)] [InlineData(100.25)]
    public async Task AperturaExplicitaSaldoInicialConsultaYTotales(decimal inicial)
    {
        await using var e = await CrearAsync(); var sesion = await e.AbrirAsync(inicial);
        Assert.Equal(inicial, sesion.EfectivoEsperado); Assert.Equal(inicial, sesion.SaldoTeorico);
        Assert.Equal("ABIERTA", sesion.Encabezado.Estado); Assert.Equal("GTQ", sesion.Encabezado.Moneda);
        Assert.Equal(inicial > 0 ? 1 : 0, sesion.Movimientos.Count);
        Assert.All(sesion.Movimientos, x => Assert.Equal("APERTURA_CAJA", x.Tipo));
        Assert.Empty(sesion.Cobros); Assert.Empty(e.Contexto.Ventas); Assert.NotEmpty(sesion.Encabezado.VersionFila);
        Assert.Single((await e.Servicio.CajasAsync(new FiltroCaja(), Seguridad, default)).Elementos);
        Assert.Single((await e.Servicio.SesionesAsync(new() { Abierta = true }, Seguridad, default)).Elementos);
        Assert.Single(await e.Contexto.Bitacoras.Where(x => x.Accion == "CAJA_ABIERTA").ToArrayAsync());
        var registroCaja = await e.Contexto.Bitacoras.SingleAsync(x => x.Accion == "CAJA_CREADA");
        Assert.Equal("Cajas", registroCaja.Entidad); Assert.Equal(sesion.Encabezado.CajaId.ToString(), registroCaja.EntidadId);
    }
    [Fact]
    public async Task DobleAperturaSeRechazaSinConsumirNumeroYUsuarioPuedeAbrirOtraCaja()
    {
        await using var e = await CrearAsync(); var sesion = await e.AbrirAsync();
        var error = await Assert.ThrowsAsync<ExcepcionCaja>(() => e.Servicio.AbrirAsync(new() { CajaId = sesion.Encabezado.CajaId, MonedaId = 1 }, Seguridad, default));
        Assert.Equal("CAJA_YA_ABIERTA", error.Codigo); Assert.Single(e.Contexto.TurnosCaja);
        Assert.Equal(1, (await e.Contexto.CorrelativosDocumentos.SingleAsync(x => x.SerieDocumento.Serie == "CA")).UltimoNumero);
        await e.AbrirAsync(codigo: "SEGUNDA"); Assert.Equal(2, await e.Contexto.TurnosCaja.CountAsync());
    }
    [Theory]
    [InlineData("inactiva")] [InlineData("sucursal")] [InlineData("empresa")] [InlineData("moneda")]
    [InlineData("negativo")] [InlineData("precision")]
    public async Task AperturaValidaAlcanceEstadoMonedaEImporte(string caso)
    {
        await using var e = await CrearAsync();
        var caja = await e.Servicio.CrearCajaAsync(new() { Codigo = "CAJ", Nombre = "Caja" }, Seguridad, default);
        var registro = await e.Contexto.Cajas.SingleAsync();
        if (caso == "inactiva") registro.EstadoId = 2;
        if (caso == "sucursal") registro.SucursalId = 2;
        if (caso == "empresa") { registro.EmpresaId = 2; registro.SucursalId = 3; }
        await e.Contexto.SaveChangesAsync();
        await Assert.ThrowsAnyAsync<ExcepcionCaja>(() => e.Servicio.AbrirAsync(new() { CajaId = caja.Id,
            MonedaId = caso == "moneda" ? 2 : 1, SaldoInicial = caso == "negativo" ? -1 : caso == "precision" ? .001m : 0 }, Seguridad, default));
        Assert.Empty(e.Contexto.TurnosCaja); Assert.Empty(e.Contexto.MovimientosCaja);
    }
    [Fact]
    public async Task IngresoEgresoNoSonVentasYConservanTrazabilidad()
    {
        await using var e = await CrearAsync(); var sesion = await e.AbrirAsync(100);
        sesion = await e.MovimientoAsync(sesion, "INGRESO", 20); sesion = await e.MovimientoAsync(sesion, "EGRESO", 30);
        Assert.Equal(90, sesion.EfectivoEsperado); Assert.Equal(90, sesion.SaldoTeorico);
        var forma = Assert.Single(sesion.TotalesPorFormaPago);
        Assert.Equal(100, forma.SaldoInicial); Assert.Equal(20, forma.Ingresos); Assert.Equal(30, forma.Egresos); Assert.Equal(90, forma.Neto);
        Assert.Equal(3, sesion.Movimientos.Count); Assert.Empty(sesion.Cobros); Assert.Empty(e.Contexto.Pagos);
        Assert.All(sesion.Movimientos, x => { Assert.Equal(1, x.UsuarioId); Assert.NotNull(x.NumeroDocumento); });
        Assert.Equal(2, await e.Contexto.Bitacoras.CountAsync(x => x.Accion == "MOVIMIENTO_CAJA_CREADO"));
    }
    [Theory]
    [InlineData(0, "INGRESO", "Motivo")] [InlineData(-1, "INGRESO", "Motivo")]
    [InlineData(.001, "INGRESO", "Motivo")] [InlineData(1, "AJUSTE", "Motivo")]
    [InlineData(1, "INGRESO", "  ")] [InlineData(101, "EGRESO", "Motivo")]
    public async Task MovimientoInvalidoNoMutaLibro(decimal monto, string tipo, string motivo)
    {
        await using var e = await CrearAsync(); var sesion = await e.AbrirAsync();
        await Assert.ThrowsAnyAsync<ExcepcionCaja>(() => e.Servicio.MovimientoAsync(sesion.Encabezado.Id, new() {
            VersionFila = sesion.Encabezado.VersionFila, Tipo = tipo, Monto = monto, Motivo = motivo }, Seguridad, default));
        var actual = await e.Servicio.ObtenerAsync(sesion.Encabezado.Id, Seguridad, default);
        Assert.Equal(JsonSerializer.Serialize(sesion), JsonSerializer.Serialize(actual));
    }
    [Theory]
    [InlineData(50, 0, 100, 150)] [InlineData(0, 50, 0, 100)] [InlineData(20, 30, 50, 120)]
    public async Task CobroEfectivoTarjetaMixtoVueltoSinTocarInventario(decimal efectivo, decimal tarjeta, decimal recibido, decimal esperado)
    {
        await using var e = await CrearAsync(); var sesion = await e.AbrirAsync(); var venta = await e.VentaAsync();
        var inventario = await HuellaInventarioAsync(e);
        var solicitud = await e.SolicitudCobroAsync(sesion, venta, efectivo, tarjeta, recibido);
        var resultado = await e.Servicio.CobrarAsync(sesion.Encabezado.Id, solicitud, Seguridad, default);
        Assert.False(resultado.Reintento); Assert.Equal(50, resultado.Cobro.Total); Assert.Equal(esperado, resultado.Sesion.EfectivoEsperado);
        Assert.Equal(150, resultado.Sesion.SaldoTeorico); Assert.Equal(50, resultado.Cobro.Detalles.Sum(x => x.Aplicado));
        Assert.Equal(efectivo > 0 ? recibido - efectivo : 0, resultado.Cobro.Detalles.Sum(x => x.Vuelto));
        Assert.Null((await e.Contexto.Pagos.SingleAsync()).ClienteId);
        Assert.Equal(inventario, await HuellaInventarioAsync(e));
        var consulta = await e.Ventas.Ventas.ObtenerAsync(venta.Encabezado.Id, Seguridad, default);
        Assert.Equal(resultado.Cobro.Id, consulta.Cobro!.PagoId);
        Assert.Equal(solicitud.ClaveOperacion, consulta.Cobro.ClaveOperacion);
        // El DTO de lectura incorpora ahora los pagos del ticket. Solo ese histórico de pagos cambia tras cobrar.
        Assert.NotNull(consulta.Comprobante);
        Assert.Equal(resultado.Cobro.Detalles.Sum(x => x.Aplicado), consulta.Comprobante.Pagos.Sum(x => x.Aplicado));
        Assert.Equal(resultado.Cobro.Detalles.Sum(x => x.Vuelto), consulta.Comprobante.Pagos.Sum(x => x.Vuelto));
        Assert.Equal(JsonSerializer.Serialize(venta), JsonSerializer.Serialize(consulta with { Cobro = null, Comprobante = consulta.Comprobante with { Pagos = [] } }));
        Assert.Empty((await e.Servicio.VentasPendientesAsync(new FiltroCaja(), Seguridad, default)).Elementos);
        Assert.Single(await e.Contexto.Bitacoras.Where(x => x.Accion == "VENTA_COBRADA").ToArrayAsync());
    }
    [Theory]
    [InlineData("BORRADOR")] [InlineData("CONFIRMADA")] [InlineData("ANULADA")]
    public async Task VentaNoCompletadaNoSeCobra(string estado)
    {
        await using var e = await CrearAsync(); var sesion = await e.AbrirAsync();
        var venta = estado == "BORRADOR" ? await e.Ventas.CrearAsync(EscenarioFase7.Solicitud()) : await e.Ventas.ConfirmarAsync();
        if (estado == "ANULADA") venta = await e.Ventas.AnularAsync(venta);
        var solicitud = await e.SolicitudCobroAsync(sesion, venta);
        var error = await Assert.ThrowsAsync<ExcepcionCaja>(() => e.Servicio.CobrarAsync(sesion.Encabezado.Id, solicitud, Seguridad, default));
        Assert.Equal("COBRO_INVALIDO", error.Codigo); Assert.Empty(e.Contexto.Pagos); Assert.Single(e.Contexto.MovimientosCaja);
    }
    [Fact]
    public async Task PendientesSoloCompletadasNoCobradasDeLaSucursal()
    {
        await using var e = await CrearAsync(); var sesion = await e.AbrirAsync(); var venta = await e.VentaAsync();
        await e.Ventas.CrearAsync(EscenarioFase7.Solicitud());
        var pendientes = await e.Servicio.VentasPendientesAsync(new FiltroCaja(), Seguridad, default);
        Assert.Equal(venta.Encabezado.Id, Assert.Single(pendientes.Elementos).Id); Assert.Null(pendientes.Elementos[0].ClienteId);
        Assert.Equal(50, pendientes.Elementos[0].SaldoPendiente);
        Assert.Empty((await e.Servicio.VentasPendientesAsync(new FiltroCaja(), Seguridad with { SucursalId = 2 }, default)).Elementos);
        await e.Servicio.CobrarAsync(sesion.Encabezado.Id, await e.SolicitudCobroAsync(sesion, venta), Seguridad, default);
        Assert.Empty((await e.Servicio.VentasPendientesAsync(new FiltroCaja(), Seguridad, default)).Elementos);
    }
    [Fact]
    public async Task UUIDRepetidaDevuelveExistenteInclusoCerradaYDistintaNoDuplica()
    {
        await using var e = await CrearAsync(); var sesion = await e.AbrirAsync(); var venta = await e.VentaAsync();
        var solicitud = await e.SolicitudCobroAsync(sesion, venta);
        var cobro = await e.Servicio.CobrarAsync(sesion.Encabezado.Id, solicitud, Seguridad, default);
        var repetido = await e.Servicio.CobrarAsync(sesion.Encabezado.Id, solicitud, Seguridad, default);
        Assert.True(repetido.Reintento); Assert.Equal(cobro.Cobro.Id, repetido.Cobro.Id);
        var distinta = await e.SolicitudCobroAsync(cobro.Sesion, venta);
        Assert.Equal("VENTA_YA_COBRADA", (await Assert.ThrowsAsync<ExcepcionCaja>(() => e.Servicio.CobrarAsync(sesion.Encabezado.Id, distinta, Seguridad, default))).Codigo);
        await e.CerrarAsync(await e.ArqueoAsync(cobro.Sesion));
        Assert.True((await e.Servicio.CobrarAsync(sesion.Encabezado.Id, solicitud, Seguridad, default)).Reintento);
        Assert.Single(e.Contexto.Pagos); Assert.Single(e.Contexto.PagoDetalles); Assert.Equal(2, await e.Contexto.MovimientosCaja.CountAsync());
    }
    [Fact]
    public async Task UUIDNoSeReutilizaParaOtraSolicitud()
    {
        await using var e = await CrearAsync(); var sesion = await e.AbrirAsync(); var venta = await e.VentaAsync();
        var solicitud = await e.SolicitudCobroAsync(sesion, venta);
        var cobro = await e.Servicio.CobrarAsync(sesion.Encabezado.Id, solicitud, Seguridad, default);
        var diferente = await e.SolicitudCobroAsync(cobro.Sesion, venta, recibido: 100, clave: solicitud.ClaveOperacion);
        Assert.Equal("COBRO_INVALIDO", (await Assert.ThrowsAsync<ExcepcionCaja>(() => e.Servicio.CobrarAsync(sesion.Encabezado.Id, diferente, Seguridad, default))).Codigo);
        Assert.Single(e.Contexto.Pagos);
    }
    [Theory]
    [InlineData("importe")] [InlineData("vuelto-tarjeta")] [InlineData("recibido-menor")]
    [InlineData("forma-inactiva")] [InlineData("credito")] [InlineData("otra-empresa")]
    [InlineData("referencia")] [InlineData("PAN")] [InlineData("CVV")]
    [InlineData("uuid")] [InlineData("version-venta")] [InlineData("version-caja")]
    public async Task CobroInvalidoRollbackSinPagoNiMovimiento(string caso)
    {
        await using var e = await CrearAsync(); var sesion = await e.AbrirAsync(); var venta = await e.VentaAsync();
        var forma = await e.Contexto.FormasPago.SingleAsync(x => x.Codigo == "TARJETA");
        if (caso == "forma-inactiva") forma.EstadoId = 2;
        if (caso == "credito") forma.EsCredito = true;
        if (caso == "otra-empresa") forma.EmpresaId = 2;
        if (caso == "referencia") forma.RequiereReferencia = true;
        await e.Contexto.SaveChangesAsync();
        var solicitud = new CobrarVentaSolicitud { VentaId = venta.Encabezado.Id,
            VersionVenta = caso == "version-venta" ? "AQ==" : venta.Encabezado.VersionFila,
            VersionFila = caso == "version-caja" ? "AQ==" : sesion.Encabezado.VersionFila,
            ClaveOperacion = caso == "uuid" ? Guid.Empty : Guid.NewGuid(), Pagos = [new() { FormaPagoId = forma.Id,
                MontoAplicado = caso == "importe" ? 49 : 50, MontoRecibido = caso == "vuelto-tarjeta" ? 100 : caso == "recibido-menor" ? 40 : 50,
                Referencia = caso == "PAN" ? "4111-1111-1111-1111" : caso == "CVV" ? "CVV-123" : null }] };
        var anterior = await HuellaInventarioAsync(e);
        await Assert.ThrowsAnyAsync<ExcepcionCaja>(() => e.Servicio.CobrarAsync(sesion.Encabezado.Id, solicitud, Seguridad, default));
        Assert.Empty(e.Contexto.Pagos); Assert.Empty(e.Contexto.PagoDetalles); Assert.Single(e.Contexto.MovimientosCaja);
        Assert.Equal(anterior, await HuellaInventarioAsync(e));
        Assert.Equal(sesion.Encabezado.VersionFila, (await e.Servicio.ObtenerAsync(sesion.Encabezado.Id, Seguridad, default)).Encabezado.VersionFila);
        Assert.Single(await e.Contexto.Bitacoras.Where(x => x.Accion == "COBRO_RECHAZADO").ToArrayAsync());
        Assert.DoesNotContain(await e.Contexto.Bitacoras.ToArrayAsync(), x => x.Comentario?.Contains("4111") == true || x.Comentario?.Contains("CVV-123") == true);
    }
    [Theory]
    [InlineData(-10)] [InlineData(0)] [InlineData(10)]
    public async Task ArqueoYCierrePersistenDiferenciaSinAjusteAutomatico(decimal diferencia)
    {
        await using var e = await CrearAsync(); var sesion = await e.AbrirAsync();
        sesion = await e.MovimientoAsync(sesion, "INGRESO", 20); sesion = await e.MovimientoAsync(sesion, "EGRESO", 5);
        sesion = await e.ArqueoAsync(sesion, 115 + diferencia);
        Assert.Equal(diferencia, Assert.Single(sesion.Arqueos).Diferencia); Assert.True(sesion.Arqueos[0].Vigente);
        var cerrada = await e.CerrarAsync(sesion);
        Assert.Equal("CERRADA", cerrada.Encabezado.Estado); Assert.Equal(115, cerrada.Encabezado.EfectivoEsperadoCierre);
        Assert.Equal(diferencia, cerrada.Encabezado.DiferenciaCierre); Assert.Equal(20, cerrada.Encabezado.TotalIngresosCierre);
        Assert.Equal(5, cerrada.Encabezado.TotalEgresosCierre); Assert.Equal(1, cerrada.Encabezado.UsuarioCierreId);
        Assert.Equal("admin", cerrada.UsuarioCierre);
        Assert.Equal(3, cerrada.Movimientos.Count); Assert.NotNull(cerrada.Encabezado.NumeroCierre);
        Assert.Empty((await e.Servicio.SesionesAsync(new() { Abierta = true }, Seguridad, default)).Elementos);
        Assert.Equal(1, await e.Contexto.Bitacoras.CountAsync(x => x.Accion == "CAJA_CERRADA"));
        var nuevo = await e.Servicio.AbrirAsync(new() { CajaId = cerrada.Encabezado.CajaId, MonedaId = 1 }, Seguridad, default);
        Assert.NotEqual(cerrada.Encabezado.Id, nuevo.Encabezado.Id);
    }
    [Fact]
    public async Task CierreExigeArqueoVigenteYVersionActual()
    {
        await using var e = await CrearAsync(); var sesion = await e.AbrirAsync();
        await Assert.ThrowsAsync<ExcepcionCaja>(() => e.Servicio.CerrarAsync(sesion.Encabezado.Id,
            new() { VersionFila = sesion.Encabezado.VersionFila, ArqueoId = 999 }, Seguridad, default));
        var arqueada = await e.ArqueoAsync(sesion); var movida = await e.MovimientoAsync(arqueada);
        Assert.False(movida.Arqueos.Single().Vigente);
        await Assert.ThrowsAsync<ExcepcionCaja>(() => e.CerrarAsync(movida));
        Assert.Equal("CONCURRENCIA_CAJA", (await Assert.ThrowsAsync<ExcepcionCaja>(() => e.MovimientoAsync(arqueada))).Codigo);
        Assert.Equal("CONCURRENCIA_CAJA", (await Assert.ThrowsAsync<ExcepcionCaja>(() => e.CerrarAsync(arqueada))).Codigo);
        Assert.Equal("CERRADA", (await e.CerrarAsync(await e.ArqueoAsync(movida))).Encabezado.Estado);
    }
    [Fact]
    public async Task CerradaNoRecibeMovimientosCobrosArqueosNiSegundoCierre()
    {
        await using var e = await CrearAsync(); var sesion = await e.CerrarAsync(await e.ArqueoAsync(await e.AbrirAsync()));
        var venta = await e.VentaAsync(); var solicitud = await e.SolicitudCobroAsync(sesion, venta);
        Assert.Equal("CAJA_NO_ABIERTA", (await Assert.ThrowsAsync<ExcepcionCaja>(() => e.MovimientoAsync(sesion))).Codigo);
        Assert.Equal("CAJA_NO_ABIERTA", (await Assert.ThrowsAsync<ExcepcionCaja>(() => e.ArqueoAsync(sesion))).Codigo);
        Assert.Equal("CAJA_NO_ABIERTA", (await Assert.ThrowsAsync<ExcepcionCaja>(() => e.CerrarAsync(sesion))).Codigo);
        Assert.Equal("CAJA_NO_ABIERTA", (await Assert.ThrowsAsync<ExcepcionCaja>(() => e.Servicio.CobrarAsync(sesion.Encabezado.Id, solicitud, Seguridad, default))).Codigo);
        Assert.Equal(JsonSerializer.Serialize(sesion), JsonSerializer.Serialize(await e.Servicio.ObtenerAsync(sesion.Encabezado.Id, Seguridad, default)));
    }
    [Fact]
    public async Task VentaCobradaNoPuedeAnularseNiAlterarInventarioFinanzas()
    {
        await using var e = await CrearAsync(); var sesion = await e.AbrirAsync(); var venta = await e.VentaAsync();
        await e.Servicio.CobrarAsync(sesion.Encabezado.Id, await e.SolicitudCobroAsync(sesion, venta), Seguridad, default);
        var huella = await HuellaInventarioAsync(e);
        var error = await Assert.ThrowsAsync<ConflictoVentasException>(() => e.Ventas.AnularAsync(venta));
        Assert.Contains("cobrada", error.Message); Assert.Equal(huella, await HuellaInventarioAsync(e));
        Assert.Single(e.Contexto.Pagos); Assert.Equal(2, await e.Contexto.MovimientosCaja.CountAsync());
        Assert.Equal("COMPLETADA", (await e.Ventas.Ventas.ObtenerAsync(venta.Encabezado.Id, Seguridad, default)).Encabezado.Estado);
    }
    [Theory]
    [InlineData(1, 2)] [InlineData(2, 3)] [InlineData(1, 4)]
    public async Task IdorNoLeeNiEscribeSesionAjena(int empresa, long sucursal)
    {
        await using var e = await CrearAsync(); var sesion = await e.AbrirAsync();
        var otro = Seguridad with { EmpresaId = empresa, SucursalId = sucursal };
        await Assert.ThrowsAsync<CajaNoEncontradaException>(() => e.Servicio.ObtenerAsync(sesion.Encabezado.Id, otro, default));
        await Assert.ThrowsAsync<CajaNoEncontradaException>(() => e.Servicio.MovimientoAsync(sesion.Encabezado.Id,
            new() { VersionFila = sesion.Encabezado.VersionFila, Tipo = "INGRESO", Monto = 10, Motivo = "Prueba" }, otro, default));
        Assert.Equal(100, (await e.Servicio.ObtenerAsync(sesion.Encabezado.Id, Seguridad, default)).EfectivoEsperado);
    }
    [Theory]
    [InlineData("VENTA_COBRADA")] [InlineData("MOVIMIENTO_CAJA_CREADO")] [InlineData("CAJA_CERRADA")]
    public async Task FalloTardioReviertePagoMovimientosTurnoYCorrelativoPermiteReintento(string accion)
    {
        await using var e = await CrearAsync(); var sesion = await e.AbrirAsync(); var venta = await e.VentaAsync();
        if (accion == "CAJA_CERRADA") sesion = await e.ArqueoAsync(sesion);
        var solicitud = await e.SolicitudCobroAsync(sesion, venta);
        var antes = JsonSerializer.Serialize(sesion);
        var contadores = await e.Contexto.CorrelativosDocumentos.AsNoTracking().OrderBy(x => x.Id).Select(x => x.UltimoNumero).ToArrayAsync();
        // Identificador de teoría constante, nunca entrada del usuario ni base real.
        Assert.Contains(accion, new[] { "VENTA_COBRADA", "MOVIMIENTO_CAJA_CREADO", "CAJA_CERRADA" });
        var disparador = "CREATE TRIGGER FalloCaja BEFORE INSERT ON Bitacoras WHEN NEW.Accion='ACCION_CONSTANTE' BEGIN SELECT RAISE(ABORT,'Fallo deliberado de prueba'); END;".Replace("ACCION_CONSTANTE", accion, StringComparison.Ordinal);
        await e.Contexto.Database.ExecuteSqlRawAsync(disparador);
        Task Operar() => accion == "CAJA_CERRADA" ? e.CerrarAsync(sesion) : accion == "MOVIMIENTO_CAJA_CREADO" ? e.MovimientoAsync(sesion)
            : e.Servicio.CobrarAsync(sesion.Encabezado.Id, solicitud, Seguridad, default);
        await Assert.ThrowsAsync<DbUpdateException>(Operar);
        Assert.Equal(antes, JsonSerializer.Serialize(await e.Servicio.ObtenerAsync(sesion.Encabezado.Id, Seguridad, default)));
        Assert.Equal(contadores, await e.Contexto.CorrelativosDocumentos.AsNoTracking().OrderBy(x => x.Id).Select(x => x.UltimoNumero).ToArrayAsync());
        Assert.Empty(e.Contexto.Pagos); Assert.Empty(e.Contexto.PagoDetalles);
        await e.Contexto.Database.ExecuteSqlRawAsync("DROP TRIGGER FalloCaja;"); await Operar();
    }
    [Fact]
    public async Task HistoricoSinMonedaNiSnapshotsEsSoloLecturaNoSeInventaSaldo()
    {
        await using var e = await CrearAsync();
        var caja = await e.Servicio.CrearCajaAsync(new() { Codigo = "HIST", Nombre = "Caja histórica" }, Seguridad, default);
        var estado = await e.Contexto.EstadosFlujo.SingleAsync(x => x.FlujoEstado.Codigo == "CAJA_SESION" && x.Codigo == "ABIERTA");
        var historico = new TurnosCaja { CajaId = caja.Id, UsuarioId = 1, FechaApertura = DateTime.UtcNow.AddDays(-1),
            MontoInicial = 100, FlujoEstadoId = estado.FlujoEstadoId, EstadoId = estado.Id };
        e.Contexto.TurnosCaja.Add(historico); await e.Contexto.SaveChangesAsync();
        var sesion = await e.Servicio.ObtenerAsync(historico.Id, Seguridad, default);
        Assert.False(sesion.Encabezado.EsFase8); Assert.Null(sesion.Encabezado.MonedaId);
        Assert.Null(sesion.SaldoTeorico); Assert.Null(sesion.EfectivoEsperado);
        await Assert.ThrowsAsync<ExcepcionCaja>(() => e.MovimientoAsync(sesion));
        await Assert.ThrowsAsync<ExcepcionCaja>(() => e.ArqueoAsync(sesion, contado: 100));
        Assert.Empty(e.Contexto.MovimientosCaja); Assert.Empty(e.Contexto.ArqueosCaja);
    }
    [Fact]
    public async Task LibroIncompatibleSeRechazaSinCorregirRegistros()
    {
        await using var e = await CrearAsync(); var sesion = await e.AbrirAsync();
        var apertura = await e.Contexto.MovimientosCaja.SingleAsync(); apertura.Monto = 99;
        await e.Contexto.SaveChangesAsync();
        var error = await Assert.ThrowsAsync<ExcepcionCaja>(() => e.ArqueoAsync(sesion));
        Assert.Contains("consistentes", error.Message); Assert.Empty(e.Contexto.ArqueosCaja);
        Assert.Equal(99, (await e.Contexto.MovimientosCaja.AsNoTracking().SingleAsync()).Monto);
        Assert.Null((await e.Contexto.TurnosCaja.AsNoTracking().SingleAsync()).FechaCierre);
    }
    [Fact]
    public async Task NoPermiteAcumularEfectivoQueNoQuepaEnElSnapshotDeCierre()
    {
        await using var e = await CrearAsync(); var sesion = await e.AbrirAsync(9999999999.99m);
        await Assert.ThrowsAsync<ValidacionCajaException>(() => e.MovimientoAsync(sesion));
        Assert.Single(e.Contexto.MovimientosCaja);
        Assert.Equal(sesion.Encabezado.VersionFila, (await e.Servicio.ObtenerAsync(sesion.Encabezado.Id, Seguridad, default)).Encabezado.VersionFila);
    }
    [Fact]
    public async Task NoCobraVentaDeOtraSucursalNiEmpresa()
    {
        await using var e = await CrearAsync(); var sesion = await e.AbrirAsync(); var venta = await e.VentaAsync();
        var solicitud = await e.SolicitudCobroAsync(sesion, venta);
        var registro = await e.Contexto.Ventas.SingleAsync(); registro.SucursalId = 2; await e.Contexto.SaveChangesAsync();
        await Assert.ThrowsAsync<CajaNoEncontradaException>(() => e.Servicio.CobrarAsync(sesion.Encabezado.Id, solicitud, Seguridad, default));
        registro = await e.Contexto.Ventas.SingleAsync(); registro.SucursalId = 3; registro.EmpresaId = 2; await e.Contexto.SaveChangesAsync();
        await Assert.ThrowsAsync<CajaNoEncontradaException>(() => e.Servicio.CobrarAsync(sesion.Encabezado.Id, solicitud, Seguridad, default));
        Assert.Empty(e.Contexto.Pagos); Assert.Single(e.Contexto.MovimientosCaja);
    }
    [Fact]
    public async Task FalloAperturaRevierteSesionMovimientoYNumeracion()
    {
        await using var e = await CrearAsync();
        var caja = await e.Servicio.CrearCajaAsync(new() { Codigo = "FALLO", Nombre = "Prueba rollback" }, Seguridad, default);
        await e.Contexto.Database.ExecuteSqlRawAsync("CREATE TRIGGER FalloApertura BEFORE INSERT ON Bitacoras WHEN NEW.Accion='CAJA_ABIERTA' BEGIN SELECT RAISE(ABORT,'Fallo deliberado'); END;");
        var solicitud = new AbrirCajaSolicitud { CajaId = caja.Id, MonedaId = 1, SaldoInicial = 100 };
        await Assert.ThrowsAsync<DbUpdateException>(() => e.Servicio.AbrirAsync(solicitud, Seguridad, default));
        Assert.Empty(e.Contexto.TurnosCaja); Assert.Empty(e.Contexto.MovimientosCaja);
        Assert.All(await e.Contexto.CorrelativosDocumentos.Where(x => x.SerieDocumento.TipoDocumento.ModuloId == "CAJA").ToArrayAsync(), x => Assert.Equal(0, x.UltimoNumero));
        await e.Contexto.Database.ExecuteSqlRawAsync("DROP TRIGGER FalloApertura;");
        Assert.Equal("ABIERTA", (await e.Servicio.AbrirAsync(solicitud, Seguridad, default)).Encabezado.Estado);
    }
    [Fact]
    public async Task FalloArqueoRevierteConteoYVersionDeSesion()
    {
        await using var e = await CrearAsync(); var sesion = await e.AbrirAsync();
        await e.Contexto.Database.ExecuteSqlRawAsync("CREATE TRIGGER FalloArqueo BEFORE INSERT ON Bitacoras WHEN NEW.Accion='ARQUEO_REGISTRADO' BEGIN SELECT RAISE(ABORT,'Fallo deliberado'); END;");
        await Assert.ThrowsAsync<DbUpdateException>(() => e.ArqueoAsync(sesion));
        Assert.Empty(e.Contexto.ArqueosCaja);
        Assert.Equal(sesion.Encabezado.VersionFila, (await e.Servicio.ObtenerAsync(sesion.Encabezado.Id, Seguridad, default)).Encabezado.VersionFila);
        await e.Contexto.Database.ExecuteSqlRawAsync("DROP TRIGGER FalloArqueo;");
        Assert.Single((await e.ArqueoAsync(sesion)).Arqueos);
    }
    private static async Task<string> HuellaInventarioAsync(EscenarioCaja e) => JsonSerializer.Serialize(new {
        Stock = await e.Contexto.Existencias.AsNoTracking().OrderBy(x => x.Id).Select(x => new { x.Id, x.Cantidad, x.CostoPromedio }).ToArrayAsync(),
        Kardex = await e.Contexto.Kardex.AsNoTracking().OrderBy(x => x.Id).Select(x => x.Id).ToArrayAsync(),
        Movimientos = await e.Contexto.Movimientos.AsNoTracking().OrderBy(x => x.Id).Select(x => x.Id).ToArrayAsync(),
        Costos = await e.Contexto.CostosProductos.CountAsync(), Historicos = await e.Contexto.HistoricoCostosProductos.CountAsync() });
}
