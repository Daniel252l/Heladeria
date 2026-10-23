using Heladeria.Aplicacion.Ventas;
using Heladeria.Infraestructura.Inventario;
using Heladeria.Infraestructura.Persistencia.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Heladeria.Pruebas.Fase7;

public sealed class VentasPruebas
{
    [Fact]
    public async Task BorradorSnapshotPrecioConversionCorrelativoSinAfectarStock()
    {
        await using var e = await EscenarioFase7.CrearAsync();
        var a = await e.CrearAsync(EscenarioFase7.Solicitud(descuento: 10));
        var b = await e.CrearAsync(EscenarioFase7.Solicitud(presentacion: null));
        Assert.Equal("VT-0000000001", a.Encabezado.NumeroVenta);
        Assert.Equal("VT-0000000002", b.Encabezado.NumeroVenta);
        Assert.Equal("BORRADOR", a.Encabezado.Estado);
        Assert.Null(a.Encabezado.ClienteId); Assert.Equal("GTQ", a.Encabezado.Moneda); Assert.Equal(1, a.Encabezado.TipoCambio);
        var l = Assert.Single(a.Detalles);
        Assert.Equal(1000, l.FactorConversion); Assert.Equal(2000, l.CantidadBase);
        Assert.Equal(25, l.PrecioUnitario); Assert.Equal("PRESENTACION", l.FuentePrecio);
        Assert.Equal(45, a.Encabezado.Total); Assert.Null(l.CostoUnitarioBase);
        Assert.Equal(1, b.Detalles[0].FactorConversion); Assert.Null(b.Detalles[0].ProductoPresentacionId);
        Assert.Equal("PRODUCTO_REFERENCIAL", b.Detalles[0].FuentePrecio);
        Assert.Empty(e.Contexto.Movimientos); Assert.Equal(5000, await Saldo(e));
    }

    [Theory]
    [InlineData("no-vendible")]
    [InlineData("inactivo")]
    [InlineData("presentacion-no-vendible")]
    [InlineData("presentacion-inactiva")]
    [InlineData("sin-precio")]
    [InlineData("precio-cero")]
    [InlineData("descuento-prohibido")]
    [InlineData("combo")]
    [InlineData("vencimiento")]
    [InlineData("categoria-ajena")]
    public async Task RechazaConfiguracionInvalidaSinCrearVenta(string caso)
    {
        await using var e = await EscenarioFase7.CrearAsync();
        var p = await e.Contexto.Productos.SingleAsync(x => x.Id == 1);
        var pp = await e.Contexto.ProductoPresentaciones.SingleAsync(x => x.Id == 1);
        switch (caso)
        {
            case "no-vendible": p.EsVendible = false; break;
            case "inactivo": p.EstadoId = 2; break;
            case "presentacion-no-vendible": pp.PermiteVenta = false; break;
            case "presentacion-inactiva": pp.EstadoId = 2; break;
            case "sin-precio": pp.PrecioVentaEstandar = null; break;
            case "precio-cero": pp.PrecioVentaEstandar = 0; break;
            case "descuento-prohibido": p.PermiteDescuento = false; break;
            case "combo": p.EsCombo = true; break;
            case "vencimiento": p.ControlaVencimiento = true; break;
            case "categoria-ajena": p.CategoriaId = 2; break;
        }
        await e.Contexto.SaveChangesAsync(); e.Contexto.ChangeTracker.Clear();
        await Assert.ThrowsAsync<ValidacionVentasException>(() => e.CrearAsync(EscenarioFase7.Solicitud(descuento: 10)));
        Assert.Empty(e.Contexto.Ventas); Assert.Empty(e.Contexto.VentasDetalle);
        Assert.Equal(0, (await e.Contexto.CorrelativosDocumentos.SingleAsync(x => x.SerieDocumento.Serie == "VT")).UltimoNumero);
    }

