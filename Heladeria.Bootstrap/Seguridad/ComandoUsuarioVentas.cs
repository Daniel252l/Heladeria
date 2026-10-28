using Heladeria.Aplicacion.Seguridad.Autenticacion;
using Heladeria.Aplicacion.Seguridad.Bootstrap;
using Heladeria.Aplicacion.Seguridad.Configuracion;
using Heladeria.Aplicacion.Seguridad.Excepciones;
using Heladeria.Infraestructura;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Heladeria.Bootstrap.Seguridad;

internal static class ComandoUsuarioVentas
{
    public static async Task<int> EjecutarAsync(string[] args)
    {
        if (args.Length != 1 || Console.IsInputRedirected)
        {
            Console.Error.WriteLine("Uso en terminal interactiva: --crear-usuario-ventas. No se aceptan contraseñas por argumentos.");
            return 2;
        }
        try
        {
            var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { Args = [], ContentRootPath = AppContext.BaseDirectory });
            // Evita que proveedores de logging registren parámetros de la preparación.
            builder.Logging.ClearProviders();
            builder.Services.AddOptions<OpcionesSeguridad>().Bind(builder.Configuration.GetRequiredSection(OpcionesSeguridad.Seccion));
            builder.Services.AgregarInfraestructura(builder.Configuration, habilitarReintentosSql: false);
            using var app = builder.Build();
            using var scope = app.Services.CreateScope();
            var destino = new SqlConnectionStringBuilder(builder.Configuration.GetConnectionString("Heladeria"));
            Console.WriteLine($"Crear usuario operativo ventas · Ambiente: {builder.Environment.EnvironmentName}");
            Console.WriteLine($"Servidor: {destino.DataSource} · Base: {destino.InitialCatalog}");
            Console.WriteLine("Alcance: únicamente la empresa y sucursal principal registradas por la instalación inicial.");
            Console.WriteLine("Permisos: " + string.Join(", ", ProvisionadorUsuarioVentas.PermisosNecesarios));
            Console.Write("Escriba CREAR VENTAS para continuar: ");
            if (Console.ReadLine() != "CREAR VENTAS") { Console.WriteLine("Operación cancelada."); return 2; }
            var servicio = ActivatorUtilities.CreateInstance<ProvisionadorUsuarioVentas>(scope.ServiceProvider);
            var resultado = await servicio.EjecutarAsync(() => {
                var primera = LectorContrasena.Leer("Contraseña temporal: ");
                var segunda = LectorContrasena.Leer("Confirmar contraseña temporal: ");
                ValidadorPoliticaContrasena.Validar(primera, segunda, scope.ServiceProvider.GetRequiredService<IOptions<OpcionesSeguridad>>().Value.Contrasenas);
                return primera;
            }, CancellationToken.None);
            Console.WriteLine(resultado.Registros.Count == 0 ? "La cuenta ventas ya está preparada. No se realizaron cambios." : "Usuario preparado. Debe cambiar su contraseña temporal al iniciar sesión.");
            foreach (var r in resultado.Registros) Console.WriteLine($"- {r.Tabla}: {r.Identificador} · {r.Descripcion}");
            return 0;
        }
        catch (BootstrapAdministradorRechazadoException e) { Console.Error.WriteLine($"Preparación rechazada: {e.Message} Sin cambios."); return 2; }
        catch (ValidacionSeguridadException e) { Console.Error.WriteLine($"Contraseña rechazada: {e.Message} Sin cambios."); return 2; }
        catch (OperationCanceledException) { Console.Error.WriteLine("Operación cancelada. Sin cambios."); return 2; }
        catch (Exception) { Console.Error.WriteLine("No se pudo preparar el usuario ventas. Se revirtió la transacción; no se muestran datos sensibles."); return 1; }
    }
}
