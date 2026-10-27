using Heladeria.Aplicacion.Configuracion;
using Heladeria.Aplicacion.Seguridad.Configuracion;
using Heladeria.Aplicacion.Ventas;
using Heladeria.Aplicacion.Tiempo;
using Heladeria.Infraestructura.Inventario;
using Heladeria.Infraestructura.Persistencia;
using Heladeria.Infraestructura.Persistencia.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VentaEntidad = Heladeria.Infraestructura.Persistencia.Entidades.Ventas;

namespace Heladeria.Infraestructura.VentasPos;

public sealed partial class ServicioVentas(HeladeriaDbContext contexto, IOptions<OpcionesSeguridad> opciones,
    TimeProvider reloj, CoordinadorAjustesInventario coordinador, ServicioInventario inventario)
    : ServicioVentasBase(contexto, opciones, reloj, coordinador, inventario), IServicioVentas
{
    private readonly Caja.ServicioCaja _caja = new(contexto, opciones, reloj, coordinador);
    protected override bool AuditarRechazos => true;

    public async Task<IReadOnlyList<ReferenciaVentaDto>> EstadosAsync(ContextoConfiguracion s, CancellationToken t)
    {
        // Lectura acotada para filtrar ventas sin conceder acceso al catálogo operativo POS.
        await ValidarSucursalAsync(s, t);
        return await Contexto.EstadosFlujo.AsNoTracking().Where(x => x.FlujoEstado.Codigo == FlujoVenta)
            .OrderBy(x => x.Orden).Select(x => new ReferenciaVentaDto(x.Id, x.Codigo, x.Nombre)).ToArrayAsync(t);
    }
    private IQueryable<VentaEntidad> VentasVisibles(ContextoConfiguracion s) => Contexto.Ventas
        .Where(x => x.EmpresaId == s.EmpresaId && x.SucursalId == s.SucursalId);

    private static IQueryable<VentaEntidad> IncluirEncabezado(IQueryable<VentaEntidad> q) => q
        .Include(x => x.Sucursal).Include(x => x.Cliente).Include(x => x.Moneda)
        .Include(x => x.EstadosFlujo).Include(x => x.UsuarioCreacion);

    private static VentaResumenDto Resumen(VentaEntidad v) => new(v.Id, v.EmpresaId, v.SucursalId,
        v.Sucursal.Nombre, v.BodegaId, v.UbicacionId, v.NumeroFactura, v.FechaVenta,
        v.ClienteId, v.Cliente?.NombreCliente, v.MonedaId, v.Moneda.CodigoISO, v.TipoCambio,
        v.SubTotal, v.Descuento, v.Impuestos, v.TotalVenta, v.EstadosFlujo.Codigo, v.EstadoId,
        v.UsuarioCreacionId, v.UsuarioCreacion.UserName, Convert.ToBase64String(v.VersionFila),
        v.FechaCompletada, v.FechaAnulacion, v.MotivoAnulacion, v.ClaveOperacion)
        { EsCredito = v.EsCredito, FechaVencimientoCredito = v.FechaVencimientoCredito };

    public async Task<RespuestaPaginada<VentaResumenDto>> ListarAsync(FiltroVentasSolicitud filtro, ContextoConfiguracion s, CancellationToken t)
    {
        Validar(filtro);
        await ValidarSucursalAsync(s, t);
        DateTime? desde, hasta;
        try
        {
            var zona = TiempoEmpresarial.Zona(ConfiguracionTiempo, s.EmpresaId, s.SucursalId);
            desde = filtro.FechaDesde is { } d ? TiempoEmpresarial.AUTC(d, zona) : null;
            hasta = filtro.FechaHasta is { } h ? TiempoEmpresarial.AUTC(h, zona) : null;
        }
        catch (ArgumentException ex) { throw new ValidacionVentasException(ex.Message); }
        if (desde > hasta) throw new ValidacionVentasException("Rango de fechas inválido.");
        var q = VentasVisibles(s).AsNoTracking();
        if (Opcional(filtro.Busqueda) is { } texto) q = q.Where(x => x.NumeroFactura.Contains(texto)
            || x.Cliente != null && x.Cliente.NombreCliente.Contains(texto));
        if (filtro.EstadoId.HasValue) q = q.Where(x => x.EstadoId == filtro.EstadoId);
        if (desde.HasValue) q = q.Where(x => x.FechaVenta >= desde);
        if (hasta.HasValue) q = q.Where(x => x.FechaVenta <= hasta);
        var total = await q.CountAsync(t);
        var filas = await IncluirEncabezado(q).OrderByDescending(x => x.FechaVenta)
            .ThenByDescending(x => x.NumeroFactura).ThenByDescending(x => x.Id)
            .Skip(Saltar(filtro)).Take(filtro.TamanioPagina).ToArrayAsync(t);
        return new(filas.Select(Resumen).ToArray(), filtro.Pagina, filtro.TamanioPagina, total);
    }

    private static int Saltar(FiltroPaginadoBase f)
    {
        var salto = (long)(f.Pagina - 1) * f.TamanioPagina;
        if (salto > int.MaxValue) throw new ValidacionVentasException("Página fuera de rango.");
        return (int)salto;
    }

    public Task<VentaConsultaDto> ObtenerAsync(long id, ContextoConfiguracion s, CancellationToken t) =>
        LecturaConsistente.EjecutarAsync(Contexto, () => LeerVentaAsync(id, s, t), t);

    private async Task<VentaConsultaDto> LeerVentaAsync(long id, ContextoConfiguracion s, CancellationToken t)
    {
        await ValidarSucursalAsync(s, t);
        var v = await IncluirEncabezado(VentasVisibles(s).AsNoTracking()).SingleOrDefaultAsync(x => x.Id == id, t)
            ?? throw new VentaNoEncontradaException("Venta no encontrada en la sucursal activa.");
        var lineas = await Contexto.VentasDetalle.AsNoTracking().Where(x => x.VentaId == id)
            .Include(x => x.Producto).Include(x => x.ProductoPresentacion!).ThenInclude(x => x.Presentaciones)
            .Include(x => x.UnidadMedida).Include(x => x.UnidadBase).Include(x => x.ImpuestosSnapshot)
            .OrderBy(x => x.NumeroLinea).ToArrayAsync(t);
        var movimientos = await Contexto.Movimientos.AsNoTracking().Where(x => x.EmpresaId == s.EmpresaId
            && x.SucursalId == s.SucursalId && x.ModuloOrigen == "VENTAS" && x.DocumentoOrigenId == id)
            .OrderBy(x => x.Id).Select(x => new MovimientoVentaDto(x.Id, x.ConceptosMovimientos.Codigo,
                x.ConceptosMovimientos.TipoOperacion, x.FechaMovimiento)).ToArrayAsync(t);
        var pago = await Contexto.Pagos.AsNoTracking().Include(x => x.PagoDetalles).ThenInclude(x => x.FormaPago)
            .SingleOrDefaultAsync(x => x.VentaId == id && x.EmpresaId == s.EmpresaId && x.SucursalId == s.SucursalId, t);
        DatosComprobanteDto? comprobante = null;
        if (v.FechaCompletada is not null && v.EstadosFlujo.Codigo is "COMPLETADA" or "ANULADA"
            && lineas.Length > 0 && lineas.All(x => x.TotalLinea.HasValue && x.SubtotalNeto.HasValue)
            && await Contexto.FlujosEstados.AnyAsync(x => x.Id == v.FlujoEstadoId && x.Codigo == FlujoVenta, t))
        {
            var empresa = await Contexto.Empresas.AsNoTracking().SingleAsync(x => x.Id == s.EmpresaId, t);
            var cuenta = await Contexto.CuentasPorCobrar.AsNoTracking().SingleOrDefaultAsync(x => x.VentaId == id && x.EmpresaId == s.EmpresaId, t);
            var usuario = await Contexto.Usuarios.AsNoTracking().Where(x => x.Id == v.CompletadoPorUsuarioId).Select(x => x.UserName).SingleOrDefaultAsync(t);
            comprobante = new(Opcional(empresa.NombreComercial) ?? empresa.RazonSocial, empresa.RazonSocial,
                v.Sucursal.Direccion, v.Sucursal.Telefono, v.Cliente?.CodigoCliente, usuario,
                TiempoEmpresarial.Zona(ConfiguracionTiempo, s.EmpresaId, s.SucursalId), cuenta?.Id, cuenta?.MontoOriginal,
                pago?.PagoDetalles.OrderBy(x => x.Id).Select(x => new PagoComprobanteDto(x.FormaPago.Nombre, x.Monto,
                    x.MontoRecibido, x.EsEfectivo == true ? x.MontoRecibido - x.Monto : x.EsEfectivo == false ? 0 : null, x.EsEfectivo)).ToArray() ?? []);
        }
        return new(Resumen(v), lineas.Select(x => new VentaLineaDto(x.Id, x.NumeroLinea, x.ProductoId,
            x.Producto.SKU, x.Producto.Nombre, x.ProductoPresentacionId, x.ProductoPresentacion?.Presentaciones.Nombre,
            x.Cantidad, x.FactorConversion, x.CantidadBase, x.UnidadMedida?.Abreviatura, x.UnidadBase?.Abreviatura,
            x.ManejaExistencia, x.PrecioUnitario, x.FuentePrecio, x.SubtotalNeto, x.PorcentajeDescuento,
            x.MontoDescuento, x.DescuentoPrecioCatalogo, x.Impuesto, x.TotalLinea, x.CostoUnitario, x.CostoTotal,
            x.ImpuestosSnapshot.OrderBy(i => i.ImpuestoId).Select(ImpuestoDto).ToArray())).ToArray(), movimientos)
        {
            Comprobante = comprobante,
            Cobro = pago?.TurnoCajaId is { } turno && pago.ClaveOperacion is { } clave
                ? new(pago.Id, turno, clave, pago.PagoDetalles.Select(x => new Heladeria.Aplicacion.Caja.FormaCobroSolicitud
                    { FormaPagoId = x.FormaPagoId, MontoAplicado = x.Monto, MontoRecibido = x.MontoRecibido ?? x.Monto, Referencia = x.Referencia }).ToArray())
                : null
        };
    }

    private static ImpuestoVentaDto ImpuestoDto(VentaDetalleImpuestos x) => new(x.ImpuestoId, x.Codigo,
        x.Nombre, x.Porcentaje, x.IncluidoEnPrecio, x.BaseImponible, x.Monto);

    public async Task<ReferenciasVentasDto> ReferenciasAsync(ContextoConfiguracion s, CancellationToken t)
    {
        await ValidarSucursalAsync(s, t);
        var moneda = await MonedaBaseAsync(s, t);
        var estados = await Contexto.EstadosFlujo.AsNoTracking().Where(x => x.FlujoEstado.Codigo == FlujoVenta
            && x.Estado.Activo && Operativos.Contains(x.Estado.Nombre)).OrderBy(x => x.Orden)
            .Select(x => new ReferenciaVentaDto(x.Id, x.Codigo, x.Nombre)).ToArrayAsync(t);
        var clientes = await Contexto.Clientes.AsNoTracking().Where(x => x.EmpresaId == s.EmpresaId
            && x.Estado.Activo && Operativos.Contains(x.Estado.Nombre)).OrderBy(x => x.NombreCliente)
            .Select(x => new ReferenciaVentaDto(x.Id, x.CodigoCliente, x.NombreCliente)).ToArrayAsync(t);
        var categorias = await Contexto.Categorias.AsNoTracking().Where(x => x.EmpresaId == s.EmpresaId
            && x.Estado.Activo && Operativos.Contains(x.Estado.Nombre)).OrderBy(x => x.Nombre)
            .Select(x => new ReferenciaVentaDto(x.Id, x.Codigo, x.Nombre)).ToArrayAsync(t);
        var almacenes = await Inventario.ListarAlmacenesAsync(s, t);
        return new(s.EmpresaId, s.SucursalId, moneda.Id, moneda.CodigoISO, 1, estados, clientes, categorias,
            almacenes.Where(x => x.SucursalId == s.SucursalId).Select(x => new AlmacenVentaDto(x.BodegaId,
                x.Bodega, x.UbicacionId, x.Ubicacion)).ToArray());
    }

    private IQueryable<Productos> ProductosVendibles(ContextoConfiguracion s) => Contexto.Productos.AsNoTracking()
        .Where(x => x.EmpresaId == s.EmpresaId && x.EsVendible && (!x.EsCombo || x.CombosDetalleProductos.Any())
            && x.Estado.Activo && Operativos.Contains(x.Estado.Nombre)
            && x.TiposProductos.EmpresaId == s.EmpresaId && x.TiposProductos.Estado.Activo && Operativos.Contains(x.TiposProductos.Estado.Nombre)
            && x.UnidadesMedida.EmpresaId == s.EmpresaId && x.UnidadesMedida.Estado.Activo && Operativos.Contains(x.UnidadesMedida.Estado.Nombre)
            && x.Categorias.EmpresaId == s.EmpresaId && x.Categorias.Estado.Activo && Operativos.Contains(x.Categorias.Estado.Nombre)
            && (x.TipoSeguimiento.EmpresaId == null || x.TipoSeguimiento.EmpresaId == s.EmpresaId)
            && x.TipoSeguimiento.ClaseCatalogo.Codigo == "TIPO_SEGUIMIENTO_PRODUCTO"
            && x.TipoSeguimiento.Estado.Activo && Operativos.Contains(x.TipoSeguimiento.Estado.Nombre)
            && x.TipoSeguimiento.ClaseCatalogo.Estado.Activo && Operativos.Contains(x.TipoSeguimiento.ClaseCatalogo.Estado.Nombre)
            && (x.MetodoValuacion.EmpresaId == null || x.MetodoValuacion.EmpresaId == s.EmpresaId)
            && x.MetodoValuacion.ClaseCatalogo.Codigo == "METODO_VALUACION_INVENTARIO"
            && x.MetodoValuacion.Estado.Activo && Operativos.Contains(x.MetodoValuacion.Estado.Nombre)
            && x.MetodoValuacion.ClaseCatalogo.Estado.Activo && Operativos.Contains(x.MetodoValuacion.ClaseCatalogo.Estado.Nombre)
            && (!x.TiposProductos.ManejaExistencia || !x.ControlaVencimiento
                && x.TipoSeguimiento.Codigo == "NINGUNO" && x.MetodoValuacion.Codigo == "PROMEDIO"))
        .Include(x => x.UnidadesMedida).Include(x => x.TiposProductos).Include(x => x.Categorias);

    public async Task<RespuestaPaginada<ProductoPosDto>> ProductosAsync(FiltroProductosPosSolicitud f, ContextoConfiguracion s, CancellationToken t)
    {
        Validar(f);
        await ValidarSucursalAsync(s, t);
        if (f.UbicacionId.HasValue && !f.BodegaId.HasValue) throw new ValidacionVentasException("La ubicación requiere bodega.");
        if (f.BodegaId.HasValue) await ValidarAlmacenAsync(f.BodegaId.Value, f.UbicacionId, s, t);
        var moneda = await MonedaBaseAsync(s, t);
        var q = ProductosVendibles(s);
        if (Opcional(f.Busqueda) is { } texto) q = q.Where(x => x.SKU.Contains(texto) || x.Nombre.Contains(texto));
        if (f.CategoriaId.HasValue) q = q.Where(x => x.CategoriaId == f.CategoriaId);
        var total = await q.CountAsync(t);
        var productos = await q.OrderBy(x => x.Nombre).ThenBy(x => x.Id).Skip(Saltar(f)).Take(f.TamanioPagina).ToArrayAsync(t);
        var lista = new List<ProductoPosDto>();
        foreach (var p in productos) lista.Add(await ProductoDtoAsync(p, moneda.CodigoISO, f.BodegaId, f.UbicacionId, s, t));
        return new(lista, f.Pagina, f.TamanioPagina, total);
    }

    public async Task<ProductoPosDto> PresentacionesAsync(long productoId, ContextoConfiguracion s, CancellationToken t)
    {
        await ValidarSucursalAsync(s, t);
        var moneda = await MonedaBaseAsync(s, t);
        var producto = await ProductosVendibles(s).SingleOrDefaultAsync(x => x.Id == productoId, t)
            ?? throw new VentaNoEncontradaException("Producto no disponible para venta en esta empresa.");
        return await ProductoDtoAsync(producto, moneda.CodigoISO, null, null, s, t);
    }

    private async Task<ProductoPosDto> ProductoDtoAsync(Productos p, string moneda, long? bodega, long? ubicacion, ContextoConfiguracion s, CancellationToken t)
    {
        var pp = await Contexto.ProductoPresentaciones.AsNoTracking().Where(x => x.ProductoId == p.Id
            && x.EmpresaId == s.EmpresaId && x.PermiteVenta && x.FactorConversion > 0
            && x.Estado.Activo && Operativos.Contains(x.Estado.Nombre)
            && x.Presentaciones.EmpresaId == s.EmpresaId && x.Presentaciones.Estado.Activo && Operativos.Contains(x.Presentaciones.Estado.Nombre)
            && x.UnidadesMedida.EmpresaId == s.EmpresaId && x.UnidadesMedida.Estado.Activo && Operativos.Contains(x.UnidadesMedida.Estado.Nombre))
            .Include(x => x.Presentaciones).Include(x => x.UnidadesMedida).OrderBy(x => x.Presentaciones.Nombre).ThenBy(x => x.Id).ToArrayAsync(t);
        var opciones = new List<PresentacionPosDto> { Opcion(p, null) };
        opciones.AddRange(pp.Select(x => Opcion(p, x)));
        var hoy = DateOnly.FromDateTime(Ahora);
        if (await Contexto.ProductoImpuestos.AsNoTracking().AnyAsync(x => x.ProductoId == p.Id
            && x.Estado.Activo && Operativos.Contains(x.Estado.Nombre) && x.Impuesto.EmpresaId != s.EmpresaId, t))
            throw new ConflictoVentasException("El producto tiene una asociación tributaria de otra empresa; corrija la configuración.");
        var impuestos = await Contexto.ProductoImpuestos.AsNoTracking().Where(x => x.ProductoId == p.Id
            && x.Estado.Activo && Operativos.Contains(x.Estado.Nombre)
            && x.Impuesto.EmpresaId == s.EmpresaId && x.Impuesto.Estado.Activo && Operativos.Contains(x.Impuesto.Estado.Nombre)
            && x.Impuesto.FechaVigenciaDesde <= hoy && (!x.Impuesto.FechaVigenciaHasta.HasValue || x.Impuesto.FechaVigenciaHasta >= hoy))
            .OrderBy(x => x.ImpuestoId).Select(x => new ImpuestoPosDto(x.ImpuestoId, x.Impuesto.Codigo,
                x.Impuesto.Nombre, x.Impuesto.Porcentaje, x.Impuesto.IncluidoEnPrecio)).ToArrayAsync(t);
        decimal? disponible = null;
        var compuesto = await Contexto.CombosDetalle.AsNoTracking().AnyAsync(x => x.EmpresaId == s.EmpresaId && x.ComboProductoId == p.Id, t);
        if (bodega.HasValue && p.TiposProductos.ManejaExistencia && !compuesto)
        {
            // Materializar permite el mismo cálculo decimal en SQL Server y en pruebas relacionales SQLite.
            var saldos = await Contexto.Existencias.AsNoTracking().Where(x => x.EmpresaId == s.EmpresaId
                && x.ProductoId == p.Id && x.BodegaId == bodega && (!ubicacion.HasValue || x.UbicacionId == ubicacion)
                && x.Ubicaciones.Estado.Activo && Operativos.Contains(x.Ubicaciones.Estado.Nombre))
                .Select(x => new { x.Cantidad, x.CantidadReservada }).ToArrayAsync(t);
            disponible = saldos.Sum(x => x.Cantidad - x.CantidadReservada);
        }
        return new(p.Id, p.SKU, p.Nombre, p.Imagen, p.CategoriaId, p.Categorias.Nombre, p.UnidadBaseId,
            p.UnidadesMedida.Nombre, p.UnidadesMedida.Abreviatura, p.TiposProductos.ManejaExistencia,
            p.PermiteDescuento, disponible, moneda, opciones, impuestos);
    }

    private static PresentacionPosDto Opcion(Productos p, ProductoPresentaciones? pp)
    {
        var unidad = pp?.UnidadesMedida ?? p.UnidadesMedida;
        var precio = pp?.PrecioVentaEstandar;
        string? fuente = precio.HasValue ? "PRESENTACION" : null;
        if (!precio.HasValue && (pp is null || pp.FactorConversion == 1 && pp.UnidadMedidaId == p.UnidadBaseId))
        { precio = p.PrecioVentaReferencial; fuente = precio.HasValue ? "PRODUCTO_REFERENCIAL" : null; }
        return new(pp?.Id, pp?.Presentaciones.Codigo ?? unidad.Codigo, pp?.Presentaciones.Nombre ?? "Unidad base",
            unidad.Id, unidad.Nombre, unidad.Abreviatura, pp?.FactorConversion ?? 1, precio, fuente,
            precio is > 0 && precio <= CalculadoraVenta.MaximoImporte && precio == decimal.Round(precio.Value, 2));
    }
}
