using Heladeria.Aplicacion.Clientes;
using Heladeria.Aplicacion.Configuracion;
using Heladeria.Aplicacion.Seguridad.Bootstrap;
using Heladeria.Infraestructura.ClientesGestion;
using Heladeria.Infraestructura.Fase4;
using Heladeria.Infraestructura.Inventario;
using Heladeria.Infraestructura.Persistencia;
using Heladeria.Infraestructura.Persistencia.Entidades;
using Heladeria.Infraestructura.Seguridad.Bootstrap;
using Heladeria.Pruebas.Fase5;
using Heladeria.Pruebas.Integracion.Persistencia;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Heladeria.Pruebas.Fase9;

public sealed class ClientesSqlPruebas
{
    [Fact]
    public async Task AltaConcurrenteRespetaClaveRealDeSqlServerYNoDejaAuditoriaParcial()
    {
        // Esquema maestro y datos exclusivamente en LocalDB privada desechable; sin conexiones de appsettings.
        await using var aislada = await BaseSqlServerAislada.CrearAsync();
        var opciones = new DbContextOptionsBuilder<HeladeriaDbContext>().UseSqlServer(aislada.CadenaConexion).Options;
        await using var db = new HeladeriaDbContext(opciones);
        var config = EscenarioFase5.Opciones; var reloj = TimeProvider.System;
        await new BootstrapAdministrador(db, new PasswordHasher<Usuarios>(), config, reloj)
            .EjecutarAsync(new SolicitudBootstrapAdministrador("admin", "admin@prueba.local", "Prueba-Aislada9!"), default);
        await new PreparadorFase4(db, config, reloj).EjecutarAsync(default);
        var coordinador = new CoordinadorAjustesInventario();
        var inventario = new ServicioInventario(db, new AuditoriaFase4(db, reloj), config, reloj, coordinador);
        await new PreparadorFase7(db, config, reloj, coordinador, inventario).EjecutarAsync(default);
        var asignacion = await db.UsuariosSucursales.Include(x => x.Sucursal).SingleAsync();
        var s = new ContextoConfiguracion(asignacion.UsuarioId, "ADMIN", asignacion.Sucursal.EmpresaId, asignacion.SucursalId, null, null);
        var barrera = new BarreraAlta();
        var concurrentes = new DbContextOptionsBuilder<HeladeriaDbContext>().UseSqlServer(aislada.CadenaConexion)
            .AddInterceptors(barrera).Options;
        async Task<(ClienteConsultaDto? Cliente, Exception? Error)> AltaAsync()
        {
            await using var otro = new HeladeriaDbContext(concurrentes);
            try { return (await new ServicioClientes(otro, config, reloj).CrearAsync(new() {
                CodigoCliente = "DUP-CONCURRENTE", NombreCliente = "Cliente aislado", LimiteCredito = 9999999999.99m,
                DiasCredito = short.MaxValue }, s, default), null); }
            catch (Exception error) { return (null, error); }
        }
        var resultados = await Task.WhenAll(AltaAsync(), AltaAsync());
        var creado = Assert.Single(resultados, x => x.Cliente is not null).Cliente!;
        var error = Assert.IsType<ExcepcionClientes>(Assert.Single(resultados, x => x.Error is not null).Error);
        Assert.Equal("CLIENTE_DUPLICADO", error.Codigo); Assert.Equal(409, error.EstadoHttp);
        Assert.Equal(creado.Id, (await db.Clientes.AsNoTracking().SingleAsync()).Id);
        var bitacora = Assert.Single(await db.Bitacoras.Where(x => x.Accion == "CLIENTE_CREADO").ToArrayAsync());
        Assert.Equal(creado.Id.ToString(), bitacora.EntidadId); Assert.Equal("Clientes", bitacora.Entidad);
        Assert.Equal(9999999999.99m, creado.LimiteCredito); Assert.Equal(short.MaxValue, creado.DiasCredito);
    }

    private sealed class BarreraAlta : SaveChangesInterceptor
    {
        private readonly TaskCompletionSource _listos = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _cantidad;
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context!.ChangeTracker.Entries<Clientes>().Any(x => x.State == EntityState.Added))
            {
                // Ambos pasaron la consulta preventiva: debe decidir UQ_ClientesCodigo, no el precheck.
                if (Interlocked.Increment(ref _cantidad) == 2) _listos.TrySetResult();
                await _listos.Task.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
            }
            return result;
        }
    }
}
