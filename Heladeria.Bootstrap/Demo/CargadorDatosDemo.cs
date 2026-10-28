using Heladeria.Aplicacion.Configuracion;
using Heladeria.Aplicacion.Seguridad.Configuracion;
using Heladeria.Aplicacion.Tiempo;
using Heladeria.Infraestructura.Caja;
using Heladeria.Infraestructura.Cartera;
using Heladeria.Infraestructura.ClientesGestion;
using Heladeria.Infraestructura.Compras;
using Heladeria.Infraestructura.Fase4;
using Heladeria.Infraestructura.Inventario;
using Heladeria.Infraestructura.Persistencia;
using Heladeria.Infraestructura.Persistencia.Entidades;
using Heladeria.Infraestructura.VentasPos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;

namespace Heladeria.Bootstrap.Demo;

public sealed class DemoRechazadaException(string mensaje) : Exception(mensaje);
public sealed record ResultadoDemo(int EmpresaId, long SucursalId, int Productos, int Proveedores, int Clientes,
    int Compras, int Ventas, int Turnos, int CxC, int CxP, DateOnly Desde, DateOnly Hasta);

public sealed partial class CargadorDatosDemo
{
    public const string Marca = "CARGA_DEMO_V1";
    public const string Equipo = "HELADERIA-DEMO-V1";
    public const string CodigoCaja = "DEMO-CAJA";
    private readonly HeladeriaDbContext db;
    private readonly IOptions<OpcionesSeguridad> opciones;
    private readonly RelojDemo reloj;
    private readonly Action<string>? progreso;
    private readonly ServicioProductos productos;
    private readonly ServicioCatalogosProductos catalogos;
    private readonly ServicioProveedores proveedores;
    private readonly ServicioClientes clientes;
    private readonly ServicioCompras compras;
    private readonly ServicioVentas ventas;
    private readonly ServicioCaja caja;
    private readonly ServicioCartera cartera;
    private ContextoConfiguracion s = null!;
    private int activo, moneda;
    private long bodega, ubicacion;
    private string zona = "";
    private DateOnly hoy;
    private readonly Dictionary<string, long> productoIds = [];
    private readonly Dictionary<string, long> presentacionIds = [];
    private readonly List<int> proveedorIds = [], clienteIds = [];
    private readonly List<long> compraIds = [], ventaIds = [], turnoIds = [], cxcIds = [], cxpIds = [];
    public static HeladeriaDbContext CrearContexto(string cadena) => new(new DbContextOptionsBuilder<HeladeriaDbContext>()
        .UseSqlServer(cadena, sql => sql.CommandTimeout(120))
        .ReplaceService<IDbContextTransactionManager, TransaccionesDemo>().Options);
    public CargadorDatosDemo(HeladeriaDbContext contexto, IOptions<OpcionesSeguridad> configuracion,
        TimeProvider tiempo, Action<string>? informar = null)
    {
        db = contexto; opciones = configuracion; reloj = new(tiempo.GetUtcNow()); progreso = informar;
        var coord = new CoordinadorAjustesInventario();
        var auditoria = new AuditoriaFase4(db, reloj);
        var inventario = new ServicioInventario(db, auditoria, opciones, reloj, coord);
        productos = new(db, auditoria, opciones, reloj, coord);
        catalogos = new(db, auditoria, opciones, reloj);
        proveedores = new(db, opciones, reloj, coord);
        clientes = new(db, opciones, reloj);
        compras = new(db, opciones, reloj, coord, inventario);
        ventas = new(db, opciones, reloj, coord, inventario);
        caja = new(db, opciones, reloj, coord);
        cartera = new(db, opciones, reloj, coord, caja);
    }
    public static void ValidarAmbiente(string ambiente, bool confirmar)
    {
        if (!string.Equals(ambiente, "Development", StringComparison.OrdinalIgnoreCase) && !confirmar)
            throw new DemoRechazadaException("Carga demo rechazada fuera de Development. Requiere --confirmar-demo explícito.");
    }
    public async Task<ResultadoDemo> EjecutarAsync(string ambiente, bool confirmar, CancellationToken t = default)
    {
        ValidarAmbiente(ambiente, confirmar);
        if (!db.Database.IsSqlServer() || db.GetService<IDbContextTransactionManager>() is not TransaccionesDemo manager)
            throw new DemoRechazadaException("Use el contexto SQL Server exclusivo del cargador demo.");
        await using var raiz = await manager.AbrirRaizAsync(t);
        try
        {
            await db.Database.ExecuteSqlRawAsync("""
                DECLARE @r int;
                EXEC @r=sys.sp_getapplock @Resource=N'Heladeria.DatosDemo.V1', @LockMode=N'Exclusive',
                    @LockOwner=N'Transaction', @LockTimeout=10000;
                IF @r<0 THROW 51120, 'Otra carga demo está en ejecución.', 1;
                IF COL_LENGTH('dbo.CombosDetalle','ProductoPresentacionId') IS NULL
                  OR COL_LENGTH('dbo.VentasDetalle','EsCompuesto') IS NULL
                  OR COL_LENGTH('dbo.MovimientoDetalles','VentaDetalleId') IS NULL
                  OR COL_LENGTH('dbo.Pagos','HuellaSolicitud') IS NULL
                    THROW 51120, 'Se requiere el esquema completo hasta Ajuste D. El cargador no instala esquema.', 1;
                """, t);
            await ResolverContextoAsync(t);
            var clave = "Heladeria.Inventario." + s.EmpresaId.ToString(System.Globalization.CultureInfo.InvariantCulture);
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                DECLARE @r int;
                EXEC @r=sys.sp_getapplock @Resource={clave}, @LockMode=N'Exclusive', @LockOwner=N'Transaction', @LockTimeout=10000;
                IF @r<0 THROW 51120, 'La empresa está ocupada. Reintente la carga demo sin operaciones simultáneas.', 1;
                """, t);
            if (await db.Bitacoras.AnyAsync(x => x.EmpresaId == s.EmpresaId && x.Accion == Marca, t)
                || await db.Productos.AnyAsync(x => x.EmpresaId == s.EmpresaId && x.SKU.StartsWith("DEMO-"), t)
                || await db.Cajas.AnyAsync(x => x.EmpresaId == s.EmpresaId && x.Codigo == CodigoCaja, t)
                || await db.Proveedores.AnyAsync(x => x.EmpresaId == s.EmpresaId && x.CodigoProveedor.StartsWith("DEMO-"), t)
                || await db.Clientes.AnyAsync(x => x.EmpresaId == s.EmpresaId && x.CodigoCliente.StartsWith("DEMO-"), t))
                throw new DemoRechazadaException("Los datos demo ya fueron cargados o existen códigos demo reservados. No se duplicarán ni sobrescribirán.");
            await ValidarPreparacionAsync(t);
            zona = TiempoEmpresarial.Zona(opciones.Value, s.EmpresaId, s.SucursalId);
            hoy = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(reloj.Limite, TimeZoneInfo.FindSystemTimeZoneById(zona)).DateTime);
            PonerFecha(hoy.AddDays(-13), 8);
            await CrearCatalogoAsync(t);
            progreso?.Invoke("CATALOGO");
            await CrearTercerosAsync(t);
            await CrearOperacionesAsync(t);
            await ValidarResultadoAsync(t);
            progreso?.Invoke("VALIDADO");
            db.Bitacoras.Add(new() { EmpresaId = s.EmpresaId, SucursalId = s.SucursalId, UsuarioId = s.UsuarioId,
                Entidad = "DatosDemo", EntidadId = "V1", Accion = Marca, Fecha = reloj.Limite.UtcDateTime,
                Equipo = Equipo, Comentario = "Datos ficticios opcionales generados mediante servicios. Caja independiente; sin usuarios ni permisos nuevos." });
            await db.SaveChangesAsync(t);
            manager.ExigirValida();
            await raiz.CommitAsync(t);
            return new(s.EmpresaId, s.SucursalId, productoIds.Count, proveedorIds.Count, clienteIds.Count,
                compraIds.Count, ventaIds.Count, turnoIds.Count, cxcIds.Count, cxpIds.Count, hoy.AddDays(-13), hoy);
        }
        catch
        {
            await raiz.RollbackAsync(CancellationToken.None);
            db.ChangeTracker.Clear();
            throw;
        }
    }
    private async Task ResolverContextoAsync(CancellationToken t)
    {
        var operativos = opciones.Value.EstadosActivos;
        var principales = await db.UsuariosSucursales.AsNoTracking().Where(x => x.EsPrincipal && x.Usuario.RoleId == "ADMIN"
            && x.Usuario.Estado.Activo && operativos.Contains(x.Usuario.Estado.Nombre) && x.Estado.Activo && operativos.Contains(x.Estado.Nombre)
            && x.Sucursal.Estado.Activo && operativos.Contains(x.Sucursal.Estado.Nombre)
            && x.Sucursal.Empresa.Estado.Activo && operativos.Contains(x.Sucursal.Empresa.Estado.Nombre))
            .OrderBy(x => x.UsuarioId).Select(x => new { x.UsuarioId, x.SucursalId, x.Sucursal.EmpresaId }).ToArrayAsync(t);
        if (principales.Length == 0 || principales.Select(x => (x.EmpresaId, x.SucursalId)).Distinct().Count() != 1)
            throw new DemoRechazadaException("Se requiere una empresa/sucursal principal administrativa inequívoca y activa.");
        var p = principales[0];
        s = new(p.UsuarioId, "ADMIN", p.EmpresaId, p.SucursalId, null, Equipo);
        activo = await db.Permisos.Where(x => x.Codigo == "SEGURIDAD.ADMINISTRAR" && x.Estado.Activo
            && operativos.Contains(x.Estado.Nombre)).Select(x => x.EstadoId).SingleAsync(t);
    }
    private async Task ValidarPreparacionAsync(CancellationToken t)
    {
        var series = new[] { "OC", "RC", "VT", "VS", "VA", "CA", "CC", "MC" };
        var disponibles = await db.SeriesDocumentos.Where(x => x.EmpresaId == s.EmpresaId && x.SucursalId == null
            && x.EstadoId == activo && x.CorrelativosDocumentos != null).Select(x => x.Serie).ToArrayAsync(t);
        if (series.Except(disponibles).Any())
            throw new DemoRechazadaException("Falta preparación estructural de Compras/Ventas/Caja. El cargador no crea permisos ni series.");
        var flujos = await db.FlujosEstados.Select(x => x.Codigo).ToArrayAsync(t);
        if (new[] { "CUENTAS_POR_COBRAR", "CUENTAS_POR_PAGAR" }.Except(flujos).Any())
            throw new DemoRechazadaException("Prepare primero Fase 9.");
        var almacen = await db.Ubicaciones.AsNoTracking().Where(x => x.EmpresaId == s.EmpresaId && x.Bodegas.SucursalId == s.SucursalId
            && x.EstadoId == activo && x.Bodegas.EstadoId == activo).OrderBy(x => x.Id).Select(x => new { x.Id, x.BodegaId }).FirstOrDefaultAsync(t)
            ?? throw new DemoRechazadaException("No existe bodega/ubicación activa en la sucursal principal.");
        bodega = almacen.BodegaId; ubicacion = almacen.Id;
        var monedas = await db.Monedas.AsNoTracking().Where(x => x.EmpresaId == s.EmpresaId).ToArrayAsync(t);
        var gtq = monedas.SingleOrDefault(x => x.CodigoISO == "GTQ");
        if (monedas.Any(x => x.EsMonedaBase && x.CodigoISO != "GTQ")
            || gtq is not null && (!gtq.EsMonedaBase || gtq.EstadoId != activo || gtq.Decimales != 2))
            throw new DemoRechazadaException("La moneda GTQ base no está configurada de forma compatible. No se modificará.");
        if (gtq is null)
        {
            gtq = new() { EmpresaId = s.EmpresaId, CodigoISO = "GTQ", NombreMoneda = "Quetzal", Simbolo = "Q",
                EsMonedaBase = true, Decimales = 2, EstadoId = activo };
            db.Monedas.Add(gtq); await db.SaveChangesAsync(t);
        }
        moneda = gtq.Id; db.ChangeTracker.Clear();
    }
    private void PonerFecha(DateOnly dia, int hora)
    {
        var utc = new DateTimeOffset(TiempoEmpresarial.AUTC(dia.ToDateTime(new TimeOnly(hora, 0)), zona));
        reloj.Actual = utc > reloj.Limite ? reloj.Limite : utc;
    }
    private sealed class RelojDemo(DateTimeOffset limite) : TimeProvider
    {
        public DateTimeOffset Limite { get; } = limite;
        public DateTimeOffset Actual { get; set; } = limite;
        public override DateTimeOffset GetUtcNow() => Actual;
    }
}
