using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Heladeria.Aplicacion.Caja;
using Heladeria.Aplicacion.Cartera;
using Heladeria.Infraestructura.Persistencia;
using Heladeria.Infraestructura.Persistencia.Entidades;
using Heladeria.Infraestructura.Seguridad.Autenticacion;
using Heladeria.Pruebas.Integracion.Persistencia;
using Heladeria.Pruebas.Seguridad;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Heladeria.Pruebas.Fase9;

public sealed class CuentaPagadaUuidHttpPruebas
{
    [Theory]
    [InlineData(false, false), InlineData(true, false)]
    [InlineData(false, true), InlineData(true, true)]
    public async Task CuentaDeOchoPagadaConTresYCincoRechazaUuidNuevaYRespetaReintentosExactos(bool pagar, bool desarrollo)
    {
        // SQL Server real en instancia/base privadas desechables, sin conexiones de la aplicación.
        await using var aislada = await BaseSqlServerAislada.CrearAsync();
        var opciones = new DbContextOptionsBuilder<HeladeriaDbContext>().UseSqlServer(aislada.CadenaConexion).Options;
        await using var db = new HeladeriaDbContext(opciones);
        var (s, cuenta, sesion, forma) = await CarteraSqlPruebas.PrepararAsync(db, pagar, importe: 8, saldoInicial: 50);
        var seguridadPrueba = EscenarioSeguridad.CrearOpciones();
        await using var fabrica = new FabricaApiSeguridad(true, servicios => {
            servicios.RemoveAll<HeladeriaDbContext>(); servicios.RemoveAll<DbContextOptions<HeladeriaDbContext>>();
            servicios.AddScoped(_ => new HeladeriaDbContext(opciones));
            // Development no debe firmar/validar con secretos del equipo. Mantiene las validaciones JWT,
            // pero ambos extremos del fixture usan exclusivamente la clave ficticia de pruebas existente.
            servicios.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, jwt => {
                jwt.TokenValidationParameters.ValidIssuer = seguridadPrueba.Jwt.Emisor;
                jwt.TokenValidationParameters.ValidAudience = seguridadPrueba.Jwt.Audiencia;
                jwt.TokenValidationParameters.IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(seguridadPrueba.Jwt.ClaveFirma));
            });
        });
        await using var app = fabrica.WithWebHostBuilder(builder => builder.UseEnvironment(desarrollo ? "Development" : "Testing"));
        using var http = app.CreateClient(new WebApplicationFactoryClientOptions {
            BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        Assert.Equal(desarrollo, app.Services.GetRequiredService<IHostEnvironment>().IsDevelopment());
        var jwt = new GeneradorTokensJwt(Options.Create(seguridadPrueba), TimeProvider.System)
            .CrearAccessToken(new Usuarios { Id = s.UsuarioId, UserName = "admin", RoleId = s.RolId },
                await db.Sucursales.AsNoTracking().SingleAsync(x => x.Id == s.SucursalId), 1, false);
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt.Valor);
        var ruta = $"/api/cuentas-por-{(pagar ? "pagar" : "cobrar")}/{cuenta.Encabezado.Id}";
        var operacion = ruta + (pagar ? "/pagos" : "/abonos");
        var rutaCaja = $"/api/caja/sesiones/{sesion.Encabezado.Id}";
        if (desarrollo)
        {
            using var swagger = JsonDocument.Parse(await http.GetStringAsync("/swagger/v1/swagger.json"));
            Assert.True(swagger.RootElement.GetProperty("paths")
                .GetProperty($"/api/cuentas-por-{(pagar ? "pagar" : "cobrar")}/{{id}}/{(pagar ? "pagos" : "abonos")}")
                .TryGetProperty("post", out _));
        }
        AplicarPagoCarteraSolicitud Solicitud(decimal monto, Guid? clave = null) => new() {
            ClaveOperacion = clave ?? Guid.NewGuid(), Monto = monto, FormaPagoId = forma,
            SesionCajaId = sesion.Encabezado.Id, VersionFila = cuenta.Encabezado.VersionFila,
            VersionCaja = sesion.Encabezado.VersionFila, Referencia = "REGRESION-UUID" };
        async Task<ResultadoPagoCarteraDto> Aplicar(AplicarPagoCarteraSolicitud solicitud)
        {
            using var respuesta = await http.PostAsJsonAsync(operacion, solicitud);
            Assert.True(respuesta.StatusCode == HttpStatusCode.OK, await respuesta.Content.ReadAsStringAsync());
            return (await respuesta.Content.ReadFromJsonAsync<ResultadoPagoCarteraDto>())!;
        }
        async Task Rechazar(AplicarPagoCarteraSolicitud solicitud, string codigo)
        {
            using var respuesta = await http.PostAsJsonAsync(operacion, solicitud);
            var contenido = await respuesta.Content.ReadAsStringAsync();
            Assert.True(respuesta.StatusCode == HttpStatusCode.Conflict, contenido);
            using var json = JsonDocument.Parse(contenido);
            Assert.False(json.RootElement.GetProperty("exito").GetBoolean());
            Assert.Equal(codigo, json.RootElement.GetProperty("codigo").GetString());
            Assert.False(json.RootElement.TryGetProperty("pagoId", out _));
        }
        async Task<string> FotoFinanciera()
        {
            // Solo excluye la bitácora de rechazo prevista: todos los datos financieros deben conservarse.
            return JsonSerializer.Serialize(new {
                Cuenta = await http.GetFromJsonAsync<DetalleCarteraDto>(ruta),
                Caja = await http.GetFromJsonAsync<SesionCajaDto>(rutaCaja),
                Pagos = await db.Pagos.AsNoTracking().OrderBy(x => x.Id).Select(x => new {
                    x.Id, x.ClaveOperacion, x.MontoTotal, x.HuellaSolicitud, x.Fecha, x.EstadoId, x.TurnoCajaId }).ToArrayAsync(),
                Detalles = await db.PagoDetalles.AsNoTracking().OrderBy(x => x.Id).Select(x => new {
                    x.Id, x.PagoId, x.FormaPagoId, x.Monto, x.MontoRecibido, x.Referencia }).ToArrayAsync(),
                Correlativos = await db.CorrelativosDocumentos.AsNoTracking().OrderBy(x => x.Id)
                    .Select(x => new { x.Id, x.UltimoNumero, x.FechaModificacion }).ToArrayAsync()
            });
        }

        var solicitudTres = Solicitud(3);
        var tres = await Aplicar(solicitudTres);
        Assert.False(tres.Reintento); Assert.Equal("PARCIAL", tres.Cuenta.Encabezado.Estado);
        Assert.Equal(5, tres.Cuenta.Encabezado.SaldoPendiente);
        cuenta = tres.Cuenta; sesion = (await http.GetFromJsonAsync<SesionCajaDto>(rutaCaja))!;
        var solicitudCinco = Solicitud(5);
        var cinco = await Aplicar(solicitudCinco);
        Assert.False(cinco.Reintento); Assert.NotEqual(tres.PagoId, cinco.PagoId);
        cuenta = (await http.GetFromJsonAsync<DetalleCarteraDto>(ruta))!;
        sesion = (await http.GetFromJsonAsync<SesionCajaDto>(rutaCaja))!;
        Assert.Equal("PAGADA", cuenta.Encabezado.Estado); Assert.Equal(0, cuenta.Encabezado.SaldoPendiente);
        Assert.Equal(8, cuenta.Encabezado.ImporteOriginal); Assert.Equal(8, cuenta.Encabezado.TotalAplicado);
        Assert.Equal(2, cuenta.Historial.Count); Assert.Equal(pagar ? 42 : 58, sesion.EfectivoEsperado);
        var antes = await FotoFinanciera();

        // Cuerpo literal del incidente de Swagger. Se envía SOLO al host de pruebas y a su cuenta aislada.
        // Al estar pagada, debe rechazarse antes de evaluar las versiones/Id de Caja de esa captura.
        var uuidIncidente = Guid.Parse("a77c133e-0d1a-4c7b-9c15-51b77be7f411");
        Assert.False(await db.Pagos.AnyAsync(x => x.ClaveOperacion == uuidIncidente));
        using (var cuerpo = new StringContent("""
            {
              "claveOperacion": "a77c133e-0d1a-4c7b-9c15-51b77be7f411",
              "versionFila": "AAAAAAAEbOk=",
              "sesionCajaId": 4,
              "versionCaja": "AAAAAAAEbOo=",
              "formaPagoId": 1,
              "monto": 1.00,
              "referencia": null,
              "observacion": "Intento controlado sobre cuenta pagada"
            }
            """, Encoding.UTF8, "application/json"))
        using (var respuesta = await http.PostAsync(operacion, cuerpo))
        {
            var contenido = await respuesta.Content.ReadAsStringAsync();
            Assert.True(respuesta.StatusCode == HttpStatusCode.Conflict, contenido);
            using var json = JsonDocument.Parse(contenido);
            Assert.False(json.RootElement.GetProperty("exito").GetBoolean());
            Assert.Equal("CUENTA_YA_PAGADA", json.RootElement.GetProperty("codigo").GetString());
            Assert.False(json.RootElement.TryGetProperty("pagoId", out _));
        }
        Assert.Equal(antes, await FotoFinanciera());
        Assert.False(await db.Pagos.AnyAsync(x => x.ClaveOperacion == uuidIncidente));

        var nueva = Solicitud(1);
        Assert.NotEqual(solicitudTres.ClaveOperacion, nueva.ClaveOperacion);
        Assert.NotEqual(solicitudCinco.ClaveOperacion, nueva.ClaveOperacion);
        await Rechazar(nueva, "CUENTA_YA_PAGADA");
        Assert.Equal(antes, await FotoFinanciera());
        Assert.False(await db.Pagos.AnyAsync(x => x.ClaveOperacion == nueva.ClaveOperacion));

        // Repetir tanto el último pago como el primero: jamás sustituirlo por el último del historial.
        foreach (var (original, pagoId) in new[] { (solicitudCinco, cinco.PagoId), (solicitudTres, tres.PagoId) })
        {
            var reintento = await Aplicar(original);
            Assert.True(reintento.Reintento); Assert.Equal(pagoId, reintento.PagoId);
            Assert.Equal("PAGADA", reintento.Cuenta.Encabezado.Estado);
            Assert.Equal(antes, await FotoFinanciera());

            var distinta = new AplicarPagoCarteraSolicitud {
                ClaveOperacion = original.ClaveOperacion, Monto = 1, FormaPagoId = original.FormaPagoId,
                SesionCajaId = original.SesionCajaId, VersionFila = original.VersionFila,
                VersionCaja = original.VersionCaja, Referencia = original.Referencia, Observacion = original.Observacion };
            await Rechazar(distinta, "CLAVE_OPERACION_CONFLICTIVA");
            Assert.Equal(antes, await FotoFinanciera());
        }
        Assert.Equal(2, await db.Pagos.CountAsync()); Assert.Equal(2, await db.PagoDetalles.CountAsync());
        Assert.Equal(3, await db.MovimientosCaja.CountAsync()); // Apertura + Q3 + Q5, sin un cuarto movimiento.
        Assert.Equal(pagar ? 2 : 0, await db.AplicacionesCuentasPorPagar.CountAsync());
        Assert.Equal(pagar ? 0 : 2, await db.AplicacionesCuentasPorCobrar.CountAsync());
    }
}
