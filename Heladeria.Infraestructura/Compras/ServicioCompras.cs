using System.Linq.Expressions;
using Heladeria.Aplicacion.Compras;
using Heladeria.Aplicacion.Configuracion;
using Heladeria.Aplicacion.Inventario;
using Heladeria.Aplicacion.Productos;
using Heladeria.Aplicacion.Seguridad.Configuracion;
using Heladeria.Infraestructura.Inventario;
using Heladeria.Infraestructura.Persistencia;
using Heladeria.Infraestructura.Persistencia.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Heladeria.Infraestructura.Compras;

public sealed class ServicioCompras(HeladeriaDbContext contexto, IOptions<OpcionesSeguridad> opciones,
    TimeProvider reloj, CoordinadorAjustesInventario coordinador, ServicioInventario inventario)
    : ServicioComprasBase(contexto, opciones, reloj, coordinador), IServicioCompras
{
    internal const string FlujoOrden = "ORDENES_COMPRA";
    internal const string FlujoRecepcion = "RECEPCIONES_COMPRA";
    private static readonly Expression<Func<OrdenesCompra, CompraResumenDto>> Proyectar = item => new(
        item.Id, item.EmpresaId, item.SucursalId, item.Sucursal.Nombre, item.ProveedorId, item.Proveedor.NombreProveedor,
        item.NumeroOrden, item.FechaEmision, item.MonedaId, item.TipoCambio, item.SubTotal, item.Impuestos,
        item.TotalEstimado, item.EstadoId, item.EstadosFlujo.Codigo)
        { EsCredito = item.EsCredito, VersionFila = Convert.ToBase64String(item.VersionFila) };
    private IQueryable<OrdenesCompra> Alcance(ContextoConfiguracion seguridad) => Contexto.OrdenesCompra
        .Where(item => item.EmpresaId == seguridad.EmpresaId && item.SucursalId == seguridad.SucursalId);

    public async Task<ReferenciasComprasDto> ObtenerReferenciasAsync(ContextoConfiguracion seguridad, CancellationToken token)
    {
        await ValidarSucursalAsync(seguridad, token);
        var monedas = await Contexto.Monedas.AsNoTracking().Where(item => item.EmpresaId == seguridad.EmpresaId
            && item.Estado.Activo && Operativos.Contains(item.Estado.Nombre)).OrderBy(item => item.CodigoISO)
            .Select(item => new MonedaCompraDto(item.Id, item.CodigoISO, item.NombreMoneda, item.EsMonedaBase, item.Decimales)).ToArrayAsync(token);
        var estados = await Contexto.EstadosFlujo.AsNoTracking().Where(item => (item.FlujoEstado.Codigo == FlujoOrden
            || item.FlujoEstado.Codigo == FlujoRecepcion) && item.Estado.Activo && Operativos.Contains(item.Estado.Nombre)
            && item.FlujoEstado.Estado.Activo && Operativos.Contains(item.FlujoEstado.Estado.Nombre))
            .OrderBy(item => item.FlujoEstado.Codigo).ThenBy(item => item.Orden)
            .Select(item => new EstadoCompraDto(item.Id, item.FlujoEstado.Codigo, item.Codigo, item.PermiteEdicion, item.EsFinal)).ToArrayAsync(token);
        var series = await Contexto.SeriesDocumentos.AsNoTracking().Where(item => item.EmpresaId == seguridad.EmpresaId
            && item.SucursalId == null && item.TipoDocumento.ModuloId == "COMPRAS" && item.Estado.Activo
            && Operativos.Contains(item.Estado.Nombre)).OrderBy(item => item.Serie)
            .Select(item => new SerieCompraDto(item.Id, item.Serie, item.Prefijo, item.Longitud)).ToArrayAsync(token);
        return new(monedas, estados, series);
    }

    public async Task<ProductoPresentacionesCompraDto> ListarPresentacionesAsync(long productoId,
        ContextoConfiguracion seguridad, CancellationToken token)
    {
        await ValidarSucursalAsync(seguridad, token);
        var producto = await Contexto.Productos.AsNoTracking()
            .Where(item => item.Id == productoId && item.EmpresaId == seguridad.EmpresaId
                && item.UnidadesMedida.EmpresaId == seguridad.EmpresaId)
            .Select(item => new
            {
                item.Id,
                item.Nombre,
                item.UnidadBaseId,
                UnidadCodigo = item.UnidadesMedida.Codigo,
                UnidadNombre = item.UnidadesMedida.Nombre,
                UnidadAbreviatura = item.UnidadesMedida.Abreviatura
            }).SingleOrDefaultAsync(token)
            ?? throw new RecursoComprasNoEncontradoException("Producto no encontrado.");

        await ValidarProductoAsync(productoId, null, seguridad, token);

        var presentaciones = await Contexto.ProductoPresentaciones.AsNoTracking()
            .Where(item => item.ProductoId == productoId && item.EmpresaId == seguridad.EmpresaId
                && item.PermiteCompra
                && item.Estado.Activo && Operativos.Contains(item.Estado.Nombre)
                && item.Presentaciones.EmpresaId == seguridad.EmpresaId
                && item.Presentaciones.Estado.Activo && Operativos.Contains(item.Presentaciones.Estado.Nombre)
                && item.UnidadesMedida.EmpresaId == seguridad.EmpresaId
                && item.UnidadesMedida.Estado.Activo && Operativos.Contains(item.UnidadesMedida.Estado.Nombre))
            .OrderByDescending(item => item.EsPresentacionBase)
            .ThenBy(item => item.Presentaciones.Nombre)
            .ThenBy(item => item.Id)
            .Select(item => new PresentacionCompraDto(item.Id, item.PresentacionId,
                item.Presentaciones.Codigo, item.Presentaciones.Nombre, item.UnidadMedidaId,
                item.UnidadesMedida.Codigo, item.UnidadesMedida.Nombre, item.UnidadesMedida.Abreviatura,
                item.FactorConversion, item.EsPresentacionBase, true))
            .ToArrayAsync(token);

        return new(producto.Id, producto.Nombre, producto.UnidadBaseId, producto.UnidadCodigo,
            producto.UnidadNombre, producto.UnidadAbreviatura, presentaciones);
    }

    public async Task<RespuestaPaginada<CompraResumenDto>> ListarAsync(FiltroComprasSolicitud filtro,
        ContextoConfiguracion seguridad, CancellationToken token)
    {
        Validar(filtro); await ValidarSucursalAsync(seguridad, token);
        if (filtro.SucursalId.HasValue && filtro.SucursalId != seguridad.SucursalId)
            throw new RecursoComprasNoEncontradoException("La sucursal no pertenece al contexto actual.");
        if (filtro.FechaDesde > filtro.FechaHasta) throw new ValidacionComprasException("El intervalo de fechas no es válido.");
        var query = Alcance(seguridad).AsNoTracking();
        if (filtro.ProveedorId is { } proveedor) query = query.Where(item => item.ProveedorId == proveedor);
        if (filtro.EstadoId is { } estado) query = query.Where(item => item.EstadoId == estado);
        if (filtro.FechaDesde is { } desde) query = query.Where(item => item.FechaEmision >= desde);
        if (filtro.FechaHasta is { } hasta) query = query.Where(item => item.FechaEmision <= hasta);
        if (Opcional(filtro.Documento)?.ToUpperInvariant() is { } documento) query = query.Where(item => item.NumeroOrden.ToUpper().Contains(documento));
        if (Opcional(filtro.Busqueda)?.ToUpperInvariant() is { } buscar) query = query.Where(item => item.NumeroOrden.ToUpper().Contains(buscar)
            || item.Proveedor.NombreProveedor.ToUpper().Contains(buscar) || item.Proveedor.CodigoProveedor.ToUpper().Contains(buscar));
        var total = await query.CountAsync(token);
        return new(await Paginar(query.OrderByDescending(item => item.FechaEmision).ThenByDescending(item => item.Id), filtro)
            .Select(Proyectar).ToArrayAsync(token), filtro.Pagina, filtro.TamanioPagina, total);
    }

    public async Task<CompraConsultaDto> ObtenerAsync(long id, ContextoConfiguracion seguridad, CancellationToken token)
    {
        await ValidarSucursalAsync(seguridad, token);
        var encabezado = await Alcance(seguridad).AsNoTracking().Where(item => item.Id == id).Select(Proyectar)
            .SingleOrDefaultAsync(token) ?? throw new RecursoComprasNoEncontradoException("Orden de compra no encontrada.");
        var entrega = await Alcance(seguridad).AsNoTracking().Where(item => item.Id == id)
            .Select(item => item.FechaEntregaEsperada).SingleAsync(token);
        // Solo se materializan líneas/recepciones del documento solicitado; el listado se pagina en SQL.
        var lineas = await Contexto.OrdenesCompraDetalle.AsNoTracking().Where(item => item.OrdenCompraId == id)
            .OrderBy(item => item.NumeroLinea).Select(item => new
            {
                item.Id, item.NumeroLinea, item.ProductoId, Producto = item.Producto.Nombre,
                item.ProductoPresentacionId, Presentacion = item.ProductoPresentacion == null ? null : item.ProductoPresentacion.Presentaciones.Nombre,
                item.Producto.UnidadBaseId, UnidadBase = item.Producto.UnidadesMedida.Nombre,
                Factor = item.ProductoPresentacion == null ? 1m : item.ProductoPresentacion.FactorConversion,
                item.Cantidad, item.PrecioUnitario, item.Descuento, item.Impuesto
            }).ToArrayAsync(token);
        var recibidas = await Contexto.RecepcionCompraDetalles.AsNoTracking().Where(item => item.RecepcionCompra.OrdenCompraId == id)
            .Select(item => new { item.OrdenCompraDetalleId, item.CantidadRecibida }).ToArrayAsync(token);
        var acumuladas = recibidas.GroupBy(item => item.OrdenCompraDetalleId).ToDictionary(item => item.Key, item => item.Sum(fila => fila.CantidadRecibida));
        var detalles = lineas.Select(item => new CompraDetalleDto(item.Id, item.NumeroLinea, item.ProductoId, item.Producto,
            item.ProductoPresentacionId, item.Presentacion, item.UnidadBaseId, item.UnidadBase, item.Cantidad, item.Factor,
            item.Cantidad * item.Factor, item.PrecioUnitario, item.Descuento, item.Impuesto,
            item.Cantidad * item.PrecioUnitario - item.Descuento + item.Impuesto, acumuladas.GetValueOrDefault(item.Id),
            item.Cantidad - acumuladas.GetValueOrDefault(item.Id))).ToArray();
        var ids = await Contexto.RecepcionesCompra.AsNoTracking().Where(item => item.OrdenCompraId == id)
            .OrderBy(item => item.Id).Select(item => item.Id).ToArrayAsync(token);
        var recepciones = new List<RecepcionCompraDto>();
        foreach (var recepcionId in ids) recepciones.Add(await ObtenerRecepcionAsync(recepcionId, seguridad, token));
        return new(encabezado, entrega, detalles, recepciones);
    }

    public Task<CompraConsultaDto> GuardarAsync(long? id, GuardarCompraSolicitud solicitud,
        ContextoConfiguracion seguridad, CancellationToken token) => EscribirAsync(seguridad, async () =>
    {
        Validar(solicitud);
        if (solicitud.SucursalId != seguridad.SucursalId) throw new RecursoComprasNoEncontradoException("La sucursal no pertenece al contexto actual.");
        if (solicitud.FechaEmision == default || solicitud.FechaEntregaEsperada < solicitud.FechaEmision)
            throw new ValidacionComprasException("Las fechas de la orden no son válidas.");
        Precision(solicitud.TipoCambio, 8, 99999999999.99999999m, "Tipo de cambio", true);
        await ValidarProveedorAsync(solicitud.ProveedorId, seguridad, token);
        var moneda = await Contexto.Monedas.AsNoTracking().SingleOrDefaultAsync(item => item.Id == solicitud.MonedaId
            && item.EmpresaId == seguridad.EmpresaId && item.Estado.Activo && Operativos.Contains(item.Estado.Nombre), token)
            ?? throw new ValidacionComprasException("La moneda no existe dentro de la empresa o no está activa.");
        if (await Contexto.Monedas.CountAsync(item => item.EmpresaId == seguridad.EmpresaId && item.EsMonedaBase
            && item.Estado.Activo && Operativos.Contains(item.Estado.Nombre), token) != 1)
            throw new ConflictoComprasException("Debe existir exactamente una moneda base activa configurada en la empresa.");
        if (moneda.EsMonedaBase && solicitud.TipoCambio != 1m)
            throw new ValidacionComprasException("El tipo de cambio de la moneda base debe ser uno.");
        if (solicitud.Detalles.Any(item => item is null)) throw new ValidacionComprasException("Los detalles no pueden contener líneas nulas.");
        if (moneda.Decimales > 2) throw new ConflictoComprasException("La moneda requiere más decimales que los importes decimal(12,2) de compras.");
        if (solicitud.Detalles.DistinctBy(item => (item.ProductoId, item.ProductoPresentacionId)).Count() != solicitud.Detalles.Length)
            throw new ValidacionComprasException("No repita producto y presentación dentro de la orden.");
        var entidad = id.HasValue ? await BuscarAsync(id.Value, seguridad, token) : new OrdenesCompra();
        if (id.HasValue && (entidad.EstadosFlujo.Codigo != "BORRADOR" || !entidad.EstadosFlujo.PermiteEdicion
            || await Contexto.RecepcionesCompra.AnyAsync(item => item.OrdenCompraId == id, token)))
            throw new ConflictoComprasException("Solo se pueden editar órdenes en borrador sin recepciones.");
        var lineas = new List<OrdenesCompraDetalle>();
        foreach (var fila in solicitud.Detalles)
        {
            Validar(fila); Precision(fila.Cantidad, 6, MaximoCantidad, "Cantidad", true);
            Precision(fila.PrecioUnitario, moneda.Decimales, MaximoMonetario, "Precio unitario");
            await ValidarProductoAsync(fila.ProductoId, fila.ProductoPresentacionId, seguridad, token);
            await ConvertirAsync(fila.ProductoId, fila.ProductoPresentacionId, fila.Cantidad, seguridad.EmpresaId, token);
            decimal bruto;
            try { bruto = fila.Cantidad * fila.PrecioUnitario; }
            catch (OverflowException) { throw new ValidacionComprasException("El importe de línea excede la capacidad admitida."); }
            if (bruto > MaximoMonetario) throw new ValidacionComprasException("El importe de línea excede decimal(12,2).");
            var descuento = Math.Min(bruto, decimal.Round(bruto * fila.PorcentajeDescuento / 100m, moneda.Decimales, MidpointRounding.AwayFromZero));
            // SQL exige descuento monetario a dos decimales. No se oculta pérdida de precisión.
            if (descuento != Dinero(descuento)) throw new ValidacionComprasException("El descuento no puede representarse con dos decimales.");
            var fecha = DateOnly.FromDateTime(solicitud.FechaEmision);
            var impuestos = await Contexto.ProductoImpuestos.AsNoTracking().Where(item => item.ProductoId == fila.ProductoId
                && item.Estado.Activo && Operativos.Contains(item.Estado.Nombre) && item.Impuesto.EmpresaId == seguridad.EmpresaId
                && item.Impuesto.Estado.Activo && Operativos.Contains(item.Impuesto.Estado.Nombre)
                && item.Impuesto.FechaVigenciaDesde <= fecha && (item.Impuesto.FechaVigenciaHasta == null || item.Impuesto.FechaVigenciaHasta >= fecha))
                .Select(item => new { item.Impuesto.Porcentaje, item.Impuesto.IncluidoEnPrecio }).ToArrayAsync(token);
            if (impuestos.Any(item => item.IncluidoEnPrecio))
                throw new ConflictoComprasException("Los impuestos incluidos en precio requieren una política fiscal de compras aprobada; no se reinterpretará el precio negociado.");
            var impuesto = decimal.Round(impuestos.Sum(item => (bruto - descuento) * item.Porcentaje / 100m), moneda.Decimales, MidpointRounding.AwayFromZero);
            Precision(impuesto, 2, MaximoMonetario, "Impuesto");
            lineas.Add(new OrdenesCompraDetalle { NumeroLinea = lineas.Count + 1, ProductoId = fila.ProductoId,
                ProductoPresentacionId = fila.ProductoPresentacionId, Cantidad = fila.Cantidad, PrecioUnitario = fila.PrecioUnitario,
                Descuento = descuento, Impuesto = impuesto });
        }
        var subtotal = decimal.Round(lineas.Sum(item => item.Cantidad * item.PrecioUnitario - item.Descuento), moneda.Decimales, MidpointRounding.AwayFromZero);
        var impuestoTotal = lineas.Sum(item => item.Impuesto);
        var total = Dinero(subtotal + impuestoTotal);
        Precision(subtotal, 2, MaximoMonetario, "Subtotal"); Precision(impuestoTotal, 2, MaximoMonetario, "Impuestos");
        Precision(total, 2, MaximoMonetario, "Total");
        if (!id.HasValue)
        {
            var estado = await EstadoAsync(FlujoOrden, "BORRADOR", token);
            var documento = await DocumentoAsync("OC", seguridad, token);
            entidad.EmpresaId = seguridad.EmpresaId; entidad.SucursalId = seguridad.SucursalId;
            entidad.NumeroOrden = documento.Texto; entidad.FlujoEstadoId = estado.FlujoEstadoId; entidad.EstadoId = estado.Id;
            entidad.UsuarioCreacionId = seguridad.UsuarioId;
            Contexto.OrdenesCompra.Add(entidad);
        }
        else
        {
            var anteriores = await Contexto.OrdenesCompraDetalle.Where(item => item.OrdenCompraId == id).ToArrayAsync(token);
            Contexto.OrdenesCompraDetalle.RemoveRange(anteriores);
            await Contexto.SaveChangesAsync(token);
        }
        entidad.ProveedorId = solicitud.ProveedorId; entidad.FechaEmision = solicitud.FechaEmision;
        entidad.EsCredito = solicitud.EsCredito ?? (id.HasValue ? entidad.EsCredito : false);
        entidad.FechaEntregaEsperada = solicitud.FechaEntregaEsperada; entidad.MonedaId = solicitud.MonedaId;
        entidad.TipoCambio = solicitud.TipoCambio; entidad.SubTotal = subtotal; entidad.Impuestos = impuestoTotal; entidad.TotalEstimado = total;
        foreach (var linea in lineas) { linea.OrdenCompra = entidad; Contexto.OrdenesCompraDetalle.Add(linea); }
        await Contexto.SaveChangesAsync(token);
        Auditar(seguridad, "OrdenesCompra", entidad.Id, id.HasValue ? "COMPRA_ACTUALIZADA" : "COMPRA_CREADA", "Orden sin aplicación de inventario.");
        return await ObtenerAsync(entidad.Id, seguridad, token);
    }, token);

    public Task<CompraConsultaDto> CambiarEstadoAsync(long id, string estado, ContextoConfiguracion seguridad,
        CancellationToken token) => EscribirAsync(seguridad, async () =>
    {
        var orden = await BuscarAsync(id, seguridad, token);
        var destino = Codigo(estado);
        if (destino == "ANULADA" && await Contexto.CuentasPorPagar.AnyAsync(x => x.FacturaProveedor.OrdenCompraId == id, token))
            throw new Heladeria.Aplicacion.Cartera.ExcepcionCartera("La compra tiene una obligación financiera; requiere conciliación previa.", "OPERACION_FINANCIERA_EXISTENTE");
        if (destino is not ("CONFIRMADA" or "ANULADA"))
            throw new ValidacionComprasException("Solo confirmación y anulación son cambios manuales; recepción controla los demás estados.");
        if (await Contexto.RecepcionesCompra.AnyAsync(item => item.OrdenCompraId == id, token))
            throw new ConflictoComprasException("Una orden con recepciones no admite anulación ni confirmación manual.");
        if (destino == "CONFIRMADA")
        {
            await ValidarMonedaDocumentoAsync(orden, token);
            await ValidarProveedorAsync(orden.ProveedorId, seguridad, token);
            var filas = await Contexto.OrdenesCompraDetalle.Where(item => item.OrdenCompraId == id).ToArrayAsync(token);
            if (filas.Length == 0) throw new ConflictoComprasException("La orden no tiene detalles.");
            foreach (var fila in filas) await ValidarProductoAsync(fila.ProductoId, fila.ProductoPresentacionId, seguridad, token);
        }
        await TransicionarAsync(orden, destino, token);
        await Contexto.SaveChangesAsync(token);
        Auditar(seguridad, "OrdenesCompra", id, destino == "CONFIRMADA" ? "COMPRA_CONFIRMADA" : "COMPRA_ANULADA", "Transición sin efecto de inventario.");
        return await ObtenerAsync(id, seguridad, token);
    }, token);

    public Task<RecepcionCompraDto> RecibirAsync(long id, CrearRecepcionSolicitud solicitud,
        ContextoConfiguracion seguridad, CancellationToken token) => EscribirAsync(seguridad, async () =>
    {
        Validar(solicitud);
        if (solicitud.ClaveOperacion == Guid.Empty) throw new ValidacionComprasException("Debe proporcionar una clave de operación UUID no vacía.");
        var orden = await BuscarAsync(id, seguridad, token);
        if (orden.EstadosFlujo.Codigo is not ("CONFIRMADA" or "PARCIALMENTE_RECIBIDA"))
            throw new ConflictoComprasException("La orden no está confirmada o no tiene cantidades pendientes de recepción.");
        var referencia = solicitud.ClaveOperacion.ToString("D");
        if (await Contexto.Movimientos.AnyAsync(item => item.EmpresaId == seguridad.EmpresaId
            && item.ModuloOrigen == "COMPRAS" && item.ReferenciaExterna == referencia, token))
            throw new ConflictoComprasException("La clave de operación ya fue aplicada. No se repetirá el inventario.");
        await ValidarProveedorAsync(orden.ProveedorId, seguridad, token);
        await ValidarMonedaDocumentoAsync(orden, token);
        if (!await Contexto.Ubicaciones.AnyAsync(item => item.Id == solicitud.UbicacionId && item.BodegaId == solicitud.BodegaId
            && item.EmpresaId == seguridad.EmpresaId && item.Bodegas.SucursalId == seguridad.SucursalId
            && item.EsRecepcion && item.Estado.Activo && Operativos.Contains(item.Estado.Nombre)
            && item.Bodegas.Estado.Activo && Operativos.Contains(item.Bodegas.Estado.Nombre), token))
            throw new RecursoComprasNoEncontradoException("La bodega/ubicación de recepción no pertenece a la sucursal actual o no está operativa.");
        if (solicitud.Detalles.Any(item => item is null)) throw new ValidacionComprasException("Los detalles no pueden contener líneas nulas.");
        if (solicitud.Detalles.DistinctBy(item => item.OrdenCompraDetalleId).Count() != solicitud.Detalles.Length)
            throw new ValidacionComprasException("No repita líneas en una recepción.");
        var lineas = await Contexto.OrdenesCompraDetalle.Where(item => item.OrdenCompraId == id).ToArrayAsync(token);
        var anteriores = await Contexto.RecepcionCompraDetalles.AsNoTracking().Where(item => item.RecepcionCompra.OrdenCompraId == id)
            .Select(item => new { item.OrdenCompraDetalleId, item.CantidadRecibida, Estado = item.RecepcionCompra.EstadosFlujo.Codigo }).ToArrayAsync(token);
        if (anteriores.Any(item => item.Estado != "APLICADA"))
            throw new ConflictoComprasException("La orden contiene recepciones externas no aplicadas; requiere revisión antes de continuar.");
        var acumuladas = anteriores.GroupBy(item => item.OrdenCompraDetalleId).ToDictionary(item => item.Key, item => item.Sum(fila => fila.CantidadRecibida));
        var preparadas = new List<(OrdenesCompraDetalle Linea, decimal Cantidad, decimal Costo)>();
        foreach (var fila in solicitud.Detalles)
        {
            Validar(fila); Precision(fila.Cantidad, 6, MaximoCantidad, "Cantidad recibida", true);
            var linea = lineas.SingleOrDefault(item => item.Id == fila.OrdenCompraDetalleId)
                ?? throw new ValidacionComprasException("La línea recibida no pertenece a la orden.");
            if (fila.Cantidad > linea.Cantidad - acumuladas.GetValueOrDefault(linea.Id))
                throw new ConflictoComprasException("La cantidad recibida supera la cantidad pendiente.");
            await ValidarProductoAsync(linea.ProductoId, linea.ProductoPresentacionId, seguridad, token);
            var conversion = await ConvertirAsync(linea.ProductoId, linea.ProductoPresentacionId, fila.Cantidad, seguridad.EmpresaId, token);
            decimal costo;
            try
            {
                var sinRedondear = ((linea.Cantidad * linea.PrecioUnitario - linea.Descuento) / linea.Cantidad)
                    * orden.TipoCambio / conversion.Factor;
                costo = decimal.Round(sinRedondear, 6, MidpointRounding.AwayFromZero);
                if (sinRedondear > 0 && costo == 0)
                    throw new ValidacionComprasException("El costo positivo convertido no se puede representar en decimal(19,6); no se recibirá como costo cero.");
            }
            catch (OverflowException) { throw new ValidacionComprasException("El costo base convertido excede la capacidad admitida."); }
            if (linea.Descuento < 0 || linea.Descuento > linea.Cantidad * linea.PrecioUnitario || linea.Impuesto < 0)
                throw new ConflictoComprasException("La línea contiene importes externos incoherentes; requiere revisión.");
            Precision(costo, 6, MaximoCantidad, "Costo unitario base");
            preparadas.Add((linea, fila.Cantidad, costo));
        }
        var estado = await EstadoAsync(FlujoRecepcion, "APLICADA", token);
        var numero = await DocumentoAsync("RC", seguridad, token);
        var recepcion = new RecepcionesCompra { EmpresaId = seguridad.EmpresaId, SucursalId = seguridad.SucursalId,
            OrdenCompraId = id, NumeroRecepcion = numero.Texto, FechaRecepcion = Ahora,
            RecibidoPorUsuarioId = seguridad.UsuarioId, FlujoEstadoId = estado.FlujoEstadoId, EstadoId = estado.Id,
            Observaciones = Opcional(solicitud.Observaciones) };
        Contexto.RecepcionesCompra.Add(recepcion);
        await Contexto.SaveChangesAsync(token);
        foreach (var fila in preparadas)
        {
            Contexto.RecepcionCompraDetalles.Add(new RecepcionCompraDetalles { RecepcionCompraId = recepcion.Id,
                OrdenCompraDetalleId = fila.Linea.Id, ProductoId = fila.Linea.ProductoId,
                CantidadRecibida = fila.Cantidad, CostoUnitario = fila.Costo });
            var documentoInventario = await DocumentoAsync("IC", seguridad, token);
            try
            {
                await inventario.AplicarMovimientoAsync(new AjusteInventarioSolicitud
                {
                    SucursalId = seguridad.SucursalId, BodegaId = solicitud.BodegaId, UbicacionId = solicitud.UbicacionId,
                    ProductoId = fila.Linea.ProductoId, ProductoPresentacionId = fila.Linea.ProductoPresentacionId,
                    Tipo = "ENTRADA", Cantidad = fila.Cantidad, CostoUnitario = fila.Costo,
                    Referencia = referencia, Observaciones = solicitud.Observaciones
                }, seguridad, UnidadCostoInventario.UnidadBase, token,
                    new(recepcion.Id, documentoInventario.Serie, documentoInventario.Numero,
                        "COMPRAS", "RECEPCION-COMPRA", "ENTRADA_COMPRA", "RECEPCION_COMPRA"));
            }
            catch (ValidacionFase4Exception error) { throw new ValidacionComprasException(error.Message); }
            catch (ConflictoFase4Exception error) { throw new ConflictoComprasException(error.Message); }
            Contexto.HistoricoPreciosCompra.Add(new HistoricoPreciosCompra { EmpresaId = seguridad.EmpresaId,
                ProveedorId = orden.ProveedorId, ProductoId = fila.Linea.ProductoId, PrecioCompra = fila.Linea.PrecioUnitario,
                MonedaId = orden.MonedaId, Fecha = Ahora, RecepcionCompraId = recepcion.Id });
            Auditar(seguridad, "CostosProductos", fila.Linea.ProductoId, "COSTO_ACTUALIZADO", $"Costo asociado a recepción {recepcion.Id}.");
            acumuladas[fila.Linea.Id] = acumuladas.GetValueOrDefault(fila.Linea.Id) + fila.Cantidad;
        }
        var final = lineas.All(item => acumuladas.GetValueOrDefault(item.Id) == item.Cantidad) ? "RECIBIDA" : "PARCIALMENTE_RECIBIDA";
        if (orden.EstadosFlujo.Codigo != final) await TransicionarAsync(orden, final, token);
        Auditar(seguridad, "RecepcionesCompra", recepcion.Id, "RECEPCION_APLICADA", $"Recepción de orden {id}; operación {referencia}.");
        await Contexto.SaveChangesAsync(token);
        return await ObtenerRecepcionAsync(recepcion.Id, seguridad, token);
    }, token);

    public async Task<RecepcionCompraDto> ObtenerRecepcionAsync(long id, ContextoConfiguracion seguridad, CancellationToken token)
    {
        await ValidarSucursalAsync(seguridad, token);
        var recibo = await Contexto.RecepcionesCompra.AsNoTracking().Where(item => item.Id == id && item.EmpresaId == seguridad.EmpresaId
            && item.SucursalId == seguridad.SucursalId && item.OrdenCompra.EmpresaId == seguridad.EmpresaId
            && item.OrdenCompra.SucursalId == seguridad.SucursalId).Select(item => new
            { item.Id, item.OrdenCompraId, item.NumeroRecepcion, item.FechaRecepcion, Estado = item.EstadosFlujo.Codigo, item.Observaciones })
            .SingleOrDefaultAsync(token) ?? throw new RecursoComprasNoEncontradoException("Recepción no encontrada.");
        var detalles = await Contexto.RecepcionCompraDetalles.AsNoTracking().Where(item => item.RecepcionCompraId == id)
            .OrderBy(item => item.Id).Select(item => new RecepcionDetalleDto(item.Id, item.OrdenCompraDetalleId,
                item.ProductoId, item.CantidadRecibida, item.CostoUnitario)).ToArrayAsync(token);
        var movimientos = await Contexto.Movimientos.AsNoTracking().Where(item => item.EmpresaId == seguridad.EmpresaId
            && item.SucursalId == seguridad.SucursalId && item.ModuloOrigen == "COMPRAS" && item.DocumentoOrigenId == id)
            .OrderBy(item => item.Id).Select(item => item.Id).ToArrayAsync(token);
        return new(recibo.Id, recibo.OrdenCompraId, recibo.NumeroRecepcion, recibo.FechaRecepcion, recibo.Estado, recibo.Observaciones, detalles, movimientos);
    }

    private async Task<OrdenesCompra> BuscarAsync(long id, ContextoConfiguracion seguridad, CancellationToken token) =>
        await Alcance(seguridad).Include(item => item.EstadosFlujo).SingleOrDefaultAsync(item => item.Id == id, token)
        ?? throw new RecursoComprasNoEncontradoException("Orden de compra no encontrada.");
    private async Task ValidarProveedorAsync(int id, ContextoConfiguracion seguridad, CancellationToken token)
    {
        if (!await Contexto.Proveedores.AnyAsync(item => item.Id == id && item.EmpresaId == seguridad.EmpresaId
            && item.Estado.Activo && Operativos.Contains(item.Estado.Nombre), token))
            throw new ValidacionComprasException("El proveedor no pertenece a la empresa o no está activo.");
    }
    private async Task ValidarMonedaDocumentoAsync(OrdenesCompra orden, CancellationToken token)
    {
        if (!await Contexto.Monedas.AnyAsync(item => item.Id == orden.MonedaId && item.EmpresaId == orden.EmpresaId
            && item.Decimales <= 2 && item.Estado.Activo && Operativos.Contains(item.Estado.Nombre), token)
            || await Contexto.Monedas.CountAsync(item => item.EmpresaId == orden.EmpresaId && item.EsMonedaBase
                && item.Estado.Activo && Operativos.Contains(item.Estado.Nombre), token) != 1)
            throw new ConflictoComprasException("La moneda del documento o la moneda base no están configuradas/operativas.");
    }
    private async Task ValidarProductoAsync(long id, long? presentacion, ContextoConfiguracion seguridad, CancellationToken token)
    {
        var valido = await Contexto.Productos.AnyAsync(item => item.Id == id && item.EmpresaId == seguridad.EmpresaId
            && item.EsComprable && item.Estado.Activo && Operativos.Contains(item.Estado.Nombre)
            && item.TiposProductos.EmpresaId == seguridad.EmpresaId && item.TiposProductos.ManejaExistencia
            && item.TiposProductos.Estado.Activo && Operativos.Contains(item.TiposProductos.Estado.Nombre)
            && item.UnidadesMedida.Estado.Activo && Operativos.Contains(item.UnidadesMedida.Estado.Nombre), token);
        if (!valido) throw new ValidacionComprasException("El producto no es comprable/inventariable, está inactivo o no pertenece a la empresa.");
        if (!await Contexto.Productos.AnyAsync(item => item.Id == id && !item.ControlaVencimiento
            && item.TipoSeguimiento.Codigo == "NINGUNO" && item.TipoSeguimiento.ClaseCatalogo.Codigo == "TIPO_SEGUIMIENTO_PRODUCTO"
            && (item.TipoSeguimiento.EmpresaId == null || item.TipoSeguimiento.EmpresaId == seguridad.EmpresaId)
            && item.TipoSeguimiento.ClaseCatalogo.Estado.Activo && Operativos.Contains(item.TipoSeguimiento.ClaseCatalogo.Estado.Nombre)
            && item.TipoSeguimiento.Estado.Activo && Operativos.Contains(item.TipoSeguimiento.Estado.Nombre)
            && item.MetodoValuacion.Codigo == "PROMEDIO" && item.MetodoValuacion.ClaseCatalogo.Codigo == "METODO_VALUACION_INVENTARIO"
            && (item.MetodoValuacion.EmpresaId == null || item.MetodoValuacion.EmpresaId == seguridad.EmpresaId)
            && item.MetodoValuacion.ClaseCatalogo.Estado.Activo && Operativos.Contains(item.MetodoValuacion.ClaseCatalogo.Estado.Nombre)
            && item.MetodoValuacion.Estado.Activo && Operativos.Contains(item.MetodoValuacion.Estado.Nombre), token))
            throw new ConflictoComprasException("Esta fase recibe únicamente productos sin lotes/series/vencimiento con valuación PROMEDIO.");
        if (presentacion.HasValue && !await Contexto.ProductoPresentaciones.AnyAsync(item => item.Id == presentacion && item.ProductoId == id
            && item.EmpresaId == seguridad.EmpresaId && item.PermiteCompra && item.Estado.Activo && Operativos.Contains(item.Estado.Nombre)
            && item.Presentaciones.Estado.Activo && Operativos.Contains(item.Presentaciones.Estado.Nombre), token))
            throw new ValidacionComprasException("La presentación no pertenece al producto o no está habilitada para compra.");
    }
    private async Task<ConversionInventario> ConvertirAsync(long producto, long? presentacion, decimal cantidad, int empresa, CancellationToken token)
    {
        try { return await inventario.ConvertirCantidadAsync(producto, presentacion, cantidad, empresa, token); }
        catch (ValidacionFase4Exception error) { throw new ValidacionComprasException(error.Message); }
    }
    private async Task<EstadosFlujo> EstadoAsync(string flujo, string codigo, CancellationToken token) =>
        await Contexto.EstadosFlujo.AsNoTracking().SingleOrDefaultAsync(item => item.FlujoEstado.Codigo == flujo && item.Codigo == codigo
            && item.FlujoEstado.Estado.Activo && Operativos.Contains(item.FlujoEstado.Estado.Nombre)
            && item.Estado.Activo && Operativos.Contains(item.Estado.Nombre), token)
        ?? throw new ConflictoComprasException("Faltan estados operativos de Fase 5; ejecute la preparación explícita.");
    private async Task TransicionarAsync(OrdenesCompra orden, string destino, CancellationToken token)
    {
        var estado = await EstadoAsync(FlujoOrden, destino, token);
        if (!await Contexto.EstadoTransiciones.AsNoTracking().AnyAsync(item => item.FlujoEstadoId == orden.FlujoEstadoId
            && item.EstadoOrigenId == orden.EstadoId && item.EstadoDestinoId == estado.Id
            && !item.RequiereComentario && item.Estado.Activo && Operativos.Contains(item.Estado.Nombre)
            && item.PermisoCodigo == (destino == "RECIBIDA" || destino == "PARCIALMENTE_RECIBIDA" ? "COMPRAS.RECIBIR" : "COMPRAS.ADMINISTRAR"), token))
            throw new ConflictoComprasException("La transición solicitada no está permitida/configurada.");
        orden.EstadoId = estado.Id;
    }
    private async Task<DocumentoReservado> DocumentoAsync(string serie, ContextoConfiguracion seguridad, CancellationToken token)
    {
        var tipoDocumento = serie switch { "OC" => "Orden de compra", "RC" => "Recepción de compra", _ => "Entrada de inventario por compra" };
        var documento = await Contexto.SeriesDocumentos.AsNoTracking().SingleOrDefaultAsync(item => item.EmpresaId == seguridad.EmpresaId
            && item.SucursalId == null && item.Serie == serie && item.TipoDocumento.ModuloId == "COMPRAS"
            && item.TipoDocumento.Nombre == tipoDocumento
            && item.TipoDocumento.ClaseTipo.Nombre == "DOCUMENTOS_COMPRA" && item.TipoDocumento.Estado.Activo
            && Operativos.Contains(item.TipoDocumento.Estado.Nombre) && item.TipoDocumento.ClaseTipo.Estado.Activo
            && Operativos.Contains(item.TipoDocumento.ClaseTipo.Estado.Nombre)
            && item.Estado.Activo && Operativos.Contains(item.Estado.Nombre), token)
            ?? throw new ConflictoComprasException($"Falta la serie {serie} preparada para la empresa.");
        var correlativo = Contexto.CorrelativosDocumentos.Local.FirstOrDefault(item => item.SerieDocumentoId == documento.Id)
            ?? await Contexto.CorrelativosDocumentos.SingleOrDefaultAsync(item => item.SerieDocumentoId == documento.Id, token)
            ?? throw new ConflictoComprasException("La serie no tiene correlativo configurado.");
        if (correlativo.UltimoNumero == long.MaxValue) throw new ConflictoComprasException("El correlativo se agotó.");
        var numero = correlativo.UltimoNumero + 1;
        var digitos = numero.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var texto = (documento.Prefijo ?? documento.Serie + "-") + digitos.PadLeft(documento.Longitud, '0');
        if (digitos.Length > documento.Longitud || texto.Length > 30)
            throw new ConflictoComprasException("El correlativo excede la longitud documental configurada.");
        correlativo.UltimoNumero = numero; correlativo.FechaModificacion = Ahora;
        return new(documento.Serie, numero, texto);
    }
    private sealed record DocumentoReservado(string Serie, long Numero, string Texto);
}
