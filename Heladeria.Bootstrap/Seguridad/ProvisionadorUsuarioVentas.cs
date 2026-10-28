using System.Data;
using System.Security.Cryptography;
using Heladeria.Aplicacion.Seguridad.Autenticacion;
using Heladeria.Aplicacion.Seguridad.Autorizacion;
using Heladeria.Aplicacion.Seguridad.Bootstrap;
using Heladeria.Aplicacion.Seguridad.Configuracion;
using Heladeria.Infraestructura.Persistencia;
using Heladeria.Infraestructura.Persistencia.Entidades;
using Heladeria.Infraestructura.Seguridad.Bootstrap;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Heladeria.Bootstrap.Seguridad;

// Solo se invoca desde el comando explícito; no se registra en el inicio de la API.
public sealed class ProvisionadorUsuarioVentas(HeladeriaDbContext db, IPasswordHasher<Usuarios> hasher,
    IOptions<OpcionesSeguridad> opciones, TimeProvider reloj)
{
    public static IReadOnlyList<string> PermisosNecesarios { get; } = Array.AsReadOnly(new[] {
        PermisosSistema.VentasConsultar, PermisosSistema.VentasAdministrar, PermisosSistema.VentasVender,
        PermisosSistema.PosOperar, PermisosSistema.CajaConsultar, PermisosSistema.CajaOperar, PermisosSistema.CajaCobrar });

    public async Task<ResultadoBootstrapAdministrador> EjecutarAsync(Func<string> leerContrasena, CancellationToken t)
    {
        if (!db.Database.IsSqlServer()) throw Rechazar("La preparación requiere SQL Server.");
        await using var transaccion = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, t);
        try
        {
            await db.Database.ExecuteSqlRawAsync("""
                DECLARE @resultado int;
                EXEC @resultado = sp_getapplock @Resource=N'Heladeria.UsuarioVentas', @LockMode='Exclusive',
                    @LockOwner='Transaction', @LockTimeout=10000;
                IF @resultado < 0 THROW 51121, 'Hay otra preparación del usuario ventas en curso.', 1;
                """, t);
            var activos = opciones.Value.EstadosActivos;
            // La bitácora original identifica la instalación incluso si los nombres comerciales cambiaron.
            var instalaciones = await db.Bitacoras.AsNoTracking().Where(x => x.Entidad == BootstrapAdministrador.EntidadMarcador
                && x.Accion == BootstrapAdministrador.AccionMarcador).ToArrayAsync(t);
            if (instalaciones.Length != 1) throw Rechazar("No se pudo identificar una única instalación administrativa inicial.");
            var instalacion = instalaciones[0];
            var referencias = await db.UsuariosSucursales.AsNoTracking().Where(x =>
                x.UsuarioId == instalacion.UsuarioId && x.Usuario.RoleId == "ADMIN" && x.EsPrincipal
                && x.SucursalId == instalacion.SucursalId && x.Sucursal.EmpresaId == instalacion.EmpresaId
                && x.Estado.Activo && activos.Contains(x.Estado.Nombre)
                && x.Usuario.Estado.Activo && activos.Contains(x.Usuario.Estado.Nombre)
                && x.Sucursal.Estado.Activo && activos.Contains(x.Sucursal.Estado.Nombre)
                && x.Sucursal.Empresa.Estado.Activo && activos.Contains(x.Sucursal.Empresa.Estado.Nombre)
                && x.Usuario.TipoInicioSesion.Estado.Activo && activos.Contains(x.Usuario.TipoInicioSesion.Estado.Nombre))
                .Select(x => new { x.SucursalId, x.Sucursal.EmpresaId, Empresa = x.Sucursal.Empresa.RazonSocial,
                    Sucursal = x.Sucursal.Nombre, x.EstadoId, x.Usuario.TipoInicioSesionId,
                    x.Usuario.Role.CambioDeContrasenia, x.Usuario.Role.Tipo.ClaseTipoId }).ToArrayAsync(t);
            if (referencias.Length != 1) throw Rechazar("La empresa y sucursal principal de la instalación deben estar activas y ser inequívocas.");
            var r = referencias[0];
            var codigos = PermisosNecesarios.ToArray();
            var permisos = await db.Permisos.AsNoTracking().Where(x => codigos.Contains(x.Codigo)
                && x.Modulo.Activo && x.Estado.Activo && activos.Contains(x.Estado.Nombre)
                && x.Accion.Estado.Activo && activos.Contains(x.Accion.Estado.Nombre)).ToArrayAsync(t);
            if (permisos.Length != codigos.Length) throw Rechazar("Faltan permisos operativos activos de Ventas/POS/Caja. Complete su preparación antes de crear ventas.");
            var roles = await db.Roles.Include(x => x.RolesPermisos).ThenInclude(x => x.Permiso).ToArrayAsync(t);
            bool Equivalente(Roles x) => x.Id is not ("ADMIN" or "CONSULTA") && !x.AplicarConfiguracionesPorRol
                && x.RolesPermisos.Count == codigos.Length && x.RolesPermisos.All(p => p.Permitido && codigos.Contains(p.Permiso.Codigo));
            var rol = roles.SingleOrDefault(x => x.Id == "VENDEDOR");
            if (rol is not null && !Equivalente(rol)) throw Rechazar("El rol VENDEDOR existente no tiene exactamente los permisos operativos esperados. No se alteró.");
            if (rol is null)
            {
                var equivalentes = roles.Where(Equivalente).ToArray();
                if (equivalentes.Length > 1) throw Rechazar("Existen varios roles operativos equivalentes; resuelva la ambigüedad antes de continuar.");
                rol = equivalentes.SingleOrDefault();
            }
            var existentes = await db.Usuarios.Include(x => x.UsuariosSucursales).Include(x => x.Estado)
                .Where(x => x.UserName == "ventas" || x.Email == "ventas@heladeria.local").ToArrayAsync(t);
            if (existentes.Length != 0)
            {
                var u = existentes.SingleOrDefault();
                if (u is null || rol is null || u.UserName != "ventas" || u.Email != "ventas@heladeria.local" || u.RoleId != rol.Id
                    || !u.Estado.Activo || !activos.Contains(u.Estado.Nombre) || u.UsuariosSucursales.Count != 1
                    || !u.UsuariosSucursales.Any(x => x.SucursalId == r.SucursalId && x.EsPrincipal && x.EstadoId == r.EstadoId))
                    throw Rechazar("La cuenta ventas existente no coincide con el alcance esperado. No se cambió su contraseña, rol ni asignaciones.");
                await transaccion.CommitAsync(t);
                return new ResultadoBootstrapAdministrador([]); // Nunca restablecer contraseña, contador ni sesiones al repetir.
            }
            var contrasena = leerContrasena();
            ValidadorPoliticaContrasena.Validar(contrasena, contrasena, opciones.Value.Contrasenas);
            if (contrasena.Length > 200) throw Rechazar("La contraseña supera la longitud máxima permitida.");
            var registros = new List<RegistroBootstrapCreado>();
            if (rol is null)
            {
                if (roles.Any(x => x.Nombre == "Vendedor")) throw Rechazar("Ya existe otro rol llamado Vendedor con permisos diferentes.");
                var niveles = await db.NivelesDeAccesos.Where(x => x.Nombre == "Operativo POS").ToArrayAsync(t);
                if (niveles.Length > 1) throw Rechazar("El nivel operativo es ambiguo.");
                var nivel = niveles.SingleOrDefault();
                if (nivel is null)
                {
                    nivel = new NivelesDeAccesos { Id = checked((await db.NivelesDeAccesos.MaxAsync(x => (int?)x.Id, t) ?? 0) + 1), Nombre = "Operativo POS" };
                    db.NivelesDeAccesos.Add(nivel);
                    registros.Add(new("NivelesDeAccesos", nivel.Id.ToString(), nivel.Nombre));
                }
                var tipo = await db.Tipos.SingleOrDefaultAsync(x => x.ClaseTipoId == r.ClaseTipoId && x.Nombre == "Rol operativo POS", t);
                if (tipo is null)
                {
                    tipo = new Tipos { Id = checked((await db.Tipos.MaxAsync(x => (int?)x.Id, t) ?? 0) + 1), Nombre = "Rol operativo POS",
                        ClaseTipoId = r.ClaseTipoId, EstadoId = r.EstadoId, ModuloId = "SEGURIDAD" };
                    db.Tipos.Add(tipo);
                    registros.Add(new("Tipos", tipo.Id.ToString(), tipo.Nombre));
                }
                else if (tipo.EstadoId != r.EstadoId) throw Rechazar("El tipo operativo existente no está en el estado esperado.");
                rol = new Roles { Id = "VENDEDOR", Nombre = "Vendedor", NivelDeAccesoId = nivel.Id, TipoId = tipo.Id,
                    CambioDeContrasenia = r.CambioDeContrasenia, AplicarConfiguracionesPorRol = false };
                db.Roles.Add(rol);
                foreach (var permiso in permisos) db.RolesPermisos.Add(new() { RoleId = rol.Id, PermisoId = permiso.Id, Permitido = true });
                registros.Add(new("Roles", rol.Id, rol.Nombre));
                registros.AddRange(permisos.Select(p => new RegistroBootstrapCreado("RolesPermisos", rol.Id, p.Codigo)));
            }
            var usuario = new Usuarios { UserName = "ventas", Email = "ventas@heladeria.local", RoleId = rol.Id,
                IdentityId = Guid.NewGuid().ToString("N"), SecurityStamp = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)),
                PasswordHash = string.Empty, ContraseniaTemporal = true, UltimoCambioDeContrasenia = null, PasswordTemporal = null,
                AccessFailedCount = 0, HorarioInicio = TimeOnly.MinValue, HorarioFinal = TimeOnly.MinValue,
                Imagen = "sin-imagen", Soundex = "V532", EstadoId = r.EstadoId, TipoInicioSesionId = r.TipoInicioSesionId,
                UtilizarConfiguracionDeRole = true };
            usuario.PasswordHash = hasher.HashPassword(usuario, contrasena);
            db.Usuarios.Add(usuario);
            db.UsuariosSucursales.Add(new() { Usuario = usuario, SucursalId = r.SucursalId, EstadoId = r.EstadoId, EsPrincipal = true });
            await db.SaveChangesAsync(t);
            db.Bitacoras.Add(new() { EmpresaId = r.EmpresaId, SucursalId = r.SucursalId, UsuarioId = instalacion.UsuarioId,
                ModuloId = "SEGURIDAD", Entidad = "Usuarios", EntidadId = usuario.Id.ToString(), Accion = "USUARIO_VENTAS_PREPARADO",
                Fecha = reloj.GetUtcNow().UtcDateTime, Comentario = "Cuenta operativa ventas con contraseña temporal y una única sucursal." });
            await db.SaveChangesAsync(t);
            await transaccion.CommitAsync(t);
            registros.Add(new("Usuarios", usuario.Id.ToString(), usuario.UserName));
            registros.Add(new("UsuariosSucursales", r.SucursalId.ToString(), $"{r.Empresa} / {r.Sucursal} (principal)"));
            return new ResultadoBootstrapAdministrador(registros);
        }
        catch { await transaccion.RollbackAsync(CancellationToken.None); db.ChangeTracker.Clear(); throw; }
    }
    private static BootstrapAdministradorRechazadoException Rechazar(string mensaje) => new(mensaje);
}
