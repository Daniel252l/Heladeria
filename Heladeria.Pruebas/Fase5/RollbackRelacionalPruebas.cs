using Heladeria.Infraestructura.Inventario;
using Heladeria.Infraestructura.Persistencia;
using Heladeria.Infraestructura.Persistencia.Entidades;
using Heladeria.Pruebas.Fase4;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using static Heladeria.Pruebas.Fase5.EscenarioFase5;

namespace Heladeria.Pruebas.Fase5;

public sealed class RollbackRelacionalPruebas
{
    private static Heladeria.Aplicacion.Configuracion.ContextoConfiguracion Seguridad => EscenarioFase5.Seguridad;
    [Fact]
    public async Task PreparacionIncompatibleRevierteInclusoPermisosYFlujosCreadosAntesDelFallo()
    {
        await using var conexion = new SqliteConnection("Data Source=:memory:"); await conexion.OpenAsync();
        var contexto = new ContextoSqlitePruebas(new DbContextOptionsBuilder<HeladeriaDbContext>().UseSqlite(conexion).Options);
        await contexto.Database.EnsureCreatedAsync(); var coordinador = new CoordinadorAjustesInventario();
        await using var e = new EscenarioFase5(await EscenarioFase4.CrearConContextoAsync(contexto, coordinador), coordinador);
        await e.SemillarAsync(preparar: false);
        contexto.ConceptosMovimientos.Add(new ConceptosMovimientos { EmpresaId = 1, Codigo = "RECEPCION-COMPRA", Nombre = "Referencia incompatible",
            TipoOperacion = "ENTRADA", EsAutomatico = false, EstadoId = 1, FechaCreacion = DateTime.UtcNow, CreadoPorUsuarioId = 1, VersionFila = [] });
        await contexto.SaveChangesAsync();
        await Assert.ThrowsAsync<Heladeria.Aplicacion.Seguridad.Bootstrap.BootstrapAdministradorRechazadoException>(() => e.Preparador.EjecutarAsync(default));
        Assert.False(await contexto.Modulos.AnyAsync(item => item.Id == "COMPRAS" || item.Id == "PROVEEDORES"));
        Assert.False(await contexto.Permisos.AnyAsync(item => item.Codigo.StartsWith("COMPRAS.") || item.Codigo.StartsWith("PROVEEDORES.")));
        Assert.Equal(1, await contexto.FlujosEstados.CountAsync()); Assert.Empty(contexto.RolesPermisos);
        Assert.Empty(contexto.SeriesDocumentos); Assert.False(await contexto.Bitacoras.AnyAsync(item => item.Accion == "FASE5_CONFIGURADA"));
        var concepto = await contexto.ConceptosMovimientos.SingleAsync(item => item.Codigo == "RECEPCION-COMPRA");
        concepto.EsAutomatico = true; concepto.AfectaCosto = true; concepto.RequiereReferencia = true; concepto.PermiteCostoCero = true;
        await contexto.SaveChangesAsync();
        await e.Preparador.EjecutarAsync(default);
        Assert.True(await contexto.Bitacoras.AnyAsync(item => item.Accion == "FASE5_CONFIGURADA"));
    }
    [Fact]
    public async Task ErrorPosteriorAActualizarStockRevierteDocumentoDetalleStockCorrelativosYCostosYPermiteReintento()
    {
        await using var conexion = new SqliteConnection("Data Source=:memory:"); await conexion.OpenAsync();
        var fallo = new FalloEnKardex();
        var opciones = new DbContextOptionsBuilder<HeladeriaDbContext>().UseSqlite(conexion).AddInterceptors(fallo).Options;
        var contexto = new ContextoSqlitePruebas(opciones); await contexto.Database.EnsureCreatedAsync();
        var coordinador = new CoordinadorAjustesInventario();
        await using var e = new EscenarioFase5(await EscenarioFase4.CrearConContextoAsync(contexto, coordinador), coordinador);
        await e.SemillarAsync(); var orden = await e.ConfirmarAsync(Orden(3, 25000));
        var auditAntes = await contexto.Bitacoras.CountAsync(); var solicitud = Recepcion(orden.Detalles[0].Id, 3);
        fallo.Activo = true;
        await Assert.ThrowsAsync<InvalidOperationException>(() => e.Compras.RecibirAsync(orden.Encabezado.Id, solicitud, Seguridad, default));
        Assert.True(fallo.StockYaPersistido);
        Assert.Empty(contexto.RecepcionesCompra); Assert.Empty(contexto.RecepcionCompraDetalles); Assert.Empty(contexto.Movimientos);
        Assert.Empty(contexto.MovimientoDetalles); Assert.Empty(contexto.Kardex); Assert.Empty(contexto.CostosProductos);
        Assert.Empty(contexto.HistoricoCostosProductos); Assert.Empty(contexto.HistoricoPreciosCompra);
        Assert.Equal(auditAntes, await contexto.Bitacoras.CountAsync());
        Assert.Equal(5000m, (await contexto.Existencias.AsNoTracking().SingleAsync(item => item.Id == 1)).Cantidad);
        Assert.Equal("CONFIRMADA", (await e.Compras.ObtenerAsync(orden.Encabezado.Id, Seguridad, default)).Encabezado.Estado);
        Assert.All(await contexto.CorrelativosDocumentos.Where(item => item.SerieDocumento.Serie != "OC").ToArrayAsync(), item => Assert.Equal(0, item.UltimoNumero));
        fallo.Activo = false;
        await e.Compras.RecibirAsync(orden.Encabezado.Id, solicitud, Seguridad, default);
        Assert.Equal(8000m, (await contexto.Existencias.AsNoTracking().SingleAsync(item => item.Id == 1)).Cantidad);
        Assert.Single(contexto.RecepcionesCompra);
    }

    private sealed class FalloEnKardex : SaveChangesInterceptor
    {
        internal bool Activo { get; set; }
        internal bool StockYaPersistido { get; private set; }
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (Activo && eventData.Context is HeladeriaDbContext contexto
                && contexto.ChangeTracker.Entries<Kardex>().Any(item => item.State == EntityState.Added))
            {
                StockYaPersistido = (await contexto.Existencias.AsNoTracking().SingleAsync(item => item.Id == 1, cancellationToken)).Cantidad == 8000m;
                throw new InvalidOperationException("FALLO_INYECTADO_DESPUES_DEL_STOCK");
            }
            return result;
        }
    }
}

// Adaptación SOLO del proveedor de pruebas: SQLite no genera rowversion de SQL Server.
// Conserva EF de producción intacto. Prueba transacción/consultas/FK, no sp_getapplock.
internal sealed class ContextoSqlitePruebas(DbContextOptions<HeladeriaDbContext> opciones) : HeladeriaDbContext(opciones)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        foreach (var entidad in modelBuilder.Model.GetEntityTypes())
            foreach (var propiedad in entidad.GetProperties().Where(item => item.IsConcurrencyToken && item.ClrType == typeof(byte[])))
                modelBuilder.Entity(entidad.ClrType).Property(propiedad.Name).HasDefaultValueSql("(randomblob(8))");
    }
}
