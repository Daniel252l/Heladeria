using System.Security.Cryptography;
using System.Runtime.InteropServices;
using System.Text;
using Heladeria.Aplicacion.Seguridad.Bootstrap;
using Heladeria.Aplicacion.Seguridad.Configuracion;
using Heladeria.Aplicacion.Seguridad.Excepciones;
using Heladeria.Infraestructura;
using Heladeria.Infraestructura.Seguridad.Bootstrap;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// Ruta opcional independiente: nunca prepara seguridad ni solicita/modifica contraseñas.
if (args.Contains("--cargar-datos-demo", StringComparer.Ordinal))
    return await Heladeria.Bootstrap.Demo.ComandoDatosDemo.EjecutarAsync(args);

if (args.Contains("--crear-usuario-ventas", StringComparer.Ordinal))
    return await Heladeria.Bootstrap.Seguridad.ComandoUsuarioVentas.EjecutarAsync(args);

const string opcionUsuarioConsulta = "--crear-usuario-consulta";
const string opcionPrepararFase3 = "--preparar-fase3";
const string opcionPrepararFase4 = "--preparar-fase4";
const string opcionPrepararFase5 = "--preparar-fase5";
const string opcionPrepararFase6 = "--preparar-fase6";
const string opcionPrepararFase7 = "--preparar-fase7";
const string opcionPrepararFase8 = "--preparar-fase8";
const string opcionPrepararFase9 = "--preparar-fase9";
var prepararFase9 = args.Length == 1 && args[0] == opcionPrepararFase9;
var prepararFase10 = args.Length == 1 && args[0] == "--preparar-fase10";

var crearUsuarioConsulta = args.Length == 1
    && string.Equals(args[0], opcionUsuarioConsulta, StringComparison.Ordinal);
var prepararFase3 = args.Length == 1
    && string.Equals(args[0], opcionPrepararFase3, StringComparison.Ordinal);
var prepararFase4 = args.Length == 1
    && string.Equals(args[0], opcionPrepararFase4, StringComparison.Ordinal);
var prepararFase5 = args.Length == 1
    && string.Equals(args[0], opcionPrepararFase5, StringComparison.Ordinal);
var prepararFase8 = args.Length == 1 && string.Equals(args[0], opcionPrepararFase8, StringComparison.Ordinal);
var prepararFase7 = args.Length == 1 && string.Equals(args[0], opcionPrepararFase7, StringComparison.Ordinal);
var prepararFase6 = args.Length == 1
    && string.Equals(args[0], opcionPrepararFase6, StringComparison.Ordinal);

if (args.Length != 0 && !crearUsuarioConsulta && !prepararFase3 && !prepararFase4 && !prepararFase5 && !prepararFase6 && !prepararFase7 && !prepararFase8 && !prepararFase9 && !prepararFase10)
{
    Console.Error.WriteLine("Uso:");
    Console.Error.WriteLine("  --preparar-fase10: preparar solo permisos de reportes/auditoría para ADMIN.");
    Console.Error.WriteLine("  Sin argumentos: bootstrap administrativo inicial.");
    Console.Error.WriteLine($"  {opcionUsuarioConsulta}: crear usuario limitado para probar autorización 403.");
    Console.Error.WriteLine($"  {opcionPrepararFase3}: crear el permiso administrativo de Fase 3.");
    Console.Error.WriteLine($"  {opcionPrepararFase4}: preparar permisos y referencias mínimas de productos e inventario.");
    Console.Error.WriteLine($"  {opcionPrepararFase5}: preparar compras y proveedores de forma idempotente y transaccional.");
    Console.Error.WriteLine($"  {opcionPrepararFase6}: preparar producción y recetas de forma idempotente y transaccional.");
    Console.Error.WriteLine($"{opcionPrepararFase7}: preparar exclusivamente configuración Ventas/POS.");
    Console.Error.WriteLine($"{opcionPrepararFase9}: preparar solo catálogos CxC/CxP; sin operaciones financieras.");
    Console.Error.WriteLine($"{opcionPrepararFase8}: preparar exclusivamente configuración Caja.");
    return 2;
}

