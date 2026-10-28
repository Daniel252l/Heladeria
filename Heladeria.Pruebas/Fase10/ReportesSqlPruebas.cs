using Heladeria.Aplicacion.Configuracion;
using Heladeria.Aplicacion.Reportes;
using Heladeria.Aplicacion.Cartera;
using Heladeria.Infraestructura.Caja;
using Heladeria.Infraestructura.Cartera;
using Heladeria.Infraestructura.Inventario;
using Heladeria.Infraestructura.Fase4;
using Heladeria.Infraestructura.Persistencia;
using Heladeria.Infraestructura.Persistencia.Entidades;
using Heladeria.Infraestructura.ReportesOperativos;
using Heladeria.Infraestructura.Seguridad.Bootstrap;
using Heladeria.Pruebas.Fase5;
using Heladeria.Pruebas.Fase9;
using Heladeria.Pruebas.Integracion.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Heladeria.Pruebas.Fase10;

public sealed class ReportesSqlPruebas
{
    [Fact]
    public async Task SqlConDetalleInventarioKardexCompraRecibidaYCobroMixto()
    {
        await using var aislada = await BaseSqlServerAislada.CrearAsync();
        await using var db = new HeladeriaDbContext(new DbContextOptionsBuilder<HeladeriaDbContext>().UseSqlServer(aislada.CadenaConexion).Options);
        var (s, _, sesion, _) = await CarteraSqlPruebas.PrepararAsync(db, false);
        var config = EscenarioFase5.Opciones; var reloj = TimeProvider.System; var ahora = reloj.GetUtcNow().UtcDateTime;
        var activo = await db.Estados.Where(x => x.Nombre == "Activo").Select(x => x.Id).SingleAsync();
        var categoria = new Categorias { EmpresaId = s.EmpresaId, Codigo = "R", Nombre = "Reportes", Nivel = 1, EstadoId = activo, FechaCreacion = ahora, CreadoPorUsuarioId = s.UsuarioId };
        var tipo = new TiposProductos { EmpresaId = s.EmpresaId, Codigo = "INV", Nombre = "Inventariable", ManejaExistencia = true, EstadoId = activo, FechaCreacion = ahora, CreadoPorUsuarioId = s.UsuarioId };
        var unidad = new UnidadesMedida { EmpresaId = s.EmpresaId, Codigo = "UN", Nombre = "Unidad", Abreviatura = "un", EstadoId = activo, FechaCreacion = ahora, CreadoPorUsuarioId = s.UsuarioId };
        db.AddRange(categoria, tipo, unidad); await db.SaveChangesAsync();
        var producto = new Productos { EmpresaId = s.EmpresaId, CategoriaId = categoria.Id, TipoProductoId = tipo.Id, UnidadBaseId = unidad.Id,
            TipoSeguimientoId = await db.Catalogos.Where(x => x.Codigo == "NINGUNO").Select(x => x.Id).SingleAsync(),
            MetodoValuacionId = await db.Catalogos.Where(x => x.Codigo == "PROMEDIO").Select(x => x.Id).SingleAsync(),
            SKU = "REPORTE", Nombre = "Producto de reporte", EsVendible = true, EsComprable = true, PrecioVentaReferencial = 25,
            EstadoId = activo, FechaCreacion = ahora, CreadoPorUsuarioId = s.UsuarioId };
        db.Productos.Add(producto); await db.SaveChangesAsync();
        var anterior = await db.Ventas.SingleAsync();
        db.VentasDetalle.Add(new VentasDetalle { VentaId = anterior.Id, ProductoId = producto.Id, NumeroLinea = 1,
            Cantidad = 4, PrecioUnitario = 25, FactorConversion = 1, CantidadBase = 4, UnidadMedidaId = unidad.Id,
            UnidadBaseId = unidad.Id, ManejaExistencia = true, SubtotalNeto = 100, TotalLinea = 100, CostoUnitario = 2, CostoTotal = 8 });
        await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        var bodega = await db.Bodegas.Select(x => x.Id).SingleAsync(); var ubicacion = await db.Ubicaciones.Select(x => x.Id).SingleAsync();
        var coord = new CoordinadorAjustesInventario(); var inv = new ServicioInventario(db, new AuditoriaFase4(db, reloj), config, reloj, coord);
        await inv.AjustarAsync(new() { SucursalId = s.SucursalId, BodegaId = bodega, UbicacionId = ubicacion,
            ProductoId = producto.Id, Cantidad = 200, CostoUnitario = 2, Tipo = "ENTRADA", Referencia = "Fixture" }, s, default);
        var ventas = new Heladeria.Infraestructura.VentasPos.ServicioVentas(db, config, reloj, coord, inv);
        var moneda = await db.Monedas.Select(x => x.Id).SingleAsync();
        var venta = await ventas.CrearAsync(new() { SucursalId = s.SucursalId, BodegaId = bodega, UbicacionId = ubicacion,
            MonedaId = moneda, Detalles = [new() { ProductoId = producto.Id, Cantidad = 2 }] }, s, default);
        venta = await ventas.ConfirmarAsync(venta.Encabezado.Id, new() { VersionFila = venta.Encabezado.VersionFila }, s, default);
        var efectivo = await db.FormasPago.Where(x => x.Codigo == "EFECTIVO").Select(x => x.Id).SingleAsync();
        var tarjeta = await db.FormasPago.Where(x => x.Codigo == "TARJETA").Select(x => x.Id).SingleAsync();
        venta = await ventas.CompletarAsync(venta.Encabezado.Id, new() { VersionFila = venta.Encabezado.VersionFila, ClaveOperacion = Guid.NewGuid(),
            Cobro = new() { SesionCajaId = sesion.Encabezado.Id, VersionCaja = sesion.Encabezado.VersionFila, Pagos = [
                new() { FormaPagoId = efectivo, MontoAplicado = 20, MontoRecibido = 20 },
                new() { FormaPagoId = tarjeta, MontoAplicado = 30, MontoRecibido = 30, Referencia = "AUT-TEST" }] } }, s, default);
        Assert.NotNull(venta.Cobro);
        db.ChangeTracker.Clear();
        var r = new ServicioReportes(db, config, reloj);
        var diario = await r.DiarioAsync(new() { TamanioPagina = 1 }, s, default);
        Assert.Equal(50, diario.CobrosPorForma.Sum(x => x.Aplicado));
        Assert.Equal(50, Assert.Single(diario.Finanzas).CobrosVentas);
        Assert.Equal(500, diario.Finanzas[0].Aperturas);
        Assert.Equal(520, Assert.Single(diario.Cajas.Elementos).EfectivoEsperado);
        Assert.Single(diario.Ventas.Elementos);
        Assert.Equal(150, Assert.Single(diario.VentasTotales).Total); // Incluye también la venta de crédito de la preparación.
        Assert.Equal(2, diario.Ventas.Elementos[0].FormasPago.Count);
        Assert.NotNull(venta.Comprobante); Assert.Equal(2, venta.Comprobante.Pagos.Count);
        var filtrado = await r.DiarioAsync(new() { FormaPagoId = tarjeta, CajaId = sesion.Encabezado.CajaId, EsCredito = false }, s, default);
        Assert.Equal(30, Assert.Single(filtrado.CobrosPorForma).Aplicado);
        Assert.Equal(30, Assert.Single(filtrado.Finanzas).Ingresos); Assert.Equal(0, filtrado.Finanzas[0].Aperturas);
        Assert.Equal(50, Assert.Single(filtrado.VentasTotales).Total); Assert.Empty(filtrado.CreditosGenerados);
        Assert.Equal(520, Assert.Single(filtrado.Cajas.Elementos).EfectivoEsperado); // El filtro de forma no falsifica el saldo del turno.
        var creditos = await r.DiarioAsync(new() { EsCredito = true }, s, default);
        Assert.Equal(100, Assert.Single(creditos.VentasTotales).Total); Assert.Empty(creditos.CobrosPorForma);
        // La fixture preexistente emite CxC el 09/10, aunque completa la venta con el reloj actual.
        // Consultar la fecha civil guardada, no atribuir esa emisión al día del cobro/cierre.
        var fechaEmision = await db.CuentasPorCobrar.Select(x => x.FechaEmision).SingleAsync();
        var emitidos = await r.DiarioAsync(new() { Fecha = fechaEmision, EsCredito = true }, s, default);
        Assert.Equal(100, Assert.Single(emitidos.CreditosGenerados).ImporteOriginal);
        if (fechaEmision != creditos.Periodo.FechaDesde) Assert.Empty(creditos.CreditosGenerados);
        var resultado = await r.VentasAsync(new() { TamanioPagina = 1 }, s, default);
        Assert.Equal(150, Assert.Single(resultado.Ventas.Totales).Total); Assert.Equal(12, resultado.Ventas.Totales[0].Costo);
        Assert.Equal(138, resultado.Ventas.Totales[0].MargenBruto);
        Assert.Equal(2, resultado.Ventas.Datos.TotalRegistros); Assert.Equal(50, resultado.CobrosPorForma.Sum(x => x.Aplicado));
        var productos = await r.ProductosAsync(new(), s, default); Assert.Equal(6, Assert.Single(productos.Elementos).Cantidad);
        Assert.Equal(12, productos.Elementos[0].Costo);
        Assert.Equal(396, (await r.InventarioAsync(new(), s, default)).ValorInventario);
        var kardex = await r.KardexAsync(new(), s, default); Assert.Equal(2, kardex.TotalRegistros);
        Assert.Equal("SALIDA", kardex.Elementos[0].Tipo); Assert.Equal(198, kardex.Elementos[0].SaldoCantidad);
        var cajaReporte = await r.CajaAsync(new(), s, default); var sesionReporte = Assert.Single(cajaReporte.Datos.Elementos);
        Assert.Equal(50, sesionReporte.IngresosOperativos); Assert.Equal(520, sesionReporte.EfectivoEsperado); Assert.Equal(550, sesionReporte.SaldoTeorico);
        Assert.Equal(2, cajaReporte.Totales.Count);
        Assert.Empty(db.ChangeTracker.Entries());
        var compras = new Heladeria.Infraestructura.Compras.ServicioCompras(db, config, reloj, coord, inv);
        var compra = await compras.GuardarAsync(null, new() { EsCredito = true, SucursalId = s.SucursalId,
            ProveedorId = await db.Proveedores.Select(x => x.Id).SingleAsync(), MonedaId = moneda,
            FechaEmision = new(2026, 10, 19), Detalles = [new() { ProductoId = producto.Id, Cantidad = 2, PrecioUnitario = 30 }] }, s, default);
        await compras.CambiarEstadoAsync(compra.Encabezado.Id, "CONFIRMADA", s, default);
        await compras.RecibirAsync(compra.Encabezado.Id, new() { ClaveOperacion = Guid.NewGuid(), BodegaId = bodega,
            UbicacionId = ubicacion, Detalles = [new() { OrdenCompraDetalleId = compra.Detalles[0].Id, Cantidad = 2 }] }, s, default);
        db.ChangeTracker.Clear();
        var informeCompra = await r.ComprasAsync(new() { FechaDesde = new(2026, 10, 1), FechaHasta = new(2026, 10, 31) }, s, default);
        var filaCompra = Assert.Single(informeCompra.Datos.Elementos); Assert.Equal(1, filaCompra.LineasRecibidasCompletas);
        Assert.Equal(1, filaCompra.Recepciones); Assert.True(filaCompra.EsCredito); Assert.Null(filaCompra.SaldoPorPagar);
        Assert.Equal(60, Assert.Single(informeCompra.Totales).Total);
    }
    [Theory, InlineData(false), InlineData(true)]
    public async Task ReportesRealesSqlTotalesPagosAuditoriaSinEscriturasYBootstrapRepetible(bool pagar)
    {
        await using var aislada = await BaseSqlServerAislada.CrearAsync();
        await using var db = new HeladeriaDbContext(new DbContextOptionsBuilder<HeladeriaDbContext>().UseSqlServer(aislada.CadenaConexion).Options);
        var (s, cuenta, sesion, forma) = await CarteraSqlPruebas.PrepararAsync(db, pagar);
        var prep = new PreparadorFase10(db, EscenarioFase5.Opciones, TimeProvider.System);
        var primera = await prep.EjecutarAsync(default); Assert.NotEmpty(primera.Registros);
        Assert.Empty((await prep.EjecutarAsync(default)).Registros);
        Assert.Equal(2, await db.RolesPermisos.CountAsync(x => x.RoleId == "ADMIN" && (x.Permiso.Codigo == "REPORTES.CONSULTAR" || x.Permiso.Codigo == "AUDITORIA.CONSULTAR")));
        var coord = new CoordinadorAjustesInventario(); var caja = new ServicioCaja(db, EscenarioFase5.Opciones, TimeProvider.System, coord);
        var cartera = new ServicioCartera(db, EscenarioFase5.Opciones, TimeProvider.System, coord, caja);
        await cartera.AplicarAsync(pagar, cuenta.Encabezado.Id, new AplicarPagoCarteraSolicitud {
            ClaveOperacion = Guid.NewGuid(), VersionFila = cuenta.Encabezado.VersionFila, SesionCajaId = sesion.Encabezado.Id,
            VersionCaja = sesion.Encabezado.VersionFila, FormaPagoId = forma, Monto = 40 }, s, default);
        var servicio = new ServicioReportes(db, EscenarioFase5.Opciones, TimeProvider.System);
        var diario = await servicio.DiarioAsync(new(), s, default);
        Assert.Equal(pagar ? 0 : 40, diario.CobrosPorForma.Sum(x => x.Aplicado));
        Assert.Equal(pagar ? 40 : 0, Assert.Single(diario.Finanzas).PagosCxp);
        Assert.Equal(pagar ? 0 : 40, diario.Finanzas[0].CobrosCxc);
        Assert.Equal(pagar ? 460 : 540, Assert.Single(diario.Cajas.Elementos).EfectivoEsperado);
        var desde = new DateOnly(2026, 1, 1); var hasta = new DateOnly(2026, 12, 31);
        db.ChangeTracker.Clear();
        var pagosAntes = await db.Pagos.CountAsync(); var bitacorasAntes = await db.Bitacoras.CountAsync();
        var versionesAntes = await db.TurnosCaja.AsNoTracking().Select(x => x.VersionFila).ToArrayAsync();
        var c = await servicio.CarteraAsync(pagar, new() { FechaDesde = desde, FechaHasta = hasta }, s, default);
        Assert.Equal(60, Assert.Single(c.Totales).Pendiente);
        Assert.Equal(40, Assert.Single(c.Datos.Elementos).Aplicado);
        Assert.Equal("VIGENTE", c.Datos.Elementos[0].Clasificacion);
        var saldoCaja = await servicio.CajaAsync(new() { FechaDesde = desde, FechaHasta = hasta }, s, default);
        var fila = Assert.Single(saldoCaja.Datos.Elementos);
        Assert.Equal(500, fila.SaldoInicial); Assert.Equal(pagar ? 0 : 40, fila.IngresosOperativos);
        Assert.Equal(pagar ? 40 : 0, fila.EgresosOperativos); Assert.Equal(pagar ? 460 : 540, fila.EfectivoEsperado);
        Assert.Equal(500, Assert.Single(saldoCaja.Totales).Apertura);
        var compras = await servicio.ComprasAsync(new() { FechaDesde = desde, FechaHasta = hasta }, s, default);
        if (pagar) { Assert.Equal(60, Assert.Single(compras.Datos.Elementos).SaldoPorPagar); Assert.True(compras.Datos.Elementos[0].EsCredito); }
        else Assert.Empty(compras.Datos.Elementos);
        var ventas = await servicio.VentasAsync(new() { FechaDesde = desde, FechaHasta = hasta }, s, default);
        if (!pagar)
        {
            Assert.Equal(100, Assert.Single(ventas.Ventas.Totales).Total);
            Assert.Null(ventas.Ventas.Totales[0].Costo); // No fabricar costo para un histórico sin detalles.
            Assert.Equal("PARCIAL", Assert.Single(ventas.Ventas.Datos.Elementos).EstadoCobro);
            Assert.Equal(40, Assert.Single(ventas.CobrosPorForma).Aplicado);
        }
        Assert.Empty((await servicio.ProductosAsync(new() { FechaDesde = desde, FechaHasta = hasta }, s, default)).Elementos);
        Assert.Empty((await servicio.InventarioAsync(new(), s, default)).Datos.Elementos);
        Assert.Empty((await servicio.KardexAsync(new() { FechaDesde = desde, FechaHasta = hasta }, s, default)).Elementos);
        var auditoria = await servicio.AuditoriaAsync(new() { FechaDesde = desde, FechaHasta = hasta, Modulo = "AUDITORIA" }, s, default);
        Assert.Equal("FASE10_CONFIGURADA", Assert.Single(auditoria.Elementos).Accion);
        Assert.Empty(db.ChangeTracker.Entries());
        Assert.Equal(pagosAntes, await db.Pagos.CountAsync()); Assert.Equal(bitacorasAntes, await db.Bitacoras.CountAsync());
        Assert.Equal(versionesAntes[0], (await db.TurnosCaja.AsNoTracking().Select(x => x.VersionFila).ToArrayAsync())[0]);

        var vencido = new ServicioReportes(db, EscenarioFase5.Opciones, new RelojVencido());
        var vencidas = await vencido.CarteraAsync(pagar, new() { FechaDesde = desde, FechaHasta = hasta, Vencimiento = "VENCIDA" }, s, default);
        Assert.Equal(60, Assert.Single(vencidas.Totales).Vencido); Assert.Equal("VENCIDA", Assert.Single(vencidas.Datos.Elementos).Clasificacion);
        var actual = await cartera.ObtenerAsync(pagar, cuenta.Encabezado.Id, s, default);
        var cajaActual = await caja.ObtenerAsync(sesion.Encabezado.Id, s, default);
        await cartera.AplicarAsync(pagar, cuenta.Encabezado.Id, new() { ClaveOperacion = Guid.NewGuid(), Monto = 60,
            VersionFila = actual.Encabezado.VersionFila, SesionCajaId = sesion.Encabezado.Id,
            VersionCaja = cajaActual.Encabezado.VersionFila, FormaPagoId = forma }, s, default);
        var pagadas = await servicio.CarteraAsync(pagar, new() { FechaDesde = desde, FechaHasta = hasta, Vencimiento = "PAGADA" }, s, default);
        Assert.Equal("PAGADA", Assert.Single(pagadas.Datos.Elementos).Clasificacion); Assert.Equal(0, Assert.Single(pagadas.Totales).Pendiente);
        Assert.Empty((await servicio.CarteraAsync(pagar, new() { FechaDesde = desde, FechaHasta = hasta, TerceroId = 9999 }, s, default)).Datos.Elementos);

        // Incompatibilidad al final debe revertir también cualquier reparación anterior de la misma transacción.
        var rol = await db.RolesPermisos.SingleAsync(x => x.Permiso.Codigo == "AUDITORIA.CONSULTAR"); rol.Permitido = false;
        var reporte = await db.Permisos.SingleAsync(x => x.Codigo == "REPORTES.CONSULTAR");
        db.RolesPermisos.RemoveRange(db.RolesPermisos.Where(x => x.PermisoId == reporte.Id)); db.Permisos.Remove(reporte);
        await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        await Assert.ThrowsAsync<Heladeria.Aplicacion.Seguridad.Bootstrap.BootstrapAdministradorRechazadoException>(() => prep.EjecutarAsync(default));
        Assert.False(await db.Permisos.AnyAsync(x => x.Codigo == "REPORTES.CONSULTAR"));
    }
    private sealed class RelojVencido : TimeProvider
    { public override DateTimeOffset GetUtcNow() => new(2026, 12, 1, 12, 0, 0, TimeSpan.Zero); }
}