    [Fact]
    public async Task ImpuestoSnapshotIncluidoSeMantieneAlCambiarCatalogo()
    {
        await using var e = await EscenarioFase7.CrearAsync();
        e.Contexto.Impuestos.Add(new() { Id = 1, EmpresaId = 1, Codigo = "IVA", Nombre = "IVA", Porcentaje = 12,
            IncluidoEnPrecio = true, FechaVigenciaDesde = new(2020, 1, 1), EstadoId = 1 });
        e.Contexto.ProductoImpuestos.Add(new() { ProductoId = 1, ImpuestoId = 1, EstadoId = 1 });
        (await e.Contexto.ProductoPresentaciones.SingleAsync(x => x.Id == 1)).PrecioVentaEstandar = 112;
        await e.Contexto.SaveChangesAsync(); e.Contexto.ChangeTracker.Clear();
        var v = await e.CrearAsync(EscenarioFase7.Solicitud(cantidad: 1, descuento: 10));
        Assert.Equal(100, v.Encabezado.Subtotal); Assert.Equal(10, v.Encabezado.Descuento);
        Assert.Equal(10.8m, v.Encabezado.Impuesto); Assert.Equal(100.8m, v.Encabezado.Total);
        Assert.Equal(10.8m, (await e.Contexto.VentaImpuestos.SingleAsync()).Monto);
        (await e.Contexto.Impuestos.SingleAsync()).Porcentaje = 15;
        await e.Contexto.SaveChangesAsync(); e.Contexto.ChangeTracker.Clear();
        var leida = await e.Ventas.ObtenerAsync(v.Encabezado.Id, EscenarioFase7.Seguridad, default);
        Assert.Equal(12, Assert.Single(leida.Detalles[0].Impuestos).Porcentaje);
        await Assert.ThrowsAsync<ConflictoVentasException>(() => e.Ventas.ConfirmarAsync(v.Encabezado.Id,
            new() { VersionFila = v.Encabezado.VersionFila }, EscenarioFase7.Seguridad, default));
    }

    [Fact]
    public async Task CierreCostoBaseKardexNoCambiaCostoProductoYCobroImpideAnulacionSinReembolso()
    {
        await using var e = await EscenarioFase7.CrearAsync();
        var v = await e.ConfirmarAsync();
        Assert.Equal(5000, await Saldo(e));
        v = await e.CompletarAsync(v);
        Assert.Equal("COMPLETADA", v.Encabezado.Estado); Assert.Equal(3000, await Saldo(e));
        Assert.Equal(0.01m, v.Detalles[0].CostoUnitarioBase); Assert.Equal(20, v.Detalles[0].CostoTotal);
        Assert.Single(e.Contexto.Kardex); Assert.Single(v.Movimientos); Assert.Empty(e.Contexto.CostosProductos);
        Assert.Empty(e.Contexto.HistoricoCostosProductos);
        var m = await e.Contexto.MovimientoDetalles.SingleAsync();
        Assert.Equal(2000, m.CantidadBase); Assert.Equal(1000, m.FactorConversion); Assert.Equal(0.01m, m.CostoUnitario);
        Assert.Contains(e.Contexto.Bitacoras, x => x.Accion == "VENTA_INVENTARIO_DESCONTADO");
        Assert.Contains("cobrada", (await Assert.ThrowsAsync<ConflictoVentasException>(() => e.AnularAsync(v))).Message);
        Assert.Equal(3000, await Saldo(e)); Assert.Single(e.Contexto.Kardex); Assert.Single(e.Contexto.Movimientos);
        Assert.Single(e.Contexto.Pagos); Assert.Single(e.Contexto.MovimientosCaja);
    }

