using Heladeria.Aplicacion.Seguridad.Autorizacion;
using Heladeria.Aplicacion.Seguridad.Configuracion;
using Heladeria.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Heladeria.Infraestructura.Seguridad.Autorizacion;

public sealed class ServicioPermisos(
    HeladeriaDbContext contexto,
    IOptions<OpcionesSeguridad> opciones) : IServicioPermisos
{
    private readonly string[] _estadosActivos = opciones.Value.EstadosActivos;

    public Task<bool> TienePermisoAsync(
        int usuarioId,
        string rolId,
        string codigoPermiso,
        CancellationToken cancellationToken) =>
        contexto.Usuarios
            .AsNoTracking()
            .AnyAsync(
                usuario =>
                    usuario.Id == usuarioId
                    && usuario.RoleId == rolId
                    && usuario.Estado.Activo
                    && _estadosActivos.Contains(usuario.Estado.Nombre)
                    && usuario.Role.RolesPermisos.Any(rolPermiso =>
                        rolPermiso.Permitido
                        && rolPermiso.Permiso.Codigo == codigoPermiso
                        && rolPermiso.Permiso.Modulo.Activo
                        && rolPermiso.Permiso.Estado.Activo
                        && _estadosActivos.Contains(rolPermiso.Permiso.Estado.Nombre)
                        && rolPermiso.Permiso.Accion.Estado.Activo
                        && _estadosActivos.Contains(rolPermiso.Permiso.Accion.Estado.Nombre)),
                cancellationToken);

    public async Task<IReadOnlyList<string>> ObtenerPermisosAsync(
        int usuarioId,
        CancellationToken cancellationToken) =>
        await contexto.RolesPermisos
            .AsNoTracking()
            .Where(rolPermiso =>
                rolPermiso.Role.Usuarios.Any(usuario => usuario.Id == usuarioId)
                && rolPermiso.Permitido
                && rolPermiso.Permiso.Modulo.Activo
                && rolPermiso.Permiso.Estado.Activo
                && _estadosActivos.Contains(rolPermiso.Permiso.Estado.Nombre)
                && rolPermiso.Permiso.Accion.Estado.Activo
                && _estadosActivos.Contains(rolPermiso.Permiso.Accion.Estado.Nombre))
            .Select(rolPermiso => rolPermiso.Permiso.Codigo)
            .Distinct()
            .OrderBy(codigo => codigo)
            .ToArrayAsync(cancellationToken);
}