var nombreUsuario = crearUsuarioConsulta
    ? BootstrapAdministrador.UsuarioConsulta
    : "admin";
var correo = crearUsuarioConsulta
    ? BootstrapAdministrador.CorreoUsuarioConsulta
    : "admin@heladeria.local";
var confirmacionRequerida = crearUsuarioConsulta ? "CREAR CONSULTA" : "CREAR";
if (prepararFase10) { confirmacionRequerida = "PREPARAR FASE 10"; }
else if (prepararFase9) { confirmacionRequerida = "PREPARAR FASE 9"; }
else if (prepararFase8) { confirmacionRequerida = "PREPARAR FASE 8"; }
else if (prepararFase7) { confirmacionRequerida = "PREPARAR FASE 7"; }
else if (prepararFase3)
{
    confirmacionRequerida = "PREPARAR FASE 3";
}
else if (prepararFase5)
{
    confirmacionRequerida = "PREPARAR FASE 5";
}
else if (prepararFase6)
{
    confirmacionRequerida = "PREPARAR FASE 6";
}
else if (prepararFase4)
{
    confirmacionRequerida = "PREPARAR FASE 4";
}

if (Console.IsInputRedirected)
{
    Console.Error.WriteLine(
        "El bootstrap requiere una terminal interactiva para leer la contraseña sin mostrarla.");
    return 2;
}

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = [],
    ContentRootPath = AppContext.BaseDirectory
});
builder.Services
    .AddOptions<OpcionesSeguridad>()
    .Bind(builder.Configuration.GetRequiredSection(OpcionesSeguridad.Seccion));
builder.Services.AgregarInfraestructura(
    builder.Configuration,
    habilitarReintentosSql: false);

using var aplicacion = builder.Build();
using var alcance = aplicacion.Services.CreateScope();

Console.WriteLine(
    prepararFase10 ? "Preparación controlada Fase 10 Reportes/Auditoría" : prepararFase9 ? "Preparación controlada Fase 9 CxC/CxP" : prepararFase8 ? "Preparación controlada Fase 8 Caja" : prepararFase7 ? "Preparación controlada Fase 7 Ventas/POS" : prepararFase5
        ? "Preparación controlada de la Fase 5 de Heladeria"
        : prepararFase6
        ? "Preparación controlada de la Fase 6 de Heladeria"
        : prepararFase4
        ? "Preparación controlada de la Fase 4 de Heladeria"
        : prepararFase3
        ? "Preparación controlada de la Fase 3 de Heladeria"
        : crearUsuarioConsulta
        ? "Creación del usuario limitado de pruebas de Heladeria"
        : "Bootstrap administrativo inicial de Heladeria");
if (!prepararFase3 && !prepararFase4 && !prepararFase5 && !prepararFase6 && !prepararFase7 && !prepararFase8 && !prepararFase9 && !prepararFase10)
{
    Console.WriteLine($"Usuario: {nombreUsuario}");
    Console.WriteLine($"Correo:  {correo}");
}
Console.WriteLine(
    prepararFase10 ? "Solo módulos REPORTES/AUDITORIA, permisos CONSULTAR, asignaciones ADMIN y marcador de bitácora. Sin históricos ni operaciones financieras." : prepararFase9 ? "Solo módulos, permisos ADMIN, estados y conceptos financieros. Sin cuentas, pagos ni movimientos reales." : prepararFase8 ? "Solo permisos ADMIN, flujo, tipos, series y formas de pago; sin cajas, sesiones, movimientos, cobros ni ventas." : prepararFase7 ? "Solo permisos ADMIN, flujo, conceptos y series de ventas; no ventas, clientes, monedas, pagos ni existencias." : prepararFase5
        ? "Se crearán o reutilizarán módulos, permisos, flujos, concepto de recepción y series/correlativos para la empresa principal del administrador. No se crean compras, stock ni moneda fiscal."
        : prepararFase6
        ? "Se crearán o reutilizarán permisos, flujo, conceptos y series/correlativos de producción. No se crean recetas, órdenes, productos ni existencias."
        : prepararFase4
        ? "Se crearán permisos de productos/inventario, referencias operativas y bodega/ubicación inicial si son necesarias."
        : prepararFase3
        ? "Se crearán el módulo CONFIGURACION, su permiso ADMINISTRAR, la asignación al rol ADMIN y el marcador de auditoría."
        : crearUsuarioConsulta
        ? "Se crearán únicamente el rol CONSULTA, el usuario y su asignación a la sucursal principal."
        : "Esta operación solo puede completarse una vez y se ejecutará en una transacción.");