    [Fact]
    public async Task AnulacionUsaCostoOriginalAunqueHuboRecepcionPosterior()
    {
        await using var e = await EscenarioFase7.CrearAsync();
        var v = await e.HistoricaSinCobroAsync(await e.ConfirmarAsync());
        await e.Inventario.AjustarAsync(new() { SucursalId = 1, BodegaId = 1, UbicacionId = 1,
            ProductoId = 1, Tipo = "ENTRADA", Cantidad = 1000, CostoUnitario = 0.03m }, EscenarioFase7.Seguridad, default);
        v = await e.AnularAsync(v);
        var existencia = await e.Contexto.Existencias.AsNoTracking().SingleAsync(x => x.Id == 1);
        Assert.Equal(6000, existencia.Cantidad); Assert.Equal(0.013333m, existencia.CostoPromedio);
        var reverso = await e.Contexto.MovimientoDetalles.AsNoTracking().SingleAsync(x => x.Movimientos.SerieDocumento == "VA");
        Assert.Equal(0.01m, reverso.CostoUnitario); Assert.Equal(2000, reverso.CantidadBase);
        Assert.Contains(e.Contexto.Bitacoras, x => x.Accion == "VENTA_INVENTARIO_REVERTIDO");
        await Assert.ThrowsAsync<ConflictoVentasException>(() => e.AnularAsync(v));
        await Assert.ThrowsAsync<ConflictoVentasException>(() => e.CompletarAsync(v, v.Encabezado.ClaveOperacion));
    }

    [Fact]
    public async Task IdempotenciaNoDuplicaYClaveNoSeReutilizaEnOtraVenta()
    {
        await using var e = await EscenarioFase7.CrearAsync();
        var v = await e.ConfirmarAsync(); var clave = Guid.NewGuid();
        var cerrada = await e.CompletarAsync(v, clave);
        var repetida = await e.Ventas.CompletarAsync(v.Encabezado.Id, new() { ClaveOperacion = clave,
            VersionFila = "AAAA", Cobro = await e.CobroAsync(v) }, EscenarioFase7.Seguridad, default);
        Assert.Equal(cerrada.Encabezado, repetida.Encabezado); Assert.Equal(3000, await Saldo(e));
        Assert.Single(e.Contexto.Kardex);
        await Assert.ThrowsAsync<ConflictoVentasException>(() => e.CompletarAsync(cerrada));
        var otra = await e.ConfirmarAsync();
        await Assert.ThrowsAsync<ConflictoVentasException>(() => e.CompletarAsync(otra, clave));
        Assert.Equal(3000, await Saldo(e));
    }

    [Fact]
    public async Task StockInsuficienteConsideraReservasYMultiplesPresentaciones()
    {
        await using var e = await EscenarioFase7.CrearAsync();
        var solicitud = new CrearVentaSolicitud { SucursalId = 1, BodegaId = 1, UbicacionId = 1, MonedaId = 1,
            Detalles = [new() { ProductoId = 1, ProductoPresentacionId = 1, Cantidad = 4 },
                new() { ProductoId = 1, ProductoPresentacionId = 2, Cantidad = 5 }] };
        var v = await e.ConfirmarAsync(solicitud);
        await Assert.ThrowsAsync<StockVentaInsuficienteException>(() => e.CompletarAsync(v));
        Assert.Empty(e.Contexto.Movimientos); Assert.Empty(e.Contexto.Kardex); Assert.Equal(5000, await Saldo(e));
        var saldo = await e.Contexto.Existencias.SingleAsync(x => x.Id == 1);
        saldo.CantidadReservada = 4000;
        await e.Contexto.SaveChangesAsync(); e.Contexto.ChangeTracker.Clear();
        var otra = await e.ConfirmarAsync();
        await Assert.ThrowsAsync<StockVentaInsuficienteException>(() => e.CompletarAsync(otra));
        Assert.Empty(e.Contexto.Movimientos);
    }

    [Theory]
    [InlineData(2, 2, 1)]
    [InlineData(3, 3, 1)]
    [InlineData(1, 2, 1)]
    [InlineData(1, 1, 2)]
    public async Task RechazaBodegaUbicacionOSucursalAjena(long bodega, long ubicacion, long sucursal)
    {
        await using var e = await EscenarioFase7.CrearAsync();
        await Assert.ThrowsAsync<VentaNoEncontradaException>(() => e.CrearAsync(EscenarioFase7.Solicitud(
            bodega: bodega, ubicacion: ubicacion, sucursal: sucursal)));
        Assert.Empty(e.Contexto.Ventas);
    }

