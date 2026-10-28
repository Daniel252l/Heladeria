using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace Heladeria.Pruebas.Integracion.Persistencia;

// Exclusivamente pruebas Windows/SQL Server LocalDB. No consulta configuración ni conexiones de aplicación.
internal sealed class BaseSqlServerAislada : IAsyncDisposable
{
    // LocalDB comparte la carpeta física predeterminada entre instancias. También la base necesita nombre único.
    private readonly string _nombreBase = "Heladeria_Pruebas_" + Guid.NewGuid().ToString("N");
    private readonly string _instancia = "HeladeriaF7Xunit_" + Guid.NewGuid().ToString("N")[..12];
    private bool _creada;
    internal string CadenaConexion => $"Server=(localdb)\\{_instancia};Database={_nombreBase};Integrated Security=true;TrustServerCertificate=true;Pooling=false;Connect Timeout=30";
    internal static async Task<BaseSqlServerAislada> CrearAsync(bool incluirFase8 = true, bool incluirFase9 = true, bool incluirAjusteD = true)
    {
        var resultado = new BaseSqlServerAislada();
        try
        {
            await LocalDbAsync("create", resultado._instancia);
            resultado._creada = true;
            await LocalDbAsync("start", resultado._instancia);
            var directorio = new DirectoryInfo(AppContext.BaseDirectory);
            while (directorio is not null && !File.Exists(Path.Combine(directorio.FullName, "BaseDatos", "Heladeria.sql")))
                directorio = directorio.Parent;
            if (directorio is null) throw new InvalidOperationException("No se encontró BaseDatos/Heladeria.sql para pruebas.");
            var script = await File.ReadAllTextAsync(Path.Combine(directorio.FullName, "BaseDatos", "Heladeria.sql"));
            if (!incluirAjusteD)
            {
                var inicio = script.IndexOf("-- AJUSTE D COMPOSICION VENTA.", StringComparison.Ordinal);
                if (inicio < 0) throw new InvalidOperationException("No se encontró el límite del esquema previo a Ajuste D.");
                script = script[..inicio];
            }
            if (!incluirFase8)
            {
                var inicioFase8 = script.IndexOf("-- FASE 8 CAJA.", StringComparison.Ordinal);
                if (inicioFase8 < 0) throw new InvalidOperationException("No se encontró el límite del esquema previo a Fase 8.");
                script = script[..inicioFase8];
            }
            if (incluirFase8 && !incluirFase9)
            {
                var inicio = script.IndexOf("-- FASE 9 CXC CXP.", StringComparison.Ordinal);
                if (inicio < 0) throw new InvalidOperationException("No se encontró el límite anterior a Fase 9.");
                script = script[..inicio];
            }
            script = script.Replace("CREATE DATABASE Heladeria;", $"CREATE DATABASE [{resultado._nombreBase}];", StringComparison.Ordinal)
                .Replace("USE Heladeria;", $"USE [{resultado._nombreBase}];", StringComparison.Ordinal);
            if (Regex.IsMatch(script, @"(?im)^\s*(USE|CREATE DATABASE)\s+Heladeria\s*;"))
                throw new InvalidOperationException("El script conserva una referencia a la base real.");
            await using var conexion = new SqlConnection(new SqlConnectionStringBuilder(resultado.CadenaConexion) { InitialCatalog = "master" }.ConnectionString);
            await conexion.OpenAsync();
            foreach (var batch in Regex.Split(script, @"(?im)^\s*GO\s*(?:--[^\r\n]*)?$").Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                await using var comando = new SqlCommand(batch, conexion) { CommandTimeout = 120 };
                await comando.ExecuteNonQueryAsync();
            }
            return resultado;
        }
        catch { await resultado.DisposeAsync(); throw; }
    }
    private static async Task LocalDbAsync(string accion, string instancia)
    {
        using var proceso = new Process { StartInfo = new ProcessStartInfo("SqlLocalDB.exe") {
            CreateNoWindow = true, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true } };
        proceso.StartInfo.ArgumentList.Add(accion); proceso.StartInfo.ArgumentList.Add(instancia);
        proceso.Start();
        var salida = proceso.StandardOutput.ReadToEndAsync(); var error = proceso.StandardError.ReadToEndAsync();
        await proceso.WaitForExitAsync();
        if (proceso.ExitCode != 0) throw new InvalidOperationException($"LocalDB de pruebas ({accion}): {await salida} {await error}");
    }
    public async ValueTask DisposeAsync()
    {
        if (!_creada) return;
        try
        {
            await using var conexion = new SqlConnection(new SqlConnectionStringBuilder(CadenaConexion) { InitialCatalog = "master" }.ConnectionString);
            await conexion.OpenAsync();
            // Nombre generado internamente con prefijo fijo + GUID, únicamente en instancia privada creada por esta clase.
            await using var comando = new SqlCommand($"IF DB_ID(N'{_nombreBase}') IS NOT NULL BEGIN ALTER DATABASE [{_nombreBase}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{_nombreBase}]; END;", conexion);
            await comando.ExecuteNonQueryAsync();
        }
        finally
        {
            await LocalDbAsync("stop", _instancia);
            await LocalDbAsync("delete", _instancia);
            _creada = false;
        }
    }
}
