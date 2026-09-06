using Heladeria.Aplicacion.Seguridad.Configuracion;
using Heladeria.Infraestructura.Persistencia;
using Heladeria.Infraestructura.Persistencia.Entidades;
using Heladeria.Infraestructura.Seguridad.Autenticacion;
using Heladeria.Infraestructura.Seguridad.Autorizacion;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Heladeria.Pruebas.Seguridad;

internal sealed class EscenarioSeguridad : IAsyncDisposable
{
    public const string ContrasenaValida = "Helado#Seguro2026";
    public const string ClaveJwtPruebas =
        "clave-jwt-exclusiva-para-pruebas-automatizadas-heladeria-2026-segura";

    private readonly PasswordHasher<Usuarios> _passwordHasher = new();

    private EscenarioSeguridad(
        HeladeriaDbContext contexto,
        OpcionesSeguridad opciones,
        TiempoFijo tiempo)
    {
        Contexto = contexto;
        Opciones = opciones;
        Tiempo = tiempo;
    }

    public HeladeriaDbContext Contexto { get; }

    public OpcionesSeguridad Opciones { get; }

    public TiempoFijo Tiempo { get; }

    public Usuarios Usuario => Contexto.Usuarios.Single(item => item.Id == 1);

    public static async Task<EscenarioSeguridad> CrearAsync(
        bool usuarioActivo = true,
        bool contrasenaTemporal = false,
        bool incluirSegundaSucursal = false,
        bool incluirPermiso = false)
    {
        var opcionesDb = new DbContextOptionsBuilder<HeladeriaDbContext>()
            .UseInMemoryDatabase($"heladeria-seguridad-{Guid.NewGuid():N}")
            .Options;
        var contexto = new HeladeriaDbContext(opcionesDb);
        var opciones = CrearOpciones();
        var tiempo = new TiempoFijo(new DateTimeOffset(2026, 9, 2, 18, 0, 0, TimeSpan.Zero));
        var escenario = new EscenarioSeguridad(contexto, opciones, tiempo);
        await escenario.SemillarAsync(
            usuarioActivo,
            contrasenaTemporal,
            incluirSegundaSucursal,
            incluirPermiso);
        return escenario;
    }

    public ServicioAutenticacion CrearServicio()
    {
        var opciones = Options.Create(Opciones);
        return new ServicioAutenticacion(
            Contexto,
            _passwordHasher,
            new GeneradorTokensJwt(opciones, Tiempo),
            opciones,
            Tiempo,
            new ServicioPermisos(Contexto, opciones),
            NullLogger<ServicioAutenticacion>.Instance);
    }

    public bool VerificarContrasena(Usuarios usuario, string contrasena) =>
        _passwordHasher.VerifyHashedPassword(
            usuario,
            usuario.PasswordHash,
            contrasena) != PasswordVerificationResult.Failed;

    public async ValueTask DisposeAsync() => await Contexto.DisposeAsync();

    public static OpcionesSeguridad CrearOpciones() => new()
    {
        Jwt = new OpcionesJwt
        {
            Emisor = "Heladeria.Api.Pruebas",
            Audiencia = "Heladeria.Pruebas",
            ClaveFirma = ClaveJwtPruebas,
            MinutosAccessToken = 10,
            DiasRefreshToken = 7
        },
        Bloqueo = new OpcionesBloqueo
        {
            IntentosMaximos = 5,
            MinutosBloqueo = 15
        },
        EstadosActivos = ["Activo"],
        ZonaHoraria = "America/Guatemala"
    };

