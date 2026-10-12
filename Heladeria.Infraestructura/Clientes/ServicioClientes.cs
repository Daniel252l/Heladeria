using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using Heladeria.Aplicacion.Clientes;
using Heladeria.Aplicacion.Configuracion;
using Heladeria.Aplicacion.Seguridad.Configuracion;
using Heladeria.Infraestructura.Persistencia;
using Heladeria.Infraestructura.Persistencia.Entidades;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using EntidadCliente = Heladeria.Infraestructura.Persistencia.Entidades.Clientes;

namespace Heladeria.Infraestructura.ClientesGestion;

public sealed class ServicioClientes(HeladeriaDbContext contexto, IOptions<OpcionesSeguridad> opciones,
    TimeProvider reloj) : IServicioClientes
{
    private readonly string[] _operativos = opciones.Value.EstadosActivos;
    private static readonly Expression<Func<EntidadCliente, ClienteConsultaDto>> Proyectar = x => new(
        x.Id, x.EmpresaId, x.CodigoCliente, x.NombreCliente, x.Nit, x.Telefono, x.Correo,
        x.LimiteCredito, x.DiasCredito, x.EstadoId, x.Estado.Nombre);

    public async Task<RespuestaPaginada<ClienteConsultaDto>> ListarAsync(FiltroConEstadoSolicitud filtro,
        ContextoConfiguracion seguridad, CancellationToken token)
    {
        Validar(filtro);
        await ValidarAlcanceAsync(seguridad, token);
        var query = Consulta(seguridad);
        if (filtro.EstadoId is { } estado) query = query.Where(x => x.EstadoId == estado);
        if (Opcional(filtro.Busqueda)?.ToUpperInvariant() is { } texto)
            query = query.Where(x => x.CodigoCliente.ToUpper().Contains(texto) || x.NombreCliente.ToUpper().Contains(texto)
                || x.Nit != null && x.Nit.ToUpper().Contains(texto));
        var total = await query.CountAsync(token);
        // Long evita desbordar la multiplicación con una página recibida por HTTP.
        var salto = ((long)filtro.Pagina - 1) * filtro.TamanioPagina;
        var elementos = salto >= total ? [] : await query.OrderBy(x => x.NombreCliente).ThenBy(x => x.Id)
            .Skip((int)salto).Take(filtro.TamanioPagina).Select(Proyectar).ToArrayAsync(token);
        return new(elementos, filtro.Pagina, filtro.TamanioPagina, total);
    }

    public async Task<ClienteConsultaDto> ObtenerAsync(int id, ContextoConfiguracion seguridad, CancellationToken token)
    {
        await ValidarAlcanceAsync(seguridad, token);
        return await Consulta(seguridad).Where(x => x.Id == id).Select(Proyectar).SingleOrDefaultAsync(token)
            ?? throw new ExcepcionClientes("Cliente no encontrado.", "CLIENTE_NO_ENCONTRADO", 404);
    }

    public async Task<ClienteConsultaDto> CrearAsync(CrearClienteSolicitud solicitud, ContextoConfiguracion seguridad,
        CancellationToken token)
    {
        Validar(solicitud);
        if (contexto.ChangeTracker.HasChanges())
            throw new ExcepcionClientes("Hay cambios pendientes ajenos al alta de cliente.", "CONFLICTO_CLIENTES", 409);
        // Alta y auditoría son una sola unidad; la clave única de SQL Server arbitra altas concurrentes.
        return await contexto.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            contexto.ChangeTracker.Clear();
            await using var transaccion = await contexto.Database.BeginTransactionAsync(token);
            try
            {
                await ValidarAlcanceAsync(seguridad, token);
                var estado = await contexto.Estados.AsNoTracking()
                    .Where(x => x.Activo && _operativos.Contains(x.Nombre) && (x.ModuloId == null || x.ModuloId == "VENTAS"))
                    .OrderBy(x => x.ModuloId == null ? 0 : 1).ThenBy(x => x.Id).FirstOrDefaultAsync(token)
                    ?? throw new ExcepcionClientes("No hay un estado operativo general o de ventas disponible para clientes.", "ESTADO_CLIENTE_NO_DISPONIBLE", 409);
                var codigo = solicitud.CodigoCliente.Trim().ToUpperInvariant();
                if (await Consulta(seguridad).AnyAsync(x => x.CodigoCliente.Trim().ToUpper() == codigo, token))
                    throw Duplicado();
                var entidad = new EntidadCliente
                {
                    EmpresaId = seguridad.EmpresaId, CodigoCliente = codigo, NombreCliente = solicitud.NombreCliente.Trim(),
                    Nit = Opcional(solicitud.Nit), Telefono = Opcional(solicitud.Telefono), Correo = Opcional(solicitud.Correo),
                    LimiteCredito = solicitud.LimiteCredito, DiasCredito = solicitud.DiasCredito, EstadoId = estado.Id
                };
                contexto.Clientes.Add(entidad);
                try { await contexto.SaveChangesAsync(token); }
                catch (DbUpdateException e) when (e.InnerException is SqlException { Number: 2601 or 2627 })
                {
                    throw Duplicado();
                }
                contexto.Bitacoras.Add(new Bitacoras
                {
                    EmpresaId = seguridad.EmpresaId, SucursalId = seguridad.SucursalId, UsuarioId = seguridad.UsuarioId,
                    ModuloId = "VENTAS", Entidad = "Clientes", EntidadId = entidad.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    Accion = "CLIENTE_CREADO", Fecha = reloj.GetUtcNow().UtcDateTime, Comentario = "Alta de cliente.",
                    DireccionIp = Limitar(seguridad.DireccionIp, 45), Equipo = Limitar(seguridad.Equipo, 200)
                });
                await contexto.SaveChangesAsync(token);
                var resultado = await Consulta(seguridad).Where(x => x.Id == entidad.Id).Select(Proyectar).SingleAsync(token);
                await transaccion.CommitAsync(token);
                return resultado;
            }
            catch
            {
                await transaccion.RollbackAsync(CancellationToken.None);
                contexto.ChangeTracker.Clear();
                throw;
            }
        });
    }

    private IQueryable<EntidadCliente> Consulta(ContextoConfiguracion seguridad) =>
        contexto.Clientes.AsNoTracking().Where(x => x.EmpresaId == seguridad.EmpresaId);

    private async Task ValidarAlcanceAsync(ContextoConfiguracion s, CancellationToken token)
    {
        if (!await contexto.UsuariosSucursales.AsNoTracking().AnyAsync(x => x.UsuarioId == s.UsuarioId
            && x.SucursalId == s.SucursalId && x.Sucursal.EmpresaId == s.EmpresaId
            && x.Usuario.Estado.Activo && _operativos.Contains(x.Usuario.Estado.Nombre)
            && x.Estado.Activo && _operativos.Contains(x.Estado.Nombre)
            && x.Sucursal.Estado.Activo && _operativos.Contains(x.Sucursal.Estado.Nombre)
            && x.Sucursal.Empresa.Estado.Activo && _operativos.Contains(x.Sucursal.Empresa.Estado.Nombre), token))
            throw new ExcepcionClientes("La empresa o sucursal no está disponible en la sesión autorizada.", "ALCANCE_CLIENTES_NO_VALIDO", 403);
    }

    private static void Validar(object solicitud)
    {
        var errores = new List<ValidationResult>();
        if (!Validator.TryValidateObject(solicitud, new(solicitud), errores, true))
            throw new ExcepcionClientes(string.Join(" ", errores.Select(x => x.ErrorMessage)), "VALIDACION_CLIENTES", 400);
    }
    private static ExcepcionClientes Duplicado() => new("Ya existe un cliente con ese código en la empresa.", "CLIENTE_DUPLICADO", 409);
    private static string? Opcional(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
    private static string? Limitar(string? valor, int longitud) => valor is null ? null : valor[..Math.Min(valor.Length, longitud)];
}
