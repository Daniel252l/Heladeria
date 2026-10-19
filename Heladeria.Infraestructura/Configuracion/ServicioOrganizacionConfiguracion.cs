using Heladeria.Aplicacion.Configuracion;
using Heladeria.Aplicacion.Seguridad.Autorizacion;
using Heladeria.Aplicacion.Seguridad.Configuracion;
using Heladeria.Infraestructura.Persistencia;
using Heladeria.Infraestructura.Persistencia.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Heladeria.Infraestructura.Configuracion;

public sealed class ServicioOrganizacionConfiguracion(
    HeladeriaDbContext contexto,
    IAuditoriaConfiguracion auditoria,
    IServicioPermisos servicioPermisos,
    IOptions<OpcionesSeguridad> opciones)
    : ServicioConfiguracionBase(contexto, auditoria), IServicioOrganizacionConfiguracion
{
    private readonly string[] _estadosOperativos = opciones.Value.EstadosActivos;

    public async Task<RespuestaPaginada<OpcionReferenciaOrganizacionDto>> ListarReferenciasAsync(
        FiltroReferenciasOrganizacionSolicitud filtro,
        ContextoConfiguracion contextoSeguridad,
        CancellationToken cancellationToken)
    {
        var tipo = filtro.Tipo.Trim().ToUpperInvariant();
        var busqueda = NormalizarOpcional(filtro.Busqueda);
        IQueryable<OpcionReferenciaOrganizacionDto> consulta;

        if (tipo is "PROVEEDORES" or "CLIENTES")
        {
            var empresaId = filtro.EmpresaId
                ?? throw new ValidacionConfiguracionException("Seleccione una empresa para consultar referencias.");
            await ValidarAlcanceEmpresaEscrituraAsync(empresaId, contextoSeguridad, cancellationToken);
            consulta = tipo == "PROVEEDORES"
                ? Contexto.Proveedores.AsNoTracking()
                    .Where(item => item.EmpresaId == empresaId && item.Estado.Activo
                        && _estadosOperativos.Contains(item.Estado.Nombre)
                        && (busqueda == null || item.CodigoProveedor.Contains(busqueda) || item.NombreProveedor.Contains(busqueda)))
                    .OrderBy(item => item.NombreProveedor).ThenBy(item => item.Id)
                    .Select(item => new OpcionReferenciaOrganizacionDto(item.Id, item.CodigoProveedor, item.NombreProveedor))
                : Contexto.Clientes.AsNoTracking()
                    .Where(item => item.EmpresaId == empresaId && item.Estado.Activo
                        && _estadosOperativos.Contains(item.Estado.Nombre)
                        && (busqueda == null || item.CodigoCliente.Contains(busqueda) || item.NombreCliente.Contains(busqueda)))
                    .OrderBy(item => item.NombreCliente).ThenBy(item => item.Id)
                    .Select(item => new OpcionReferenciaOrganizacionDto(item.Id, item.CodigoCliente, item.NombreCliente));
        }
        else if (tipo == "DEPARTAMENTOS")
        {
            consulta = Contexto.Departamentos.AsNoTracking()
                .Where(item => busqueda == null || item.Codigo.Contains(busqueda) || item.Nombre.Contains(busqueda))
                .OrderBy(item => item.Nombre).ThenBy(item => item.Id)
                .Select(item => new OpcionReferenciaOrganizacionDto(item.Id, item.Codigo, item.Nombre));
        }
        else if (tipo == "MUNICIPIOS")
        {
            consulta = Contexto.Municipios.AsNoTracking()
                .Where(item => !filtro.DepartamentoId.HasValue
                    || item.DepartamentoId == filtro.DepartamentoId.Value)
                .Where(item => busqueda == null || item.Codigo.Contains(busqueda) || item.Nombre.Contains(busqueda))
                .OrderBy(item => item.Nombre).ThenBy(item => item.Id)
                .Select(item => new OpcionReferenciaOrganizacionDto(item.Id, item.Codigo, item.Nombre));
        }
        else
        {
            throw new ValidacionConfiguracionException("El tipo de referencia solicitado no está disponible.");
        }

        var total = await consulta.CountAsync(cancellationToken);
        var elementos = await consulta
            .Skip(filtro.CalcularDesplazamiento())
            .Take(filtro.TamanioPagina)
            .ToArrayAsync(cancellationToken);
        return new(elementos, filtro.Pagina, filtro.TamanioPagina, total);
    }

    public async Task<RespuestaPaginada<EmpresaDto>> ListarEmpresasAsync(
        FiltroConEstadoSolicitud filtro,
        ContextoConfiguracion contextoSeguridad,
        CancellationToken cancellationToken)
    {
        var consulta = Contexto.Empresas.AsNoTracking().AsQueryable();
        if (!await TieneAlcanceGlobalAsync(contextoSeguridad, cancellationToken))
            consulta = consulta.Where(item => item.Id == contextoSeguridad.EmpresaId);
        var busqueda = NormalizarOpcional(filtro.Busqueda);
        if (busqueda is not null)
            consulta = consulta.Where(item => item.RazonSocial.Contains(busqueda) || item.Nit.Contains(busqueda) || item.Email.Contains(busqueda));
        if (filtro.EstadoId.HasValue) consulta = consulta.Where(item => item.EstadoId == filtro.EstadoId.Value);
        var total = await consulta.CountAsync(cancellationToken);
        var elementos = await Paginar(consulta.OrderBy(item => item.RazonSocial).ThenBy(item => item.Id), filtro)
            .Select(EmpresaProyeccion)
            .ToArrayAsync(cancellationToken);
        return new(elementos, filtro.Pagina, filtro.TamanioPagina, total);
    }

    public async Task<EmpresaDto> ObtenerEmpresaAsync(int id, ContextoConfiguracion contextoSeguridad, CancellationToken cancellationToken)
    {
        var consulta = Contexto.Empresas.AsNoTracking().Where(item => item.Id == id);
        if (!await TieneAlcanceGlobalAsync(contextoSeguridad, cancellationToken))
            consulta = consulta.Where(item => item.Id == contextoSeguridad.EmpresaId);
        return await consulta.Select(EmpresaProyeccion).SingleOrDefaultAsync(cancellationToken)
            ?? throw new RecursoConfiguracionNoEncontradoException("La empresa no existe dentro del alcance autorizado.");
    }

    public async Task<EmpresaDto> CrearEmpresaAsync(GuardarEmpresaSolicitud solicitud, ContextoConfiguracion contextoSeguridad, CancellationToken cancellationToken)
    {
        await using var transaccion = await IniciarTransaccionSerializableAsync("Empresas", cancellationToken);
        await ValidarEmpresaAsync(solicitud, null, contextoSeguridad.RolId, cancellationToken);
        var entidad = new Empresas { RoleId = contextoSeguridad.RolId };
        AplicarEmpresa(entidad, solicitud);
        Contexto.Empresas.Add(entidad);
        await Contexto.SaveChangesAsync(cancellationToken);
        Auditoria.Registrar(contextoSeguridad, "Empresas", entidad.Id.ToString(), "CREAR", "Empresa creada.");
        await Contexto.SaveChangesAsync(cancellationToken);
        await ConfirmarAsync(transaccion, cancellationToken);
        return await ObtenerEmpresaAsync(entidad.Id, contextoSeguridad, cancellationToken);
    }

    public async Task<EmpresaDto> ActualizarEmpresaAsync(int id, GuardarEmpresaSolicitud solicitud, ContextoConfiguracion contextoSeguridad, CancellationToken cancellationToken)
    {
        await ValidarAlcanceEmpresaEscrituraAsync(id, contextoSeguridad, cancellationToken);
        var entidad = await Contexto.Empresas.SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new RecursoConfiguracionNoEncontradoException("La empresa no existe.");
        await ValidarEmpresaAsync(solicitud, id, entidad.RoleId, cancellationToken);
        await ValidarCambioEstadoEmpresaEstructuralAsync(
            entidad,
            solicitud.EstadoId,
            cancellationToken);
        AplicarEmpresa(entidad, solicitud);
        ValidarUnicaEmpresaAlterada(id);
        Auditoria.Registrar(contextoSeguridad, "Empresas", id.ToString(), "ACTUALIZAR", "Empresa actualizada.");
        await Contexto.SaveChangesAsync(cancellationToken);
        return await ObtenerEmpresaAsync(id, contextoSeguridad, cancellationToken);
    }

    public async Task<EmpresaDto> CambiarEstadoEmpresaAsync(int id, int estadoId, ContextoConfiguracion contextoSeguridad, CancellationToken cancellationToken)
    {
        await ValidarAlcanceEmpresaEscrituraAsync(id, contextoSeguridad, cancellationToken);
        var entidad = await Contexto.Empresas.SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new RecursoConfiguracionNoEncontradoException("La empresa no existe.");
        await ValidarEstadoDisponibleAsync(estadoId, cancellationToken);
        await ValidarCambioEstadoEmpresaEstructuralAsync(
            entidad,
            estadoId,
            cancellationToken);
        entidad.EstadoId = estadoId;
        ValidarUnicaEmpresaAlterada(id);
        Auditoria.Registrar(contextoSeguridad, "Empresas", id.ToString(), "CAMBIAR_ESTADO", $"EstadoId={estadoId}.");
        await Contexto.SaveChangesAsync(cancellationToken);
        return await ObtenerEmpresaAsync(id, contextoSeguridad, cancellationToken);
    }

    public async Task<RespuestaPaginada<SucursalDto>> ListarSucursalesAsync(
        FiltroSucursalesSolicitud filtro,
        ContextoConfiguracion contextoSeguridad,
        CancellationToken cancellationToken)
    {
        var consulta = Contexto.Sucursales.AsNoTracking().AsQueryable();
        if (!await TieneAlcanceGlobalAsync(contextoSeguridad, cancellationToken))
        {
            consulta = consulta.Where(item => item.EmpresaId == contextoSeguridad.EmpresaId
                && item.UsuariosSucursales.Any(asignacion => asignacion.UsuarioId == contextoSeguridad.UsuarioId
                    && asignacion.Estado.Activo));
        }
        if (filtro.EmpresaId.HasValue)
            consulta = consulta.Where(item => item.EmpresaId == filtro.EmpresaId.Value);
        var busqueda = NormalizarOpcional(filtro.Busqueda);
        if (busqueda is not null)
            consulta = consulta.Where(item => item.Nombre.Contains(busqueda) || item.Email.Contains(busqueda) || (item.Codigo != null && item.Codigo.Contains(busqueda)));
        if (filtro.EstadoId.HasValue) consulta = consulta.Where(item => item.EstadoId == filtro.EstadoId.Value);
        var total = await consulta.CountAsync(cancellationToken);
        var elementos = await Paginar(consulta.OrderBy(item => item.Nombre).ThenBy(item => item.Id), filtro)
            .Select(SucursalProyeccion)
            .ToArrayAsync(cancellationToken);
        return new(elementos, filtro.Pagina, filtro.TamanioPagina, total);
    }

    public async Task<SucursalDto> ObtenerSucursalAsync(long id, ContextoConfiguracion contextoSeguridad, CancellationToken cancellationToken)
    {
        var consulta = Contexto.Sucursales.AsNoTracking().Where(item => item.Id == id);
        if (!await TieneAlcanceGlobalAsync(contextoSeguridad, cancellationToken))
        {
            consulta = consulta.Where(item => item.EmpresaId == contextoSeguridad.EmpresaId
                && item.UsuariosSucursales.Any(asignacion => asignacion.UsuarioId == contextoSeguridad.UsuarioId
                    && asignacion.Estado.Activo));
        }
        return await consulta.Select(SucursalProyeccion).SingleOrDefaultAsync(cancellationToken)
            ?? throw new RecursoConfiguracionNoEncontradoException("La sucursal no existe dentro del alcance autorizado.");
    }

    public async Task<SucursalDto> CrearSucursalAsync(GuardarSucursalSolicitud solicitud, ContextoConfiguracion contextoSeguridad, CancellationToken cancellationToken)
    {
        await ValidarAlcanceEmpresaEscrituraAsync(solicitud.EmpresaId, contextoSeguridad, cancellationToken);
        await using var transaccion = await IniciarTransaccionSerializableAsync("Sucursales", cancellationToken);
        await ValidarSucursalAsync(solicitud, null, cancellationToken);
        var entidad = new Sucursales();
        AplicarSucursal(entidad, solicitud);
        Contexto.Sucursales.Add(entidad);
        await Contexto.SaveChangesAsync(cancellationToken);
        Auditoria.Registrar(contextoSeguridad, "Sucursales", entidad.Id.ToString(), "CREAR", "Sucursal creada.");
        await Contexto.SaveChangesAsync(cancellationToken);
        await ConfirmarAsync(transaccion, cancellationToken);
        return await ObtenerSucursalAsync(entidad.Id, contextoSeguridad, cancellationToken);
    }

    public async Task<SucursalDto> ActualizarSucursalAsync(long id, GuardarSucursalSolicitud solicitud, ContextoConfiguracion contextoSeguridad, CancellationToken cancellationToken)
    {
        await ValidarAlcanceSucursalEscrituraAsync(id, contextoSeguridad, cancellationToken);
        var entidad = await Contexto.Sucursales.SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new RecursoConfiguracionNoEncontradoException("La sucursal no existe.");
        if (entidad.EmpresaId != solicitud.EmpresaId)
            throw new ValidacionConfiguracionException("No se permite trasladar una sucursal entre empresas.");
        await ValidarSucursalAsync(solicitud, id, cancellationToken);
        await ValidarCambioEstadoSucursalEstructuralAsync(
            entidad,
            solicitud.EstadoId,
            cancellationToken);
        AplicarSucursal(entidad, solicitud);
        ValidarUnicaSucursalAlterada(id);
        Auditoria.Registrar(contextoSeguridad, "Sucursales", id.ToString(), "ACTUALIZAR", "Sucursal actualizada.");
        await Contexto.SaveChangesAsync(cancellationToken);
        return await ObtenerSucursalAsync(id, contextoSeguridad, cancellationToken);
    }

    public async Task<SucursalDto> CambiarEstadoSucursalAsync(long id, int estadoId, ContextoConfiguracion contextoSeguridad, CancellationToken cancellationToken)
    {
        await ValidarAlcanceSucursalEscrituraAsync(id, contextoSeguridad, cancellationToken);
        var entidad = await Contexto.Sucursales.SingleOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new RecursoConfiguracionNoEncontradoException("La sucursal no existe.");
        await ValidarEstadoDisponibleAsync(estadoId, cancellationToken);
        await ValidarCambioEstadoSucursalEstructuralAsync(
            entidad,
            estadoId,
            cancellationToken);
        entidad.EstadoId = estadoId;
        ValidarUnicaSucursalAlterada(id);
        Auditoria.Registrar(contextoSeguridad, "Sucursales", id.ToString(), "CAMBIAR_ESTADO", $"EstadoId={estadoId}.");
        await Contexto.SaveChangesAsync(cancellationToken);
        return await ObtenerSucursalAsync(id, contextoSeguridad, cancellationToken);
    }

    private async Task ValidarEmpresaAsync(GuardarEmpresaSolicitud solicitud, int? id, string roleId, CancellationToken token)
    {
        await ValidarEstadoDisponibleAsync(solicitud.EstadoId, token);
        if (!await Contexto.Roles.AsNoTracking().AnyAsync(item => item.Id == roleId, token))
            throw new ValidacionConfiguracionException("El rol del contexto autenticado no existe.");
        var nit = NormalizarTexto(solicitud.Nit);
        var email = NormalizarTexto(solicitud.Email).ToLowerInvariant();
        if (await Contexto.Empresas.AnyAsync(item => (!id.HasValue || item.Id != id.Value) && (item.Nit == nit || item.Email == email), token))
            throw new ConflictoConfiguracionException("Ya existe una empresa con el mismo NIT o correo.");
        if (solicitud.ClienteId.HasValue
            && !await Contexto.Tipos.AsNoTracking().AnyAsync(item => item.Id == solicitud.ClienteId.Value && item.Estado.Activo, token))
            throw new ValidacionConfiguracionException("El tipo de cliente indicado no existe o no está disponible.");
    }

    private static void AplicarEmpresa(Empresas entidad, GuardarEmpresaSolicitud solicitud)
    {
        entidad.RazonSocial = NormalizarTexto(solicitud.RazonSocial);
        entidad.NombreComercial = NormalizarOpcional(solicitud.NombreComercial);
        entidad.Eslogan = NormalizarTexto(solicitud.Eslogan);
        entidad.Direccion = NormalizarTexto(solicitud.Direccion);
        entidad.Nit = NormalizarTexto(solicitud.Nit);
        entidad.Dpi = NormalizarOpcional(solicitud.Dpi);
        entidad.Telefono = NormalizarTexto(solicitud.Telefono);
        entidad.Email = NormalizarTexto(solicitud.Email).ToLowerInvariant();
        entidad.Imagen = NormalizarTexto(solicitud.Imagen);
        entidad.Icono = NormalizarTexto(solicitud.Icono);
        entidad.ImagenReportes = NormalizarTexto(solicitud.ImagenReportes);
        entidad.Url = NormalizarOpcional(solicitud.Url);
        entidad.EstadoId = solicitud.EstadoId;
        entidad.ClienteId = solicitud.ClienteId;
    }

    private async Task ValidarSucursalAsync(GuardarSucursalSolicitud solicitud, long? id, CancellationToken token)
    {
        await ValidarEstadoDisponibleAsync(solicitud.EstadoId, token);
        if (!await Contexto.Empresas.AsNoTracking().AnyAsync(item => item.Id == solicitud.EmpresaId && item.Estado.Activo, token))
            throw new ValidacionConfiguracionException("La empresa no existe o no está disponible.");
        if (!await Contexto.Proveedores.AsNoTracking().AnyAsync(item => item.Id == solicitud.ProveedorId
            && item.EmpresaId == solicitud.EmpresaId && item.Estado.Activo, token))
            throw new ValidacionConfiguracionException("El proveedor no existe, está inactivo o pertenece a otra empresa.");
        if (solicitud.ClienteId.HasValue
            && !await Contexto.Clientes.AsNoTracking().AnyAsync(item => item.Id == solicitud.ClienteId.Value
                && item.EmpresaId == solicitud.EmpresaId && item.Estado.Activo, token))
            throw new ValidacionConfiguracionException("El cliente no existe, está inactivo o pertenece a otra empresa.");
        if (solicitud.DepartamentoId.HasValue
            && !await Contexto.Departamentos.AsNoTracking().AnyAsync(item => item.Id == solicitud.DepartamentoId.Value, token))
            throw new ValidacionConfiguracionException("El departamento no existe.");
        if (solicitud.MunicipioId.HasValue)
        {
            var municipio = await Contexto.Municipios.AsNoTracking()
                .Where(item => item.Id == solicitud.MunicipioId.Value)
                .Select(item => new { item.DepartamentoId })
                .SingleOrDefaultAsync(token)
                ?? throw new ValidacionConfiguracionException("El municipio no existe.");
            if (solicitud.DepartamentoId.HasValue && municipio.DepartamentoId != solicitud.DepartamentoId.Value)
                throw new ValidacionConfiguracionException("El municipio no pertenece al departamento indicado.");
        }
        await ValidarSucursalRelacionadaAsync(solicitud.SucursalCentralId, solicitud.EmpresaId, id, "central", token);
        await ValidarSucursalRelacionadaAsync(solicitud.SucursalTransporteId, solicitud.EmpresaId, id, "de transporte", token);
        var email = NormalizarTexto(solicitud.Email).ToLowerInvariant();
        var codigo = NormalizarOpcional(solicitud.Codigo)?.ToUpperInvariant();
        if (await Contexto.Sucursales.AnyAsync(item => item.EmpresaId == solicitud.EmpresaId
            && (!id.HasValue || item.Id != id.Value)
            && (item.Email == email || (codigo != null && item.Codigo == codigo)), token))
            throw new ConflictoConfiguracionException("Ya existe una sucursal con el mismo correo o código en la empresa.");
        if (solicitud.EsCentral && await Contexto.Sucursales.AnyAsync(item => item.EmpresaId == solicitud.EmpresaId
            && item.EsCentral && (!id.HasValue || item.Id != id.Value), token))
            throw new ConflictoConfiguracionException("La empresa ya tiene una sucursal central.");
    }

    private async Task ValidarSucursalRelacionadaAsync(long? sucursalId, int empresaId, long? actualId, string descripcion, CancellationToken token)
    {
        if (!sucursalId.HasValue) return;
        if (actualId.HasValue && sucursalId.Value == actualId.Value)
            throw new ValidacionConfiguracionException($"La sucursal no puede referenciarse a sí misma como sucursal {descripcion}.");
        if (!await Contexto.Sucursales.AsNoTracking().AnyAsync(item => item.Id == sucursalId.Value
            && item.EmpresaId == empresaId && item.Estado.Activo, token))
            throw new ValidacionConfiguracionException($"La sucursal {descripcion} no existe, está inactiva o pertenece a otra empresa.");
    }

    private static void AplicarSucursal(Sucursales entidad, GuardarSucursalSolicitud solicitud)
    {
        entidad.Nombre = NormalizarTexto(solicitud.Nombre);
        entidad.Direccion = NormalizarTexto(solicitud.Direccion);
        entidad.DireccionCorta = NormalizarTexto(solicitud.DireccionCorta);
        entidad.Telefono = NormalizarTexto(solicitud.Telefono);
        entidad.Email = NormalizarTexto(solicitud.Email).ToLowerInvariant();
        entidad.EmpresaId = solicitud.EmpresaId;
        entidad.MunicipioId = solicitud.MunicipioId;
        entidad.DepartamentoId = solicitud.DepartamentoId;
        entidad.EsCentral = solicitud.EsCentral;
        entidad.ClienteId = solicitud.ClienteId;
        entidad.EstadoId = solicitud.EstadoId;
        entidad.ProveedorId = solicitud.ProveedorId;
        entidad.EsBodega = solicitud.EsBodega;
        entidad.SucursalCentralId = solicitud.SucursalCentralId;
        entidad.Imagen = NormalizarOpcional(solicitud.Imagen);
        entidad.SucursalTransporteId = solicitud.SucursalTransporteId;
        entidad.RepresentanteLegal = NormalizarOpcional(solicitud.RepresentanteLegal);
        entidad.Codigo = NormalizarOpcional(solicitud.Codigo)?.ToUpperInvariant();
        entidad.ImagenReportes = NormalizarOpcional(solicitud.ImagenReportes);
        entidad.Transportes = solicitud.Transportes;
        var prefijo = entidad.Codigo ?? entidad.Nombre;
        entidad.CodigoNombre = $"{prefijo} {entidad.Nombre}"[..Math.Min(175, prefijo.Length + 1 + entidad.Nombre.Length)];
    }

    private Task<bool> TieneAlcanceGlobalAsync(ContextoConfiguracion contextoSeguridad, CancellationToken token) =>
        servicioPermisos.TienePermisoAsync(contextoSeguridad.UsuarioId, contextoSeguridad.RolId, PermisosConfiguracion.Administrar, token);

    private async Task ValidarAlcanceEmpresaEscrituraAsync(int empresaId, ContextoConfiguracion contextoSeguridad, CancellationToken token)
    {
        if (!await TieneAlcanceGlobalAsync(contextoSeguridad, token) && empresaId != contextoSeguridad.EmpresaId)
            throw new RecursoConfiguracionNoEncontradoException("La empresa no existe dentro del alcance autorizado.");
    }

    private async Task ValidarAlcanceSucursalEscrituraAsync(long sucursalId, ContextoConfiguracion contextoSeguridad, CancellationToken token)
    {
        if (await TieneAlcanceGlobalAsync(contextoSeguridad, token)) return;
        if (!await Contexto.UsuariosSucursales.AsNoTracking().AnyAsync(item => item.UsuarioId == contextoSeguridad.UsuarioId
            && item.SucursalId == sucursalId && item.Sucursal.EmpresaId == contextoSeguridad.EmpresaId && item.Estado.Activo, token))
            throw new RecursoConfiguracionNoEncontradoException("La sucursal no existe dentro del alcance autorizado.");
    }

    private async Task ValidarCambioEstadoEmpresaEstructuralAsync(
        Empresas empresa,
        int estadoDestinoId,
        CancellationToken token)
    {
        if (empresa.EstadoId == estadoDestinoId
            || await EsEstadoOperativoAsync(estadoDestinoId, token))
        {
            return;
        }

        var sostieneAccesoAdministrativo = await Contexto.UsuariosSucursales
            .AsNoTracking()
            .AnyAsync(asignacion =>
                asignacion.Sucursal.EmpresaId == empresa.Id
                && asignacion.Estado.Activo
                && _estadosOperativos.Contains(asignacion.Estado.Nombre)
                && asignacion.Sucursal.Estado.Activo
                && _estadosOperativos.Contains(asignacion.Sucursal.Estado.Nombre)
                && asignacion.Usuario.Estado.Activo
                && _estadosOperativos.Contains(asignacion.Usuario.Estado.Nombre)
                && asignacion.Usuario.Role.RolesPermisos.Any(rolPermiso =>
                    rolPermiso.Permitido
                    && rolPermiso.Permiso.Codigo == PermisosSistema.SeguridadAdministrar
                    && rolPermiso.Permiso.Modulo.Activo
                    && rolPermiso.Permiso.Estado.Activo
                    && _estadosOperativos.Contains(rolPermiso.Permiso.Estado.Nombre)
                    && rolPermiso.Permiso.Accion.Estado.Activo
                    && _estadosOperativos.Contains(rolPermiso.Permiso.Accion.Estado.Nombre)),
                token);

        if (sostieneAccesoAdministrativo)
        {
            throw new ConflictoConfiguracionException(
                "La empresa principal utilizada por la administración del sistema no puede inactivarse.");
        }
    }

    private async Task ValidarCambioEstadoSucursalEstructuralAsync(
        Sucursales sucursal,
        int estadoDestinoId,
        CancellationToken token)
    {
        if (sucursal.EstadoId == estadoDestinoId
            || await EsEstadoOperativoAsync(estadoDestinoId, token))
        {
            return;
        }

        var sostieneAccesoAdministrativo = await Contexto.UsuariosSucursales
            .AsNoTracking()
            .AnyAsync(asignacion =>
                asignacion.SucursalId == sucursal.Id
                && asignacion.Estado.Activo
                && _estadosOperativos.Contains(asignacion.Estado.Nombre)
                && asignacion.Usuario.Estado.Activo
                && _estadosOperativos.Contains(asignacion.Usuario.Estado.Nombre)
                && asignacion.Usuario.Role.RolesPermisos.Any(rolPermiso =>
                    rolPermiso.Permitido
                    && rolPermiso.Permiso.Codigo == PermisosSistema.SeguridadAdministrar
                    && rolPermiso.Permiso.Modulo.Activo
                    && rolPermiso.Permiso.Estado.Activo
                    && _estadosOperativos.Contains(rolPermiso.Permiso.Estado.Nombre)
                    && rolPermiso.Permiso.Accion.Estado.Activo
                    && _estadosOperativos.Contains(rolPermiso.Permiso.Accion.Estado.Nombre)),
                token);

        if (sostieneAccesoAdministrativo)
        {
            throw new ConflictoConfiguracionException(
                "La sucursal utilizada por la administración del sistema no puede inactivarse.");
        }
    }

    private Task<bool> EsEstadoOperativoAsync(int estadoId, CancellationToken token) =>
        Contexto.Estados.AsNoTracking().AnyAsync(
            item => item.Id == estadoId
                && item.Activo
                && _estadosOperativos.Contains(item.Nombre),
            token);

    private void ValidarUnicaEmpresaAlterada(int empresaIdSolicitada)
    {
        Contexto.ChangeTracker.DetectChanges();
        var empresaAjena = Contexto.ChangeTracker.Entries<Empresas>()
            .FirstOrDefault(entrada =>
                entrada.Entity.Id != empresaIdSolicitada
                && entrada.State is EntityState.Added or EntityState.Modified or EntityState.Deleted);

        if (empresaAjena is not null)
        {
            throw new InvalidOperationException(
                $"La operación sobre la empresa {empresaIdSolicitada} intentó alterar la empresa {empresaAjena.Entity.Id}.");
        }
    }

    private void ValidarUnicaSucursalAlterada(long sucursalIdSolicitada)
    {
        Contexto.ChangeTracker.DetectChanges();
        var sucursalAjena = Contexto.ChangeTracker.Entries<Sucursales>()
            .FirstOrDefault(entrada =>
                entrada.Entity.Id != sucursalIdSolicitada
                && entrada.State is EntityState.Added or EntityState.Modified or EntityState.Deleted);

        if (sucursalAjena is not null)
        {
            throw new InvalidOperationException(
                $"La operación sobre la sucursal {sucursalIdSolicitada} intentó alterar la sucursal {sucursalAjena.Entity.Id}.");
        }
    }

    private static readonly System.Linq.Expressions.Expression<Func<Empresas, EmpresaDto>> EmpresaProyeccion = item =>
        new(item.Id, item.RazonSocial, item.NombreComercial, item.Eslogan, item.Direccion, item.Nit, item.Dpi, item.Telefono, item.Email, item.Imagen, item.Icono, item.ImagenReportes, item.Url, item.EstadoId, item.Estado.Nombre, item.ClienteId);

    private static readonly System.Linq.Expressions.Expression<Func<Sucursales, SucursalDto>> SucursalProyeccion = item =>
        new(item.Id, item.Nombre, item.Direccion, item.DireccionCorta, item.Telefono, item.Email, item.EmpresaId, item.Empresa.RazonSocial, item.MunicipioId, item.DepartamentoId, item.EsCentral, item.ClienteId, item.EstadoId, item.Estado.Nombre, item.ProveedorId, item.Proveedor.NombreProveedor, item.EsBodega, item.SucursalCentralId, item.Imagen, item.SucursalTransporteId, item.RepresentanteLegal, item.Codigo, item.ImagenReportes, item.Transportes, item.CodigoNombre);
}
