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

public sealed class PreparadorFase9(HeladeriaDbContext contexto, IOptions<OpcionesSeguridad> opciones,
    TimeProvider reloj, CoordinadorAjustesInventario coordinador)
    : ServicioCajaBase(contexto, opciones, reloj, coordinador), IPreparadorFase9
{
    public const string AccionMarcador = "FASE9_CONFIGURADA";
    public async Task<ResultadoBootstrapAdministrador> EjecutarAsync(CancellationToken token)
    {
        var s = await Contexto.UsuariosSucursales.AsNoTracking().Where(x => x.EsPrincipal && x.Usuario.RoleId == "ADMIN"
            && x.Usuario.Estado.Activo && Operativos.Contains(x.Usuario.Estado.Nombre)
            && x.Estado.Activo && Operativos.Contains(x.Estado.Nombre)
            && x.Sucursal.Estado.Activo && Operativos.Contains(x.Sucursal.Estado.Nombre)
            && x.Sucursal.Empresa.Estado.Activo && Operativos.Contains(x.Sucursal.Empresa.Estado.Nombre))
            .OrderBy(x => x.UsuarioId).Select(x => new ContextoConfiguracion(x.UsuarioId, "ADMIN", x.Sucursal.EmpresaId, x.SucursalId, null, null))
            .FirstOrDefaultAsync(token) ?? throw new BootstrapAdministradorRechazadoException("No existe administrador principal operativo.");
        try
        {
            return await EscribirAsync(s, async () => {
                if (Contexto.Database.IsSqlServer())
                    await Contexto.Database.ExecuteSqlRawAsync("""
                        IF COL_LENGTH('dbo.CuentasPorCobrar','VersionFila') IS NULL
                            OR COL_LENGTH('dbo.CuentasPorPagar','FechaCreacion') IS NULL
                            OR COL_LENGTH('dbo.Pagos','HuellaSolicitud') IS NULL
                            OR NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE name='CK_Pagos_Huella_Fase9' AND is_disabled=0 AND is_not_trusted=0)
                            OR NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name='UX_FacturasProveedores_ClaveOperacion' AND is_unique=1 AND is_disabled=0)
                            THROW 51090, 'Aplique primero el script controlado de Fase 9.', 1;
                        DECLARE @r int;
                        EXEC @r=sys.sp_getapplock @Resource=N'Heladeria.Bootstrap.Fase9', @LockMode=N'Exclusive',@LockOwner=N'Transaction',@LockTimeout=10000;
                        IF @r<0 THROW 51002, 'Preparacion ocupada.', 1;
                        """, token);
                var activo = await ActivoAsync(token);
                await EstadoAsync("ABIERTA", token);
                var registros = new List<RegistroBootstrapCreado>();
                async Task<T> Asegurar<T>(DbSet<T> tabla, Func<Task<T?>> buscar, Func<T> crear, Func<T, bool> compatible,
                    Func<T, string> id, string descripcion) where T : class
                {
                    var e = await buscar();
                    if (e is not null)
                    {
                        if (!compatible(e)) throw new ExcepcionCaja($"{typeof(T).Name}: {descripcion} incompatible; no se sobrescribe.");
                        return e;
                    }
                    e = crear(); tabla.Add(e); await Contexto.SaveChangesAsync(token);
                    registros.Add(new(typeof(T).Name, id(e), descripcion)); return e;
                }
                foreach (var (modulo, flujoCodigo, operacion) in new[] { ("CXC", "CUENTAS_POR_COBRAR", "COBRAR"), ("CXP", "CUENTAS_POR_PAGAR", "PAGAR") })
                {
                    await Asegurar(Contexto.Modulos, () => Contexto.Modulos.SingleOrDefaultAsync(x => x.Id == modulo, token),
                        () => new Modulos { Id = modulo, Nombre = modulo == "CXC" ? "Cuentas por cobrar" : "Cuentas por pagar", Icono = "payments", Activo = true },
                        x => x.Activo, x => x.Id, modulo);
                    foreach (var codigo in new[] { "CONSULTAR", "ADMINISTRAR", operacion })
                    {
                        var accion = await Asegurar(Contexto.AccionesSistema, () => Contexto.AccionesSistema.SingleOrDefaultAsync(x => x.Codigo == codigo, token),
                            () => new AccionesSistema { Codigo = codigo, Nombre = codigo, EstadoId = activo }, x => x.EstadoId == activo, x => x.Id.ToString(), codigo);
                        var permiso = await Asegurar(Contexto.Permisos, () => Contexto.Permisos.SingleOrDefaultAsync(x => x.Codigo == modulo + "." + codigo, token),
                            () => new Permisos { Codigo = modulo + "." + codigo, ModuloId = modulo, Recurso = modulo, AccionId = accion.Id, EstadoId = activo },
                            x => x.ModuloId == modulo && x.AccionId == accion.Id && x.EstadoId == activo && x.Recurso == modulo, x => x.Id.ToString(), modulo + "." + codigo);
                        await Asegurar(Contexto.RolesPermisos, () => Contexto.RolesPermisos.SingleOrDefaultAsync(x => x.RoleId == "ADMIN" && x.PermisoId == permiso.Id, token),
                            () => new RolesPermisos { RoleId = "ADMIN", PermisoId = permiso.Id, Permitido = true }, x => x.Permitido, x => x.Id.ToString(), "ADMIN → " + permiso.Codigo);
                    }
                    var flujo = await Asegurar(Contexto.FlujosEstados, () => Contexto.FlujosEstados.SingleOrDefaultAsync(x => x.Codigo == flujoCodigo, token),
                        () => new FlujosEstados { Codigo = flujoCodigo, Nombre = flujoCodigo, EstadoId = activo, FechaCreacion = Ahora, CreadoPorUsuarioId = s.UsuarioId },
                        x => x.EstadoId == activo, x => x.Id.ToString(), flujoCodigo);
                    var codigos = new[] { "PENDIENTE", "PARCIAL", "PAGADA", "ANULADA" };
                    foreach (var (codigo, i) in codigos.Select((c, i) => (c, i)))
                        await Asegurar(Contexto.EstadosFlujo, () => Contexto.EstadosFlujo.SingleOrDefaultAsync(x => x.FlujoEstadoId == flujo.Id && x.Codigo == codigo, token),
                            () => new EstadosFlujo { FlujoEstadoId = flujo.Id, Codigo = codigo, Nombre = codigo, Orden = (short)(i + 1),
                                EsInicial = i == 0, EsFinal = i >= 2, PermiteEdicion = false, RequiereMotivo = false, EsSistema = true,
                                EstadoId = activo, FechaCreacion = Ahora, CreadoPorUsuarioId = s.UsuarioId },
                            x => x.EstadoId == activo && x.EsInicial == (i == 0) && x.EsFinal == (i >= 2) && !x.PermiteEdicion
                                && !x.RequiereMotivo && x.EsSistema && x.Orden == i + 1, x => x.Id.ToString(), flujoCodigo + ":" + codigo);
                    if (await Contexto.EstadosFlujo.AnyAsync(x => x.FlujoEstadoId == flujo.Id && !codigos.Contains(x.Codigo), token)
                        || await Contexto.EstadoTransiciones.AnyAsync(x => x.FlujoEstadoId == flujo.Id, token))
                        throw new ExcepcionCaja("El flujo financiero tiene estados/transiciones personalizados. Requiere revisión explícita.");
                    // Estados derivados del saldo; no hay endpoint ni transición manual que pueda marcarlos pagados.
                }
                foreach (var claseNombre in new[] { "MOVIMIENTOS_CAJA", "OPERACIONES_PAGO_CAJA" })
                {
                    var clase = await Contexto.ClaseTipos.SingleOrDefaultAsync(x => x.Nombre == claseNombre && x.EstadoId == activo, token)
                        ?? throw new ExcepcionCaja("Prepare primero las clases de Caja Fase 8.");
                    foreach (var nombre in new[] { "COBRO_CUENTA_POR_COBRAR", "PAGO_CUENTA_POR_PAGAR" })
                        await Asegurar(Contexto.Tipos, () => Contexto.Tipos.SingleOrDefaultAsync(x => x.ClaseTipoId == clase.Id && x.Nombre == nombre, token),
                            () => new Tipos { Id = (Contexto.Tipos.Select(x => (int?)x.Id).Max() ?? 0) + 1, ClaseTipoId = clase.Id,
                                Nombre = nombre, ModuloId = "CAJA", EstadoId = activo },
                            x => x.ModuloId == "CAJA" && x.EstadoId == activo, x => x.Id.ToString(), claseNombre + ":" + nombre);
                }
                if (!await Contexto.Bitacoras.AnyAsync(x => x.EmpresaId == s.EmpresaId && x.Accion == AccionMarcador, token))
                {
                    Auditar(s, s.EmpresaId, AccionMarcador, "Solo configuración CxC/CxP; sin operaciones ni reclasificación histórica.", "Empresas");
                    await Contexto.SaveChangesAsync(token);
                    registros.Add(new("Bitacoras", (await Contexto.Bitacoras.SingleAsync(x => x.EmpresaId == s.EmpresaId && x.Accion == AccionMarcador, token)).Id.ToString(), AccionMarcador));
                }
                return new ResultadoBootstrapAdministrador(registros);
            }, token);
        }
        catch (ExcepcionCaja e) { throw new BootstrapAdministradorRechazadoException(e.Message); }
        catch (Microsoft.Data.SqlClient.SqlException e) when (e.Number == 51090)
        { throw new BootstrapAdministradorRechazadoException("Aplique primero el script controlado de Fase 9."); }
    }
}
