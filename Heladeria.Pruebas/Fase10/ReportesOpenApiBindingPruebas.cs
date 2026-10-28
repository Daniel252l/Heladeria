using System.Net;
using System.Text.Json;
using Heladeria.Aplicacion.Configuracion;
using Heladeria.Aplicacion.Reportes;
using Heladeria.Infraestructura.ReportesOperativos;
using Heladeria.Pruebas.Fase5;
using Heladeria.Pruebas.Fase7;
using Heladeria.Pruebas.Seguridad;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Heladeria.Pruebas.Fase10;

public sealed class ReportesOpenApiBindingPruebas
{
    private static readonly JsonSerializerOptions JsonWeb = new(JsonSerializerDefaults.Web);
    [Fact]
    public async Task DiarioBindingNativoTodosLosFiltrosYDefaults()
    {
        var espia = new ServicioCaptura();
        await using var fabrica = CrearFabricaBinding(espia); using var c = fabrica.CrearClienteAutenticado();
        var respuesta = await c.GetAsync("/api/reportes/diario?fecha=2026-10-19&sucursalId=4&cajaId=7&formaPagoId=2&esCredito=false&pagina=2&paginaOperaciones=3&paginaCajas=4&tamanioPagina=5");
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode); Assert.Equal("no-store", respuesta.Headers.CacheControl?.ToString());
        var f = Assert.IsType<FiltroDiario>(espia.Diario);
        Assert.Equal(new DateOnly(2026, 10, 19), f.Fecha); Assert.Equal(4, f.SucursalId); Assert.Equal(7, f.CajaId);
        Assert.Equal(2, f.FormaPagoId); Assert.False(f.EsCredito); Assert.Equal(2, f.Pagina); Assert.Equal(3, f.PaginaOperaciones); Assert.Equal(4, f.PaginaCajas); Assert.Equal(5, f.TamanioPagina);
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync("/api/reportes/diario")).StatusCode);
        Assert.Equal(JsonSerializer.Serialize(new FiltroDiario()), JsonSerializer.Serialize(espia.Diario));
    }
    public static TheoryData<string> Rutas => ReportesHttpPruebas.Rutas;

    [Theory, MemberData(nameof(Rutas))]
    public async Task OpenApiNativoPublicaCadaFiltroQueryConTiposYValidaciones(string ruta)
    {
        await using var fabrica = new FabricaCookieRefresh();
        using var cliente = fabrica.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        using var swagger = JsonDocument.Parse(await cliente.GetStringAsync("/swagger/v1/swagger.json"));
        var operacion = swagger.RootElement.GetProperty("paths").GetProperty(ruta).GetProperty("get");
        Assert.False(operacion.TryGetProperty("requestBody", out _));
        var parametros = operacion.GetProperty("parameters").EnumerateArray()
            .ToDictionary(x => x.GetProperty("name").GetString()!, x => x);
        // ApiExplorer publica las propiedades del DTO sin necesitar un filtro Swagger ni renombrarlas.
        // El envío HTTP en camelCase se comprueba por separado contra el binding MVC real.
        var esperados = CrearFiltro(ruta).GetType().GetProperties().Select(x => x.Name);
        Assert.Equal(esperados.Order(), parametros.Keys.Order());
        foreach (var parametro in parametros.Values)
        {
            Assert.Equal("query", parametro.GetProperty("in").GetString());
            Assert.True(!parametro.TryGetProperty("required", out var requerido) || !requerido.GetBoolean());
            Assert.DoesNotContain(".", parametro.GetProperty("name").GetString()!);
        }
        Assert.Equal(1, parametros["Pagina"].GetProperty("schema").GetProperty("minimum").GetInt32());
        Assert.Equal(1, parametros["TamanioPagina"].GetProperty("schema").GetProperty("minimum").GetInt32());
        Assert.Equal(100, parametros["TamanioPagina"].GetProperty("schema").GetProperty("maximum").GetInt32());
        Assert.Equal("integer", parametros["SucursalId"].GetProperty("schema").GetProperty("type").GetString());
        if (ruta != "/api/reportes/inventario")
            foreach (var fecha in new[] { "FechaDesde", "FechaHasta" })
            {
                Assert.Equal("string", parametros[fecha].GetProperty("schema").GetProperty("type").GetString());
                Assert.Equal("date", parametros[fecha].GetProperty("schema").GetProperty("format").GetString());
            }
        if (ruta == "/api/reportes/compras")
            Assert.Equal("boolean", parametros["EsCredito"].GetProperty("schema").GetProperty("type").GetString());
    }

    [Theory, MemberData(nameof(Rutas))]
    public async Task QueryRealLlegaAlServicioConTodosLosValoresSinPrefijo(string ruta)
    {
        var esperado = CrearFiltro(ruta);
        var espia = new ServicioCaptura();
        await using var fabrica = CrearFabricaBinding(espia);
        using var cliente = fabrica.CrearClienteAutenticado();
        var propiedades = JsonSerializer.SerializeToElement(esperado, esperado.GetType(), JsonWeb);
        var query = propiedades.EnumerateObject().ToDictionary(x => x.Name,
            x => x.Value.ValueKind == JsonValueKind.String ? x.Value.GetString() : x.Value.GetRawText());
        using var respuesta = await cliente.GetAsync(QueryHelpers.AddQueryString(ruta, query));
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal(1, espia.Llamadas);
        Assert.IsType(esperado.GetType(), espia.Filtro);
        Assert.Equal(JsonSerializer.Serialize(esperado, esperado.GetType()),
            JsonSerializer.Serialize(espia.Filtro, espia.Filtro!.GetType()));
        Assert.Equal(1, espia.Contexto!.SucursalId); // El filtro solicitado (4) no sustituye el contexto autenticado.
        if (ruta.Contains("cuentas-por-", StringComparison.Ordinal))
            Assert.Equal(ruta.EndsWith("pagar", StringComparison.Ordinal), espia.Pagar);
    }

    [Theory, MemberData(nameof(Rutas))]
    public async Task SinQueryConservaLosDefaultsYNoInventaFiltros(string ruta)
    {
        var espia = new ServicioCaptura();
        await using var fabrica = CrearFabricaBinding(espia);
        using var cliente = fabrica.CrearClienteAutenticado();
        using var respuesta = await cliente.GetAsync(ruta);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var tipo = CrearFiltro(ruta).GetType();
        Assert.IsType(tipo, espia.Filtro);
        Assert.Equal(1, espia.Filtro!.Pagina);
        Assert.Equal(20, espia.Filtro.TamanioPagina);
        Assert.Equal(JsonSerializer.Serialize(Activator.CreateInstance(tipo), tipo), JsonSerializer.Serialize(espia.Filtro, tipo));
    }

    [Fact]
    public async Task ServicioRealConservaSucursalActivaUltimosTreintaDiasYVentasCompletadas()
    {
        await using var escenario = await EscenarioFase7.CrearAsync();
        await escenario.CompletarAsync(await escenario.ConfirmarAsync());
        await escenario.ConfirmarAsync();
        var reloj = new ReportesServicioPruebas.RelojFijo();
        foreach (var venta in await escenario.Contexto.Ventas.ToArrayAsync()) venta.FechaVenta = reloj.GetUtcNow().UtcDateTime;
        await escenario.Contexto.SaveChangesAsync();
        var servicio = new ServicioReportes(escenario.Contexto, EscenarioFase5.Opciones, reloj);
        var resultado = await servicio.VentasAsync(new FiltroVentasReporte(), EscenarioFase5.Seguridad, default);
        Assert.Equal(EscenarioFase5.Seguridad.SucursalId, resultado.Ventas.Periodo.SucursalId);
        Assert.Equal(new DateOnly(2026, 9, 20), resultado.Ventas.Periodo.FechaDesde);
        Assert.Equal(new DateOnly(2026, 10, 19), resultado.Ventas.Periodo.FechaHasta);
        Assert.Equal(1, resultado.Ventas.Datos.Pagina);
        Assert.Equal(20, resultado.Ventas.Datos.TamanioPagina);
        Assert.Equal("COMPLETADA", Assert.Single(resultado.Ventas.Datos.Elementos).Estado);
    }

    private static FabricaApiSeguridad CrearFabricaBinding(ServicioCaptura espia) => new(true, servicios =>
    {
        servicios.RemoveAll<IServicioReportes>();
        servicios.AddSingleton<IServicioReportes>(espia);
    });

    private static FiltroPaginadoReporte CrearFiltro(string ruta) => ruta switch
    {
        "/api/reportes/ventas" => new FiltroVentasReporte { SucursalId = 4, FechaDesde = new(2026, 10, 1), FechaHasta = new(2026, 10, 19),
            ClienteId = 7, Estado = "CONFIRMADA", MonedaId = 2, Busqueda = "helado crema & café", Pagina = 3, TamanioPagina = 7 },
        "/api/reportes/productos-vendidos" => new FiltroProductosVendidos { SucursalId = 4, FechaDesde = new(2026, 10, 1), FechaHasta = new(2026, 10, 19),
            ProductoId = 8, ClienteId = 7, Estado = "COMPLETADA", MonedaId = 2, Busqueda = "helado crema & café", Pagina = 3, TamanioPagina = 7 },
        "/api/reportes/inventario" => new FiltroInventarioReporte { SucursalId = 4, ProductoId = 8, BodegaId = 6, UbicacionId = 5,
            Busqueda = "helado crema & café", Pagina = 3, TamanioPagina = 7 },
        "/api/reportes/kardex" => new FiltroKardexReporte { SucursalId = 4, FechaDesde = new(2026, 10, 1), FechaHasta = new(2026, 10, 19),
            ProductoId = 8, BodegaId = 6, Concepto = "COMPRA", Tipo = "ENTRADA", Busqueda = "helado crema & café", Pagina = 3, TamanioPagina = 7 },
        "/api/reportes/compras" => new FiltroComprasReporte { SucursalId = 4, FechaDesde = new(2026, 10, 1), FechaHasta = new(2026, 10, 19),
            ProveedorId = 7, MonedaId = 2, Estado = "RECIBIDA", EsCredito = true, Busqueda = "helado crema & café", Pagina = 3, TamanioPagina = 7 },
        "/api/reportes/caja" => new FiltroCajaReporte { SucursalId = 4, FechaDesde = new(2026, 10, 1), FechaHasta = new(2026, 10, 19),
            CajaId = 6, UsuarioId = 5, Estado = "CERRADA", Busqueda = "helado crema & café", Pagina = 3, TamanioPagina = 7 },
        "/api/reportes/cuentas-por-cobrar" or "/api/reportes/cuentas-por-pagar" => new FiltroCarteraReporte { SucursalId = 4,
            FechaDesde = new(2026, 10, 1), FechaHasta = new(2026, 10, 19), TerceroId = 7, MonedaId = 2, Estado = "PAGADA",
            Vencimiento = "PAGADA", Busqueda = "helado crema & café", Pagina = 3, TamanioPagina = 7 },
        "/api/auditoria" => new FiltroAuditoria { SucursalId = 4, FechaDesde = new(2026, 10, 1), FechaHasta = new(2026, 10, 19),
            UsuarioId = 5, Modulo = "VENTAS", Accion = "CREADA", Entidad = "Ventas", Busqueda = "helado crema & café", Pagina = 3, TamanioPagina = 7 },
        _ => throw new ArgumentOutOfRangeException(nameof(ruta))
    };

    private sealed class ServicioCaptura : IServicioReportes
    {
        public FiltroDiario? Diario { get; private set; }
        public Task<ReporteDiarioDto> DiarioAsync(FiltroDiario f, ContextoConfiguracion s, CancellationToken t) {
            Diario = f;
            return Task.FromResult(new ReporteDiarioDto(new(new(2026, 10, 19), new(2026, 10, 19), "America/Guatemala", s.SucursalId),
                DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow, "Sucursal", [], [], [], [], new([], f.Pagina, f.TamanioPagina, 0), new([], f.PaginaOperaciones, f.TamanioPagina, 0), new([], f.PaginaCajas, f.TamanioPagina, 0)));
        }
        public FiltroPaginadoReporte? Filtro { get; private set; }
        public ContextoConfiguracion? Contexto { get; private set; }
        public bool? Pagar { get; private set; }
        public int Llamadas { get; private set; }
        private Task<T> Capturar<T>(FiltroPaginadoReporte filtro, ContextoConfiguracion contexto, T resultado)
        {
            Filtro = filtro; Contexto = contexto; Llamadas++;
            return Task.FromResult(resultado);
        }
        private static RespuestaPaginada<T> Pagina<T>(FiltroPaginadoReporte f) => new([], f.Pagina, f.TamanioPagina, 0);
        private static ReporteDto<T, TResumen> Reporte<T, TResumen>(FiltroReporte f) =>
            new(Pagina<T>(f), [], new(new(2026, 10, 1), new(2026, 10, 19), "America/Guatemala", f.SucursalId ?? 1));
        public Task<ReporteVentasDto> VentasAsync(FiltroVentasReporte f, ContextoConfiguracion s, CancellationToken t) =>
            Capturar(f, s, new ReporteVentasDto(Reporte<VentaReporteDto, TotalVentasDto>(f), []));
        public Task<RespuestaPaginada<ProductoVendidoDto>> ProductosAsync(FiltroProductosVendidos f, ContextoConfiguracion s, CancellationToken t) =>
            Capturar(f, s, Pagina<ProductoVendidoDto>(f));
        public Task<ReporteInventarioDto> InventarioAsync(FiltroInventarioReporte f, ContextoConfiguracion s, CancellationToken t) =>
            Capturar(f, s, new ReporteInventarioDto(Pagina<InventarioReporteDto>(f), 0, null, f.SucursalId ?? s.SucursalId));
        public Task<RespuestaPaginada<KardexReporteDto>> KardexAsync(FiltroKardexReporte f, ContextoConfiguracion s, CancellationToken t) =>
            Capturar(f, s, Pagina<KardexReporteDto>(f));
        public Task<ReporteDto<CompraReporteDto, TotalComprasDto>> ComprasAsync(FiltroComprasReporte f, ContextoConfiguracion s, CancellationToken t) =>
            Capturar(f, s, Reporte<CompraReporteDto, TotalComprasDto>(f));
        public Task<ReporteDto<SesionCajaReporteDto, FormaCajaReporteDto>> CajaAsync(FiltroCajaReporte f, ContextoConfiguracion s, CancellationToken t) =>
            Capturar(f, s, Reporte<SesionCajaReporteDto, FormaCajaReporteDto>(f));
        public Task<ReporteDto<CuentaReporteDto, TotalCarteraReporteDto>> CarteraAsync(bool pagar, FiltroCarteraReporte f, ContextoConfiguracion s, CancellationToken t)
        {
            Pagar = pagar;
            return Capturar(f, s, Reporte<CuentaReporteDto, TotalCarteraReporteDto>(f));
        }
        public Task<RespuestaPaginada<AuditoriaDto>> AuditoriaAsync(FiltroAuditoria f, ContextoConfiguracion s, CancellationToken t) =>
            Capturar(f, s, Pagina<AuditoriaDto>(f));
    }
}
