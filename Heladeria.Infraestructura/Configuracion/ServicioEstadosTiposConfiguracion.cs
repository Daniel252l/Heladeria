using Heladeria.Aplicacion.Configuracion;
using Heladeria.Infraestructura.Persistencia;
using Heladeria.Infraestructura.Persistencia.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Heladeria.Infraestructura.Configuracion;

public sealed class ServicioEstadosTiposConfiguracion(
    HeladeriaDbContext contexto,
    IAuditoriaConfiguracion auditoria)
    : ServicioConfiguracionBase(contexto, auditoria), IServicioEstadosTiposConfiguracion
{
    public async Task<RespuestaPaginada<ClaseEstadoDto>> ListarClasesEstadoAsync(
        FiltroActivosSolicitud filtro,
        CancellationToken cancellationToken)
    {
        var consulta = Contexto.ClaseEstados.AsNoTracking().AsQueryable();
        var busqueda = NormalizarOpcional(filtro.Busqueda);
        if (busqueda is not null)
        {
            consulta = consulta.Where(item => item.Nombre.Contains(busqueda));
        }

        if (filtro.Activo.HasValue)
        {
            consulta = consulta.Where(item => item.Activo == filtro.Activo.Value);
        }

        var total = await consulta.CountAsync(cancellationToken);
        var elementos = await Paginar(consulta.OrderBy(item => item.Nombre).ThenBy(item => item.Id), filtro)
            .Select(item => new ClaseEstadoDto(item.Id, item.Nombre, item.Activo))
            .ToArrayAsync(cancellationToken);
        return new(elementos, filtro.Pagina, filtro.TamanioPagina, total);
    }

    public async Task<ClaseEstadoDto> ObtenerClaseEstadoAsync(int id, CancellationToken cancellationToken) =>
        await Contexto.ClaseEstados.AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => new ClaseEstadoDto(item.Id, item.Nombre, item.Activo))
            .SingleOrDefaultAsync(cancellationToken)
        ?? throw new RecursoConfiguracionNoEncontradoException("La clase de estados no existe.");

    public async Task<ClaseEstadoDto> CrearClaseEstadoAsync(
        GuardarClaseEstadoSolicitud solicitud,
        ContextoConfiguracion contextoSeguridad,
        CancellationToken cancellationToken)
    {
        var nombre = NormalizarTexto(solicitud.Nombre);
        await using var transaccion = await IniciarTransaccionSerializableAsync("ClaseEstados", cancellationToken);
        await ValidarNombreClaseEstadoAsync(nombre, null, cancellationToken);
        var id = checked((await Contexto.ClaseEstados.MaxAsync(item => (int?)item.Id, cancellationToken) ?? 0) + 1);
        var entidad = new ClaseEstados { Id = id, Nombre = nombre, Activo = solicitud.Activo };
        Contexto.ClaseEstados.Add(entidad);
        Auditoria.Registrar(contextoSeguridad, "ClaseEstados", id.ToString(), "CREAR", "Clase de estados creada.");
        await Contexto.SaveChangesAsync(cancellationToken);
        await ConfirmarAsync(transaccion, cancellationToken);
        return new(entidad.Id, entidad.Nombre, entidad.Activo);
    }

    public async Task<ClaseEstadoDto> ActualizarClaseEstadoAsync(
        int id,
        GuardarClaseEstadoSolicitud solicitud,
        ContextoConfiguracion contextoSeguridad,
        CancellationToken cancellationToken)
    {
        var entidad = await Contexto.ClaseEstados.SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new RecursoConfiguracionNoEncontradoException("La clase de estados no existe.");
        if (EsClaseSeguridad(entidad) && (!solicitud.Activo || !string.Equals(entidad.Nombre, solicitud.Nombre.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            throw new ConflictoConfiguracionException("La clase estructural de seguridad no puede renombrarse ni desactivarse.");
        }

        var nombre = NormalizarTexto(solicitud.Nombre);
        await ValidarNombreClaseEstadoAsync(nombre, id, cancellationToken);
        entidad.Nombre = nombre;
        entidad.Activo = solicitud.Activo;
        Auditoria.Registrar(contextoSeguridad, "ClaseEstados", id.ToString(), "ACTUALIZAR", "Clase de estados actualizada.");
        await Contexto.SaveChangesAsync(cancellationToken);
        return new(entidad.Id, entidad.Nombre, entidad.Activo);
    }

    public async Task<ClaseEstadoDto> CambiarActivoClaseEstadoAsync(
        int id,
        bool activo,
        ContextoConfiguracion contextoSeguridad,
        CancellationToken cancellationToken)
    {
        var entidad = await Contexto.ClaseEstados.SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new RecursoConfiguracionNoEncontradoException("La clase de estados no existe.");
        if (!activo && EsClaseSeguridad(entidad))
        {
            throw new ConflictoConfiguracionException("La clase estructural de seguridad no puede desactivarse.");
        }

        entidad.Activo = activo;
        Auditoria.Registrar(contextoSeguridad, "ClaseEstados", id.ToString(), "CAMBIAR_ESTADO", $"Activo={activo}.");
        await Contexto.SaveChangesAsync(cancellationToken);
        return new(entidad.Id, entidad.Nombre, entidad.Activo);
    }

    public async Task<RespuestaPaginada<EstadoDto>> ListarEstadosAsync(
        FiltroEstadosSolicitud filtro,
        CancellationToken cancellationToken)
    {
        var consulta = Contexto.Estados.AsNoTracking().AsQueryable();
        var busqueda = NormalizarOpcional(filtro.Busqueda);
        var moduloId = NormalizarOpcional(filtro.ModuloId)?.ToUpperInvariant();
        if (busqueda is not null) consulta = consulta.Where(item => item.Nombre.Contains(busqueda));
        if (filtro.Activo.HasValue) consulta = consulta.Where(item => item.Activo == filtro.Activo.Value);
        if (filtro.ClaseEstadoId.HasValue) consulta = consulta.Where(item => item.ClaseEstadoId == filtro.ClaseEstadoId.Value);
        if (moduloId is not null) consulta = consulta.Where(item => item.ModuloId == moduloId);

        var total = await consulta.CountAsync(cancellationToken);
        var elementos = await Paginar(consulta.OrderBy(item => item.Nombre).ThenBy(item => item.Id), filtro)
            .Select(item => new EstadoDto(item.Id, item.Nombre, item.Activo, item.ClaseEstadoId, item.ClaseEstado.Nombre, item.ModuloId))
            .ToArrayAsync(cancellationToken);
        return new(elementos, filtro.Pagina, filtro.TamanioPagina, total);
    }

    public async Task<EstadoDto> ObtenerEstadoAsync(int id, CancellationToken cancellationToken) =>
        await Contexto.Estados.AsNoTracking().Where(item => item.Id == id)
            .Select(item => new EstadoDto(item.Id, item.Nombre, item.Activo, item.ClaseEstadoId, item.ClaseEstado.Nombre, item.ModuloId))
            .SingleOrDefaultAsync(cancellationToken)
        ?? throw new RecursoConfiguracionNoEncontradoException("El estado no existe.");

    public async Task<EstadoDto> CrearEstadoAsync(
        GuardarEstadoSolicitud solicitud,
        ContextoConfiguracion contextoSeguridad,
        CancellationToken cancellationToken)
    {
        var nombre = NormalizarTexto(solicitud.Nombre);
        var moduloId = NormalizarOpcional(solicitud.ModuloId)?.ToUpperInvariant();
        await using var transaccion = await IniciarTransaccionSerializableAsync("Estados", cancellationToken);
        await ValidarDependenciasEstadoAsync(solicitud.ClaseEstadoId, moduloId, cancellationToken);
        await ValidarNombreEstadoAsync(nombre, solicitud.ClaseEstadoId, moduloId, null, cancellationToken);
        var id = checked((await Contexto.Estados.MaxAsync(item => (int?)item.Id, cancellationToken) ?? 0) + 1);
        var entidad = new Estados { Id = id, Nombre = nombre, Activo = solicitud.Activo, ClaseEstadoId = solicitud.ClaseEstadoId, ModuloId = moduloId };
        Contexto.Estados.Add(entidad);
        Auditoria.Registrar(contextoSeguridad, "Estados", id.ToString(), "CREAR", "Estado creado.");
        await Contexto.SaveChangesAsync(cancellationToken);
        await ConfirmarAsync(transaccion, cancellationToken);
        return await ObtenerEstadoAsync(id, cancellationToken);
    }

    public async Task<EstadoDto> ActualizarEstadoAsync(
        int id,
        GuardarEstadoSolicitud solicitud,
        ContextoConfiguracion contextoSeguridad,
        CancellationToken cancellationToken)
    {
        var entidad = await Contexto.Estados.SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new RecursoConfiguracionNoEncontradoException("El estado no existe.");
        if (await EsEstadoEstructuralAsync(entidad, cancellationToken)
            && (!solicitud.Activo
                || !string.Equals(entidad.Nombre, solicitud.Nombre.Trim(), StringComparison.OrdinalIgnoreCase)
                || entidad.ClaseEstadoId != solicitud.ClaseEstadoId
                || !string.Equals(entidad.ModuloId, NormalizarOpcional(solicitud.ModuloId), StringComparison.OrdinalIgnoreCase)))
        {
            throw new ConflictoConfiguracionException("El estado activo estructural de seguridad no puede alterarse de esa forma.");
        }

        var nombre = NormalizarTexto(solicitud.Nombre);
        var moduloId = NormalizarOpcional(solicitud.ModuloId)?.ToUpperInvariant();
        await ValidarDependenciasEstadoAsync(solicitud.ClaseEstadoId, moduloId, cancellationToken);
        await ValidarNombreEstadoAsync(nombre, solicitud.ClaseEstadoId, moduloId, id, cancellationToken);
        entidad.Nombre = nombre;
        entidad.Activo = solicitud.Activo;
        entidad.ClaseEstadoId = solicitud.ClaseEstadoId;
        entidad.ModuloId = moduloId;
        Auditoria.Registrar(contextoSeguridad, "Estados", id.ToString(), "ACTUALIZAR", "Estado actualizado.");
        await Contexto.SaveChangesAsync(cancellationToken);
        return await ObtenerEstadoAsync(id, cancellationToken);
    }

    public async Task<EstadoDto> CambiarActivoEstadoAsync(
        int id,
        bool activo,
        ContextoConfiguracion contextoSeguridad,
        CancellationToken cancellationToken)
    {
        var entidad = await Contexto.Estados.SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new RecursoConfiguracionNoEncontradoException("El estado no existe.");
        if (!activo && await EsEstadoEstructuralAsync(entidad, cancellationToken))
        {
            throw new ConflictoConfiguracionException("El estado activo estructural de seguridad no puede desactivarse.");
        }

        entidad.Activo = activo;
        Auditoria.Registrar(contextoSeguridad, "Estados", id.ToString(), "CAMBIAR_ESTADO", $"Activo={activo}.");
        await Contexto.SaveChangesAsync(cancellationToken);
        return await ObtenerEstadoAsync(id, cancellationToken);
    }

    public async Task<RespuestaPaginada<ClaseTipoDto>> ListarClasesTipoAsync(
        FiltroConEstadoSolicitud filtro,
        CancellationToken cancellationToken)
    {
        var consulta = Contexto.ClaseTipos.AsNoTracking().AsQueryable();
        var busqueda = NormalizarOpcional(filtro.Busqueda);
        if (busqueda is not null) consulta = consulta.Where(item => item.Nombre.Contains(busqueda));
        if (filtro.EstadoId.HasValue) consulta = consulta.Where(item => item.EstadoId == filtro.EstadoId.Value);
        var total = await consulta.CountAsync(cancellationToken);
        var elementos = await Paginar(consulta.OrderBy(item => item.Nombre).ThenBy(item => item.Id), filtro)
            .Select(item => new ClaseTipoDto(item.Id, item.Nombre, item.EstadoId, item.Estado.Nombre))
            .ToArrayAsync(cancellationToken);
        return new(elementos, filtro.Pagina, filtro.TamanioPagina, total);
    }

    public async Task<ClaseTipoDto> ObtenerClaseTipoAsync(int id, CancellationToken cancellationToken) =>
        await Contexto.ClaseTipos.AsNoTracking().Where(item => item.Id == id)
            .Select(item => new ClaseTipoDto(item.Id, item.Nombre, item.EstadoId, item.Estado.Nombre))
            .SingleOrDefaultAsync(cancellationToken)
        ?? throw new RecursoConfiguracionNoEncontradoException("La clase de tipos no existe.");

    public async Task<ClaseTipoDto> CrearClaseTipoAsync(GuardarClaseTipoSolicitud solicitud, ContextoConfiguracion contextoSeguridad, CancellationToken cancellationToken)
    {
        var nombre = NormalizarTexto(solicitud.Nombre);
        await using var transaccion = await IniciarTransaccionSerializableAsync("ClaseTipos", cancellationToken);
        await ValidarEstadoDisponibleAsync(solicitud.EstadoId, cancellationToken);
        await ValidarNombreClaseTipoAsync(nombre, null, cancellationToken);
        var id = checked((await Contexto.ClaseTipos.MaxAsync(item => (int?)item.Id, cancellationToken) ?? 0) + 1);
        Contexto.ClaseTipos.Add(new ClaseTipos { Id = id, Nombre = nombre, EstadoId = solicitud.EstadoId });
        Auditoria.Registrar(contextoSeguridad, "ClaseTipos", id.ToString(), "CREAR", "Clase de tipos creada.");
        await Contexto.SaveChangesAsync(cancellationToken);
        await ConfirmarAsync(transaccion, cancellationToken);
        return await ObtenerClaseTipoAsync(id, cancellationToken);
    }

    public async Task<ClaseTipoDto> ActualizarClaseTipoAsync(int id, GuardarClaseTipoSolicitud solicitud, ContextoConfiguracion contextoSeguridad, CancellationToken cancellationToken)
    {
        var entidad = await Contexto.ClaseTipos.SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new RecursoConfiguracionNoEncontradoException("La clase de tipos no existe.");
        if (EsClaseSeguridad(entidad) && (!string.Equals(entidad.Nombre, solicitud.Nombre.Trim(), StringComparison.OrdinalIgnoreCase) || entidad.EstadoId != solicitud.EstadoId))
        {
            throw new ConflictoConfiguracionException("La clase estructural de tipos de seguridad no puede modificarse.");
        }

        var nombre = NormalizarTexto(solicitud.Nombre);
        await ValidarEstadoDisponibleAsync(solicitud.EstadoId, cancellationToken);
        await ValidarNombreClaseTipoAsync(nombre, id, cancellationToken);
        entidad.Nombre = nombre;
        entidad.EstadoId = solicitud.EstadoId;
        Auditoria.Registrar(contextoSeguridad, "ClaseTipos", id.ToString(), "ACTUALIZAR", "Clase de tipos actualizada.");
        await Contexto.SaveChangesAsync(cancellationToken);
        return await ObtenerClaseTipoAsync(id, cancellationToken);
    }

    public async Task<ClaseTipoDto> CambiarEstadoClaseTipoAsync(int id, int estadoId, ContextoConfiguracion contextoSeguridad, CancellationToken cancellationToken)
    {
        var entidad = await Contexto.ClaseTipos.SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new RecursoConfiguracionNoEncontradoException("La clase de tipos no existe.");
        if (EsClaseSeguridad(entidad) && entidad.EstadoId != estadoId)
        {
            throw new ConflictoConfiguracionException("La clase estructural de tipos de seguridad no puede desactivarse.");
        }

        await ValidarEstadoDisponibleAsync(estadoId, cancellationToken);
        entidad.EstadoId = estadoId;
        Auditoria.Registrar(contextoSeguridad, "ClaseTipos", id.ToString(), "CAMBIAR_ESTADO", $"EstadoId={estadoId}.");
        await Contexto.SaveChangesAsync(cancellationToken);
        return await ObtenerClaseTipoAsync(id, cancellationToken);
    }

    public async Task<RespuestaPaginada<TipoDto>> ListarTiposAsync(FiltroTiposSolicitud filtro, CancellationToken cancellationToken)
    {
        var consulta = Contexto.Tipos.AsNoTracking().AsQueryable();
        var busqueda = NormalizarOpcional(filtro.Busqueda);
        var moduloId = NormalizarOpcional(filtro.ModuloId)?.ToUpperInvariant();
        if (busqueda is not null) consulta = consulta.Where(item => item.Nombre.Contains(busqueda));
        if (filtro.EstadoId.HasValue) consulta = consulta.Where(item => item.EstadoId == filtro.EstadoId.Value);
        if (filtro.ClaseTipoId.HasValue) consulta = consulta.Where(item => item.ClaseTipoId == filtro.ClaseTipoId.Value);
        if (moduloId is not null) consulta = consulta.Where(item => item.ModuloId == moduloId);
        var total = await consulta.CountAsync(cancellationToken);
        var elementos = await Paginar(consulta.OrderBy(item => item.Nombre).ThenBy(item => item.Id), filtro)
            .Select(item => new TipoDto(item.Id, item.Nombre, item.EstadoId, item.Estado.Nombre, item.ModuloId, item.ClaseTipoId, item.ClaseTipo.Nombre))
            .ToArrayAsync(cancellationToken);
        return new(elementos, filtro.Pagina, filtro.TamanioPagina, total);
    }

    public async Task<TipoDto> ObtenerTipoAsync(int id, CancellationToken cancellationToken) =>
        await Contexto.Tipos.AsNoTracking().Where(item => item.Id == id)
            .Select(item => new TipoDto(item.Id, item.Nombre, item.EstadoId, item.Estado.Nombre, item.ModuloId, item.ClaseTipoId, item.ClaseTipo.Nombre))
            .SingleOrDefaultAsync(cancellationToken)
        ?? throw new RecursoConfiguracionNoEncontradoException("El tipo no existe.");

    public async Task<TipoDto> CrearTipoAsync(GuardarTipoSolicitud solicitud, ContextoConfiguracion contextoSeguridad, CancellationToken cancellationToken)
    {
        var nombre = NormalizarTexto(solicitud.Nombre);
        var moduloId = NormalizarOpcional(solicitud.ModuloId)?.ToUpperInvariant();
        await using var transaccion = await IniciarTransaccionSerializableAsync("Tipos", cancellationToken);
        await ValidarDependenciasTipoAsync(solicitud.EstadoId, solicitud.ClaseTipoId, moduloId, cancellationToken);
        await ValidarNombreTipoAsync(nombre, solicitud.ClaseTipoId, null, cancellationToken);
        var id = checked((await Contexto.Tipos.MaxAsync(item => (int?)item.Id, cancellationToken) ?? 0) + 1);
        Contexto.Tipos.Add(new Tipos { Id = id, Nombre = nombre, EstadoId = solicitud.EstadoId, ClaseTipoId = solicitud.ClaseTipoId, ModuloId = moduloId });
        Auditoria.Registrar(contextoSeguridad, "Tipos", id.ToString(), "CREAR", "Tipo creado.");
        await Contexto.SaveChangesAsync(cancellationToken);
        await ConfirmarAsync(transaccion, cancellationToken);
        return await ObtenerTipoAsync(id, cancellationToken);
    }

    public async Task<TipoDto> ActualizarTipoAsync(int id, GuardarTipoSolicitud solicitud, ContextoConfiguracion contextoSeguridad, CancellationToken cancellationToken)
    {
        var entidad = await Contexto.Tipos.SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new RecursoConfiguracionNoEncontradoException("El tipo no existe.");
        var estructural = await Contexto.Roles.AnyAsync(item => item.TipoId == id, cancellationToken)
            || await Contexto.Usuarios.AnyAsync(item => item.TipoInicioSesionId == id, cancellationToken);
        var moduloId = NormalizarOpcional(solicitud.ModuloId)?.ToUpperInvariant();
        if (estructural && (!string.Equals(entidad.Nombre, solicitud.Nombre.Trim(), StringComparison.OrdinalIgnoreCase)
            || entidad.EstadoId != solicitud.EstadoId || entidad.ClaseTipoId != solicitud.ClaseTipoId || entidad.ModuloId != moduloId))
        {
            throw new ConflictoConfiguracionException("Un tipo estructural utilizado por autenticación o roles no puede modificarse.");
        }

        var nombre = NormalizarTexto(solicitud.Nombre);
        await ValidarDependenciasTipoAsync(solicitud.EstadoId, solicitud.ClaseTipoId, moduloId, cancellationToken);
        await ValidarNombreTipoAsync(nombre, solicitud.ClaseTipoId, id, cancellationToken);
        entidad.Nombre = nombre;
        entidad.EstadoId = solicitud.EstadoId;
        entidad.ClaseTipoId = solicitud.ClaseTipoId;
        entidad.ModuloId = moduloId;
        Auditoria.Registrar(contextoSeguridad, "Tipos", id.ToString(), "ACTUALIZAR", "Tipo actualizado.");
        await Contexto.SaveChangesAsync(cancellationToken);
        return await ObtenerTipoAsync(id, cancellationToken);
    }

    public async Task<TipoDto> CambiarEstadoTipoAsync(int id, int estadoId, ContextoConfiguracion contextoSeguridad, CancellationToken cancellationToken)
    {
        var entidad = await Contexto.Tipos.SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new RecursoConfiguracionNoEncontradoException("El tipo no existe.");
        if (entidad.EstadoId != estadoId && (await Contexto.Roles.AnyAsync(item => item.TipoId == id, cancellationToken)
            || await Contexto.Usuarios.AnyAsync(item => item.TipoInicioSesionId == id, cancellationToken)))
        {
            throw new ConflictoConfiguracionException("Un tipo estructural utilizado por autenticación o roles no puede desactivarse.");
        }

        await ValidarEstadoDisponibleAsync(estadoId, cancellationToken);
        entidad.EstadoId = estadoId;
        Auditoria.Registrar(contextoSeguridad, "Tipos", id.ToString(), "CAMBIAR_ESTADO", $"EstadoId={estadoId}.");
        await Contexto.SaveChangesAsync(cancellationToken);
        return await ObtenerTipoAsync(id, cancellationToken);
    }

    private async Task ValidarNombreClaseEstadoAsync(string nombre, int? id, CancellationToken token)
    {
        if (await Contexto.ClaseEstados.AnyAsync(
                item => item.Nombre == nombre && (!id.HasValue || item.Id != id.Value),
                token))
        {
            throw new ConflictoConfiguracionException(
                "Ya existe una clase de estados con ese nombre.");
        }
    }

    private async Task ValidarNombreEstadoAsync(string nombre, int claseId, string? moduloId, int? id, CancellationToken token)
    {
        if (await Contexto.Estados.AnyAsync(item => item.Nombre == nombre && item.ClaseEstadoId == claseId && item.ModuloId == moduloId && (!id.HasValue || item.Id != id.Value), token))
            throw new ConflictoConfiguracionException("Ya existe un estado con ese nombre, clase y módulo.");
    }

    private async Task ValidarDependenciasEstadoAsync(int claseId, string? moduloId, CancellationToken token)
    {
        if (!await Contexto.ClaseEstados.AsNoTracking().AnyAsync(item => item.Id == claseId && item.Activo, token))
            throw new ValidacionConfiguracionException("La clase de estados no existe o está inactiva.");
        if (moduloId is not null && !await Contexto.Modulos.AsNoTracking().AnyAsync(item => item.Id == moduloId && item.Activo, token))
            throw new ValidacionConfiguracionException("El módulo no existe o está inactivo.");
    }

    private async Task ValidarNombreClaseTipoAsync(string nombre, int? id, CancellationToken token)
    {
        if (await Contexto.ClaseTipos.AnyAsync(item => item.Nombre == nombre && (!id.HasValue || item.Id != id.Value), token))
            throw new ConflictoConfiguracionException("Ya existe una clase de tipos con ese nombre.");
    }

    private async Task ValidarNombreTipoAsync(string nombre, int claseId, int? id, CancellationToken token)
    {
        if (await Contexto.Tipos.AnyAsync(item => item.Nombre == nombre && item.ClaseTipoId == claseId && (!id.HasValue || item.Id != id.Value), token))
            throw new ConflictoConfiguracionException("Ya existe un tipo con ese nombre dentro de la clase.");
    }

    private async Task ValidarDependenciasTipoAsync(int estadoId, int claseId, string? moduloId, CancellationToken token)
    {
        await ValidarEstadoDisponibleAsync(estadoId, token);
        if (!await Contexto.ClaseTipos.AsNoTracking().AnyAsync(item => item.Id == claseId && item.Estado.Activo, token))
            throw new ValidacionConfiguracionException("La clase de tipos no existe o no está disponible.");
        if (moduloId is not null && !await Contexto.Modulos.AsNoTracking().AnyAsync(item => item.Id == moduloId && item.Activo, token))
            throw new ValidacionConfiguracionException("El módulo no existe o está inactivo.");
    }

    private async Task<bool> EsEstadoEstructuralAsync(Estados entidad, CancellationToken token) =>
        string.Equals(entidad.Nombre, "Activo", StringComparison.OrdinalIgnoreCase)
        && (await Contexto.Usuarios.AnyAsync(item => item.EstadoId == entidad.Id, token)
            || await Contexto.UsuariosSucursales.AnyAsync(item => item.EstadoId == entidad.Id, token)
            || await Contexto.Permisos.AnyAsync(item => item.EstadoId == entidad.Id, token));

    private static bool EsClaseSeguridad(ClaseEstados entidad) =>
        string.Equals(entidad.Nombre, "Seguridad", StringComparison.OrdinalIgnoreCase);

    private static bool EsClaseSeguridad(ClaseTipos entidad) =>
        string.Equals(entidad.Nombre, "Seguridad", StringComparison.OrdinalIgnoreCase);
}