    [Fact]
    public async Task IdorLecturaYEscrituraNoCruzaEmpresaOSucursal()
    {
        await using var e = await EscenarioFase7.CrearAsync(); var v = await e.ConfirmarAsync();
        foreach (var s in new[] { EscenarioFase7.Seguridad with { SucursalId = 2 }, EscenarioFase7.Seguridad with { EmpresaId = 2 } })
        {
            await Assert.ThrowsAsync<VentaNoEncontradaException>(() => e.Ventas.ObtenerAsync(v.Encabezado.Id, s, default));
            await Assert.ThrowsAsync<VentaNoEncontradaException>(() => e.Ventas.CompletarAsync(v.Encabezado.Id,
                new() { VersionFila = v.Encabezado.VersionFila, ClaveOperacion = Guid.NewGuid() }, s, default));
        }
        await Assert.ThrowsAsync<VentaNoEncontradaException>(() => e.Ventas.ObtenerAsync(long.MaxValue, EscenarioFase7.Seguridad, default));
        Assert.Equal(5000, await Saldo(e));
    }

    [Fact]
    public async Task VersionObsoletaRechazaEdicionYCierre()
    {
        await using var e = await EscenarioFase7.CrearAsync(); var v = await e.ConfirmarAsync();
        await Assert.ThrowsAsync<ConflictoVentasException>(() => e.Ventas.CompletarAsync(v.Encabezado.Id,
            new() { VersionFila = "Ag==", ClaveOperacion = Guid.NewGuid() }, EscenarioFase7.Seguridad, default));
        await Assert.ThrowsAsync<ValidacionVentasException>(() => e.Ventas.CompletarAsync(v.Encabezado.Id,
            new() { VersionFila = "no base64!", ClaveOperacion = Guid.NewGuid() }, EscenarioFase7.Seguridad, default));
        await Assert.ThrowsAsync<ValidacionVentasException>(() => e.CompletarAsync(v, Guid.Empty));
    }

    [Fact]
    public async Task EdicionSoloBorradorYDosVentasNoCruzanEntidades()
    {
        await using var e = await EscenarioFase7.CrearAsync();
        var a = await e.CrearAsync(EscenarioFase7.Solicitud()); var b = await e.CrearAsync(EscenarioFase7.Solicitud());
        foreach (var v in new[] { b, a })
            await e.Ventas.ActualizarAsync(v.Encabezado.Id, new() { SucursalId = 1, BodegaId = 1, UbicacionId = 1,
                MonedaId = 1, VersionFila = v.Encabezado.VersionFila,
                Detalles = [new() { ProductoId = 1, ProductoPresentacionId = 1, Cantidad = v.Encabezado.Id + 1 }] }, EscenarioFase7.Seguridad, default);
        Assert.Equal(50, (await e.Ventas.ObtenerAsync(a.Encabezado.Id, EscenarioFase7.Seguridad, default)).Encabezado.Total);
        Assert.Equal(75, (await e.Ventas.ObtenerAsync(b.Encabezado.Id, EscenarioFase7.Seguridad, default)).Encabezado.Total);
        b = await e.AnularAsync(b);
        Assert.Empty(b.Movimientos); Assert.Equal(5000, await Saldo(e));
    }

    [Fact]
    public async Task NoInventariableSeVendeSinStockNiCostoInventario()
    {
        await using var e = await EscenarioFase7.CrearAsync();
        (await e.Contexto.TiposProductos.SingleAsync(x => x.Id == 1)).ManejaExistencia = false;
        await e.Contexto.SaveChangesAsync(); e.Contexto.ChangeTracker.Clear();
        var v = await e.CompletarAsync(await e.ConfirmarAsync());
        Assert.Equal(0, v.Detalles[0].CostoTotal); Assert.Empty(v.Movimientos); Assert.Equal(5000, await Saldo(e));
        await Assert.ThrowsAsync<ConflictoVentasException>(() => e.AnularAsync(v)); Assert.Equal(5000, await Saldo(e));
    }

