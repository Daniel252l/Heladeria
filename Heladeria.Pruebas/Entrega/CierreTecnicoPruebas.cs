using System.Security.Cryptography;
using System.Text.Json;
using System.Xml.Linq;
using Heladeria.Aplicacion.Seguridad.Bootstrap;
using Heladeria.Aplicacion.Seguridad.Configuracion;
using Heladeria.Infraestructura;
using Heladeria.Infraestructura.Persistencia;
using Heladeria.Pruebas.Integracion.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Heladeria.Pruebas.Entrega;

public sealed class CierreTecnicoPruebas
{
    private static string Raiz()
    {
        var directorio = new DirectoryInfo(AppContext.BaseDirectory);
        while (directorio is not null && !File.Exists(Path.Combine(directorio.FullName, "Heladeria.sln")))
            directorio = directorio.Parent;
        return directorio?.FullName ?? throw new InvalidOperationException("No se encontró la solución.");
    }

    [Theory]
    [InlineData("appsettings.json")]
    [InlineData("appsettings.Production.json")]
    public void ConfiguracionPublicableNoContieneConexionNiClave(string nombre)
    {
        using var json = JsonDocument.Parse(File.ReadAllText(Path.Combine(Raiz(), "Heladeria.Api", nombre)));
        Assert.Equal("", json.RootElement.GetProperty("ConnectionStrings").GetProperty("Heladeria").GetString());
        Assert.Empty(json.RootElement.GetProperty("Cors").GetProperty("OrigenesPermitidos").EnumerateArray());
        if (json.RootElement.TryGetProperty("Seguridad", out var seguridad))
            Assert.Equal("", seguridad.GetProperty("Jwt").GetProperty("ClaveFirma").GetString());
    }

    [Fact]
    public void PublicacionApiExcluyeAmbientesNoProductivos()
    {
        var proyecto = XDocument.Load(Path.Combine(Raiz(), "Heladeria.Api", "Heladeria.Api.csproj"));
        var contenido = proyecto.Descendants("Content").ToArray();
        Assert.Contains(contenido, x => (string?)x.Attribute("Update") == "appsettings.*.json"
            && (string?)x.Attribute("CopyToPublishDirectory") == "Never");
        Assert.Contains(contenido, x => (string?)x.Attribute("Update") == "appsettings.Production.json"
            && (string?)x.Attribute("CopyToPublishDirectory") == "PreserveNewest");
    }

    [Fact]
    public void PublicacionBootstrapConservaProduccionYExcluyeDevelopment()
    {
        var proyecto = XDocument.Load(Path.Combine(Raiz(), "Heladeria.Bootstrap", "Heladeria.Bootstrap.csproj"));
        var contenido = proyecto.Descendants("Content").ToArray();
        Assert.Contains(contenido, x => (string?)x.Attribute("Link") == "appsettings.Development.json"
            && (string?)x.Attribute("CopyToPublishDirectory") == "Never");
        Assert.Contains(contenido, x => (string?)x.Attribute("Link") == "appsettings.Production.json"
            && (string?)x.Attribute("CopyToPublishDirectory") == "PreserveNewest");
    }