Console.WriteLine("La operación se ejecutará dentro de una única transacción.");
Console.Write($"Escriba {confirmacionRequerida} para continuar: ");

if (!string.Equals(Console.ReadLine(), confirmacionRequerida, StringComparison.Ordinal))
{
    Console.WriteLine("Operación cancelada. No se realizaron cambios.");
    return 2;
}

string? contrasena = null;
string? confirmacion = null;

try
{
    if (!prepararFase3 && !prepararFase4 && !prepararFase5 && !prepararFase6 && !prepararFase7 && !prepararFase8 && !prepararFase9 && !prepararFase10)
    {
        contrasena = LectorContrasena.Leer("Contraseña inicial: ");
        confirmacion = LectorContrasena.Leer("Confirmar contraseña: ");

        if (!ContrasenasCoinciden(contrasena, confirmacion))
        {
            Console.Error.WriteLine("Las contraseñas no coinciden. No se realizaron cambios.");
            return 2;
        }
    }

    ResultadoBootstrapAdministrador resultado;
    if (prepararFase10)
    {
        resultado = await alcance.ServiceProvider.GetRequiredService<IPreparadorFase10>().EjecutarAsync(CancellationToken.None);
    }
    else if (prepararFase9)
    {
        resultado = await alcance.ServiceProvider.GetRequiredService<IPreparadorFase9>().EjecutarAsync(CancellationToken.None);
    }
    else if (prepararFase8)
    {
        resultado = await alcance.ServiceProvider.GetRequiredService<IPreparadorFase8>().EjecutarAsync(CancellationToken.None);
    }
    else if (prepararFase7)
    {
        resultado = await alcance.ServiceProvider.GetRequiredService<IPreparadorFase7>().EjecutarAsync(CancellationToken.None);
    }
    else if (prepararFase5)
    {
        var preparador = alcance.ServiceProvider.GetRequiredService<IPreparadorFase5>();
        resultado = await preparador.EjecutarAsync(CancellationToken.None);
    }
    else if (prepararFase6)
    {
        var preparador = alcance.ServiceProvider.GetRequiredService<IPreparadorFase6>();
        resultado = await preparador.EjecutarAsync(CancellationToken.None);
    }
    else if (prepararFase4)
    {
        var preparador = alcance.ServiceProvider.GetRequiredService<IPreparadorFase4>();
        resultado = await preparador.EjecutarAsync(CancellationToken.None);
    }
    else if (prepararFase3)
    {
        var preparador = alcance.ServiceProvider.GetRequiredService<IPreparadorFase3>();
        resultado = await preparador.EjecutarAsync(CancellationToken.None);
    }
    else if (crearUsuarioConsulta)
    {
        var provisionador = alcance.ServiceProvider.GetRequiredService<IProvisionadorUsuarioConsulta>();
        resultado = await provisionador.EjecutarAsync(contrasena!, CancellationToken.None);
    }
    else
    {
        var bootstrap = alcance.ServiceProvider.GetRequiredService<IBootstrapAdministrador>();
        resultado = await bootstrap.EjecutarAsync(
            new SolicitudBootstrapAdministrador(nombreUsuario, correo, contrasena!),
            CancellationToken.None);
    }

    Console.WriteLine();
    Console.WriteLine("Operación completada. Registros creados:");
    foreach (var registro in resultado.Registros)
    {
        Console.WriteLine(
            $"- {registro.Tabla}: {registro.Identificador} — {registro.Descripcion}");
    }

    Console.WriteLine();
    Console.WriteLine(
        prepararFase10 ? "Preparación Fase 10 validada; repetible, solo permisos ADMIN. Pendiente validación manual." : prepararFase9 ? "Preparación Fase 9 validada; sin operaciones ficticias ni alteración de históricos." : prepararFase8 ? "Preparación Fase 8 validada. Sin operaciones ficticias ni reinicio de correlativos." : prepararFase7 ? "Preparación Fase 7 validada. Repetir no duplica registros ni reinicia correlativos." : prepararFase5
            ? "Preparación Fase 5 validada. Una segunda ejecución compatible no duplica registros ni reinicia correlativos."
            : prepararFase6
            ? "Preparación Fase 6 validada. Una segunda ejecución compatible no duplica registros ni reinicia correlativos."
            : prepararFase4
            ? "El rol ADMIN ya dispone de los permisos y referencias mínimas para probar la Fase 4."
            : prepararFase3
            ? "El rol ADMIN ya puede administrar los mantenimientos de Fase 3; los demás usuarios solo conservan el alcance de lectura autorizado."
            : crearUsuarioConsulta
            ? "El usuario puede iniciar sesión y debe recibir 403 al solicitar SEGURIDAD.ADMINISTRAR."
            : "El usuario debe iniciar sesión y cambiar la contraseña temporal antes de usar módulos protegidos.");
    return 0;
}
catch (BootstrapAdministradorRechazadoException excepcion)
{
    Console.Error.WriteLine($"Operación rechazada: {excepcion.Message}");
    Console.Error.WriteLine("No se realizaron cambios.");
    return 2;
}
catch (ValidacionSeguridadException excepcion)
{
    Console.Error.WriteLine($"Contraseña rechazada: {excepcion.Message}");
    Console.Error.WriteLine("No se realizaron cambios.");
    return 2;
}
catch (OperationCanceledException)
{
    Console.WriteLine("Operación cancelada. No se realizaron cambios.");
    return 2;
}
catch (Exception)
{
    Console.Error.WriteLine(
        "La operación no pudo completarse. La transacción fue revertida y no se mostraron datos sensibles.");
    return 1;
}
finally
{
    contrasena = null;
    confirmacion = null;
}

