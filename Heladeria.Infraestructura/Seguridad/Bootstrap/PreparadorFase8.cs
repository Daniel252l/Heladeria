using Heladeria.Aplicacion.Caja;
using Heladeria.Aplicacion.Configuracion;
using Heladeria.Aplicacion.Seguridad.Bootstrap;
using Heladeria.Aplicacion.Seguridad.Configuracion;
using Heladeria.Infraestructura.Caja;
using Heladeria.Infraestructura.Inventario;
using Heladeria.Infraestructura.Persistencia;
using Heladeria.Infraestructura.Persistencia.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Heladeria.Infraestructura.Seguridad.Bootstrap;

public sealed class PreparadorFase8(HeladeriaDbContext contexto, IOptions<OpcionesSeguridad> opciones,
    TimeProvider reloj, CoordinadorAjustesInventario coordinador)
    : ServicioCajaBase(contexto, opciones, reloj, coordinador), IPreparadorFase8
{
    public const string AccionMarcador = "FASE8_CONFIGURADA";
    public async Task<ResultadoBootstrapAdministrador> EjecutarAsync(CancellationToken token)
    {
        var s = await Contexto.UsuariosSucursales.AsNoTracking().Where(x => x.EsPrincipal && x.Usuario.RoleId == "ADMIN"
            && x.Usuario.Estado.Activo && Operativos.Contains(x.Usuario.Estado.Nombre)
            && x.Estado.Activo && Operativos.Contains(x.Estado.Nombre)
            && x.Sucursal.Estado.Activo && Operativos.Contains(x.Sucursal.Estado.Nombre)
            && x.Sucursal.Empresa.Estado.Activo && Operativos.Contains(x.Sucursal.Empresa.Estado.Nombre))
            .OrderBy(x => x.UsuarioId).Select(x => new ContextoConfiguracion(x.UsuarioId, "ADMIN",
                x.Sucursal.EmpresaId, x.SucursalId, null, null)).FirstOrDefaultAsync(token)
            ?? throw new BootstrapAdministradorRechazadoException("No existe administrador principal operativo.");
        try
        {
            return await EscribirAsync(s, async () =>
            {
                if (Contexto.Database.IsSqlServer())
                    await Contexto.Database.ExecuteSqlRawAsync("""
                        IF EXISTS(SELECT 1 FROM (VALUES
                            (N'TurnosCaja',N'MonedaId'),(N'TurnosCaja',N'UsuarioCierreId'),(N'TurnosCaja',N'FechaModificacion'),
                            (N'TurnosCaja',N'NumeroApertura'),(N'TurnosCaja',N'NumeroCierre'),(N'TurnosCaja',N'TotalIngresos'),
                            (N'TurnosCaja',N'TotalEgresos'),(N'TurnosCaja',N'ArqueoCierreId'),(N'TurnosCaja',N'VersionFila'),
                            (N'Pagos',N'VentaId'),(N'Pagos',N'ClaveOperacion'),(N'PagoDetalles',N'MontoRecibido'),
                            (N'PagoDetalles',N'EsEfectivo'),(N'MovimientosCaja',N'PagoDetalleId'),(N'MovimientosCaja',N'CodigoConcepto'),
                            (N'MovimientosCaja',N'EsEfectivo'),(N'MovimientosCaja',N'NumeroDocumento'),(N'ArqueosCaja',N'VersionTurno')
                        ) c(tabla,columna) WHERE COL_LENGTH(N'dbo.'+c.tabla,c.columna) IS NULL)
                        OR EXISTS(SELECT 1 FROM (VALUES
                            (N'TurnosCaja',N'UX_TurnosCaja_CajaAbierta'),(N'Pagos',N'UX_Pagos_Venta'),
                            (N'Pagos',N'UX_Pagos_ClaveOperacion'),(N'MovimientosCaja',N'UX_MovimientosCaja_PagoDetalle')
                        ) requerido(tabla,nombre) WHERE NOT EXISTS(SELECT 1 FROM sys.indexes i
                            WHERE i.object_id=OBJECT_ID(N'dbo.'+requerido.tabla) AND i.name=requerido.nombre
                            AND i.is_unique=1 AND i.is_disabled=0 AND i.has_filter=1))
                        OR EXISTS(SELECT 1 FROM (VALUES
                            (N'TurnosCaja',N'CK_TurnosCaja_Snapshot_Fase8'),(N'PagoDetalles',N'CK_PagoDetalles_Snapshot_Fase8'),
                            (N'MovimientosCaja',N'CK_MovimientosCaja_Snapshot_Fase8'),(N'ArqueosCaja',N'CK_ArqueosCaja_Version_Fase8')
                        ) requerido(tabla,nombre) WHERE NOT EXISTS(SELECT 1 FROM sys.check_constraints c
                            WHERE c.parent_object_id=OBJECT_ID(N'dbo.'+requerido.tabla) AND c.name=requerido.nombre
                            AND c.is_disabled=0 AND c.is_not_trusted=0))
                        OR NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID(N'dbo.Pagos')
                            AND name=N'CK_Pagos_Tercero' AND definition LIKE N'%VentaId%' AND is_disabled=0 AND is_not_trusted=0)
                            THROW 51080, 'Aplique primero el script controlado Fase 8.', 1;
                        DECLARE @r int;
                        EXEC @r=sys.sp_getapplock @Resource=N'Heladeria.Bootstrap.Fase8',
                            @LockMode=N'Exclusive',@LockOwner=N'Transaction',@LockTimeout=10000;
                        IF @r<0 THROW 51002, 'Preparación no disponible.', 1;
                        """, token);
                var activo = await Contexto.Permisos.AsNoTracking().Where(x => x.Codigo == "SEGURIDAD.ADMINISTRAR"
                    && x.Estado.Activo && Operativos.Contains(x.Estado.Nombre) && x.Accion.Codigo == "ADMINISTRAR")
                    .Select(x => (int?)x.EstadoId).SingleOrDefaultAsync(token)
                    ?? throw new ExcepcionCaja("Falta la configuración administrativa inicial.");
                if (!await Contexto.EstadosFlujo.AnyAsync(x => x.FlujoEstado.Codigo == "VENTA_POS" && x.Codigo == "COMPLETADA"
                    && x.Estado.Activo && Operativos.Contains(x.Estado.Nombre), token))
                    throw new ExcepcionCaja("Prepare primero Ventas/POS Fase 7.");
                var registros = new List<RegistroBootstrapCreado>();
                async Task<T> Asegurar<T>(DbSet<T> tabla, Func<Task<T?>> buscar, Func<T> crear,
                    Func<T, bool> compatible, string nombre, Func<T, string> identidad, string descripcion) where T : class
                {
                    var entidad = await buscar();
                    if (entidad is not null)
                    {
                        if (!compatible(entidad)) throw new ExcepcionCaja($"{nombre}: {descripcion} existente incompatible; no se sobrescribirá.");
                        return entidad;
                    }
                    entidad = crear(); tabla.Add(entidad); await Contexto.SaveChangesAsync(token);
                    registros.Add(new(nombre, identidad(entidad), descripcion)); return entidad;
                }
                await Asegurar(Contexto.Modulos, () => Contexto.Modulos.SingleOrDefaultAsync(x => x.Id == "CAJA", token),
                    () => new Modulos { Id = "CAJA", Nombre = "Caja", Icono = "payments", Activo = true },
                    x => x.Activo, "Modulos", x => x.Id, "Caja");
                foreach (var codigo in new[] { "CONSULTAR", "ABRIR", "OPERAR", "CERRAR", "ANULAR", "MOVIMIENTO_MANUAL", "COBRAR" })
                {
                    var accion = await Asegurar(Contexto.AccionesSistema, () => Contexto.AccionesSistema.SingleOrDefaultAsync(x => x.Codigo == codigo, token),
                        () => new AccionesSistema { Codigo = codigo, Nombre = codigo, EstadoId = activo },
                        x => x.EstadoId == activo, "AccionesSistema", x => x.Id.ToString(), codigo);
                    var permiso = await Asegurar(Contexto.Permisos, () => Contexto.Permisos.SingleOrDefaultAsync(x => x.Codigo == "CAJA." + codigo, token),
                        () => new Permisos { ModuloId = "CAJA", AccionId = accion.Id, Recurso = "Caja", Codigo = "CAJA." + codigo, EstadoId = activo },
                        x => x.ModuloId == "CAJA" && x.AccionId == accion.Id && x.Recurso == "Caja" && x.EstadoId == activo,
                        "Permisos", x => x.Id.ToString(), "CAJA." + codigo);
                    await Asegurar(Contexto.RolesPermisos, () => Contexto.RolesPermisos.SingleOrDefaultAsync(x => x.RoleId == "ADMIN" && x.PermisoId == permiso.Id, token),
                        () => new RolesPermisos { RoleId = "ADMIN", PermisoId = permiso.Id, Permitido = true }, x => x.Permitido,
                        "RolesPermisos", x => x.Id.ToString(), "ADMIN → " + permiso.Codigo);
                }
                var flujo = await Asegurar(Contexto.FlujosEstados, () => Contexto.FlujosEstados.SingleOrDefaultAsync(x => x.Codigo == FlujoCaja, token),
                    () => new FlujosEstados { Codigo = FlujoCaja, Nombre = "Sesión de caja", EstadoId = activo, FechaCreacion = Ahora, CreadoPorUsuarioId = s.UsuarioId },
                    x => x.EstadoId == activo, "FlujosEstados", x => x.Id.ToString(), FlujoCaja);
                var estados = new Dictionary<string, int>();
                foreach (var codigo in new[] { "ABIERTA", "CERRADA" })
                {
                    var abierta = codigo == "ABIERTA";
                    var estado = await Asegurar(Contexto.EstadosFlujo, () => Contexto.EstadosFlujo.SingleOrDefaultAsync(x => x.FlujoEstadoId == flujo.Id && x.Codigo == codigo, token),
                        () => new EstadosFlujo { FlujoEstadoId = flujo.Id, Codigo = codigo, Nombre = abierta ? "Abierta" : "Cerrada",
                            Orden = (short)(abierta ? 1 : 2), EsInicial = abierta, EsFinal = !abierta, PermiteEdicion = abierta,
                            RequiereMotivo = false, EsSistema = true, EstadoId = activo, FechaCreacion = Ahora, CreadoPorUsuarioId = s.UsuarioId },
                        x => x.EstadoId == activo && x.EsInicial == abierta && x.EsFinal == !abierta && x.PermiteEdicion == abierta
                            && !x.RequiereMotivo && x.EsSistema && x.Orden == (abierta ? 1 : 2), "EstadosFlujo", x => x.Id.ToString(), codigo);
                    estados[codigo] = estado.Id;
                }
                if (await Contexto.EstadosFlujo.AnyAsync(x => x.FlujoEstadoId == flujo.Id && x.Codigo != "ABIERTA" && x.Codigo != "CERRADA", token))
                    throw new ExcepcionCaja("El flujo Caja tiene estados personalizados no compatibles.");
                await Asegurar(Contexto.EstadoTransiciones, () => Contexto.EstadoTransiciones.SingleOrDefaultAsync(x => x.FlujoEstadoId == flujo.Id
                        && x.EstadoOrigenId == estados["ABIERTA"] && x.EstadoDestinoId == estados["CERRADA"], token),
                    () => new EstadoTransiciones { FlujoEstadoId = flujo.Id, EstadoOrigenId = estados["ABIERTA"], EstadoDestinoId = estados["CERRADA"],
                        PermisoCodigo = "CAJA.CERRAR", RequiereAutorizacion = true, RequiereComentario = false, EstadoId = activo,
                        FechaCreacion = Ahora, CreadoPorUsuarioId = s.UsuarioId },
                    x => x.EstadoId == activo && x.PermisoCodigo == "CAJA.CERRAR" && x.RequiereAutorizacion && !x.RequiereComentario,
                    "EstadoTransiciones", x => x.Id.ToString(), "ABIERTA → CERRADA");
                if (await Contexto.EstadoTransiciones.AnyAsync(x => x.FlujoEstadoId == flujo.Id
                    && (x.EstadoOrigenId != estados["ABIERTA"] || x.EstadoDestinoId != estados["CERRADA"]), token))
                    throw new ExcepcionCaja("Caja tiene transiciones personalizadas no compatibles.");

                async Task<Tipos> AsegurarTipo(string claseNombre, string nombre)
                {
                    var clase = await Asegurar(Contexto.ClaseTipos, () => Contexto.ClaseTipos.SingleOrDefaultAsync(x => x.Nombre == claseNombre, token),
                        () => new ClaseTipos { Id = (Contexto.ClaseTipos.Select(x => (int?)x.Id).Max() ?? 0) + 1, Nombre = claseNombre, EstadoId = activo },
                        x => x.EstadoId == activo, "ClaseTipos", x => x.Id.ToString(), claseNombre);
                    return await Asegurar(Contexto.Tipos, () => Contexto.Tipos.SingleOrDefaultAsync(x => x.ClaseTipoId == clase.Id && x.Nombre == nombre, token),
                        () => new Tipos { Id = (Contexto.Tipos.Select(x => (int?)x.Id).Max() ?? 0) + 1, ClaseTipoId = clase.Id,
                            Nombre = nombre, ModuloId = "CAJA", EstadoId = activo }, x => x.EstadoId == activo && x.ModuloId == "CAJA",
                        "Tipos", x => x.Id.ToString(), nombre);
                }
                foreach (var concepto in new[] { "APERTURA_CAJA", "COBRO_VENTA", "INGRESO_CAJA", "EGRESO_CAJA" })
                    await AsegurarTipo("MOVIMIENTOS_CAJA", concepto);
                await AsegurarTipo("OPERACIONES_PAGO_CAJA", "COBRO_VENTA_POS");
                foreach (var (serie, nombre) in new[] { ("CA", "Apertura caja"), ("MC", "Movimiento caja"), ("CC", "Cierre caja") })
                {
                    if (await Contexto.SeriesDocumentos.CountAsync(x => x.EmpresaId == s.EmpresaId && x.SucursalId == null && x.Serie == serie, token) > 1)
                        throw new ExcepcionCaja("Series empresariales de Caja duplicadas; concilie la configuración.");
                    var tipo = await AsegurarTipo("DOCUMENTOS_CAJA", nombre);
                    var doc = await Asegurar(Contexto.SeriesDocumentos, () => Contexto.SeriesDocumentos.SingleOrDefaultAsync(x => x.EmpresaId == s.EmpresaId && x.SucursalId == null && x.Serie == serie, token),
                        () => new SeriesDocumentos { EmpresaId = s.EmpresaId, TipoDocumentoId = tipo.Id, Serie = serie, Prefijo = serie + "-", Longitud = 10, EstadoId = activo },
                        x => x.EstadoId == activo && x.TipoDocumentoId == tipo.Id && x.Prefijo == serie + "-" && x.Longitud == 10,
                        "SeriesDocumentos", x => x.Id.ToString(), serie);
                    if (!await Contexto.CorrelativosDocumentos.AnyAsync(x => x.SerieDocumentoId == doc.Id, token)
                        && (serie == "MC" ? await Contexto.MovimientosCaja.AnyAsync(x => x.TurnoCaja.Caja.EmpresaId == s.EmpresaId && x.NumeroDocumento != null, token)
                            : await Contexto.TurnosCaja.AnyAsync(x => x.Caja.EmpresaId == s.EmpresaId && (serie == "CA" ? x.NumeroApertura != null : x.NumeroCierre != null), token)))
                        throw new ExcepcionCaja("Existen documentos de caja sin correlativo; no se reiniciará la numeración.");
                    await Asegurar(Contexto.CorrelativosDocumentos, () => Contexto.CorrelativosDocumentos.SingleOrDefaultAsync(x => x.SerieDocumentoId == doc.Id, token),
                        () => new CorrelativosDocumentos { SerieDocumentoId = doc.Id, UltimoNumero = 0, FechaModificacion = Ahora },
                        x => x.UltimoNumero >= 0, "CorrelativosDocumentos", x => x.Id.ToString(), serie + ": contador inicial cero");
                }
                foreach (var codigo in new[] { "EFECTIVO", "TARJETA" })
                {
                    var efectivo = codigo == "EFECTIVO";
                    await Asegurar(Contexto.FormasPago, () => Contexto.FormasPago.SingleOrDefaultAsync(x => x.EmpresaId == s.EmpresaId && x.Codigo == codigo, token),
                        () => new FormasPago { EmpresaId = s.EmpresaId, Codigo = codigo, Nombre = efectivo ? "Efectivo" : "Tarjeta",
                            AfectaCaja = efectivo, PermiteCambio = efectivo, EsCredito = false, RequiereReferencia = false, EstadoId = activo },
                        x => x.EstadoId == activo && !x.EsCredito && x.AfectaCaja == efectivo && x.PermiteCambio == efectivo,
                        "FormasPago", x => x.Id.ToString(), codigo);
                }
                if (!await Contexto.Bitacoras.AnyAsync(x => x.EmpresaId == s.EmpresaId && x.Accion == AccionMarcador, token))
                {
                    Auditar(s, s.EmpresaId, AccionMarcador, "Configuración explícita de caja; sin cajas físicas, sesiones, cobros ni movimientos.", "Empresas");
                    await Contexto.SaveChangesAsync(token);
                    var marcador = await Contexto.Bitacoras.SingleAsync(x => x.EmpresaId == s.EmpresaId && x.Accion == AccionMarcador, token);
                    registros.Add(new("Bitacoras", marcador.Id.ToString(), AccionMarcador));
                }
                return new ResultadoBootstrapAdministrador(registros);
            }, token);
        }
        catch (ExcepcionCaja e) { throw new BootstrapAdministradorRechazadoException(e.Message); }
    }
}
