using System.Data;
using System.Net.Mail;
using System.Security.Cryptography;
using Heladeria.Aplicacion.Seguridad.Autenticacion;
using Heladeria.Aplicacion.Seguridad.Bootstrap;
using Heladeria.Aplicacion.Seguridad.Configuracion;
using Heladeria.Infraestructura.Persistencia;
using Heladeria.Infraestructura.Persistencia.Entidades;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;

namespace Heladeria.Infraestructura.Seguridad.Bootstrap;

public sealed class BootstrapAdministrador(
    HeladeriaDbContext contexto,
    IPasswordHasher<Usuarios> passwordHasher,
    IOptions<OpcionesSeguridad> opciones,
    TimeProvider timeProvider) : IBootstrapAdministrador, IProvisionadorUsuarioConsulta, IPreparadorFase3
{
    public const string RolAdministradorId = "ADMIN";
    public const string RolConsultaId = "CONSULTA";
    public const string UsuarioConsulta = "consulta";
    public const string CorreoUsuarioConsulta = "consulta@heladeria.local";
    public const string ModuloSeguridadId = "SEGURIDAD";
    public const string ModuloConfiguracionId = "CONFIGURACION";
    public const string CodigoPermisoAdministrar = "SEGURIDAD.ADMINISTRAR";
    public const string CodigoPermisoConfiguracionAdministrar = "CONFIGURACION.ADMINISTRAR";
    public const string CodigoProveedorInicial = "PROV-INICIAL";
    public const string EntidadMarcador = "Instalacion";
    public const string AccionMarcador = "BOOTSTRAP_ADMIN_COMPLETADO";
    private static readonly int MesesCambioContrasenaAdministrador = 12;
    private static readonly short DiasCreditoProveedorInicial = 0;

    async Task<ResultadoBootstrapAdministrador> IProvisionadorUsuarioConsulta.EjecutarAsync(
        string contrasena,
        CancellationToken cancellationToken) =>
        await CrearUsuarioConsultaAsync(contrasena, cancellationToken);

    async Task<ResultadoBootstrapAdministrador> IPreparadorFase3.EjecutarAsync(
        CancellationToken cancellationToken) => await PrepararFase3Async(cancellationToken);

    public async Task<ResultadoBootstrapAdministrador> PrepararFase3Async(
        CancellationToken cancellationToken)
    {
        await using var transaccion = await IniciarTransaccionAsync(cancellationToken);
        await TomarBloqueoExclusivoAsync(cancellationToken);

        var existeModulo = await contexto.Modulos.AnyAsync(
            item => item.Id == ModuloConfiguracionId,
            cancellationToken);
        var existePermiso = await contexto.Permisos.AnyAsync(
            item => item.Codigo == CodigoPermisoConfiguracionAdministrar,
            cancellationToken);
        var existeAsignacion = await contexto.RolesPermisos.AnyAsync(
            item => item.RoleId == RolAdministradorId
                && item.Permiso.Codigo == CodigoPermisoConfiguracionAdministrar,
            cancellationToken);
        var existeMarcador = await contexto.Bitacoras.AnyAsync(
            item => item.Entidad == EntidadMarcador
                && item.Accion == "FASE3_CONFIGURADA",
            cancellationToken);

        if (existeModulo || existePermiso || existeAsignacion || existeMarcador)
        {
            var completa = existeModulo && existePermiso && existeAsignacion && existeMarcador;
            throw new BootstrapAdministradorRechazadoException(
                completa
                    ? "La preparación de la Fase 3 ya fue completada."
                    : "Se detectó una preparación parcial de la Fase 3; revise los registros antes de continuar.");
        }

        var referencia = await contexto.Roles
            .AsNoTracking()
            .Where(item => item.Id == RolAdministradorId)
            .Select(item => new
            {
                RolId = item.Id,
                Usuario = item.Usuarios
                    .Where(usuario => usuario.Estado.Activo)
                    .SelectMany(usuario => usuario.UsuariosSucursales
                        .Where(asignacion => asignacion.EsPrincipal && asignacion.Estado.Activo)
                        .Select(asignacion => new
                        {
                            UsuarioId = usuario.Id,
                            EmpresaId = asignacion.Sucursal.EmpresaId,
                            SucursalId = asignacion.SucursalId
                        }))
                    .FirstOrDefault()
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (referencia?.Usuario is null)
        {
            throw new BootstrapAdministradorRechazadoException(
                "No existe un rol ADMIN con un usuario activo y sucursal principal activa.");
        }

        var estadoId = await contexto.Permisos
            .AsNoTracking()
            .Where(item => item.Codigo == CodigoPermisoAdministrar
                && item.Estado.Activo
                && item.Accion.Codigo == "ADMINISTRAR"
                && item.Accion.Estado.Activo)
            .Select(item => new { item.EstadoId, item.AccionId })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new BootstrapAdministradorRechazadoException(
                "No existe el permiso estructural SEGURIDAD.ADMINISTRAR activo con su acción ADMINISTRAR activa.");

        var modulo = new Modulos
        {
            Id = ModuloConfiguracionId,
            Nombre = "Configuración",
            Icono = "settings",
            Activo = true
        };
        contexto.Modulos.Add(modulo);
        await contexto.SaveChangesAsync(cancellationToken);

        var permiso = new Permisos
        {
            ModuloId = modulo.Id,
            AccionId = estadoId.AccionId,
            Recurso = "Configuración y mantenimientos",
            Codigo = CodigoPermisoConfiguracionAdministrar,
            EstadoId = estadoId.EstadoId
        };
        contexto.Permisos.Add(permiso);
        await contexto.SaveChangesAsync(cancellationToken);
        ValidarIdentidadGenerada(permiso.Id, "Permisos");

        var asignacion = new RolesPermisos
        {
            RoleId = referencia.RolId,
            PermisoId = permiso.Id,
            Permitido = true
        };
        contexto.RolesPermisos.Add(asignacion);
        await contexto.SaveChangesAsync(cancellationToken);
        ValidarIdentidadGenerada(asignacion.Id, "RolesPermisos");

        var marcador = new Bitacoras
        {
            EmpresaId = referencia.Usuario.EmpresaId,
            SucursalId = referencia.Usuario.SucursalId,
            ModuloId = modulo.Id,
            Entidad = EntidadMarcador,
            EntidadId = permiso.Id.ToString(),
            Accion = "FASE3_CONFIGURADA",
            Fecha = timeProvider.GetUtcNow().UtcDateTime,
            UsuarioId = referencia.Usuario.UsuarioId,
            Comentario = "Permiso administrativo de configuración preparado para el rol ADMIN."
        };
        contexto.Bitacoras.Add(marcador);
        await contexto.SaveChangesAsync(cancellationToken);
        await ConfirmarAsync(transaccion, cancellationToken);

        return new ResultadoBootstrapAdministrador(
        [
            new("Modulos", modulo.Id, modulo.Nombre),
            new("Permisos", permiso.Id.ToString(), permiso.Codigo),
            new("RolesPermisos", asignacion.Id.ToString(), $"{asignacion.RoleId} -> {permiso.Codigo}"),
            new("Bitacoras", marcador.Id.ToString(), marcador.Accion)
        ]);
    }

    public async Task<ResultadoBootstrapAdministrador> CrearUsuarioConsultaAsync(
        string contrasena,
        CancellationToken cancellationToken)
    {
        ValidarSolicitud(UsuarioConsulta, CorreoUsuarioConsulta, contrasena);

        await using var transaccion = await IniciarTransaccionAsync(cancellationToken);
        await TomarBloqueoExclusivoAsync(cancellationToken);
        ValidarContratoDependencias();
        ValidarRestriccionesEsquema();

        if (await contexto.Roles.AnyAsync(
                rol => rol.Id == RolConsultaId || rol.Nombre == "Consulta",
                cancellationToken))
        {
            throw new BootstrapAdministradorRechazadoException(
                "Ya existe el rol CONSULTA o un rol con nombre Consulta.");
        }

        if (await contexto.Usuarios.AnyAsync(
                usuario => usuario.UserName == UsuarioConsulta
                    || usuario.Email == CorreoUsuarioConsulta,
                cancellationToken))
        {
            throw new BootstrapAdministradorRechazadoException(
                "Ya existe el usuario o el correo reservado para la cuenta de consulta.");
        }

        var estadosActivos = opciones.Value.EstadosActivos;
        var referencias = await contexto.Usuarios
            .AsNoTracking()
            .Where(usuario =>
                usuario.RoleId == RolAdministradorId
                && usuario.Estado.Activo
                && estadosActivos.Contains(usuario.Estado.Nombre))
            .SelectMany(
                usuario => usuario.UsuariosSucursales.Where(asignacion =>
                    asignacion.EsPrincipal
                    && asignacion.Estado.Activo
                    && estadosActivos.Contains(asignacion.Estado.Nombre)
                    && asignacion.Sucursal.Estado.Activo
                    && estadosActivos.Contains(asignacion.Sucursal.Estado.Nombre)
                    && asignacion.Sucursal.Empresa.Estado.Activo
                    && estadosActivos.Contains(asignacion.Sucursal.Empresa.Estado.Nombre)),
                (usuario, asignacion) => new
                {
                    usuario.Role.NivelDeAccesoId,
                    usuario.Role.TipoId,
                    usuario.Role.CambioDeContrasenia,
                    usuario.EstadoId,
                    usuario.TipoInicioSesionId,
                    SucursalId = asignacion.Sucursal.Id,
                    EstadoAsignacionId = asignacion.EstadoId
                })
            .Take(2)
            .ToArrayAsync(cancellationToken);

        if (referencias.Length != 1)
        {
            throw new BootstrapAdministradorRechazadoException(
                "Debe existir un único administrador activo con una sucursal principal, empresa y asignación activas.");
        }

        var referencia = referencias[0];
        if (referencia.CambioDeContrasenia is <= 0 or >= 49)
        {
            throw new BootstrapAdministradorRechazadoException(
                "El período de cambio de contraseña del rol ADMIN no es reutilizable.");
        }

        var rol = new Roles
        {
            Id = RolConsultaId,
            Nombre = "Consulta",
            NivelDeAccesoId = referencia.NivelDeAccesoId,
            CambioDeContrasenia = referencia.CambioDeContrasenia,
            TipoId = referencia.TipoId,
            AplicarConfiguracionesPorRol = false
        };
        contexto.Roles.Add(rol);
        await contexto.SaveChangesAsync(cancellationToken);

        var usuario = new Usuarios
        {
            Email = CorreoUsuarioConsulta,
            UserName = UsuarioConsulta,
            IdentityId = Guid.NewGuid().ToString("N"),
            PasswordHash = string.Empty,
            SecurityStamp = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)),
            RoleId = rol.Id,
            AccessFailedCount = 0,
            HorarioInicio = new TimeOnly(0, 0),
            HorarioFinal = new TimeOnly(0, 0),
            UltimoCambioDeContrasenia = ObtenerFechaLocal(),
            Imagen = "sin-imagen",
            Soundex = "C524",
            ContraseniaTemporal = false,
            EstadoId = referencia.EstadoId,
            PasswordTemporal = null,
            TipoInicioSesionId = referencia.TipoInicioSesionId,
            UtilizarConfiguracionDeRole = true
        };
        usuario.PasswordHash = passwordHasher.HashPassword(usuario, contrasena);
        contexto.Usuarios.Add(usuario);
        await contexto.SaveChangesAsync(cancellationToken);
        ValidarIdentidadGenerada(usuario.Id, "Usuarios");

        var asignacion = new UsuariosSucursales
        {
            UsuarioId = usuario.Id,
            SucursalId = referencia.SucursalId,
            EsPrincipal = true,
            EstadoId = referencia.EstadoAsignacionId
        };
        contexto.UsuariosSucursales.Add(asignacion);
        await contexto.SaveChangesAsync(cancellationToken);
        ValidarIdentidadGenerada(asignacion.Id, "UsuariosSucursales");

        if (await contexto.RolesPermisos.AnyAsync(
                rolPermiso => rolPermiso.RoleId == RolConsultaId,
                cancellationToken))
        {
            throw new BootstrapAdministradorRechazadoException(
                "El rol CONSULTA recibió permisos inesperados; la operación fue revertida.");
        }

        await ConfirmarAsync(transaccion, cancellationToken);

        return new ResultadoBootstrapAdministrador(
        [
            new("Roles", rol.Id, rol.Nombre),
            new("Usuarios", usuario.Id.ToString(), $"{usuario.UserName} ({usuario.Email})"),
            new("UsuariosSucursales", asignacion.Id.ToString(), $"Sucursal principal {asignacion.SucursalId}")
        ]);
    }

    public async Task<ResultadoBootstrapAdministrador> EjecutarAsync(
        SolicitudBootstrapAdministrador solicitud,
        CancellationToken cancellationToken)
    {
        var nombreUsuario = solicitud.NombreUsuario.Trim();
        var correo = solicitud.Correo.Trim();
        ValidarSolicitud(nombreUsuario, correo, solicitud.Contrasena);

        await using var transaccion = await IniciarTransaccionAsync(cancellationToken);
        await TomarBloqueoExclusivoAsync(cancellationToken);
        ValidarContratoDependencias();
        ValidarRestriccionesEsquema();
        await ValidarInstalacionVaciaAsync(nombreUsuario, correo, cancellationToken);

        var claseEstadoId = checked(
            (await contexto.ClaseEstados.MaxAsync(
                item => (int?)item.Id,
                cancellationToken) ?? 0) + 1);
        var estadoActivoId = checked(
            (await contexto.Estados.MaxAsync(
                item => (int?)item.Id,
                cancellationToken) ?? 0) + 1);
        var claseTipoId = checked(
            (await contexto.ClaseTipos.MaxAsync(
                item => (int?)item.Id,
                cancellationToken) ?? 0) + 1);
        var tipoRolId = checked(
            (await contexto.Tipos.MaxAsync(
                item => (int?)item.Id,
                cancellationToken) ?? 0) + 1);
        var tipoInicioSesionId = checked(tipoRolId + 1);
        var nivelAccesoId = checked(
            (await contexto.NivelesDeAccesos.MaxAsync(
                item => (int?)item.Id,
                cancellationToken) ?? 0) + 1);

        var claseEstado = new ClaseEstados
        {
            Id = claseEstadoId,
            Nombre = "Seguridad",
            Activo = true
        };
        contexto.ClaseEstados.Add(claseEstado);
        await contexto.SaveChangesAsync(cancellationToken);

        var estadoActivo = new Estados
        {
            Id = estadoActivoId,
            Nombre = "Activo",
            Activo = true,
            ClaseEstadoId = claseEstado.Id,
            ModuloId = null
        };
        contexto.Estados.Add(estadoActivo);
        await contexto.SaveChangesAsync(cancellationToken);

        var modulo = new Modulos
        {
            Id = ModuloSeguridadId,
            Nombre = "Seguridad",
            Icono = "shield",
            Activo = true
        };
        contexto.Modulos.Add(modulo);
        await contexto.SaveChangesAsync(cancellationToken);

        var claseTipo = new ClaseTipos
        {
            Id = claseTipoId,
            Nombre = "Seguridad",
            EstadoId = estadoActivo.Id
        };
        contexto.ClaseTipos.Add(claseTipo);
        await contexto.SaveChangesAsync(cancellationToken);

        var tipoRol = new Tipos
        {
            Id = tipoRolId,
            Nombre = "Rol administrador",
            EstadoId = estadoActivo.Id,
            ModuloId = modulo.Id,
            ClaseTipoId = claseTipo.Id
        };
        var tipoInicioSesion = new Tipos
        {
            Id = tipoInicioSesionId,
            Nombre = "Inicio de sesión local",
            EstadoId = estadoActivo.Id,
            ModuloId = modulo.Id,
            ClaseTipoId = claseTipo.Id
        };
        contexto.AddRange(tipoRol, tipoInicioSesion);
        await contexto.SaveChangesAsync(cancellationToken);

        var nivelAcceso = new NivelesDeAccesos
        {
            Id = nivelAccesoId,
            Nombre = "Administrador"
        };
        contexto.NivelesDeAccesos.Add(nivelAcceso);
        await contexto.SaveChangesAsync(cancellationToken);

        var rol = new Roles
        {
            Id = RolAdministradorId,
            Nombre = "Administrador",
            NivelDeAccesoId = nivelAcceso.Id,
            CambioDeContrasenia = MesesCambioContrasenaAdministrador,
            TipoId = tipoRol.Id,
            AplicarConfiguracionesPorRol = true
        };
        contexto.Roles.Add(rol);
        await contexto.SaveChangesAsync(cancellationToken);

        var accion = new AccionesSistema
        {
            Codigo = "ADMINISTRAR",
            Nombre = "Administrar",
            EstadoId = estadoActivo.Id
        };
        contexto.AccionesSistema.Add(accion);
        await contexto.SaveChangesAsync(cancellationToken);
        ValidarIdentidadGenerada(accion.Id, "AccionesSistema");

        var permiso = new Permisos
        {
            ModuloId = modulo.Id,
            AccionId = accion.Id,
            Recurso = "Seguridad",
            Codigo = CodigoPermisoAdministrar,
            EstadoId = estadoActivo.Id
        };
        contexto.Permisos.Add(permiso);
        await contexto.SaveChangesAsync(cancellationToken);
        ValidarIdentidadGenerada(permiso.Id, "Permisos");

        var rolPermiso = new RolesPermisos
        {
            RoleId = rol.Id,
            PermisoId = permiso.Id,
            Permitido = true
        };
        contexto.RolesPermisos.Add(rolPermiso);
        await contexto.SaveChangesAsync(cancellationToken);
        ValidarIdentidadGenerada(rolPermiso.Id, "RolesPermisos");

        var empresa = new Empresas
        {
            RazonSocial = "Heladeria Inicial",
            NombreComercial = "Heladeria",
            Eslogan = "Configuración inicial",
            Direccion = "Pendiente de configurar",
            Nit = "CF",
            Telefono = "Pendiente",
            Email = "empresa@heladeria.local",
            Imagen = "sin-imagen",
            Icono = "sin-icono",
            EstadoId = estadoActivo.Id,
            ImagenReportes = "sin-imagen",
            RoleId = rol.Id
        };
        contexto.Empresas.Add(empresa);
        await contexto.SaveChangesAsync(cancellationToken);

        ValidarIdentidadGenerada(empresa.Id, "Empresas");
        var proveedor = new Proveedores
        {
            EmpresaId = empresa.Id,
            CodigoProveedor = CodigoProveedorInicial,
            NombreProveedor = "Proveedor inicial",
            Nit = null,
            Telefono = null,
            Correo = null,
            Direccion = null,
            DiasCredito = DiasCreditoProveedorInicial,
            LimiteCredito = null,
            MonedaId = null,
            EstadoId = estadoActivo.Id
        };
        contexto.Proveedores.Add(proveedor);
        await contexto.SaveChangesAsync(cancellationToken);

        ValidarIdentidadGenerada(proveedor.Id, "Proveedores");

        var sucursal = new Sucursales
        {
            Nombre = "Sucursal Central",
            Direccion = "Pendiente de configurar",
            DireccionCorta = "Central",
            Telefono = "Pendiente",
            Email = "central@heladeria.local",
            EmpresaId = empresa.Id,
            EsCentral = true,
            EstadoId = estadoActivo.Id,
            ProveedorId = proveedor.Id,
            EsBodega = false,
            Transportes = false,
            Codigo = "CENTRAL",
            CodigoNombre = "CENTRAL Sucursal Central"
        };
        contexto.Sucursales.Add(sucursal);
        await contexto.SaveChangesAsync(cancellationToken);
        ValidarIdentidadGenerada(sucursal.Id, "Sucursales");

        var usuario = new Usuarios
        {
            Email = correo,
            UserName = nombreUsuario,
            IdentityId = Guid.NewGuid().ToString("N"),
            PasswordHash = string.Empty,
            SecurityStamp = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)),
            RoleId = rol.Id,
            AccessFailedCount = 0,
            HorarioInicio = new TimeOnly(0, 0),
            HorarioFinal = new TimeOnly(0, 0),
            UltimoCambioDeContrasenia = null,
            Imagen = "sin-imagen",
            Soundex = "A353",
            ContraseniaTemporal = true,
            EstadoId = estadoActivo.Id,
            PasswordTemporal = null,
            TipoInicioSesionId = tipoInicioSesion.Id,
            UtilizarConfiguracionDeRole = true
        };
        usuario.PasswordHash = passwordHasher.HashPassword(usuario, solicitud.Contrasena);
        contexto.Usuarios.Add(usuario);
        await contexto.SaveChangesAsync(cancellationToken);
        ValidarIdentidadGenerada(usuario.Id, "Usuarios");

        var usuarioSucursal = new UsuariosSucursales
        {
            UsuarioId = usuario.Id,
            SucursalId = sucursal.Id,
            EsPrincipal = true,
            EstadoId = estadoActivo.Id
        };
        contexto.UsuariosSucursales.Add(usuarioSucursal);
        await contexto.SaveChangesAsync(cancellationToken);
        ValidarIdentidadGenerada(usuarioSucursal.Id, "UsuariosSucursales");

        var marcador = new Bitacoras
        {
            EmpresaId = empresa.Id,
            SucursalId = sucursal.Id,
            ModuloId = modulo.Id,
            Entidad = EntidadMarcador,
            EntidadId = usuario.Id.ToString(),
            Accion = AccionMarcador,
            Fecha = timeProvider.GetUtcNow().UtcDateTime,
            UsuarioId = usuario.Id,
            Comentario = "Instalación administrativa inicial completada."
        };
        contexto.Bitacoras.Add(marcador);
        await contexto.SaveChangesAsync(cancellationToken);
        ValidarIdentidadGenerada(marcador.Id, "Bitacoras");
        await ConfirmarAsync(transaccion, cancellationToken);

        return new ResultadoBootstrapAdministrador(
        [
            new("ClaseEstados", claseEstado.Id.ToString(), claseEstado.Nombre),
            new("Estados", estadoActivo.Id.ToString(), estadoActivo.Nombre),
            new("Modulos", modulo.Id, modulo.Nombre),
            new("ClaseTipos", claseTipo.Id.ToString(), claseTipo.Nombre),
            new("Tipos", tipoRol.Id.ToString(), tipoRol.Nombre),
            new("Tipos", tipoInicioSesion.Id.ToString(), tipoInicioSesion.Nombre),
            new("NivelesDeAccesos", nivelAcceso.Id.ToString(), nivelAcceso.Nombre),
            new("Roles", rol.Id, rol.Nombre),
            new("AccionesSistema", accion.Id.ToString(), accion.Codigo),
            new("Permisos", permiso.Id.ToString(), permiso.Codigo),
            new("RolesPermisos", rolPermiso.Id.ToString(), $"{rol.Id} -> {permiso.Codigo}"),
            new("Empresas", empresa.Id.ToString(), empresa.RazonSocial),
            new("Proveedores", proveedor.Id.ToString(), $"{proveedor.CodigoProveedor} - {proveedor.NombreProveedor}"),
            new("Sucursales", sucursal.Id.ToString(), sucursal.Nombre),
            new("Usuarios", usuario.Id.ToString(), $"{usuario.UserName} ({usuario.Email})"),
            new("UsuariosSucursales", usuarioSucursal.Id.ToString(), "Sucursal principal"),
            new("Bitacoras", marcador.Id.ToString(), AccionMarcador)
        ]);
    }

    private void ValidarSolicitud(
        string nombreUsuario,
        string correo,
        string contrasena)
    {
        if (nombreUsuario.Length is < 1 or > 50)
        {
            throw new BootstrapAdministradorRechazadoException(
                "El nombre de usuario debe tener entre 1 y 50 caracteres.");
        }

        if (correo.Length > 100 || !MailAddress.TryCreate(correo, out _))
        {
            throw new BootstrapAdministradorRechazadoException(
                "El correo del administrador no es válido.");
        }

        var politica = opciones.Value.Contrasenas;
        if (politica.LongitudMinima < 10
            || !politica.RequiereMayuscula
            || !politica.RequiereMinuscula
            || !politica.RequiereNumero
            || !politica.RequiereCaracterEspecial)
        {
            throw new BootstrapAdministradorRechazadoException(
                "La configuración de contraseñas no cumple los mínimos exigidos por el bootstrap.");
        }

        ValidadorPoliticaContrasena.Validar(contrasena, contrasena, politica);
    }

    private async Task ValidarInstalacionVaciaAsync(
        string nombreUsuario,
        string correo,
        CancellationToken cancellationToken)
    {
        if (await contexto.Usuarios.AnyAsync(
                usuario => usuario.RoleId == RolAdministradorId
                    || usuario.UserName == nombreUsuario
                    || usuario.Email == correo
                    || usuario.Role.Nombre == "Administrador",
                cancellationToken))
        {
            throw new BootstrapAdministradorRechazadoException(
                "Ya existe un usuario administrador o la identidad inicial solicitada.");
        }

        if (await contexto.Roles.AnyAsync(
                rol => rol.Id == RolAdministradorId || rol.Nombre == "Administrador",
                cancellationToken))
        {
            throw new BootstrapAdministradorRechazadoException(
                "Ya existe el rol ADMIN o un rol Administrador.");
        }

        if (await contexto.Bitacoras.AnyAsync(
                item => item.Entidad == EntidadMarcador && item.Accion == AccionMarcador,
                cancellationToken))
        {
            throw new BootstrapAdministradorRechazadoException(
                "La instalación administrativa inicial ya fue completada.");
        }

        var instalacionParcial = await contexto.Modulos.AnyAsync(
                item => item.Id == ModuloSeguridadId,
                cancellationToken)
            || await contexto.Permisos.AnyAsync(
                item => item.Codigo == CodigoPermisoAdministrar,
                cancellationToken)
            || await contexto.AccionesSistema.AnyAsync(
                item => item.Codigo == "ADMINISTRAR",
                cancellationToken)
            || await contexto.ClaseEstados.AnyAsync(
                item => item.Nombre == "Seguridad",
                cancellationToken)
            || await contexto.ClaseTipos.AnyAsync(
                item => item.Nombre == "Seguridad",
                cancellationToken)
            || await contexto.Tipos.AnyAsync(
                item => item.ClaseTipo.Nombre == "Seguridad"
                    && (item.Nombre == "Rol administrador"
                        || item.Nombre == "Inicio de sesión local"),
                cancellationToken)
            || await contexto.Empresas.AnyAsync(
                item => item.Nit == "CF"
                    || item.Email == "empresa@heladeria.local",
                cancellationToken)
            || await contexto.Proveedores.AnyAsync(
                item => item.CodigoProveedor == CodigoProveedorInicial
                    && item.NombreProveedor == "Proveedor inicial",
                cancellationToken)
            || await contexto.Sucursales.AnyAsync(
                item => item.Codigo == "CENTRAL"
                    && item.Email == "central@heladeria.local",
                cancellationToken);

        if (instalacionParcial)
        {
            throw new BootstrapAdministradorRechazadoException(
                "Se detectó una instalación de seguridad parcial. No se realizaron cambios.");
        }
    }

    private void ValidarContratoDependencias()
    {
        ValidarClave<ClaseEstados>(nameof(ClaseEstados.Id), ValueGenerated.Never);
        ValidarClave<Estados>(nameof(Estados.Id), ValueGenerated.Never);
        ValidarClave<Modulos>(nameof(Modulos.Id), ValueGenerated.Never);
        ValidarClave<ClaseTipos>(nameof(ClaseTipos.Id), ValueGenerated.Never);
        ValidarClave<Tipos>(nameof(Tipos.Id), ValueGenerated.Never);
        ValidarClave<NivelesDeAccesos>(nameof(NivelesDeAccesos.Id), ValueGenerated.Never);
        ValidarClave<Roles>(nameof(Roles.Id), ValueGenerated.Never);
        ValidarClave<AccionesSistema>(nameof(AccionesSistema.Id), ValueGenerated.OnAdd);
        ValidarClave<Permisos>(nameof(Permisos.Id), ValueGenerated.OnAdd);
        ValidarClave<RolesPermisos>(nameof(RolesPermisos.Id), ValueGenerated.OnAdd);
        ValidarClave<Empresas>(nameof(Empresas.Id), ValueGenerated.OnAdd);
        ValidarClave<Proveedores>(nameof(Proveedores.Id), ValueGenerated.OnAdd);
        ValidarClave<Sucursales>(nameof(Sucursales.Id), ValueGenerated.OnAdd);
        ValidarClave<Usuarios>(nameof(Usuarios.Id), ValueGenerated.OnAdd);
        ValidarClave<UsuariosSucursales>(nameof(UsuariosSucursales.Id), ValueGenerated.OnAdd);
        ValidarClave<Bitacoras>(nameof(Bitacoras.Id), ValueGenerated.OnAdd);

        ValidarRelacion<Estados, ClaseEstados>(nameof(Estados.ClaseEstadoId), requerida: true);
        ValidarRelacion<Estados, Modulos>(nameof(Estados.ModuloId), requerida: false);
        ValidarRelacion<ClaseTipos, Estados>(nameof(ClaseTipos.EstadoId), requerida: true);
        ValidarRelacion<Tipos, Estados>(nameof(Tipos.EstadoId), requerida: true);
        ValidarRelacion<Tipos, Modulos>(nameof(Tipos.ModuloId), requerida: false);
        ValidarRelacion<Tipos, ClaseTipos>(nameof(Tipos.ClaseTipoId), requerida: true);
        ValidarRelacion<Roles, NivelesDeAccesos>(nameof(Roles.NivelDeAccesoId), requerida: true);
        ValidarRelacion<Roles, Tipos>(nameof(Roles.TipoId), requerida: true);
        ValidarRelacion<AccionesSistema, Estados>(nameof(AccionesSistema.EstadoId), requerida: true);
        ValidarRelacion<Permisos, Modulos>(nameof(Permisos.ModuloId), requerida: true);
        ValidarRelacion<Permisos, AccionesSistema>(nameof(Permisos.AccionId), requerida: true);
        ValidarRelacion<Permisos, Estados>(nameof(Permisos.EstadoId), requerida: true);
        ValidarRelacion<RolesPermisos, Roles>(nameof(RolesPermisos.RoleId), requerida: true);
        ValidarRelacion<RolesPermisos, Permisos>(nameof(RolesPermisos.PermisoId), requerida: true);
        ValidarRelacion<Empresas, Estados>(nameof(Empresas.EstadoId), requerida: true);
        ValidarRelacion<Empresas, Roles>(nameof(Empresas.RoleId), requerida: true);
        ValidarRelacion<Empresas, Tipos>(nameof(Empresas.ClienteId), requerida: false);
        ValidarRelacion<Proveedores, Empresas>(nameof(Proveedores.EmpresaId), requerida: true);
        ValidarRelacion<Proveedores, Estados>(nameof(Proveedores.EstadoId), requerida: true);
        ValidarRelacion<Proveedores, Monedas>(nameof(Proveedores.MonedaId), requerida: false);
        ValidarRelacion<Sucursales, Empresas>(nameof(Sucursales.EmpresaId), requerida: true);
        ValidarRelacion<Sucursales, Estados>(nameof(Sucursales.EstadoId), requerida: true);
        ValidarRelacion<Sucursales, Proveedores>(nameof(Sucursales.ProveedorId), requerida: true);
        ValidarRelacion<Sucursales, Clientes>(nameof(Sucursales.ClienteId), requerida: false);
        ValidarRelacion<Sucursales, Municipios>(nameof(Sucursales.MunicipioId), requerida: false);
        ValidarRelacion<Sucursales, Departamentos>(nameof(Sucursales.DepartamentoId), requerida: false);
        ValidarRelacion<Sucursales, Sucursales>(nameof(Sucursales.SucursalCentralId), requerida: false);
        ValidarRelacion<Sucursales, Sucursales>(nameof(Sucursales.SucursalTransporteId), requerida: false);
        ValidarRelacion<Usuarios, Roles>(nameof(Usuarios.RoleId), requerida: true);
        ValidarRelacion<Usuarios, Estados>(nameof(Usuarios.EstadoId), requerida: true);
        ValidarRelacion<Usuarios, Tipos>(nameof(Usuarios.TipoInicioSesionId), requerida: true);
        ValidarRelacion<UsuariosSucursales, Usuarios>(nameof(UsuariosSucursales.UsuarioId), requerida: true);
        ValidarRelacion<UsuariosSucursales, Sucursales>(nameof(UsuariosSucursales.SucursalId), requerida: true);
        ValidarRelacion<UsuariosSucursales, Estados>(nameof(UsuariosSucursales.EstadoId), requerida: true);
        ValidarRelacion<Bitacoras, Empresas>(nameof(Bitacoras.EmpresaId), requerida: false);
        ValidarRelacion<Bitacoras, Sucursales>(nameof(Bitacoras.SucursalId), requerida: false);
        ValidarRelacion<Bitacoras, Modulos>(nameof(Bitacoras.ModuloId), requerida: false);
        ValidarRelacion<Bitacoras, Usuarios>(nameof(Bitacoras.UsuarioId), requerida: false);
    }

    private static void ValidarRestriccionesEsquema()
    {
        if (MesesCambioContrasenaAdministrador is <= 0 or >= 49)
        {
            throw new BootstrapAdministradorRechazadoException(
                "El período de cambio de contraseña del rol no cumple CK_Roles_CambioDeContrasenia.");
        }

        if (DiasCreditoProveedorInicial < 0)
        {
            throw new BootstrapAdministradorRechazadoException(
                "Los días de crédito del proveedor no cumplen CK_Proveedores_DiasCredito.");
        }
    }

    private DateOnly ObtenerFechaLocal()
    {
        var zonaHoraria = TimeZoneInfo.FindSystemTimeZoneById(opciones.Value.ZonaHoraria);
        return DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), zonaHoraria).DateTime);
    }

    private void ValidarClave<TEntidad>(string propiedad, ValueGenerated generacion)
    {
        var entidad = contexto.Model.FindEntityType(typeof(TEntidad));
        var clave = entidad?.FindProperty(propiedad);

        if (clave is null || clave.ValueGenerated != generacion)
        {
            throw new BootstrapAdministradorRechazadoException(
                $"El modelo EF Core no coincide con la generación esperada de {typeof(TEntidad).Name}.{propiedad}.");
        }
    }

    private void ValidarRelacion<TDependiente, TPrincipal>(
        string propiedad,
        bool requerida)
    {
        var dependiente = contexto.Model.FindEntityType(typeof(TDependiente));
        var propiedadForanea = dependiente?.FindProperty(propiedad);
        var relacion = dependiente?.GetForeignKeys().SingleOrDefault(
            item => item.Properties.Count == 1
                && item.Properties[0].Name == propiedad
                && item.PrincipalEntityType.ClrType == typeof(TPrincipal));

        if (propiedadForanea is null
            || relacion is null
            || propiedadForanea.IsNullable != !requerida)
        {
            throw new BootstrapAdministradorRechazadoException(
                $"El modelo EF Core no contiene la dependencia esperada {typeof(TDependiente).Name}.{propiedad} -> {typeof(TPrincipal).Name}.");
        }
    }

    private static void ValidarIdentidadGenerada(long id, string tabla)
    {
        if (id <= 0)
        {
            throw new BootstrapAdministradorRechazadoException(
                $"SQL Server no generó un identificador válido para {tabla}.");
        }
    }

    private async Task<IDbContextTransaction?> IniciarTransaccionAsync(
        CancellationToken cancellationToken) =>
        contexto.Database.IsRelational()
            ? await contexto.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken)
            : null;

    private Task TomarBloqueoExclusivoAsync(CancellationToken cancellationToken)
    {
        if (!contexto.Database.IsSqlServer())
        {
            return Task.CompletedTask;
        }

        return contexto.Database.ExecuteSqlRawAsync(
            """
            DECLARE @resultado int;
            EXEC @resultado = sys.sp_getapplock
                @Resource = N'Heladeria.BootstrapAdministrador',
                @LockMode = N'Exclusive',
                @LockOwner = N'Transaction',
                @LockTimeout = 0;
            IF @resultado < 0
                THROW 51000, 'No fue posible adquirir el bloqueo exclusivo del bootstrap.', 1;
            """,
            cancellationToken);
    }

    private static Task ConfirmarAsync(
        IDbContextTransaction? transaccion,
        CancellationToken cancellationToken) =>
        transaccion is null
            ? Task.CompletedTask
            : transaccion.CommitAsync(cancellationToken);
}
