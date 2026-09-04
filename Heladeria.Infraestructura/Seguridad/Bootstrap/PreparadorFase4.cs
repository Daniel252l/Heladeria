using System.Data;
using Heladeria.Aplicacion.Seguridad.Autorizacion;
using Heladeria.Aplicacion.Seguridad.Bootstrap;
using Heladeria.Aplicacion.Seguridad.Configuracion;
using Heladeria.Infraestructura.Persistencia;
using Heladeria.Infraestructura.Persistencia.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;

namespace Heladeria.Infraestructura.Seguridad.Bootstrap;

public sealed class PreparadorFase4(
    HeladeriaDbContext contexto,
    IOptions<OpcionesSeguridad> opciones,
    TimeProvider timeProvider) : IPreparadorFase4
{
    public const string ModuloId = "INVENTARIO";
    public const string AccionMarcador = "FASE4_CONFIGURADA";
    public const string FlujoMovimientosCodigo = "MOVIMIENTOS_INVENTARIO";
    public const string BodegaInicialCodigo = "BOD-INICIAL";
    public const string UbicacionInicialCodigo = "GENERAL";
    private const string RolAdministrador = "ADMIN";
    private const string EntidadMarcador = "Instalacion";

    private static readonly string[] CodigosPermisos =
    [
        PermisosSistema.ProductosConsultar,
        PermisosSistema.ProductosAdministrar,
        PermisosSistema.InventarioConsultar,
        PermisosSistema.InventarioAdministrar
    ];

    public async Task<ResultadoBootstrapAdministrador> EjecutarAsync(CancellationToken token)
    {
        await using var transaccion = await IniciarTransaccionAsync(token);
        await TomarBloqueoAsync(token);
        await ValidarNoEjecutadoAsync(token);

        var estadosActivos = opciones.Value.EstadosActivos;
        var referencia = await contexto.UsuariosSucursales.AsNoTracking()
            .Where(asignacion => asignacion.Usuario.RoleId == RolAdministrador
                && asignacion.Usuario.Estado.Activo
                && estadosActivos.Contains(asignacion.Usuario.Estado.Nombre)
                && asignacion.EsPrincipal
                && asignacion.Estado.Activo
                && estadosActivos.Contains(asignacion.Estado.Nombre)
                && asignacion.Sucursal.Estado.Activo
                && estadosActivos.Contains(asignacion.Sucursal.Estado.Nombre)
                && asignacion.Sucursal.Empresa.Estado.Activo
                && estadosActivos.Contains(asignacion.Sucursal.Empresa.Estado.Nombre))
            .OrderBy(asignacion => asignacion.UsuarioId)
            .Select(asignacion => new ReferenciaAdministrador(
                asignacion.UsuarioId,
                asignacion.Sucursal.EmpresaId,
                asignacion.SucursalId))
            .FirstOrDefaultAsync(token)
            ?? throw new BootstrapAdministradorRechazadoException(
                "No existe un administrador activo con empresa y sucursal principal operativas.");

        var seguridad = await contexto.Permisos.AsNoTracking()
            .Where(item => item.Codigo == PermisosSistema.SeguridadAdministrar
                && item.Estado.Activo
                && estadosActivos.Contains(item.Estado.Nombre)
                && item.Accion.Codigo == "ADMINISTRAR"
                && item.Accion.Estado.Activo)
            .Select(item => new { item.EstadoId, AccionAdministrarId = item.AccionId })
            .SingleOrDefaultAsync(token)
            ?? throw new BootstrapAdministradorRechazadoException(
                "No existe SEGURIDAD.ADMINISTRAR activo con su acción ADMINISTRAR.");

        var creados = new List<RegistroBootstrapCreado>();
        var modulo = new Modulos
        {
            Id = ModuloId,
            Nombre = "Productos e inventario",
            Icono = "inventory_2",
            Activo = true
        };
        contexto.Modulos.Add(modulo);
        await contexto.SaveChangesAsync(token);
        creados.Add(new("Modulos", modulo.Id, modulo.Nombre));

        var accionConsultar = await contexto.AccionesSistema.SingleOrDefaultAsync(
            item => item.Codigo == "CONSULTAR",
            token);
        if (accionConsultar is null)
        {
            accionConsultar = new AccionesSistema
            {
                Codigo = "CONSULTAR",
                Nombre = "Consultar",
                EstadoId = seguridad.EstadoId
            };
            contexto.AccionesSistema.Add(accionConsultar);
            await contexto.SaveChangesAsync(token);
            creados.Add(new("AccionesSistema", accionConsultar.Id.ToString(), accionConsultar.Codigo));
        }
        else if (accionConsultar.EstadoId != seguridad.EstadoId)
        {
            throw new BootstrapAdministradorRechazadoException(
                "La acción CONSULTAR existente no utiliza el estado operativo requerido.");
        }

        foreach (var definicion in new[]
        {
            new DefinicionPermiso(PermisosSistema.ProductosConsultar, accionConsultar.Id, "Productos"),
            new DefinicionPermiso(PermisosSistema.ProductosAdministrar, seguridad.AccionAdministrarId, "Productos"),
            new DefinicionPermiso(PermisosSistema.InventarioConsultar, accionConsultar.Id, "Inventario"),
            new DefinicionPermiso(PermisosSistema.InventarioAdministrar, seguridad.AccionAdministrarId, "Inventario")
        })
        {
            var permiso = new Permisos
            {
                ModuloId = modulo.Id,
                AccionId = definicion.AccionId,
                Recurso = definicion.Recurso,
                Codigo = definicion.Codigo,
                EstadoId = seguridad.EstadoId
            };
            contexto.Permisos.Add(permiso);
            await contexto.SaveChangesAsync(token);
            creados.Add(new("Permisos", permiso.Id.ToString(), permiso.Codigo));

            var rolPermiso = new RolesPermisos
            {
                RoleId = RolAdministrador,
                PermisoId = permiso.Id,
                Permitido = true
            };
            contexto.RolesPermisos.Add(rolPermiso);
            await contexto.SaveChangesAsync(token);
            creados.Add(new("RolesPermisos", rolPermiso.Id.ToString(), $"ADMIN -> {permiso.Codigo}"));
        }

        var ahora = timeProvider.GetUtcNow().UtcDateTime;
        var claseSeguimiento = await ObtenerOCrearClaseCatalogoAsync(
            "TIPO_SEGUIMIENTO_PRODUCTO", "Tipos de seguimiento de producto", referencia.UsuarioId, seguridad.EstadoId, ahora, creados, token);
        var claseValuacion = await ObtenerOCrearClaseCatalogoAsync(
            "METODO_VALUACION_INVENTARIO", "Métodos de valuación de inventario", referencia.UsuarioId, seguridad.EstadoId, ahora, creados, token);
        var claseUbicacion = await ObtenerOCrearClaseCatalogoAsync(
            "TIPO_UBICACION", "Tipos de ubicación de bodega", referencia.UsuarioId, seguridad.EstadoId, ahora, creados, token);
        await ObtenerOCrearCatalogoAsync(claseSeguimiento.Id, referencia.EmpresaId, "NINGUNO", "Sin seguimiento", referencia.UsuarioId, seguridad.EstadoId, ahora, creados, token);
        await ObtenerOCrearCatalogoAsync(claseValuacion.Id, referencia.EmpresaId, "PROMEDIO", "Costo promedio", referencia.UsuarioId, seguridad.EstadoId, ahora, creados, token);
        var tipoUbicacion = await ObtenerOCrearCatalogoAsync(claseUbicacion.Id, referencia.EmpresaId, "GENERAL", "Ubicación general", referencia.UsuarioId, seguridad.EstadoId, ahora, creados, token);

        var flujo = await ObtenerOCrearFlujoAsync(referencia.UsuarioId, seguridad.EstadoId, ahora, creados, token);
        await ObtenerOCrearEstadoFlujoAsync(flujo.Id, referencia.UsuarioId, seguridad.EstadoId, ahora, creados, token);
        await ObtenerOCrearConceptoAsync(referencia, "AJUSTE-ENTRADA", "Ajuste manual de entrada", "ENTRADA", seguridad.EstadoId, ahora, creados, token);
        await ObtenerOCrearConceptoAsync(referencia, "AJUSTE-SALIDA", "Ajuste manual de salida", "SALIDA", seguridad.EstadoId, ahora, creados, token);

        var bodega = await contexto.Bodegas.FirstOrDefaultAsync(item =>
            item.EmpresaId == referencia.EmpresaId
            && item.SucursalId == referencia.SucursalId
            && item.EsPrincipal
            && item.Estado.Activo
            && estadosActivos.Contains(item.Estado.Nombre),
            token);
        if (bodega is null)
        {
            bodega = new Bodegas
            {
                EmpresaId = referencia.EmpresaId,
                SucursalId = referencia.SucursalId,
                Codigo = BodegaInicialCodigo,
                Nombre = "Bodega inicial",
                Descripcion = "Bodega técnica inicial preparada por Fase 4.",
                PermiteExistenciaNegativa = false,
                EsPrincipal = true,
                EstadoId = seguridad.EstadoId,
                FechaCreacion = ahora,
                CreadoPorUsuarioId = referencia.UsuarioId,
                VersionFila = []
            };
            contexto.Bodegas.Add(bodega);
            await contexto.SaveChangesAsync(token);
            creados.Add(new("Bodegas", bodega.Id.ToString(), bodega.Codigo));
        }

        var ubicacion = await contexto.Ubicaciones.FirstOrDefaultAsync(item =>
            item.EmpresaId == referencia.EmpresaId
            && item.BodegaId == bodega.Id
            && item.Codigo == UbicacionInicialCodigo
            && item.Estado.Activo
            && estadosActivos.Contains(item.Estado.Nombre),
            token);
        if (ubicacion is null)
        {
            ubicacion = new Ubicaciones
            {
                EmpresaId = referencia.EmpresaId,
                BodegaId = bodega.Id,
                Codigo = UbicacionInicialCodigo,
                Nombre = "Ubicación general",
                TipoUbicacionId = tipoUbicacion.Id,
                EsRecepcion = true,
                EsDespacho = true,
                EstadoId = seguridad.EstadoId,
                FechaCreacion = ahora,
                CreadoPorUsuarioId = referencia.UsuarioId,
                VersionFila = []
            };
            contexto.Ubicaciones.Add(ubicacion);
            await contexto.SaveChangesAsync(token);
            creados.Add(new("Ubicaciones", ubicacion.Id.ToString(), ubicacion.Codigo));
        }

        var marcador = new Bitacoras
        {
            EmpresaId = referencia.EmpresaId,
            SucursalId = referencia.SucursalId,
            ModuloId = modulo.Id,
            Entidad = EntidadMarcador,
            EntidadId = referencia.EmpresaId.ToString(),
            Accion = AccionMarcador,
            Fecha = ahora,
            UsuarioId = referencia.UsuarioId,
            Comentario = "Permisos y referencias mínimas de productos e inventario preparados para Fase 4."
        };
        contexto.Bitacoras.Add(marcador);
        await contexto.SaveChangesAsync(token);
        creados.Add(new("Bitacoras", marcador.Id.ToString(), marcador.Accion));
        if (transaccion is not null)
            await transaccion.CommitAsync(token);
        return new(creados);
    }

    private async Task ValidarNoEjecutadoAsync(CancellationToken token)
    {
        var modulo = await contexto.Modulos.AnyAsync(item => item.Id == ModuloId, token);
        var permisos = await contexto.Permisos.CountAsync(item => CodigosPermisos.Contains(item.Codigo), token);
        var asignaciones = await contexto.RolesPermisos.CountAsync(item =>
            item.RoleId == RolAdministrador && CodigosPermisos.Contains(item.Permiso.Codigo), token);
        var marcador = await contexto.Bitacoras.AnyAsync(item => item.Entidad == EntidadMarcador && item.Accion == AccionMarcador, token);
        if (modulo || permisos > 0 || asignaciones > 0 || marcador)
        {
            var completa = modulo
                && permisos == CodigosPermisos.Length
                && asignaciones == CodigosPermisos.Length
                && marcador;
            throw new BootstrapAdministradorRechazadoException(
                completa
                    ? "La preparación de la Fase 4 ya fue completada."
                    : "Se detectó una preparación parcial de la Fase 4; revise los registros antes de continuar.");
        }
    }

    private async Task<ClasesCatalogos> ObtenerOCrearClaseCatalogoAsync(string codigo, string nombre, int usuarioId, int estadoId, DateTime ahora, List<RegistroBootstrapCreado> creados, CancellationToken token)
    {
        var entidad = await contexto.ClasesCatalogos.SingleOrDefaultAsync(item => item.Codigo == codigo, token);
        if (entidad is not null)
            return entidad;
        entidad = new ClasesCatalogos { Codigo = codigo, Nombre = nombre, EstadoId = estadoId, FechaCreacion = ahora, CreadoPorUsuarioId = usuarioId };
        contexto.ClasesCatalogos.Add(entidad);
        await contexto.SaveChangesAsync(token);
        creados.Add(new("ClasesCatalogos", entidad.Id.ToString(), entidad.Codigo));
        return entidad;
    }

    private async Task<Catalogos> ObtenerOCrearCatalogoAsync(int claseId, int empresaId, string codigo, string nombre, int usuarioId, int estadoId, DateTime ahora, List<RegistroBootstrapCreado> creados, CancellationToken token)
    {
        var entidad = await contexto.Catalogos.SingleOrDefaultAsync(item => item.ClaseCatalogoId == claseId && item.EmpresaId == empresaId && item.Codigo == codigo, token);
        if (entidad is not null)
            return entidad;
        entidad = new Catalogos { ClaseCatalogoId = claseId, EmpresaId = empresaId, Codigo = codigo, Nombre = nombre, EstadoId = estadoId, FechaCreacion = ahora, CreadoPorUsuarioId = usuarioId };
        contexto.Catalogos.Add(entidad);
        await contexto.SaveChangesAsync(token);
        creados.Add(new("Catalogos", entidad.Id.ToString(), entidad.Codigo));
        return entidad;
    }

    private async Task<FlujosEstados> ObtenerOCrearFlujoAsync(int usuarioId, int estadoId, DateTime ahora, List<RegistroBootstrapCreado> creados, CancellationToken token)
    {
        var entidad = await contexto.FlujosEstados.SingleOrDefaultAsync(item => item.Codigo == FlujoMovimientosCodigo, token);
        if (entidad is not null)
            return entidad;
        entidad = new FlujosEstados { Codigo = FlujoMovimientosCodigo, Nombre = "Movimientos de inventario", EstadoId = estadoId, FechaCreacion = ahora, CreadoPorUsuarioId = usuarioId };
        contexto.FlujosEstados.Add(entidad);
        await contexto.SaveChangesAsync(token);
        creados.Add(new("FlujosEstados", entidad.Id.ToString(), entidad.Codigo));
        return entidad;
    }

    private async Task ObtenerOCrearEstadoFlujoAsync(int flujoId, int usuarioId, int estadoId, DateTime ahora, List<RegistroBootstrapCreado> creados, CancellationToken token)
    {
        if (await contexto.EstadosFlujo.AnyAsync(item => item.FlujoEstadoId == flujoId && item.Codigo == "APLICADO", token))
            return;
        var entidad = new EstadosFlujo { FlujoEstadoId = flujoId, Codigo = "APLICADO", Nombre = "Aplicado", Orden = 1, EsInicial = true, EsFinal = true, PermiteEdicion = false, RequiereMotivo = false, EsSistema = true, EstadoId = estadoId, FechaCreacion = ahora, CreadoPorUsuarioId = usuarioId, VersionFila = [] };
        contexto.EstadosFlujo.Add(entidad);
        await contexto.SaveChangesAsync(token);
        creados.Add(new("EstadosFlujo", entidad.Id.ToString(), entidad.Codigo));
    }

    private async Task ObtenerOCrearConceptoAsync(ReferenciaAdministrador referencia, string codigo, string nombre, string tipo, int estadoId, DateTime ahora, List<RegistroBootstrapCreado> creados, CancellationToken token)
    {
        if (await contexto.ConceptosMovimientos.AnyAsync(item => item.EmpresaId == referencia.EmpresaId && item.Codigo == codigo, token))
            return;
        var entidad = new ConceptosMovimientos { EmpresaId = referencia.EmpresaId, Codigo = codigo, Nombre = nombre, TipoOperacion = tipo, RequiereReferencia = false, AfectaCosto = tipo == "ENTRADA", PermiteCostoCero = true, EsAutomatico = false, EstadoId = estadoId, FechaCreacion = ahora, CreadoPorUsuarioId = referencia.UsuarioId, VersionFila = [] };
        contexto.ConceptosMovimientos.Add(entidad);
        await contexto.SaveChangesAsync(token);
        creados.Add(new("ConceptosMovimientos", entidad.Id.ToString(), entidad.Codigo));
    }

    private async Task<IDbContextTransaction?> IniciarTransaccionAsync(CancellationToken token) =>
        contexto.Database.IsRelational()
            ? await contexto.Database.BeginTransactionAsync(IsolationLevel.Serializable, token)
            : null;

    private async Task TomarBloqueoAsync(CancellationToken token)
    {
        if (!contexto.Database.IsSqlServer())
            return;
        await contexto.Database.ExecuteSqlRawAsync(
            """
            DECLARE @resultado int;
            EXEC @resultado = sys.sp_getapplock
                @Resource = N'Heladeria.Bootstrap.Fase4',
                @LockMode = N'Exclusive',
                @LockOwner = N'Transaction',
                @LockTimeout = 10000;
            IF @resultado < 0
                THROW 51003, 'No fue posible reservar la preparación de Fase 4.', 1;
            """,
            token);
    }

    private sealed record ReferenciaAdministrador(int UsuarioId, int EmpresaId, long SucursalId);
    private sealed record DefinicionPermiso(string Codigo, int AccionId, string Recurso);
}
