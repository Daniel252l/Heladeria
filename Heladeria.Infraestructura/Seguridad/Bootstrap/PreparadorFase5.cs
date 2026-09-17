using Heladeria.Aplicacion.Compras;
using Heladeria.Aplicacion.Configuracion;
using Heladeria.Aplicacion.Seguridad.Autorizacion;
using Heladeria.Aplicacion.Seguridad.Bootstrap;
using Heladeria.Aplicacion.Seguridad.Configuracion;
using Heladeria.Infraestructura.Compras;
using Heladeria.Infraestructura.Inventario;
using Heladeria.Infraestructura.Persistencia;
using Heladeria.Infraestructura.Persistencia.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Heladeria.Infraestructura.Seguridad.Bootstrap;

public sealed class PreparadorFase5(HeladeriaDbContext contexto, IOptions<OpcionesSeguridad> opciones,
    TimeProvider reloj, CoordinadorAjustesInventario coordinador)
    : ServicioComprasBase(contexto, opciones, reloj, coordinador), IPreparadorFase5
{
    public const string AccionMarcador = "FASE5_CONFIGURADA";

    public async Task<ResultadoBootstrapAdministrador> EjecutarAsync(CancellationToken token)
    {
        // ADMIN solo identifica al destinatario del preparador; endpoints autorizan por permisos efectivos.
        var referencia = await Contexto.UsuariosSucursales.AsNoTracking().Where(item => item.EsPrincipal
            && item.Usuario.RoleId == "ADMIN" && item.Usuario.Estado.Activo && Operativos.Contains(item.Usuario.Estado.Nombre)
            && item.Estado.Activo && Operativos.Contains(item.Estado.Nombre) && item.Sucursal.Estado.Activo
            && Operativos.Contains(item.Sucursal.Estado.Nombre) && item.Sucursal.Empresa.Estado.Activo
            && Operativos.Contains(item.Sucursal.Empresa.Estado.Nombre))
            .OrderBy(item => item.UsuarioId).Select(item => new ContextoConfiguracion(item.UsuarioId, "ADMIN",
                item.Sucursal.EmpresaId, item.SucursalId, null, null)).FirstOrDefaultAsync(token)
            ?? throw new BootstrapAdministradorRechazadoException("No existe administrador con asignación principal operativa.");
        try
        {
            return await EscribirAsync(referencia, async () =>
            {
                if (Contexto.Database.IsSqlServer())
                    await Contexto.Database.ExecuteSqlRawAsync("""
                        DECLARE @resultado int;
                        EXEC @resultado = sys.sp_getapplock @Resource=N'Heladeria.Bootstrap.Fase5',
                            @LockMode=N'Exclusive', @LockOwner=N'Transaction', @LockTimeout=10000;
                        IF @resultado<0 THROW 51002, 'Preparación no disponible.', 1;
                        """, token);
                var seguridad = await Contexto.Permisos.AsNoTracking().Where(item => item.Codigo == PermisosSistema.SeguridadAdministrar
                    && item.Estado.Activo && Operativos.Contains(item.Estado.Nombre) && item.Accion.Codigo == "ADMINISTRAR")
                    .Select(item => new { item.EstadoId }).SingleOrDefaultAsync(token)
                    ?? throw new ConflictoComprasException("Falta el permiso operativo de seguridad del bootstrap.");
                if (!await Contexto.EstadosFlujo.AnyAsync(item => item.FlujoEstado.Codigo == PreparadorFase4.FlujoMovimientosCodigo
                    && item.Codigo == "APLICADO" && item.Estado.Activo && Operativos.Contains(item.Estado.Nombre), token))
                    throw new ConflictoComprasException("Debe preparar primero el inventario de Fase 4.");
                var estadoId = seguridad.EstadoId;
                var registros = new List<RegistroBootstrapCreado>();
                async Task<T> Asegurar<T>(DbSet<T> tabla, Func<Task<T?>> buscar, Func<T> crear, Func<T, bool> compatible,
                    string nombre, Func<T, string> identidad) where T : class
                {
                    var entidad = await buscar();
                    if (entidad is not null)
                    {
                        if (!compatible(entidad)) throw new ConflictoComprasException($"{nombre} existente incompatible con la preparación; no se sobrescribirá.");
                        return entidad;
                    }
                    entidad = crear(); tabla.Add(entidad); await Contexto.SaveChangesAsync(token);
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
                        _ => "Referencia mínima Fase 5"
                    };
                    registros.Add(new(nombre, identidad(entidad), descripcion));
                    return entidad;
                }
                foreach (var (codigo, nombre) in new[] { ("COMPRAS", "Compras"), ("PROVEEDORES", "Proveedores") })
                    await Asegurar(Contexto.Modulos, () => Contexto.Modulos.SingleOrDefaultAsync(item => item.Id == codigo, token),
                        () => new Modulos { Id = codigo, Nombre = nombre, Icono = codigo == "COMPRAS" ? "shopping_cart" : "local_shipping", Activo = true }, item => item.Activo,
                        "Modulos", item => item.Id);
                var acciones = new Dictionary<string, int>();
                foreach (var codigo in new[] { "CONSULTAR", "ADMINISTRAR", "RECIBIR" })
                {
                    var accion = await Asegurar(Contexto.AccionesSistema,
                        () => Contexto.AccionesSistema.SingleOrDefaultAsync(item => item.Codigo == codigo, token),
                        () => new AccionesSistema { Codigo = codigo, Nombre = codigo, EstadoId = estadoId },
                        item => item.EstadoId == estadoId, "AccionesSistema", item => item.Id.ToString());
                    acciones[codigo] = accion.Id;
                }
                foreach (var (codigo, modulo, accion, recurso) in new[]
                {
                    (PermisosSistema.ComprasConsultar, "COMPRAS", "CONSULTAR", "Compras"),
                    (PermisosSistema.ComprasAdministrar, "COMPRAS", "ADMINISTRAR", "Compras"),
                    (PermisosSistema.ComprasRecibir, "COMPRAS", "RECIBIR", "Compras"),
                    (PermisosSistema.ProveedoresConsultar, "PROVEEDORES", "CONSULTAR", "Proveedores"),
                    (PermisosSistema.ProveedoresAdministrar, "PROVEEDORES", "ADMINISTRAR", "Proveedores")
                })
                {
                    var permiso = await Asegurar(Contexto.Permisos,
                        () => Contexto.Permisos.SingleOrDefaultAsync(item => item.Codigo == codigo, token),
                        () => new Permisos { ModuloId = modulo, AccionId = acciones[accion], Recurso = recurso, Codigo = codigo, EstadoId = estadoId },
                        item => item.ModuloId == modulo && item.AccionId == acciones[accion] && item.Recurso == recurso && item.EstadoId == estadoId,
                        "Permisos", item => item.Id.ToString());
                    await Asegurar(Contexto.RolesPermisos,
                        () => Contexto.RolesPermisos.SingleOrDefaultAsync(item => item.RoleId == "ADMIN" && item.PermisoId == permiso.Id, token),
                        () => new RolesPermisos { RoleId = "ADMIN", PermisoId = permiso.Id, Permitido = true }, item => item.Permitido,
                        "RolesPermisos", item => item.Id.ToString());
                }
                foreach (var (codigo, nombre) in new[] { (ServicioCompras.FlujoOrden, "Órdenes de compra"), (ServicioCompras.FlujoRecepcion, "Recepciones de compra") })
                {
                    var flujo = await Asegurar(Contexto.FlujosEstados,
                        () => Contexto.FlujosEstados.SingleOrDefaultAsync(item => item.Codigo == codigo, token),
                        () => new FlujosEstados { Codigo = codigo, Nombre = nombre, EstadoId = estadoId, FechaCreacion = Ahora, CreadoPorUsuarioId = referencia.UsuarioId },
                        item => item.EstadoId == estadoId, "FlujosEstados", item => item.Id.ToString());
                    var definiciones = codigo == ServicioCompras.FlujoOrden
                        ? new[] { ("BORRADOR", "Borrador", true, false, true), ("CONFIRMADA", "Confirmada", false, false, false),
                            ("PARCIALMENTE_RECIBIDA", "Parcialmente recibida", false, false, false), ("RECIBIDA", "Recibida", false, true, false),
                            ("ANULADA", "Anulada", false, true, false) }
                        : new[] { ("APLICADA", "Aplicada", true, true, false) };
                    var estados = new Dictionary<string, int>();
                    var orden = 0;
                    foreach (var (clave, titulo, inicial, final, editable) in definiciones)
                    {
                        var posicion = (short)++orden;
                        var estado = await Asegurar(Contexto.EstadosFlujo,
                            () => Contexto.EstadosFlujo.SingleOrDefaultAsync(item => item.FlujoEstadoId == flujo.Id && item.Codigo == clave, token),
                            () => new EstadosFlujo { FlujoEstadoId = flujo.Id, Codigo = clave, Nombre = titulo, Orden = posicion,
                                EsInicial = inicial, EsFinal = final, PermiteEdicion = editable, RequiereMotivo = false, EsSistema = true,
                                EstadoId = estadoId, FechaCreacion = Ahora, CreadoPorUsuarioId = referencia.UsuarioId, VersionFila = [] },
                            item => item.EstadoId == estadoId && item.EsInicial == inicial && item.EsFinal == final
                                && item.PermiteEdicion == editable && !item.RequiereMotivo && item.EsSistema,
                            "EstadosFlujo", item => item.Id.ToString());
                        estados[clave] = estado.Id;
                    }
                    if (await Contexto.EstadosFlujo.AnyAsync(item => item.FlujoEstadoId == flujo.Id && !estados.Keys.Contains(item.Codigo), token))
                        throw new ConflictoComprasException("El flujo contiene estados personalizados; revise su compatibilidad antes de prepararlo.");
                    if (codigo == ServicioCompras.FlujoOrden)
                        foreach (var (desde, hasta) in new[] { ("BORRADOR", "CONFIRMADA"), ("BORRADOR", "ANULADA"),
                            ("CONFIRMADA", "ANULADA"), ("CONFIRMADA", "PARCIALMENTE_RECIBIDA"), ("CONFIRMADA", "RECIBIDA"),
                            ("PARCIALMENTE_RECIBIDA", "RECIBIDA") })
                        {
                            var permiso = hasta is "RECIBIDA" or "PARCIALMENTE_RECIBIDA" ? PermisosSistema.ComprasRecibir : PermisosSistema.ComprasAdministrar;
                            await Asegurar(Contexto.EstadoTransiciones,
                                () => Contexto.EstadoTransiciones.SingleOrDefaultAsync(item => item.FlujoEstadoId == flujo.Id
                                    && item.EstadoOrigenId == estados[desde] && item.EstadoDestinoId == estados[hasta], token),
                                () => new EstadoTransiciones { FlujoEstadoId = flujo.Id, EstadoOrigenId = estados[desde], EstadoDestinoId = estados[hasta],
                                    RequiereAutorizacion = true, RequiereComentario = false, PermisoCodigo = permiso, EstadoId = estadoId,
                                    FechaCreacion = Ahora, CreadoPorUsuarioId = referencia.UsuarioId, VersionFila = [] },
                                item => item.EstadoId == estadoId && item.RequiereAutorizacion && !item.RequiereComentario && item.PermisoCodigo == permiso,
                                "EstadoTransiciones", item => item.Id.ToString());
                        }
                }
                await Asegurar(Contexto.ConceptosMovimientos,
                    () => Contexto.ConceptosMovimientos.SingleOrDefaultAsync(item => item.EmpresaId == referencia.EmpresaId && item.Codigo == "RECEPCION-COMPRA", token),
                    () => new ConceptosMovimientos { EmpresaId = referencia.EmpresaId, Codigo = "RECEPCION-COMPRA", Nombre = "Recepción de compra",
                        TipoOperacion = "ENTRADA", RequiereReferencia = true, AfectaCosto = true, PermiteCostoCero = true, EsAutomatico = true,
                        EstadoId = estadoId, FechaCreacion = Ahora, CreadoPorUsuarioId = referencia.UsuarioId, VersionFila = [] },
                    item => item.EstadoId == estadoId && item.TipoOperacion == "ENTRADA" && item.RequiereReferencia && item.AfectaCosto
                        && item.PermiteCostoCero && item.EsAutomatico, "ConceptosMovimientos", item => item.Id.ToString());
                var clase = await Asegurar(Contexto.ClaseTipos,
                    () => Contexto.ClaseTipos.SingleOrDefaultAsync(item => item.Nombre == "DOCUMENTOS_COMPRA", token),
                    () => new ClaseTipos { Id = (Contexto.ClaseTipos.Select(item => (int?)item.Id).Max() ?? 0) + 1, Nombre = "DOCUMENTOS_COMPRA", EstadoId = estadoId },
                    item => item.EstadoId == estadoId, "ClaseTipos", item => item.Id.ToString());
                foreach (var (serie, nombre) in new[] { ("OC", "Orden de compra"), ("RC", "Recepción de compra"), ("IC", "Entrada de inventario por compra") })
                {
                    if (await Contexto.SeriesDocumentos.CountAsync(item => item.EmpresaId == referencia.EmpresaId
                        && item.SucursalId == null && item.Serie == serie, token) > 1)
                        throw new ConflictoComprasException("Hay varias series empresariales con el mismo código; revise la numeración antes de preparar compras.");
                    var tipo = await Asegurar(Contexto.Tipos,
                        () => Contexto.Tipos.SingleOrDefaultAsync(item => item.ClaseTipoId == clase.Id && item.Nombre == nombre, token),
                        () => new Tipos { Id = (Contexto.Tipos.Select(item => (int?)item.Id).Max() ?? 0) + 1, ClaseTipoId = clase.Id,
                            Nombre = nombre, ModuloId = "COMPRAS", EstadoId = estadoId },
                        item => item.EstadoId == estadoId && item.ModuloId == "COMPRAS", "Tipos", item => item.Id.ToString());
                    var documento = await Asegurar(Contexto.SeriesDocumentos,
                        () => Contexto.SeriesDocumentos.SingleOrDefaultAsync(item => item.EmpresaId == referencia.EmpresaId && item.SucursalId == null && item.Serie == serie, token),
                        () => new SeriesDocumentos { EmpresaId = referencia.EmpresaId, TipoDocumentoId = tipo.Id, Serie = serie,
                            Prefijo = serie + "-", Longitud = 10, EstadoId = estadoId },
                        item => item.EstadoId == estadoId && item.TipoDocumentoId == tipo.Id && item.Prefijo == serie + "-" && item.Longitud == 10,
                        "SeriesDocumentos", item => item.Id.ToString());
                    if (!await Contexto.CorrelativosDocumentos.AnyAsync(item => item.SerieDocumentoId == documento.Id, token))
                    {
                        var existenDocumentos = serie switch
                        {
                            "OC" => await Contexto.OrdenesCompra.AnyAsync(item => item.EmpresaId == referencia.EmpresaId && item.NumeroOrden.StartsWith("OC-"), token),
                            "RC" => await Contexto.RecepcionesCompra.AnyAsync(item => item.EmpresaId == referencia.EmpresaId && item.NumeroRecepcion.StartsWith("RC-"), token),
                            _ => await Contexto.Movimientos.AnyAsync(item => item.EmpresaId == referencia.EmpresaId && item.SerieDocumento == "IC", token)
                        };
                        if (existenDocumentos) throw new ConflictoComprasException("Hay documentos existentes sin correlativo; concilie la serie antes de prepararla.");
                    }
                    await Asegurar(Contexto.CorrelativosDocumentos,
                        () => Contexto.CorrelativosDocumentos.SingleOrDefaultAsync(item => item.SerieDocumentoId == documento.Id, token),
                        () => new CorrelativosDocumentos { SerieDocumentoId = documento.Id, UltimoNumero = 0, FechaModificacion = Ahora, VersionFila = [] },
                        item => item.UltimoNumero >= 0, "CorrelativosDocumentos", item => item.Id.ToString());
                }
                if (!await Contexto.Bitacoras.AnyAsync(item => item.EmpresaId == referencia.EmpresaId && item.Accion == AccionMarcador, token))
                {
                    Auditar(referencia, "Instalacion", referencia.EmpresaId, AccionMarcador, "Permisos, flujos, concepto y series de compras preparados explícitamente.");
                    await Contexto.SaveChangesAsync(token);
                    var marcador = await Contexto.Bitacoras.SingleAsync(item => item.EmpresaId == referencia.EmpresaId && item.Accion == AccionMarcador, token);
                    registros.Add(new("Bitacoras", marcador.Id.ToString(), AccionMarcador));
                }
                return new ResultadoBootstrapAdministrador(registros);
            }, token);
        }
        catch (ExcepcionCompras error) { throw new BootstrapAdministradorRechazadoException(error.Message); }
    }
}
