using Heladeria.Aplicacion.Seguridad.Autorizacion;
using Heladeria.Infraestructura.Persistencia;
using Heladeria.Infraestructura.Persistencia.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Heladeria.Infraestructura.Seguridad.Autorizacion;

public sealed class ServicioAuditoriaSeguridad(
    HeladeriaDbContext contexto,
    TimeProvider timeProvider) : IServicioAuditoriaSeguridad
{
    public async Task RegistrarAccesoDenegadoAsync(
        int usuarioId,
        long? sucursalId,
        string recurso,
        string? direccionIp,
        CancellationToken cancellationToken)
    {
        var empresaId = sucursalId is null
            ? null
            : await contexto.Sucursales
                .Where(sucursal => sucursal.Id == sucursalId)
                .Select(sucursal => (int?)sucursal.EmpresaId)
                .SingleOrDefaultAsync(cancellationToken);

        contexto.Bitacoras.Add(new Bitacoras
        {
            EmpresaId = empresaId,
            SucursalId = sucursalId,
            Entidad = "Permisos",
            Accion = "ACCESO_DENEGADO",
            Fecha = timeProvider.GetUtcNow().UtcDateTime,
            UsuarioId = usuarioId,
            DireccionIp = Recortar(direccionIp, 45),
            Comentario = Recortar($"Permiso requerido: {recurso}", 500)
        });

        await contexto.SaveChangesAsync(cancellationToken);
    }

    private static string? Recortar(string? valor, int longitudMaxima)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return null;
        }

        var limpio = valor.Trim();
        return limpio[..Math.Min(limpio.Length, longitudMaxima)];
    }
}
