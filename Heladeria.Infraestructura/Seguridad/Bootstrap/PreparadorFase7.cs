using Heladeria.Aplicacion.Configuracion;
using Heladeria.Aplicacion.Ventas;
using Heladeria.Aplicacion.Seguridad.Autorizacion;
using Heladeria.Aplicacion.Seguridad.Bootstrap;
using Heladeria.Aplicacion.Seguridad.Configuracion;
using Heladeria.Infraestructura.Inventario;
using Heladeria.Infraestructura.Persistencia;
using Heladeria.Infraestructura.Persistencia.Entidades;
using Heladeria.Infraestructura.VentasPos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Heladeria.Infraestructura.Seguridad.Bootstrap;

public sealed class PreparadorFase7(
    HeladeriaDbContext contexto,
    IOptions<OpcionesSeguridad> opciones,
    TimeProvider reloj,
    CoordinadorAjustesInventario coordinador,
    ServicioInventario inventario)
    : ServicioVentasBase(contexto, opciones, reloj, coordinador, inventario), IPreparadorFase7
{
    public const string AccionMarcador = "FASE7_CONFIGURADA";

    public async Task<ResultadoBootstrapAdministrador> EjecutarAsync(CancellationToken token)
    {
        var referencia = await Contexto.UsuariosSucursales.AsNoTracking()
            .Where(item => item.EsPrincipal && item.Usuario.RoleId == "ADMIN"
                && item.Usuario.Estado.Activo && Operativos.Contains(item.Usuario.Estado.Nombre)
                && item.Estado.Activo && Operativos.Contains(item.Estado.Nombre)
                && item.Sucursal.Estado.Activo && Operativos.Contains(item.Sucursal.Estado.Nombre)
                && item.Sucursal.Empresa.Estado.Activo && Operativos.Contains(item.Sucursal.Empresa.Estado.Nombre))
            .OrderBy(item => item.UsuarioId)
            .Select(item => new ContextoConfiguracion(item.UsuarioId, "ADMIN",
                item.Sucursal.EmpresaId, item.SucursalId, null, null))
            .FirstOrDefaultAsync(token)
            ?? throw new BootstrapAdministradorRechazadoException(
                "No existe administrador con asignación principal operativa.");

        try
        {
            return await EscribirAsync(referencia, async () =>
            {
                if (Contexto.Database.IsSqlServer())
                {
                    await Contexto.Database.ExecuteSqlRawAsync("""
                        IF EXISTS (SELECT 1 FROM (VALUES
                            (N'Ventas',N'VersionFila'),(N'Ventas',N'ClaveOperacion'),
                            (N'Ventas',N'BodegaId'),(N'Ventas',N'UbicacionId'),
                            (N'Ventas',N'FechaModificacion'),(N'Ventas',N'ModificadoPorUsuarioId'),
                            (N'Ventas',N'FechaCompletada'),(N'Ventas',N'CompletadoPorUsuarioId'),
                            (N'Ventas',N'FechaAnulacion'),(N'Ventas',N'AnuladoPorUsuarioId'),(N'Ventas',N'MotivoAnulacion'),
                            (N'VentasDetalle',N'FactorConversion'),(N'VentasDetalle',N'CantidadBase'),
                            (N'VentasDetalle',N'UnidadMedidaId'),(N'VentasDetalle',N'UnidadBaseId'),
                            (N'VentasDetalle',N'ManejaExistencia'),(N'VentasDetalle',N'SubtotalNeto'),
                            (N'VentasDetalle',N'DescuentoPrecioCatalogo'),(N'VentasDetalle',N'TotalLinea'),
                            (N'VentasDetalle',N'CostoTotal'),(N'VentasDetalle',N'FuentePrecio')
                        ) columnas(tabla,columna) WHERE COL_LENGTH(N'dbo.'+columnas.tabla,columnas.columna) IS NULL)
                           OR OBJECT_ID(N'dbo.VentaDetalleImpuestos', N'U') IS NULL
                           OR NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID(N'dbo.Ventas')
                               AND name=N'UX_Ventas_ClaveOperacion' AND is_unique=1 AND is_disabled=0)
                            THROW 51070, 'Ejecute primero el script SQL controlado de Fase 7.', 1;
                        DECLARE @resultado int;
                        EXEC @resultado = sys.sp_getapplock @Resource=N'Heladeria.Bootstrap.Fase7',
                            @LockMode=N'Exclusive', @LockOwner=N'Transaction', @LockTimeout=10000;
                        IF @resultado<0 THROW 51002, 'Preparación no disponible.', 1;
                        """, token);
                }

                var seguridad = await Contexto.Permisos.AsNoTracking()
                    .Where(item => item.Codigo == PermisosSistema.SeguridadAdministrar
                        && item.Estado.Activo && Operativos.Contains(item.Estado.Nombre)
                        && item.Accion.Codigo == "ADMINISTRAR")
                    .Select(item => new { item.EstadoId }).SingleOrDefaultAsync(token)
                    ?? throw new ConflictoVentasException(
                        "Falta el permiso operativo de seguridad del bootstrap.");
                if (!await Contexto.EstadosFlujo.AnyAsync(item =>
                    item.FlujoEstado.Codigo == PreparadorFase4.FlujoMovimientosCodigo
                    && item.Codigo == "APLICADO" && item.Estado.Activo
                    && Operativos.Contains(item.Estado.Nombre), token))
                    throw new ConflictoVentasException(
                        "Debe preparar primero productos e inventario de Fase 4.");

                var estadoId = seguridad.EstadoId;
                var registros = new List<RegistroBootstrapCreado>();
                async Task<T> Asegurar<T>(
                    DbSet<T> tabla, Func<Task<T?>> buscar, Func<T> crear,
                    Func<T, bool> compatible, string nombre, Func<T, string> identidad)
                    where T : class
                {
                    var entidad = await buscar();
                    if (entidad is not null)
                    {
                        if (!compatible(entidad))
                            throw new ConflictoVentasException(
                                $"{nombre} existente incompatible con Fase 7; no se sobrescribirá.");
                        return entidad;
                    }
                    entidad = crear();
                    tabla.Add(entidad);
                    await Contexto.SaveChangesAsync(token);
                    var descripcion = entidad switch
                    {
                        Modulos item => item.Nombre,
                        AccionesSistema item => item.Codigo,
                        Permisos item => item.Codigo,
                        RolesPermisos item => $"ADMIN -> permiso {item.PermisoId}",
                        FlujosEstados item => item.Codigo,
                        EstadosFlujo item => item.Codigo,
                        EstadoTransiciones item => $"{item.EstadoOrigenId} -> {item.EstadoDestinoId}: {item.PermisoCodigo}",
                        ConceptosMovimientos item => item.Codigo,
                        ClaseTipos item => item.Nombre,
                        Tipos item => item.Nombre,
                        SeriesDocumentos item => item.Serie,
                        CorrelativosDocumentos item => $"Serie {item.SerieDocumentoId}; último número {item.UltimoNumero}",
                        _ => "Referencia mínima Fase 7"
                    };
                    registros.Add(new(nombre, identidad(entidad), descripcion));
                    return entidad;
                }

                foreach (var (codigo, nombre, icono) in new[]
                {
                    ("VENTAS", "Ventas", "point_of_sale"),
                    ("POS", "Punto de venta", "shopping_cart")
                })
                    await Asegurar(Contexto.Modulos,
                        () => Contexto.Modulos.SingleOrDefaultAsync(item => item.Id == codigo, token),
                        () => new Modulos { Id = codigo, Nombre = nombre, Icono = icono, Activo = true },
                        item => item.Activo, "Modulos", item => item.Id);

                var acciones = new Dictionary<string, int>();
                foreach (var codigo in new[] { "CONSULTAR", "ADMINISTRAR", "VENDER", "ANULAR", "OPERAR" })
                {
                    var accion = await Asegurar(Contexto.AccionesSistema,
                        () => Contexto.AccionesSistema.SingleOrDefaultAsync(item => item.Codigo == codigo, token),
                        () => new AccionesSistema { Codigo = codigo, Nombre = codigo, EstadoId = estadoId },
                        item => item.EstadoId == estadoId,
                        "AccionesSistema", item => item.Id.ToString());
                    acciones[codigo] = accion.Id;
                }

                foreach (var (codigo, modulo, accion, recurso) in new[]
                {
                    (PermisosSistema.VentasConsultar, "VENTAS", "CONSULTAR", "Ventas"),
                    (PermisosSistema.VentasAdministrar, "VENTAS", "ADMINISTRAR", "Ventas"),
                    (PermisosSistema.VentasVender, "VENTAS", "VENDER", "Ventas"),
                    (PermisosSistema.VentasAnular, "VENTAS", "ANULAR", "Ventas"),
                    (PermisosSistema.PosOperar, "POS", "OPERAR", "Pos")
                })
                {
                    var permiso = await Asegurar(Contexto.Permisos,
                        () => Contexto.Permisos.SingleOrDefaultAsync(item => item.Codigo == codigo, token),
                        () => new Permisos
                        {
                            ModuloId = modulo, AccionId = acciones[accion], Recurso = recurso,
                            Codigo = codigo, EstadoId = estadoId
                        },
                        item => item.ModuloId == modulo && item.AccionId == acciones[accion]
                            && item.Recurso == recurso && item.EstadoId == estadoId,
                        "Permisos", item => item.Id.ToString());
                    await Asegurar(Contexto.RolesPermisos,
                        () => Contexto.RolesPermisos.SingleOrDefaultAsync(item =>
                            item.RoleId == "ADMIN" && item.PermisoId == permiso.Id, token),
                        () => new RolesPermisos { RoleId = "ADMIN", PermisoId = permiso.Id, Permitido = true },
                        item => item.Permitido,
                        "RolesPermisos", item => item.Id.ToString());
                }

                var flujo = await Asegurar(Contexto.FlujosEstados,
                    () => Contexto.FlujosEstados.SingleOrDefaultAsync(item => item.Codigo == FlujoVenta, token),
                    () => new FlujosEstados
                    {
                        Codigo = FlujoVenta,
                        Nombre = "Ventas POS", EstadoId = estadoId,
                        FechaCreacion = Ahora, CreadoPorUsuarioId = referencia.UsuarioId
                    },
                    item => item.EstadoId == estadoId,
                    "FlujosEstados", item => item.Id.ToString());
                var estados = new Dictionary<string, int>();
                var posicion = 0;
                foreach (var (codigo, nombre, inicial, final, editable) in new[]
                {
                    ("BORRADOR", "Borrador", true, false, true),
                    ("CONFIRMADA", "Confirmada", false, false, false),
                    ("COMPLETADA", "Completada", false, true, false),
                    ("ANULADA", "Anulada", false, true, false)
                })
                {
                    var orden = (short)++posicion;
                    var estado = await Asegurar(Contexto.EstadosFlujo,
                        () => Contexto.EstadosFlujo.SingleOrDefaultAsync(item =>
                            item.FlujoEstadoId == flujo.Id && item.Codigo == codigo, token),
                        () => new EstadosFlujo
                        {
                            FlujoEstadoId = flujo.Id, Codigo = codigo, Nombre = nombre,
                            Orden = orden, EsInicial = inicial, EsFinal = final,
                            PermiteEdicion = editable, RequiereMotivo = codigo == "ANULADA", EsSistema = true,
                            EstadoId = estadoId, FechaCreacion = Ahora,
                            CreadoPorUsuarioId = referencia.UsuarioId, VersionFila = []
                        },
                        item => item.EstadoId == estadoId && item.Orden == orden && item.EsInicial == inicial
                            && item.EsFinal == final && item.PermiteEdicion == editable
                            && item.RequiereMotivo == (codigo == "ANULADA") && item.EsSistema,
                        "EstadosFlujo", item => item.Id.ToString());
                    estados[codigo] = estado.Id;
                }
                if (await Contexto.EstadosFlujo.AnyAsync(item => item.FlujoEstadoId == flujo.Id
                    && !estados.Keys.Contains(item.Codigo), token))
                    throw new ConflictoVentasException(
                        "El flujo de ventas tiene estados personalizados; revise su compatibilidad.");

                foreach (var (desde, hasta, permiso) in new[]
                {
                    ("BORRADOR", "CONFIRMADA", PermisosSistema.VentasAdministrar),
                    ("BORRADOR", "ANULADA", PermisosSistema.VentasAnular),
                    ("CONFIRMADA", "COMPLETADA", PermisosSistema.VentasVender),
                    ("CONFIRMADA", "ANULADA", PermisosSistema.VentasAnular),
                    ("COMPLETADA", "ANULADA", PermisosSistema.VentasAnular)
                })
                    await Asegurar(Contexto.EstadoTransiciones,
                        () => Contexto.EstadoTransiciones.SingleOrDefaultAsync(item =>
                            item.FlujoEstadoId == flujo.Id && item.EstadoOrigenId == estados[desde]
                            && item.EstadoDestinoId == estados[hasta], token),
                        () => new EstadoTransiciones
                        {
                            FlujoEstadoId = flujo.Id, EstadoOrigenId = estados[desde],
                            EstadoDestinoId = estados[hasta], RequiereAutorizacion = true,
                            RequiereComentario = hasta == "ANULADA", PermisoCodigo = permiso,
                            EstadoId = estadoId, FechaCreacion = Ahora,
                            CreadoPorUsuarioId = referencia.UsuarioId, VersionFila = []
                        },
                        item => item.EstadoId == estadoId && item.RequiereAutorizacion
                            && item.RequiereComentario == (hasta == "ANULADA") && item.PermisoCodigo == permiso,
                        "EstadoTransiciones", item => item.Id.ToString());

                foreach (var (codigo, nombre, tipo, afectaCosto) in new[]
                {
                    ("VENTA-SALIDA", "Salida por venta", "SALIDA", false),
                    ("VENTA-ANULACION-ENTRADA", "Reversión de venta anulada", "ENTRADA", true)
                })
                    await Asegurar(Contexto.ConceptosMovimientos,
                        () => Contexto.ConceptosMovimientos.SingleOrDefaultAsync(item =>
                            item.EmpresaId == referencia.EmpresaId && item.Codigo == codigo, token),
                        () => new ConceptosMovimientos
                        {
                            EmpresaId = referencia.EmpresaId, Codigo = codigo, Nombre = nombre,
                            TipoOperacion = tipo, RequiereReferencia = true,
                            AfectaCosto = afectaCosto, PermiteCostoCero = true, EsAutomatico = true,
                            EstadoId = estadoId, FechaCreacion = Ahora,
                            CreadoPorUsuarioId = referencia.UsuarioId, VersionFila = []
                        },
                        item => item.EstadoId == estadoId && item.TipoOperacion == tipo
                            && item.RequiereReferencia && item.AfectaCosto == afectaCosto
                            && item.PermiteCostoCero && item.EsAutomatico,
                        "ConceptosMovimientos", item => item.Id.ToString());

                var clase = await Asegurar(Contexto.ClaseTipos,
                    () => Contexto.ClaseTipos.SingleOrDefaultAsync(item => item.Nombre == "DOCUMENTOS_VENTAS", token),
                    () => new ClaseTipos
                    {
                        Id = (Contexto.ClaseTipos.Select(item => (int?)item.Id).Max() ?? 0) + 1,
                        Nombre = "DOCUMENTOS_VENTAS", EstadoId = estadoId
                    },
                    item => item.EstadoId == estadoId,
                    "ClaseTipos", item => item.Id.ToString());
                foreach (var (serie, nombre) in new[]
                {
                    ("VT", "Venta POS"),
                    ("VS", "Salida por venta"),
                    ("VA", "Anulación de venta")
                })
                {
                    if (await Contexto.SeriesDocumentos.CountAsync(item =>
                        item.EmpresaId == referencia.EmpresaId && item.SucursalId == null
                        && item.Serie == serie, token) > 1)
                        throw new ConflictoVentasException(
                            "Hay varias series empresariales con el mismo código.");
                    var tipo = await Asegurar(Contexto.Tipos,
                        () => Contexto.Tipos.SingleOrDefaultAsync(item =>
                            item.ClaseTipoId == clase.Id && item.Nombre == nombre, token),
                        () => new Tipos
                        {
                            Id = (Contexto.Tipos.Select(item => (int?)item.Id).Max() ?? 0) + 1,
                            ClaseTipoId = clase.Id, Nombre = nombre,
                            ModuloId = "VENTAS", EstadoId = estadoId
                        },
                        item => item.EstadoId == estadoId && item.ModuloId == "VENTAS",
                        "Tipos", item => item.Id.ToString());
                    var documento = await Asegurar(Contexto.SeriesDocumentos,
                        () => Contexto.SeriesDocumentos.SingleOrDefaultAsync(item =>
                            item.EmpresaId == referencia.EmpresaId && item.SucursalId == null
                            && item.Serie == serie, token),
                        () => new SeriesDocumentos
                        {
                            EmpresaId = referencia.EmpresaId, TipoDocumentoId = tipo.Id,
                            Serie = serie, Prefijo = serie + "-", Longitud = 10, EstadoId = estadoId
                        },
                        item => item.EstadoId == estadoId && item.TipoDocumentoId == tipo.Id
                            && item.Prefijo == serie + "-" && item.Longitud == 10,
                        "SeriesDocumentos", item => item.Id.ToString());
                    if (!await Contexto.CorrelativosDocumentos.AnyAsync(item =>
                        item.SerieDocumentoId == documento.Id, token))
                    {
                        var existen = serie == "VT"
                            ? await Contexto.Ventas.AnyAsync(item =>
                                item.EmpresaId == referencia.EmpresaId && item.NumeroFactura.StartsWith("VT-"), token)
                            : await Contexto.Movimientos.AnyAsync(item =>
                                item.EmpresaId == referencia.EmpresaId && item.SerieDocumento == serie, token);
                        if (existen)
                            throw new ConflictoVentasException(
                                "Hay documentos existentes sin correlativo; concilie la serie antes de continuar.");
                    }
                    await Asegurar(Contexto.CorrelativosDocumentos,
                        () => Contexto.CorrelativosDocumentos.SingleOrDefaultAsync(item =>
                            item.SerieDocumentoId == documento.Id, token),
                        () => new CorrelativosDocumentos
                        {
                            SerieDocumentoId = documento.Id, UltimoNumero = 0,
                            FechaModificacion = Ahora, VersionFila = []
                        },
                        item => item.UltimoNumero >= 0,
                        "CorrelativosDocumentos", item => item.Id.ToString());
                }

                if (!await Contexto.Bitacoras.AnyAsync(item =>
                    item.EmpresaId == referencia.EmpresaId && item.Accion == AccionMarcador, token))
                {
                    Auditar(referencia, referencia.EmpresaId, AccionMarcador,
                        "Permisos, flujo, conceptos y series de ventas preparados explícitamente.");
                    await Contexto.SaveChangesAsync(token);
                    var marcador = await Contexto.Bitacoras.SingleAsync(item =>
                        item.EmpresaId == referencia.EmpresaId && item.Accion == AccionMarcador, token);
                    registros.Add(new("Bitacoras", marcador.Id.ToString(), AccionMarcador));
                }
                return new ResultadoBootstrapAdministrador(registros);
            }, token);
        }
        catch (ExcepcionVentas error)
        {
            throw new BootstrapAdministradorRechazadoException(error.Message);
        }
    }
}


