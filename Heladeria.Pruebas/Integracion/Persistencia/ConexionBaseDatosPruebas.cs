using Heladeria.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace Heladeria.Pruebas.Integracion.Persistencia;

public sealed class ConexionBaseDatosPruebas
{
    [Fact]
    [Trait("Categoria", "Integracion")]
    public async Task BaseAisladaDelMaestro_EsAccesibleYCoincideConElModelo()
    {
        // Nunca conecta a appsettings/BD del desarrollador. El maestro se verifica en SQL Server real desechable.
        await using var basePrueba = await BaseSqlServerAislada.CrearAsync();
        var opciones = new DbContextOptionsBuilder<HeladeriaDbContext>()
            .UseSqlServer(basePrueba.CadenaConexion)
            .Options;

        await using var contexto = new HeladeriaDbContext(opciones);

        Assert.True(await contexto.Database.CanConnectAsync());
        Assert.Equal(128, contexto.Model.GetEntityTypes().Count());

        var tablasReales = await contexto.Database
            .SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM sys.tables")
            .SingleAsync();

        Assert.Equal(128, tablasReales);
        var tablasModelo = contexto.Model.GetEntityTypes().Select(x => x.GetTableName()).Order().ToArray();
        var tablasSql = await contexto.Database.SqlQuery<string>($"SELECT name AS [Value] FROM sys.tables").ToArrayAsync();
        Assert.Equal(tablasModelo, tablasSql.Order().ToArray());
        Assert.Empty(await contexto.Ventas.AsNoTracking().ToArrayAsync());
        Assert.Empty(await contexto.VentasDetalle.AsNoTracking().ToArrayAsync());
        Assert.Empty(await contexto.VentaDetalleImpuestos.AsNoTracking().ToArrayAsync());
    }
}
