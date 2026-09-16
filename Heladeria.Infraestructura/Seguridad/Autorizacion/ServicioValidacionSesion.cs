using Heladeria.Aplicacion.Seguridad.Autorizacion;
using Heladeria.Aplicacion.Seguridad.Configuracion;
using Heladeria.Infraestructura.Persistencia;
using Heladeria.Infraestructura.Seguridad.Autenticacion;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Heladeria.Infraestructura.Seguridad.Autorizacion;

public sealed class ServicioValidacionSesion(
    HeladeriaDbContext contexto,
    IOptions<OpcionesSeguridad> opciones,
    TimeProvider timeProvider) : IServicioValidacionSesion
{
    private readonly OpcionesSeguridad _opciones = opciones.Value;

    public Task<bool> EsValidaAsync(
        int usuarioId,
        long sesionId,
        long sucursalId,
        CancellationToken cancellationToken)
    {
        var filtroSesionVigente = ReglasSesionPersistida.CrearFiltroVigente(
            timeProvider.GetUtcNow(),
            _opciones.Jwt.DiasRefreshToken);
        var estadosActivos = _opciones.EstadosActivos;

        return contexto.SesionesUsuarios
            .AsNoTracking()
            .Where(filtroSesionVigente)
            .AnyAsync(
                sesion =>
                    sesion.Id == sesionId
                    && sesion.UsuarioId == usuarioId
                    && sesion.Usuario.Estado.Activo
                    && estadosActivos.Contains(sesion.Usuario.Estado.Nombre)
                    && sesion.Usuario.UsuariosSucursales.Any(asignacion =>
                        asignacion.SucursalId == sucursalId
                        && asignacion.Estado.Activo
                        && estadosActivos.Contains(asignacion.Estado.Nombre)
                        && asignacion.Sucursal.Estado.Activo
                        && estadosActivos.Contains(asignacion.Sucursal.Estado.Nombre)
                        && asignacion.Sucursal.Empresa.Estado.Activo
                        && estadosActivos.Contains(asignacion.Sucursal.Empresa.Estado.Nombre)),
                cancellationToken);
    }
}
