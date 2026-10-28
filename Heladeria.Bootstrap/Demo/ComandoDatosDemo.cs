using Heladeria.Aplicacion.Seguridad.Configuracion;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Heladeria.Bootstrap.Demo;

public static class ComandoDatosDemo
{
    public static async Task<int> EjecutarAsync(string[] args)
    {
        if (args.Count(x => x == "--cargar-datos-demo") != 1 || args.Count(x => x == "--confirmar-demo") > 1
            || args.Any(x => x is not ("--cargar-datos-demo" or "--confirmar-demo")))
        {
            Console.Error.WriteLine("Uso: --cargar-datos-demo [--confirmar-demo]. No admite credenciales ni IDs por argumentos.");
            return 2;
        }
        var etapa = "PREVALIDACION";
        try
        {
            var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { Args = [], ContentRootPath = AppContext.BaseDirectory });
            var confirmar = args.Contains("--confirmar-demo", StringComparer.Ordinal);
            CargadorDatosDemo.ValidarAmbiente(builder.Environment.EnvironmentName, confirmar);
            var cadena = builder.Configuration.GetConnectionString("Heladeria");
            if (string.IsNullOrWhiteSpace(cadena)) throw new DemoRechazadaException("Configure ConnectionStrings:Heladeria mediante el mecanismo existente; no incluya secretos en argumentos.");
            var destino = new SqlConnectionStringBuilder(cadena);
            Console.WriteLine($"Carga DEMO opcional. Ambiente: {builder.Environment.EnvironmentName}; servidor: {destino.DataSource}; base: {destino.InitialCatalog}.");
            Console.WriteLine("Se crearán datos ficticios y una caja independiente. Los correlativos avanzarán mediante servicios; no se alterarán usuarios, permisos ni turnos existentes.");
            await using var db = CargadorDatosDemo.CrearContexto(cadena);
            var opciones = Options.Create(builder.Configuration.GetSection(OpcionesSeguridad.Seccion).Get<OpcionesSeguridad>() ?? new());
            var resultado = await new CargadorDatosDemo(db, opciones, TimeProvider.System, e => {
                etapa = e; Console.WriteLine($"Etapa demo: {e}. Pendiente commit global.");
            }).EjecutarAsync(builder.Environment.EnvironmentName, confirmar);
            Console.WriteLine($"Carga confirmada: empresa {resultado.EmpresaId}, sucursal {resultado.SucursalId}; {resultado.Productos} productos, {resultado.Proveedores} proveedores, {resultado.Clientes} clientes, {resultado.Compras} compras, {resultado.Ventas} ventas, {resultado.Turnos} turnos cerrados, {resultado.CxC} CxC y {resultado.CxP} CxP.");
            Console.WriteLine($"Período local: {resultado.Desde:yyyy-MM-dd} a {resultado.Hasta:yyyy-MM-dd}. Identificador: {CargadorDatosDemo.Equipo}.");
            return 0;
        }
        catch (DemoRechazadaException e) { Console.Error.WriteLine(e.Message); Console.Error.WriteLine("Sin carga parcial."); return 2; }
        catch (SqlException e) when (e.Number == 51120) { Console.Error.WriteLine(e.Message); Console.Error.WriteLine("Sin carga parcial."); return 2; }
        catch (Exception) { Console.Error.WriteLine($"Carga demo no completada en {etapa}. Rollback global; sin detalles de conexión ni excepciones sensibles."); return 1; }
    }
}
