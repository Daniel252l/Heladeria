using System.Data;
using Heladeria.Aplicacion.Seguridad.Bootstrap;
using Heladeria.Aplicacion.Seguridad.Configuracion;
using Heladeria.Infraestructura.Persistencia;
using Heladeria.Infraestructura.Persistencia.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Heladeria.Infraestructura.Seguridad.Bootstrap;

// Preparación explícita: solo catálogo de permisos ADMIN. Nunca se invoca desde Program de la API.
public sealed class PreparadorFase10(HeladeriaDbContext db, IOptions<OpcionesSeguridad> opciones, TimeProvider reloj) : IPreparadorFase10
{
    public async Task<ResultadoBootstrapAdministrador> EjecutarAsync(CancellationToken token)
    {
        if (db.ChangeTracker.HasChanges()) throw new BootstrapAdministradorRechazadoException("Hay cambios pendientes ajenos a esta preparación.");
        await using var tx = db.Database.IsRelational() ? await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, token) : null;
        try
        {
            if (db.Database.IsSqlServer()) await db.Database.ExecuteSqlRawAsync("""
                DECLARE @r int;
                EXEC @r=sys.sp_getapplock @Resource=N'Heladeria.Bootstrap.Fase10', @LockMode=N'Exclusive', @LockOwner=N'Transaction', @LockTimeout=10000;
                IF @r<0 THROW 51002, 'Preparacion ocupada.', 1;
                """, token);
            var activos = opciones.Value.EstadosActivos;
            var s = await db.UsuariosSucursales.AsNoTracking().Where(x => x.EsPrincipal && x.Usuario.RoleId == "ADMIN"
                && x.Usuario.Estado.Activo && activos.Contains(x.Usuario.Estado.Nombre)
                && x.Estado.Activo && activos.Contains(x.Estado.Nombre)
                && x.Sucursal.Estado.Activo && activos.Contains(x.Sucursal.Estado.Nombre)
                && x.Sucursal.Empresa.Estado.Activo && activos.Contains(x.Sucursal.Empresa.Estado.Nombre))
                .OrderBy(x => x.UsuarioId).Select(x => new { x.UsuarioId, x.SucursalId, x.Sucursal.EmpresaId }).FirstOrDefaultAsync(token)
                ?? throw new BootstrapAdministradorRechazadoException("No existe administrador principal operativo.");
            var activo = await db.Permisos.Where(x => x.Codigo == "SEGURIDAD.ADMINISTRAR" && x.Modulo.Activo
                && x.Estado.Activo && activos.Contains(x.Estado.Nombre)).Select(x => (int?)x.EstadoId).SingleOrDefaultAsync(token)
                ?? throw new BootstrapAdministradorRechazadoException("No existe el permiso administrativo inicial activo.");
            var registros = new List<RegistroBootstrapCreado>();
            async Task<T> Asegurar<T>(DbSet<T> tabla, Func<Task<T?>> buscar, Func<T> crear, Func<T, bool> compatible,
                Func<T, string> id, string descripcion) where T : class
            {
                var e = await buscar();
                if (e is not null)
                {
                    if (!compatible(e)) throw new BootstrapAdministradorRechazadoException($"{descripcion}: configuración incompatible; no se sobrescribe.");
                    return e;
                }
                e = crear(); tabla.Add(e); await db.SaveChangesAsync(token);
                registros.Add(new(typeof(T).Name, id(e), descripcion)); return e;
            }
            var accion = await Asegurar(db.AccionesSistema, () => db.AccionesSistema.SingleOrDefaultAsync(x => x.Codigo == "CONSULTAR", token),
                () => new AccionesSistema { Codigo = "CONSULTAR", Nombre = "Consultar", EstadoId = activo },
                x => x.EstadoId == activo, x => x.Id.ToString(), "CONSULTAR");
            foreach (var modulo in new[] { "REPORTES", "AUDITORIA" })
            {
                await Asegurar(db.Modulos, () => db.Modulos.SingleOrDefaultAsync(x => x.Id == modulo, token),
                    () => new Modulos { Id = modulo, Nombre = modulo == "REPORTES" ? "Reportes" : "Auditoría", Activo = true, Icono = "description" },
                    x => x.Activo, x => x.Id, modulo);
                var permiso = await Asegurar(db.Permisos, () => db.Permisos.SingleOrDefaultAsync(x => x.Codigo == modulo + ".CONSULTAR", token),
                    () => new Permisos { Codigo = modulo + ".CONSULTAR", ModuloId = modulo, Recurso = modulo, AccionId = accion.Id, EstadoId = activo },
                    x => x.ModuloId == modulo && x.Recurso == modulo && x.AccionId == accion.Id && x.EstadoId == activo,
                    x => x.Id.ToString(), modulo + ".CONSULTAR");
                await Asegurar(db.RolesPermisos, () => db.RolesPermisos.SingleOrDefaultAsync(x => x.RoleId == "ADMIN" && x.PermisoId == permiso.Id, token),
                    () => new RolesPermisos { RoleId = "ADMIN", PermisoId = permiso.Id, Permitido = true },
                    x => x.Permitido, x => x.Id.ToString(), "ADMIN → " + permiso.Codigo);
            }
            if (!await db.Bitacoras.AnyAsync(x => x.EmpresaId == s.EmpresaId && x.Accion == "FASE10_CONFIGURADA", token))
            {
                var bitacora = new Bitacoras { EmpresaId = s.EmpresaId, SucursalId = s.SucursalId, UsuarioId = s.UsuarioId,
                    ModuloId = "AUDITORIA", Entidad = "Permisos", Accion = "FASE10_CONFIGURADA", Fecha = reloj.GetUtcNow().UtcDateTime };
                db.Bitacoras.Add(bitacora); await db.SaveChangesAsync(token);
                registros.Add(new("Bitacoras", bitacora.Id.ToString(), "FASE10_CONFIGURADA"));
            }
            if (tx is not null) await tx.CommitAsync(token);
            return new(registros);
        }
        catch
        {
            if (tx is not null) await tx.RollbackAsync(CancellationToken.None);
            db.ChangeTracker.Clear(); throw;
        }
    }
}