    [Fact]
    [Trait("Categoria", "Integracion")]
    public async Task InstalacionLimpiaYPreparadoresCompletos_SinDatosDemoNiOperaciones()
    {
        // Instancia y BD nuevas con GUID. No se lee appsettings, entorno ni User Secrets.
        await using var aislada = await BaseSqlServerAislada.CrearAsync();
        var configuracion = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Heladeria"] = aislada.CadenaConexion
        }).Build();
        var servicios = new ServiceCollection();
        servicios.AddLogging();
        servicios.AddSingleton(Options.Create(new OpcionesSeguridad { EstadosActivos = ["Activo"] }));
        servicios.AgregarInfraestructura(configuracion, habilitarReintentosSql: false);
        await using var proveedor = servicios.BuildServiceProvider();
        async Task Ejecutar<T>(Func<T, Task> operacion) where T : notnull
        {
            await using var scope = proveedor.CreateAsyncScope();
            await operacion(scope.ServiceProvider.GetRequiredService<T>());
        }
        await Ejecutar<HeladeriaDbContext>(async db =>
        {
            var filas = await db.Database.SqlQuery<long>($"SELECT COALESCE(SUM(p.rows),0) AS [Value] FROM sys.partitions p JOIN sys.tables t ON p.object_id=t.object_id WHERE p.index_id IN (0,1)").SingleAsync();
            Assert.Equal(0L, filas); // Maestro = estructura, ningún catálogo duplicado ni dato operativo.
        });
        var contrasena = "Aislada-" + Convert.ToHexString(RandomNumberGenerator.GetBytes(16)) + "a1!";
        var solicitud = new SolicitudBootstrapAdministrador("admin", "admin@prueba.local", contrasena);
        await Ejecutar<IBootstrapAdministrador>(x => x.EjecutarAsync(solicitud, default));
        await Ejecutar<IPreparadorFase3>(x => x.EjecutarAsync(default));
        await Ejecutar<IPreparadorFase4>(x => x.EjecutarAsync(default));
        await Ejecutar<IPreparadorFase5>(x => x.EjecutarAsync(default));
        await Ejecutar<IPreparadorFase6>(x => x.EjecutarAsync(default));
        await Ejecutar<IPreparadorFase7>(x => x.EjecutarAsync(default));
        await Ejecutar<IPreparadorFase8>(x => x.EjecutarAsync(default));
        await Ejecutar<IPreparadorFase9>(x => x.EjecutarAsync(default));
        await Ejecutar<IPreparadorFase10>(x => x.EjecutarAsync(default));
        await Ejecutar<IBootstrapAdministrador>(x => Assert.ThrowsAsync<BootstrapAdministradorRechazadoException>(() => x.EjecutarAsync(solicitud, default)));
        await Ejecutar<IPreparadorFase3>(x => Assert.ThrowsAsync<BootstrapAdministradorRechazadoException>(() => x.EjecutarAsync(default)));
        await Ejecutar<IPreparadorFase4>(x => Assert.ThrowsAsync<BootstrapAdministradorRechazadoException>(() => x.EjecutarAsync(default)));
        await Ejecutar<IPreparadorFase5>(async x => Assert.Empty((await x.EjecutarAsync(default)).Registros));
        await Ejecutar<IPreparadorFase6>(async x => Assert.Empty((await x.EjecutarAsync(default)).Registros));
        await Ejecutar<IPreparadorFase7>(async x => Assert.Empty((await x.EjecutarAsync(default)).Registros));
        await Ejecutar<IPreparadorFase8>(async x => Assert.Empty((await x.EjecutarAsync(default)).Registros));
        await Ejecutar<IPreparadorFase9>(async x => Assert.Empty((await x.EjecutarAsync(default)).Registros));
        await Ejecutar<IPreparadorFase10>(async x => Assert.Empty((await x.EjecutarAsync(default)).Registros));
        await Ejecutar<HeladeriaDbContext>(async db =>
        {
            var usuario = Assert.Single(await db.Usuarios.ToArrayAsync());
            Assert.True(usuario.ContraseniaTemporal);
            Assert.Null(usuario.PasswordTemporal);
            Assert.Null(usuario.UltimoCambioDeContrasenia);
            Assert.NotEqual(contrasena, usuario.PasswordHash);
            Assert.False(string.IsNullOrEmpty(usuario.SecurityStamp));
            Assert.All(await db.RolesPermisos.ToArrayAsync(), x => Assert.Equal("ADMIN", x.RoleId));
            foreach (var codigo in new[] { "CONFIGURACION.ADMINISTRAR", "VENTAS.VENDER", "CAJA.CONSULTAR", "CXC.COBRAR", "CXP.PAGAR", "REPORTES.CONSULTAR", "AUDITORIA.CONSULTAR" })
                Assert.True(await db.RolesPermisos.AnyAsync(x => x.RoleId == "ADMIN" && x.Permitido && x.Permiso.Codigo == codigo));
            Assert.Empty(await db.Clientes.ToArrayAsync());
            Assert.Empty(await db.Productos.ToArrayAsync());
            Assert.Empty(await db.Monedas.ToArrayAsync());
            Assert.Empty(await db.Cajas.ToArrayAsync());
            Assert.Empty(await db.Ventas.ToArrayAsync());
            Assert.Empty(await db.OrdenesCompra.ToArrayAsync());
            Assert.Empty(await db.OrdenesProduccion.ToArrayAsync());
            Assert.Empty(await db.TurnosCaja.ToArrayAsync());
            Assert.Empty(await db.Pagos.ToArrayAsync());
            Assert.Empty(await db.CuentasPorCobrar.ToArrayAsync());
            Assert.Empty(await db.CuentasPorPagar.ToArrayAsync());
            Assert.Empty(await db.Movimientos.ToArrayAsync());
            Assert.Empty(await db.Kardex.ToArrayAsync());
            Assert.Empty(await db.SesionesUsuarios.ToArrayAsync());
            Assert.Equal("PROV-INICIAL", Assert.Single(await db.Proveedores.ToArrayAsync()).CodigoProveedor);
        });
    }
}