    [Fact]
    public async Task ConcurrenciaDosVentasNoSobrevendeMismoStock()
    {
        var nombre = $"concurrencia-ventas-{Guid.NewGuid():N}"; var coordinador = new CoordinadorAjustesInventario();
        await using var e = await EscenarioFase7.CrearAsync(nombre, true, coordinador);
        var a = await e.ConfirmarAsync(EscenarioFase7.Solicitud(4)); var b = await e.ConfirmarAsync(EscenarioFase7.Solicitud(4));
        await using var otro = await EscenarioFase7.CrearAsync(nombre, false, coordinador);
        static async Task<bool> Intentar(EscenarioFase7 contexto, VentaConsultaDto venta)
        { try { await contexto.CompletarAsync(venta); return true; } catch (StockVentaInsuficienteException) { return false; } }
        var resultados = await Task.WhenAll(Task.Run(() => Intentar(e, a)), Task.Run(() => Intentar(otro, b)));
        Assert.Single(resultados, x => x); Assert.Equal(1000, await Saldo(e)); Assert.Single(e.Contexto.Kardex);
    }

    [Theory]
    [InlineData("HEL-CHO", 1)]
    [InlineData("Vainilla", 1)]
    [InlineData("inexistente", 0)]
    public async Task PosBuscaSkuNombre(string texto, int cantidad)
    {
        await using var e = await EscenarioFase7.CrearAsync();
        var r = await e.Ventas.ProductosAsync(new() { Busqueda = texto }, EscenarioFase7.Seguridad, default);
        Assert.Equal(cantidad, r.TotalRegistros);
    }

    [Fact]
    public async Task PosPresentacionesPrecioStockPaginacionYFiltros()
    {
        await using var e = await EscenarioFase7.CrearAsync();
        var r = await e.Ventas.ProductosAsync(new() { TamanioPagina = 1, BodegaId = 1, UbicacionId = 1, CategoriaId = 1 }, EscenarioFase7.Seguridad, default);
        Assert.Equal(2, r.TotalRegistros); Assert.Single(r.Elementos); Assert.Equal(2, r.TotalPaginas);
        Assert.Equal(5000, r.Elementos[0].DisponibleBase);
        var p = await e.Ventas.PresentacionesAsync(1, EscenarioFase7.Seguridad, default);
        Assert.Equal(3, p.Presentaciones.Count); Assert.Null(p.Presentaciones[0].ProductoPresentacionId);
        Assert.Equal(25, p.Presentaciones.Single(x => x.ProductoPresentacionId == 1).Precio);
        Assert.Equal(1000, p.Presentaciones.Single(x => x.ProductoPresentacionId == 1).FactorConversion);
        (await e.Contexto.Productos.SingleAsync(x => x.Id == 1)).EsVendible = false;
        (await e.Contexto.Productos.SingleAsync(x => x.Id == 2)).EstadoId = 2;
        await e.Contexto.SaveChangesAsync(); e.Contexto.ChangeTracker.Clear();
        Assert.Empty((await e.Ventas.ProductosAsync(new(), EscenarioFase7.Seguridad, default)).Elementos);
    }

    [Fact]
    public async Task PreparacionIdempotenteNoReiniciaCorrelativosNiCreaVentas()
    {
        await using var e = await EscenarioFase7.CrearAsync();
        Assert.Empty(e.Contexto.Ventas);
        var v = await e.CrearAsync(EscenarioFase7.Solicitud());
        var resultado = await e.Preparador.EjecutarAsync(default);
        Assert.Empty(resultado.Registros);
        Assert.Equal(1, (await e.Contexto.CorrelativosDocumentos.SingleAsync(x => x.SerieDocumento.Serie == "VT")).UltimoNumero);
        Assert.Equal(5, await e.Contexto.Permisos.CountAsync(x => x.ModuloId == "VENTAS" || x.ModuloId == "POS"));
        Assert.All(await e.Contexto.RolesPermisos.ToArrayAsync(), x => Assert.Equal("ADMIN", x.RoleId));
        Assert.Single(e.Contexto.Bitacoras.Where(x => x.Accion == "FASE7_CONFIGURADA"));
        Assert.Equal(v.Encabezado.Id, (await e.Contexto.Ventas.SingleAsync()).Id);
    }

    private static async Task<decimal> Saldo(EscenarioFase7 e) => (await e.Contexto.Existencias.AsNoTracking().SingleAsync(x => x.Id == 1)).Cantidad;
}