static bool ContrasenasCoinciden(string primera, string segunda)
{
    var primeraBytes = Encoding.UTF8.GetBytes(primera);
    var segundaBytes = Encoding.UTF8.GetBytes(segunda);

    try
    {
        return primeraBytes.Length == segundaBytes.Length
            && CryptographicOperations.FixedTimeEquals(primeraBytes, segundaBytes);
    }
    finally
    {
        CryptographicOperations.ZeroMemory(primeraBytes);
        CryptographicOperations.ZeroMemory(segundaBytes);
    }
}

internal static class LectorContrasena
{
    private const int LongitudMaxima = 200;

    public static string Leer(string indicacion)
    {
        Console.Write(indicacion);
        var caracteres = new List<char>();

        try
        {
            while (true)
            {
                var tecla = Console.ReadKey(intercept: true);

                if (tecla.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    return new string([.. caracteres]);
                }

                if (tecla.Key == ConsoleKey.Escape
                    || tecla.Key == ConsoleKey.C
                    && tecla.Modifiers.HasFlag(ConsoleModifiers.Control))
                {
                    Console.WriteLine();
                    throw new OperationCanceledException();
                }

                if (tecla.Key == ConsoleKey.Backspace)
                {
                    if (caracteres.Count > 0)
                    {
                        caracteres.RemoveAt(caracteres.Count - 1);
                    }

                    continue;
                }

                if (!char.IsControl(tecla.KeyChar))
                {
                    if (caracteres.Count >= LongitudMaxima)
                    {
                        throw new InvalidOperationException(
                            "La contraseña supera la longitud máxima permitida.");
                    }

                    caracteres.Add(tecla.KeyChar);
                }
            }
        }
        finally
        {
            CollectionsMarshal.AsSpan(caracteres).Clear();
            caracteres.Clear();
        }
    }
}
