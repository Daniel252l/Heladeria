using Heladeria.Aplicacion.Configuracion;
using Heladeria.Infraestructura.Persistencia;
using Heladeria.Infraestructura.Persistencia.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Heladeria.Infraestructura.Configuracion;

public sealed class ServicioCatalogosConfiguracion(
    HeladeriaDbContext contexto,
    IAuditoriaConfiguracion auditoria)
    : ServicioConfiguracionBase(contexto, auditoria), IServicioCatalogosConfiguracion
{
    public async Task<RespuestaPaginada<ModuloDto>> ListarModulosAsync(FiltroActivosSolicitud filtro, CancellationToken cancellationToken)
    {
        var consulta = Contexto.Modulos.AsNoTracking().AsQueryable();
        var busqueda = NormalizarOpcional(filtro.Busqueda);
        if (busqueda is not null) consulta = consulta.Where(item => item.Id.Contains(busqueda) || item.Nombre.Contains(busqueda));
        if (filtro.Activo.HasValue) consulta = consulta.Where(item => item.Activo == filtro.Activo.Value);
        var total = await consulta.CountAsync(cancellationToken);
        var elementos = await Paginar(consulta.OrderBy(item => item.Nombre).ThenBy(item => item.Id), filtro)
            .Select(item => new ModuloDto(item.Id, item.Nombre, item.Icono, item.Activo))
            .ToArrayAsync(cancellationToken);
        return new(elementos, filtro.Pagina, filtro.TamanioPagina, total);
    }

    public async Task<ModuloDto> ObtenerModuloAsync(string id, CancellationToken cancellationToken)
    {
        var codigo = NormalizarCodigo(id);
        return await Contexto.Modulos.AsNoTracking().Where(item => item.Id == codigo)
            .Select(item => new ModuloDto(item.Id, item.Nombre, item.Icono, item.Activo))
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new RecursoConfiguracionNoEncontradoException("El módulo no existe.");
    }

    public async Task<ModuloDto> CrearModuloAsync(GuardarModuloSolicitud solicitud, ContextoConfiguracion contextoSeguridad, CancellationToken cancellationToken)
    {
        var id = NormalizarCodigo(solicitud.Id);
        var nombre = NormalizarTexto(solicitud.Nombre);
        if (await Contexto.Modulos.AnyAsync(item => item.Id == id || item.Nombre == nombre, cancellationToken))
            throw new ConflictoConfiguracionException("Ya existe un módulo con ese código o nombre.");
        var entidad = new Modulos { Id = id, Nombre = nombre, Icono = NormalizarTexto(solicitud.Icono), Activo = solicitud.Activo };
        Contexto.Modulos.Add(entidad);
        Auditoria.Registrar(contextoSeguridad, "Modulos", id, "CREAR", "Módulo creado.");
        await Contexto.SaveChangesAsync(cancellationToken);
        return new(entidad.Id, entidad.Nombre, entidad.Icono, entidad.Activo);
    }

    public async Task<ModuloDto> ActualizarModuloAsync(string id, ActualizarModuloSolicitud solicitud, ContextoConfiguracion contextoSeguridad, CancellationToken cancellationToken)
    {
        var codigo = NormalizarCodigo(id);
        var entidad = await Contexto.Modulos.SingleOrDefaultAsync(item => item.Id == codigo, cancellationToken)
            ?? throw new RecursoConfiguracionNoEncontradoException("El módulo no existe.");
        var nombre = NormalizarTexto(solicitud.Nombre);
        if (await Contexto.Modulos.AnyAsync(item => item.Id != codigo && item.Nombre == nombre, cancellationToken))
            throw new ConflictoConfiguracionException("Ya existe un módulo con ese nombre.");
        entidad.Nombre = nombre;
        entidad.Icono = NormalizarTexto(solicitud.Icono);
        Auditoria.Registrar(contextoSeguridad, "Modulos", codigo, "ACTUALIZAR", "Módulo actualizado.");
        await Contexto.SaveChangesAsync(cancellationToken);
        return new(entidad.Id, entidad.Nombre, entidad.Icono, entidad.Activo);
    }

    public async Task<ModuloDto> CambiarActivoModuloAsync(string id, bool activo, ContextoConfiguracion contextoSeguridad, CancellationToken cancellationToken)
    {
        var codigo = NormalizarCodigo(id);
        var entidad = await Contexto.Modulos.SingleOrDefaultAsync(item => item.Id == codigo, cancellationToken)
            ?? throw new RecursoConfiguracionNoEncontradoException("El módulo no existe.");
        if (!activo && (codigo == "SEGURIDAD" || codigo == "CONFIGURACION"))
            throw new ConflictoConfiguracionException("Un módulo estructural de seguridad o configuración no puede desactivarse.");
        entidad.Activo = activo;
        Auditoria.Registrar(contextoSeguridad, "Modulos", codigo, "CAMBIAR_ESTADO", $"Activo={activo}.");
        await Contexto.SaveChangesAsync(cancellationToken);
        return new(entidad.Id, entidad.Nombre, entidad.Icono, entidad.Activo);
    }

    public async Task<RespuestaPaginada<AccionSistemaDto>> ListarAccionesAsync(FiltroConEstadoSolicitud filtro, CancellationToken cancellationToken)
    {
        var consulta = Contexto.AccionesSistema.AsNoTracking().AsQueryable();
        var busqueda = NormalizarOpcional(filtro.Busqueda);
        if (busqueda is not null) consulta = consulta.Where(item => item.Codigo.Contains(busqueda) || item.Nombre.Contains(busqueda));
        if (filtro.EstadoId.HasValue) consulta = consulta.Where(item => item.EstadoId == filtro.EstadoId.Value);
        var total = await consulta.CountAsync(cancellationToken);
        var elementos = await Paginar(consulta.OrderBy(item => item.Codigo).ThenBy(item => item.Id), filtro)
            .Select(item => new AccionSistemaDto(item.Id, item.Codigo, item.Nombre, item.EstadoId, item.Estado.Nombre))
            .ToArrayAsync(cancellationToken);
        return new(elementos, filtro.Pagina, filtro.TamanioPagina, total);
    }

    public async Task<AccionSistemaDto> ObtenerAccionAsync(int id, CancellationToken cancellationToken) =>
        await Contexto.AccionesSistema.AsNoTracking().Where(item => item.Id == id)
            .Select(item => new AccionSistemaDto(item.Id, item.Codigo, item.Nombre, item.EstadoId, item.Estado.Nombre))
            .SingleOrDefaultAsync(cancellationToken)
        ?? throw new RecursoConfiguracionNoEncontradoException("La acción del sistema no existe.");

    public async Task<AccionSistemaDto> CrearAccionAsync(GuardarAccionSistemaSolicitud solicitud, ContextoConfiguracion contextoSeguridad, CancellationToken cancellationToken)
    {
        var codigo = NormalizarCodigo(solicitud.Codigo);
        await using var transaccion = await IniciarTransaccionSerializableAsync("AccionesSistema", cancellationToken);
        await ValidarEstadoDisponibleAsync(solicitud.EstadoId, cancellationToken);
        if (await Contexto.AccionesSistema.AnyAsync(item => item.Codigo == codigo, cancellationToken))
            throw new ConflictoConfiguracionException("Ya existe una acción con ese código.");
        var entidad = new AccionesSistema { Codigo = codigo, Nombre = NormalizarTexto(solicitud.Nombre), EstadoId = solicitud.EstadoId };
        Contexto.AccionesSistema.Add(entidad);
        await Contexto.SaveChangesAsync(cancellationToken);
        Auditoria.Registrar(contextoSeguridad, "AccionesSistema", entidad.Id.ToString(), "CREAR", "Acción del sistema creada.");
        await Contexto.SaveChangesAsync(cancellationToken);
        await ConfirmarAsync(transaccion, cancellationToken);
        return await ObtenerAccionAsync(entidad.Id, cancellationToken);
    }

    public async Task<AccionSistemaDto> ActualizarAccionAsync(int id, GuardarAccionSistemaSolicitud solicitud, ContextoConfiguracion contextoSeguridad, CancellationToken cancellationToken)
    {
        var entidad = await Contexto.AccionesSistema.SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new RecursoConfiguracionNoEncontradoException("La acción del sistema no existe.");
        var codigo = NormalizarCodigo(solicitud.Codigo);
        if (await EsAccionEstructuralAsync(entidad, cancellationToken) && (codigo != entidad.Codigo || solicitud.EstadoId != entidad.EstadoId))
            throw new ConflictoConfiguracionException("La acción ADMINISTRAR vinculada a permisos estructurales no puede cambiar de código o estado.");
        await ValidarEstadoDisponibleAsync(solicitud.EstadoId, cancellationToken);
        if (await Contexto.AccionesSistema.AnyAsync(item => item.Id != id && item.Codigo == codigo, cancellationToken))
            throw new ConflictoConfiguracionException("Ya existe una acción con ese código.");
        entidad.Codigo = codigo;
        entidad.Nombre = NormalizarTexto(solicitud.Nombre);
        entidad.EstadoId = solicitud.EstadoId;
        Auditoria.Registrar(contextoSeguridad, "AccionesSistema", id.ToString(), "ACTUALIZAR", "Acción del sistema actualizada.");
        await Contexto.SaveChangesAsync(cancellationToken);
        return await ObtenerAccionAsync(id, cancellationToken);
    }

    public async Task<AccionSistemaDto> CambiarEstadoAccionAsync(int id, int estadoId, ContextoConfiguracion contextoSeguridad, CancellationToken cancellationToken)
    {
        var entidad = await Contexto.AccionesSistema.SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new RecursoConfiguracionNoEncontradoException("La acción del sistema no existe.");
        if (entidad.EstadoId != estadoId && await EsAccionEstructuralAsync(entidad, cancellationToken))
            throw new ConflictoConfiguracionException("La acción ADMINISTRAR vinculada a permisos estructurales no puede desactivarse.");
        await ValidarEstadoDisponibleAsync(estadoId, cancellationToken);
        entidad.EstadoId = estadoId;
        Auditoria.Registrar(contextoSeguridad, "AccionesSistema", id.ToString(), "CAMBIAR_ESTADO", $"EstadoId={estadoId}.");
        await Contexto.SaveChangesAsync(cancellationToken);
        return await ObtenerAccionAsync(id, cancellationToken);
    }

    public async Task<RespuestaPaginada<NivelAccesoDto>> ListarNivelesAsync(FiltroNivelesSolicitud filtro, CancellationToken cancellationToken)
    {
        var consulta = Contexto.NivelesDeAccesos.AsNoTracking().AsQueryable();
        var busqueda = NormalizarOpcional(filtro.Busqueda);
        if (busqueda is not null) consulta = consulta.Where(item => item.Nombre.Contains(busqueda));
        var total = await consulta.CountAsync(cancellationToken);
        var elementos = await Paginar(consulta.OrderBy(item => item.Nombre).ThenBy(item => item.Id), filtro)
            .Select(item => new NivelAccesoDto(item.Id, item.Nombre))
            .ToArrayAsync(cancellationToken);
        return new(elementos, filtro.Pagina, filtro.TamanioPagina, total);
    }

    public async Task<NivelAccesoDto> ObtenerNivelAsync(int id, CancellationToken cancellationToken) =>
        await Contexto.NivelesDeAccesos.AsNoTracking().Where(item => item.Id == id)
            .Select(item => new NivelAccesoDto(item.Id, item.Nombre))
            .SingleOrDefaultAsync(cancellationToken)
        ?? throw new RecursoConfiguracionNoEncontradoException("El nivel de acceso no existe.");

    public async Task<NivelAccesoDto> CrearNivelAsync(GuardarNivelAccesoSolicitud solicitud, ContextoConfiguracion contextoSeguridad, CancellationToken cancellationToken)
    {
        var nombre = NormalizarTexto(solicitud.Nombre);
        await using var transaccion = await IniciarTransaccionSerializableAsync("NivelesDeAccesos", cancellationToken);
        if (await Contexto.NivelesDeAccesos.AnyAsync(item => item.Nombre == nombre, cancellationToken))
            throw new ConflictoConfiguracionException("Ya existe un nivel de acceso con ese nombre.");
        var id = checked((await Contexto.NivelesDeAccesos.MaxAsync(item => (int?)item.Id, cancellationToken) ?? 0) + 1);
        var entidad = new NivelesDeAccesos { Id = id, Nombre = nombre };
        Contexto.NivelesDeAccesos.Add(entidad);
        Auditoria.Registrar(contextoSeguridad, "NivelesDeAccesos", id.ToString(), "CREAR", "Nivel de acceso creado.");
        await Contexto.SaveChangesAsync(cancellationToken);
        await ConfirmarAsync(transaccion, cancellationToken);
        return new(entidad.Id, entidad.Nombre);
    }

    public async Task<NivelAccesoDto> ActualizarNivelAsync(int id, GuardarNivelAccesoSolicitud solicitud, ContextoConfiguracion contextoSeguridad, CancellationToken cancellationToken)
    {
        var entidad = await Contexto.NivelesDeAccesos.SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new RecursoConfiguracionNoEncontradoException("El nivel de acceso no existe.");
        if (await Contexto.Roles.AnyAsync(item => item.NivelDeAccesoId == id
            && item.RolesPermisos.Any(rolPermiso => rolPermiso.Permitido
                && rolPermiso.Permiso.Codigo == "SEGURIDAD.ADMINISTRAR"), cancellationToken))
            throw new ConflictoConfiguracionException("El nivel estructural del rol ADMIN no puede modificarse.");
        var nombre = NormalizarTexto(solicitud.Nombre);
        if (await Contexto.NivelesDeAccesos.AnyAsync(item => item.Id != id && item.Nombre == nombre, cancellationToken))
            throw new ConflictoConfiguracionException("Ya existe un nivel de acceso con ese nombre.");
        entidad.Nombre = nombre;
        Auditoria.Registrar(contextoSeguridad, "NivelesDeAccesos", id.ToString(), "ACTUALIZAR", "Nivel de acceso actualizado.");
        await Contexto.SaveChangesAsync(cancellationToken);
        return new(entidad.Id, entidad.Nombre);
    }

    private async Task<bool> EsAccionEstructuralAsync(AccionesSistema entidad, CancellationToken token) =>
        entidad.Codigo == "ADMINISTRAR"
        && await Contexto.Permisos.AnyAsync(item => item.AccionId == entidad.Id
            && (item.Codigo == "SEGURIDAD.ADMINISTRAR" || item.Codigo == "CONFIGURACION.ADMINISTRAR"), token);
}