    private async Task SemillarAsync(
        bool usuarioActivo,
        bool contrasenaTemporal,
        bool incluirSegundaSucursal,
        bool incluirPermiso)
    {
        var estadoActivo = new Estados
        {
            Id = 1,
            Nombre = "Activo",
            Activo = true,
            ClaseEstadoId = 1
        };
        var estadoInactivo = new Estados
        {
            Id = 2,
            Nombre = "Inactivo",
            Activo = false,
            ClaseEstadoId = 1
        };
        var rol = new Roles
        {
            Id = "ADMIN",
            Nombre = "Administrador",
            NivelDeAccesoId = 1,
            CambioDeContrasenia = 12,
            TipoId = 1,
            AplicarConfiguracionesPorRol = true
        };
        var empresa = new Empresas
        {
            Id = 1,
            RazonSocial = "Heladeria Pruebas, S.A.",
            NombreComercial = "Heladeria Pruebas",
            Eslogan = "Pruebas seguras",
            Direccion = "Ciudad de Guatemala",
            Nit = "CF",
            Telefono = "55550000",
            Email = "empresa@pruebas.local",
            Imagen = "sin-imagen",
            Icono = "sin-icono",
            ImagenReportes = "sin-imagen",
            EstadoId = estadoActivo.Id,
            RoleId = rol.Id
        };
        var sucursal = CrearSucursal(1, "Central", empresa.Id, estadoActivo.Id);
        var usuario = new Usuarios
        {
            Id = 1,
            Email = "admin@heladeria.local",
            UserName = "admin",
            IdentityId = "usuario-admin-pruebas",
            RoleId = rol.Id,
            AccessFailedCount = 0,
            HorarioInicio = new TimeOnly(0, 0),
            HorarioFinal = new TimeOnly(0, 0),
            UltimoCambioDeContrasenia = new DateOnly(2026, 9, 1),
            Imagen = "sin-imagen",
            Soundex = "A353",
            ContraseniaTemporal = contrasenaTemporal,
            EstadoId = usuarioActivo ? estadoActivo.Id : estadoInactivo.Id,
            TipoInicioSesionId = 1
        };
        usuario.PasswordHash = _passwordHasher.HashPassword(usuario, ContrasenaValida);
        var asignacion = new UsuariosSucursales
        {
            Id = 1,
            UsuarioId = usuario.Id,
            SucursalId = sucursal.Id,
            EsPrincipal = true,
            EstadoId = estadoActivo.Id
        };

        Contexto.AddRange(estadoActivo, estadoInactivo, rol, empresa, sucursal, usuario, asignacion);

        if (incluirSegundaSucursal)
        {
            var segunda = CrearSucursal(2, "Zona 10", empresa.Id, estadoActivo.Id);
            Contexto.AddRange(
                segunda,
                new UsuariosSucursales
                {
                    Id = 2,
                    UsuarioId = usuario.Id,
                    SucursalId = segunda.Id,
                    EsPrincipal = false,
                    EstadoId = estadoActivo.Id
                });
        }

        if (incluirPermiso)
        {
            var modulo = new Modulos
            {
                Id = "SEGURIDAD",
                Nombre = "Seguridad",
                Icono = "shield",
                Activo = true
            };
            var accion = new AccionesSistema
            {
                Id = 1,
                Codigo = "ADMINISTRAR",
                Nombre = "Administrar",
                EstadoId = estadoActivo.Id
            };
            var permiso = new Permisos
            {
                Id = 1,
                ModuloId = modulo.Id,
                AccionId = accion.Id,
                Recurso = "Seguridad",
                Codigo = "SEGURIDAD.ADMINISTRAR",
                EstadoId = estadoActivo.Id
            };
            Contexto.AddRange(
                modulo,
                accion,
                permiso,
                new RolesPermisos
                {
                    Id = 1,
                    RoleId = rol.Id,
                    PermisoId = permiso.Id,
                    Permitido = true
                });
        }

        await Contexto.SaveChangesAsync();
        Contexto.ChangeTracker.Clear();
    }

    private static Sucursales CrearSucursal(
        long id,
        string nombre,
        int empresaId,
        int estadoId)
    {
        return new Sucursales
        {
            Id = id,
            Nombre = nombre,
            Direccion = "Ciudad de Guatemala",
            DireccionCorta = "Guatemala",
            Telefono = "55550000",
            Email = $"sucursal{id}@pruebas.local",
            EmpresaId = empresaId,
            EsCentral = id == 1,
            EstadoId = estadoId,
            ProveedorId = 1,
            EsBodega = false,
            Transportes = false,
            CodigoNombre = $"SUC-{id} {nombre}"
        };
    }
}

internal sealed class TiempoFijo(DateTimeOffset fechaUtc) : TimeProvider
{
    private DateTimeOffset _fechaUtc = fechaUtc;

    public override DateTimeOffset GetUtcNow() => _fechaUtc;

    public void Avanzar(TimeSpan tiempo) => _fechaUtc = _fechaUtc.Add(tiempo);
}
