using Heladeria.Aplicacion.Configuracion;
using Heladeria.Aplicacion.Inventario;
using Heladeria.Aplicacion.Productos;
using Heladeria.Aplicacion.Produccion;
using Heladeria.Aplicacion.Tiempo;
using Heladeria.Aplicacion.Seguridad.Autorizacion;
using Heladeria.Aplicacion.Seguridad.Configuracion;
using Heladeria.Infraestructura.Inventario;
using Heladeria.Infraestructura.Persistencia;
using Heladeria.Infraestructura.Persistencia.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Heladeria.Infraestructura.Produccion;

public sealed class ServicioProduccion(
    HeladeriaDbContext contexto,
    IOptions<OpcionesSeguridad> opciones,
    TimeProvider reloj,
    CoordinadorAjustesInventario coordinador,
    ServicioInventario inventario)
    : ServicioProduccionBase(contexto, opciones, reloj, coordinador, inventario), IServicioProduccion
{
    internal const string FlujoOrden = "ORDENES_PRODUCCION";

    private DateTime FechaUtc(DateTime valor, ContextoConfiguracion s)
    {
        try { return TiempoEmpresarial.AUTC(valor, TiempoEmpresarial.Zona(ConfiguracionTiempo, s.EmpresaId, s.SucursalId)); }
        catch (ArgumentException ex) { throw new ValidacionProduccionException(ex.Message); }
    }

    private IQueryable<OrdenesProduccion> Alcance(ContextoConfiguracion seguridad) =>
        Contexto.OrdenesProduccion.Where(item => item.EmpresaId == seguridad.EmpresaId
            && item.SucursalId == seguridad.SucursalId);

    public async Task<ReferenciasProduccionDto> ObtenerReferenciasAsync(
        ContextoConfiguracion seguridad, CancellationToken token)
    {
        await ValidarSucursalAsync(seguridad, token);
        var hoy = DateOnly.FromDateTime(Ahora);
        var estados = await Contexto.EstadosFlujo.AsNoTracking()
            .Where(item => item.FlujoEstado.Codigo == FlujoOrden
                && item.Estado.Activo && Operativos.Contains(item.Estado.Nombre)
                && item.FlujoEstado.Estado.Activo && Operativos.Contains(item.FlujoEstado.Estado.Nombre))
            .OrderBy(item => item.Orden)
            .Select(item => new EstadoProduccionDto(
                item.Id, item.Codigo, item.Nombre, item.PermiteEdicion, item.EsFinal))
            .ToArrayAsync(token);
        var recetas = await Contexto.Recetas.AsNoTracking()
            .Where(item => item.EmpresaId == seguridad.EmpresaId && item.VigenteHasta == null
                && item.VigenteDesde <= hoy && item.Estado.Activo && Operativos.Contains(item.Estado.Nombre)
                && item.ProductoTerminado.Estado.Activo && Operativos.Contains(item.ProductoTerminado.Estado.Nombre))
            .OrderBy(item => item.ProductoTerminado.Nombre)
            .Select(item => new RecetaVigenteDto(
                item.Id, item.ProductoTerminadoId, item.ProductoTerminado.Nombre,
                item.Codigo, item.Nombre, item.Version, item.RendimientoBase,
                item.ProductoTerminado.UnidadesMedida.Nombre))
            .ToArrayAsync(token);
        var almacenes = await Contexto.Ubicaciones.AsNoTracking()
            .Where(item => item.EmpresaId == seguridad.EmpresaId
                && item.Bodegas.SucursalId == seguridad.SucursalId
                && item.Estado.Activo && Operativos.Contains(item.Estado.Nombre)
                && item.Bodegas.Estado.Activo && Operativos.Contains(item.Bodegas.Estado.Nombre))
            .OrderByDescending(item => item.Bodegas.EsPrincipal)
            .ThenBy(item => item.Bodegas.Nombre).ThenBy(item => item.Nombre)
            .Select(item => new AlmacenProduccionDto(
                item.BodegaId, item.Bodegas.Nombre, item.Id, item.Nombre))
            .ToArrayAsync(token);
        return new(estados, recetas, almacenes);
    }

    public async Task<RespuestaPaginada<OrdenProduccionResumenDto>> ListarAsync(
        FiltroProduccionSolicitud filtro, ContextoConfiguracion seguridad, CancellationToken token)
    {
        Validar(filtro);
        await ValidarSucursalAsync(seguridad, token);
        var fechaDesde = filtro.FechaDesde is { } d ? FechaUtc(d, seguridad) : (DateTime?)null;
        var fechaHasta = filtro.FechaHasta is { } h ? FechaUtc(h, seguridad) : (DateTime?)null;
        if (fechaDesde > fechaHasta)
            throw new ValidacionProduccionException("El intervalo de fechas no es válido.");
        var consulta = Alcance(seguridad).AsNoTracking();
        if (filtro.ProductoId is { } producto)
            consulta = consulta.Where(item => item.ProductoAProducirId == producto);
        if (filtro.RecetaId is { } receta)
            consulta = consulta.Where(item => item.RecetaId == receta);
        if (filtro.EstadoId is { } estado)
            consulta = consulta.Where(item => item.EstadoId == estado);
        if (fechaDesde is { } desde)
            consulta = consulta.Where(item => item.FechaInicio >= desde);
        if (fechaHasta is { } hasta)
            consulta = consulta.Where(item => item.FechaInicio <= hasta);
        if (Opcional(filtro.Busqueda)?.ToUpperInvariant() is { } texto)
            consulta = consulta.Where(item => item.NumeroOrden.ToUpper().Contains(texto)
                || item.ProductoAProducir.Nombre.ToUpper().Contains(texto)
                || item.Receta.Nombre.ToUpper().Contains(texto));
        var total = await consulta.CountAsync(token);
        var elementos = await ProyectarResumen(Paginar(
                consulta.OrderByDescending(item => item.FechaInicio).ThenByDescending(item => item.Id), filtro))
            .ToArrayAsync(token);
        return new(elementos, filtro.Pagina, filtro.TamanioPagina, total);
    }

    public async Task<OrdenProduccionConsultaDto> ObtenerAsync(
        long id, ContextoConfiguracion seguridad, CancellationToken token)
    {
        await ValidarSucursalAsync(seguridad, token);
        var encabezado = await ProyectarResumen(Alcance(seguridad).AsNoTracking().Where(item => item.Id == id))
            .SingleOrDefaultAsync(token)
            ?? throw new RecursoProduccionNoEncontradoException("Orden de producción no encontrada.");
        var detalles = await Contexto.OrdenProduccionDetalles.AsNoTracking()
            .Where(item => item.OrdenProduccionId == id)
            .OrderBy(item => item.Id)
            .Select(item => new OrdenProduccionDetalleDto(
                item.Id, item.ProductoInsumoId, item.ProductoInsumo.Nombre,
                item.ProductoPresentacionId,
                item.ProductoPresentacion == null ? null : item.ProductoPresentacion.Presentaciones.Nombre,
                item.UnidadMedidaId, item.UnidadMedida.Nombre, item.UnidadMedida.Abreviatura,
                item.CantidadPlanificada, item.FactorConversion,
                item.CantidadPlanificadaBase, item.CostoPlanificado)
            { AbreviaturaUnidadBase = item.ProductoInsumo.UnidadesMedida.Abreviatura })
            .ToArrayAsync(token);
        var consumos = await Contexto.ConsumosProduccion.AsNoTracking()
            .Where(item => item.OrdenProduccionId == id)
            .OrderBy(item => item.Id)
            .Select(item => new ConsumoProduccionDto(
                item.Id, item.ProductoId, item.Producto.Nombre,
                item.UnidadMedidaId, item.UnidadMedida.Nombre,
                item.CantidadConsumida, item.CostoUnitario,
                item.CantidadConsumida * item.CostoUnitario, item.FechaConsumo))
            .ToArrayAsync(token);
        var terminada = await Contexto.ProduccionTerminada.AsNoTracking()
            .Where(item => item.OrdenProduccionId == id)
            .Select(item => new ProduccionTerminadaDto(
                item.Id, item.ProductoId, item.Producto.Nombre, item.CantidadProducida,
                item.CostoMateriaPrima, item.CostoManoObra, item.CostoIndirecto,
                item.CostoTotal ?? item.CostoMateriaPrima + item.CostoManoObra + item.CostoIndirecto,
                item.CantidadProducida == 0 ? 0 : item.CostoMateriaPrima / item.CantidadProducida,
                item.FechaFinalizacion))
            .SingleOrDefaultAsync(token);
        var movimientos = await Contexto.Movimientos.AsNoTracking()
            .Where(item => item.EmpresaId == seguridad.EmpresaId
                && item.SucursalId == seguridad.SucursalId
                && item.ModuloOrigen == "PRODUCCION" && item.DocumentoOrigenId == id)
            .OrderBy(item => item.Id).Select(item => item.Id).ToArrayAsync(token);
        return new(encabezado, detalles, consumos, terminada, movimientos);
    }

    public Task<OrdenProduccionConsultaDto> CrearAsync(
        CrearOrdenProduccionSolicitud solicitud, ContextoConfiguracion seguridad, CancellationToken token) =>
        GuardarAsync(null, solicitud, null, seguridad, token);

    public Task<OrdenProduccionConsultaDto> ActualizarAsync(
        long id, ActualizarOrdenProduccionSolicitud solicitud,
        ContextoConfiguracion seguridad, CancellationToken token) =>
        GuardarAsync(id, solicitud, solicitud.VersionFila, seguridad, token);

    private Task<OrdenProduccionConsultaDto> GuardarAsync(
        long? id, GuardarOrdenProduccionSolicitudBase solicitud, string? version,
        ContextoConfiguracion seguridad, CancellationToken token) =>
        EscribirAsync(seguridad, async () =>
        {
            Validar(solicitud);
            if (solicitud.SucursalId != seguridad.SucursalId)
                throw new RecursoProduccionNoEncontradoException("La sucursal no pertenece al contexto actual.");
            if (solicitud.FechaInicio == default)
                throw new ValidacionProduccionException("FechaInicio es obligatoria.");
            Precision(solicitud.CantidadPlanificada, "Cantidad planificada");
            await ValidarBodegaAsync(solicitud.BodegaProduccionId, seguridad, token);
            var receta = await CargarRecetaVigenteAsync(solicitud.RecetaId, seguridad, token);
            await ValidarProductoAsync(receta.ProductoTerminadoId, seguridad, token);
            foreach (var ingrediente in receta.RecetaDetalles)
                await ValidarProductoAsync(ingrediente.ProductoInsumoId, seguridad, token);
            await ValidarPresentacionAsync(receta.ProductoTerminadoId,
                solicitud.ProductoPresentacionId, seguridad, token);
            var conversion = await ConvertirAsync(receta.ProductoTerminadoId,
                solicitud.ProductoPresentacionId, solicitud.CantidadPlanificada, seguridad, token);
            var detalles = PrepararDetalles(receta, conversion.CantidadBase);

            OrdenesProduccion orden;
            if (!id.HasValue)
            {
                var estado = await EstadoFlujoAsync("BORRADOR", token);
                var documento = await DocumentoAsync("OP", seguridad, token);
                orden = new OrdenesProduccion
                {
                    EmpresaId = seguridad.EmpresaId,
                    SucursalId = seguridad.SucursalId,
                    NumeroOrden = documento.Texto,
                    FlujoEstadoId = estado.FlujoEstadoId,
                    EstadoId = estado.Id,
                    ResponsableUsuarioId = seguridad.UsuarioId,
                    VersionFila = []
                };
                Contexto.OrdenesProduccion.Add(orden);
            }
            else
            {
                orden = await BuscarOrdenAsync(id.Value, seguridad, token);
                ValidarVersion(orden.VersionFila, version!);
                if (orden.EstadosFlujo.Codigo != "BORRADOR" || !orden.EstadosFlujo.PermiteEdicion)
                    throw new ConflictoProduccionException("Solo se pueden editar órdenes en borrador.");
                if (await Contexto.Movimientos.AnyAsync(item => item.ModuloOrigen == "PRODUCCION"
                    && item.DocumentoOrigenId == orden.Id, token))
                    throw new ConflictoProduccionException("La orden ya tiene movimientos y no se puede editar.");
                var anteriores = await Contexto.OrdenProduccionDetalles
                    .Where(item => item.OrdenProduccionId == orden.Id).ToArrayAsync(token);
                Contexto.OrdenProduccionDetalles.RemoveRange(anteriores);
                await Contexto.SaveChangesAsync(token);
            }

            orden.BodegaProduccionId = solicitud.BodegaProduccionId;
            orden.ProductoAProducirId = receta.ProductoTerminadoId;
            orden.RecetaId = receta.Id;
            orden.ProductoPresentacionId = solicitud.ProductoPresentacionId;
            orden.CantidadPlanificada = solicitud.CantidadPlanificada;
            orden.FactorConversion = conversion.Factor;
            orden.CantidadPlanificadaBase = conversion.CantidadBase;
            // Fecha planificada (editable), no el timestamp autoritativo de consumo/finalización/auditoría.
            orden.FechaInicio = FechaUtc(solicitud.FechaInicio, seguridad);
            orden.FechaFin = null;
            orden.CantidadProducida = null;
            await Contexto.SaveChangesAsync(token);
            foreach (var fila in detalles)
                Contexto.OrdenProduccionDetalles.Add(new OrdenProduccionDetalles
                {
                    OrdenProduccionId = orden.Id,
                    ProductoInsumoId = fila.ProductoId,
                    ProductoPresentacionId = fila.ProductoPresentacionId,
                    UnidadMedidaId = fila.UnidadMedidaId,
                    CantidadPlanificada = fila.Cantidad,
                    FactorConversion = fila.Factor,
                    CantidadPlanificadaBase = fila.CantidadBase,
                    CostoPlanificado = null
                });
            await Contexto.SaveChangesAsync(token);
            Auditar(seguridad, "OrdenesProduccion", orden.Id,
                id.HasValue ? "ORDEN_PRODUCCION_ACTUALIZADA" : "ORDEN_PRODUCCION_CREADA",
                $"Orden {orden.NumeroOrden}; receta {receta.Id} v{receta.Version}; sin afectación de inventario.",
                "PRODUCCION");
            await Contexto.SaveChangesAsync(token);
            return await ObtenerAsync(orden.Id, seguridad, token);
        }, token);

    public Task<OrdenProduccionConsultaDto> ConfirmarAsync(
        long id, CambiarEstadoProduccionSolicitud solicitud,
        ContextoConfiguracion seguridad, CancellationToken token) =>
        EscribirAsync(seguridad, async () =>
        {
            Validar(solicitud);
            var orden = await BuscarOrdenAsync(id, seguridad, token);
            ValidarVersion(orden.VersionFila, solicitud.VersionFila);
            if (orden.EstadosFlujo.Codigo != "BORRADOR")
                throw new ConflictoProduccionException("Solo una orden en borrador puede confirmarse.");
            var receta = await CargarRecetaVigenteAsync(orden.RecetaId, seguridad, token);
            if (receta.ProductoTerminadoId != orden.ProductoAProducirId)
                throw new ConflictoProduccionException("La receta vigente ya no corresponde al producto de la orden.");
            await ValidarPresentacionAsync(orden.ProductoAProducirId,
                orden.ProductoPresentacionId, seguridad, token);
            await ValidarBodegaAsync(orden.BodegaProduccionId, seguridad, token);
            var ingredientes = await Contexto.OrdenProduccionDetalles.AsNoTracking()
                .Where(item => item.OrdenProduccionId == id)
                .Select(item => item.ProductoInsumoId).ToArrayAsync(token);
            if (ingredientes.Length == 0)
                throw new ConflictoProduccionException("La orden no contiene ingredientes.");
            foreach (var ingrediente in ingredientes)
                await ValidarProductoAsync(ingrediente, seguridad, token);
            await TransicionarAsync(orden, "CONFIRMADA", PermisosSistema.ProduccionAdministrar, token);
            Auditar(seguridad, "OrdenesProduccion", id, "ORDEN_PRODUCCION_CONFIRMADA",
                "Confirmación sin afectación física de inventario.", "PRODUCCION");
            await Contexto.SaveChangesAsync(token);
            return await ObtenerAsync(id, seguridad, token);
        }, token);

    public Task<OrdenProduccionConsultaDto> AnularAsync(
        long id, CambiarEstadoProduccionSolicitud solicitud,
        ContextoConfiguracion seguridad, CancellationToken token) =>
        EscribirAsync(seguridad, async () =>
        {
            Validar(solicitud);
            var orden = await BuscarOrdenAsync(id, seguridad, token);
            ValidarVersion(orden.VersionFila, solicitud.VersionFila);
            if (orden.EstadosFlujo.Codigo is not ("BORRADOR" or "CONFIRMADA"))
                throw new ConflictoProduccionException("La orden ya no admite anulación.");
            if (await Contexto.Movimientos.AnyAsync(item => item.ModuloOrigen == "PRODUCCION"
                    && item.DocumentoOrigenId == id, token)
                || await Contexto.ConsumosProduccion.AnyAsync(item => item.OrdenProduccionId == id, token)
                || await Contexto.ProduccionTerminada.AnyAsync(item => item.OrdenProduccionId == id, token))
                throw new ConflictoProduccionException("Una orden con afectación de inventario no puede anularse.");
            await TransicionarAsync(orden, "ANULADA", PermisosSistema.ProduccionAdministrar, token);
            Auditar(seguridad, "OrdenesProduccion", id, "ORDEN_PRODUCCION_ANULADA",
                "Anulación sin afectación de inventario.", "PRODUCCION");
            await Contexto.SaveChangesAsync(token);
            return await ObtenerAsync(id, seguridad, token);
        }, token);

    public Task<OrdenProduccionConsultaDto> FinalizarAsync(
        long id, FinalizarProduccionSolicitud solicitud,
        ContextoConfiguracion seguridad, CancellationToken token) =>
        EscribirAsync(seguridad, async () =>
        {
            Validar(solicitud);
            if (solicitud.ClaveOperacion == Guid.Empty)
                throw new ValidacionProduccionException("Debe indicar una ClaveOperacion UUID no vacía.");
            Precision(solicitud.CantidadProducida, "Cantidad producida");
            var orden = await BuscarOrdenAsync(id, seguridad, token);
            ValidarVersion(orden.VersionFila, solicitud.VersionFila);
            if (orden.EstadosFlujo.Codigo != "CONFIRMADA")
                throw new ConflictoProduccionException("Solo una orden confirmada puede finalizarse.");
            if (await Contexto.ProduccionTerminada.AnyAsync(item => item.OrdenProduccionId == id, token))
                throw new ConflictoProduccionException("La producción ya fue finalizada; no se repetirá el inventario.");
            var referencia = solicitud.ClaveOperacion.ToString("D");
            if (await Contexto.Movimientos.AnyAsync(item => item.EmpresaId == seguridad.EmpresaId
                && item.ModuloOrigen == "PRODUCCION" && item.ReferenciaExterna == referencia, token))
                throw new ConflictoProduccionException("La clave de operación ya fue aplicada; no se repetirá el inventario.");
            await ValidarUbicacionAsync(orden, solicitud.UbicacionId, seguridad, token);
            await ValidarProductoAsync(orden.ProductoAProducirId, seguridad, token);
            var cantidadBase = Multiplicar(solicitud.CantidadProducida,
                orden.FactorConversion, "Cantidad producida base");
            if (orden.CantidadPlanificadaBase <= 0)
                throw new ConflictoProduccionException("La orden no contiene una cantidad base planificada válida.");
            var factorEscala = cantidadBase / orden.CantidadPlanificadaBase;
            if (factorEscala <= 0)
                throw new ValidacionProduccionException("La escala de producción debe ser positiva.");

            var detalles = await Contexto.OrdenProduccionDetalles.AsNoTracking()
                .Where(item => item.OrdenProduccionId == id).OrderBy(item => item.ProductoInsumoId)
                .Select(item => new { item.ProductoInsumoId, item.CantidadPlanificadaBase, item.ProductoInsumo.UnidadBaseId })
                .ToArrayAsync(token);
            if (detalles.Length == 0)
                throw new ConflictoProduccionException("La orden no contiene ingredientes.");
            var consumos = new List<ConsumoPreparado>();
            foreach (var detalle in detalles)
            {
                await ValidarProductoAsync(detalle.ProductoInsumoId, seguridad, token);
                var cantidad = decimal.Round(detalle.CantidadPlanificadaBase * factorEscala, 6,
                    MidpointRounding.AwayFromZero);
                Precision(cantidad, "Cantidad real de ingrediente");
                var existencia = await Contexto.Existencias.AsNoTracking().SingleOrDefaultAsync(item =>
                    item.EmpresaId == seguridad.EmpresaId
                    && item.ProductoId == detalle.ProductoInsumoId
                    && item.BodegaId == orden.BodegaProduccionId
                    && item.UbicacionId == solicitud.UbicacionId, token);
                var disponible = existencia is null ? 0m : existencia.Cantidad - existencia.CantidadReservada;
                if (disponible < cantidad)
                    throw new ExistenciaProduccionInsuficienteException(
                        $"Existencia insuficiente para el ingrediente {detalle.ProductoInsumoId}. Disponible: {disponible}; requerido: {cantidad}.");
                var total = decimal.Round(cantidad * (existencia?.CostoPromedio ?? 0m), 6,
                    MidpointRounding.AwayFromZero);
                Precision(total, "Costo del ingrediente", positivo: false);
                consumos.Add(new(detalle.ProductoInsumoId, detalle.UnidadBaseId,
                    cantidad, existencia?.CostoPromedio ?? 0m, total));
            }
            var costoMateriaPrima = consumos.Sum(item => item.CostoTotal);
            Precision(costoMateriaPrima, "Costo total de materia prima", positivo: false);
            var costoUnitarioBase = decimal.Round(costoMateriaPrima / cantidadBase, 6,
                MidpointRounding.AwayFromZero);
            if (costoMateriaPrima > 0 && costoUnitarioBase == 0)
                throw new ValidacionProduccionException(
                    "El costo unitario base positivo no se puede representar en decimal(19,6).");
            Precision(costoUnitarioBase, "Costo unitario base", positivo: false);

            foreach (var consumo in consumos)
            {
                var documento = await DocumentoAsync("PC", seguridad, token);
                try
                {
                    await Inventario.AplicarMovimientoAsync(new AjusteInventarioSolicitud
                    {
                        SucursalId = orden.SucursalId,
                        BodegaId = orden.BodegaProduccionId,
                        UbicacionId = solicitud.UbicacionId,
                        ProductoId = consumo.ProductoId,
                        ProductoPresentacionId = null,
                        Tipo = "SALIDA",
                        Cantidad = consumo.CantidadBase,
                        Referencia = referencia,
                        Observaciones = $"Consumo de {orden.NumeroOrden}"
                    }, seguridad, UnidadCostoInventario.UnidadBase, token,
                        new OrigenMovimientoInventario(orden.Id, documento.Serie, documento.Numero,
                            "PRODUCCION", "PRODUCCION-CONSUMO",
                            "CONSUMO_PRODUCCION", "PRODUCCION"));
                }
                catch (ExistenciaInsuficienteException error)
                {
                    throw new ExistenciaProduccionInsuficienteException(error.Message);
                }
                catch (ValidacionFase4Exception error)
                {
                    throw new ValidacionProduccionException(error.Message);
                }
                catch (ConflictoFase4Exception error)
                {
                    throw new ConflictoProduccionException(error.Message);
                }
                Contexto.ConsumosProduccion.Add(new ConsumosProduccion
                {
                    OrdenProduccionId = orden.Id,
                    ProductoId = consumo.ProductoId,
                    LoteId = null,
                    UnidadMedidaId = consumo.UnidadBaseId,
                    CantidadConsumida = consumo.CantidadBase,
                    CostoUnitario = consumo.CostoUnitario,
                    FechaConsumo = Ahora
                });
                Auditar(seguridad, "ConsumosProduccion", orden.Id, "CONSUMO_PRODUCCION_REGISTRADO",
                    $"Producto {consumo.ProductoId}: {consumo.CantidadBase} unidades base.", "PRODUCCION");
            }

            var entrada = await DocumentoAsync("PT", seguridad, token);
            try
            {
                await Inventario.AplicarMovimientoAsync(new AjusteInventarioSolicitud
                {
                    SucursalId = orden.SucursalId,
                    BodegaId = orden.BodegaProduccionId,
                    UbicacionId = solicitud.UbicacionId,
                    ProductoId = orden.ProductoAProducirId,
                    ProductoPresentacionId = null,
                    Tipo = "ENTRADA",
                    Cantidad = cantidadBase,
                    CostoUnitario = costoUnitarioBase,
                    Referencia = referencia,
                    Observaciones = $"Producto terminado de {orden.NumeroOrden}"
                }, seguridad, UnidadCostoInventario.UnidadBase, token,
                    new OrigenMovimientoInventario(orden.Id, entrada.Serie, entrada.Numero,
                        "PRODUCCION", "PRODUCCION-ENTRADA",
                        "ENTRADA_PRODUCCION", "PRODUCCION"));
            }
            catch (ValidacionFase4Exception error)
            {
                throw new ValidacionProduccionException(error.Message);
            }
            catch (ConflictoFase4Exception error)
            {
                throw new ConflictoProduccionException(error.Message);
            }

            Contexto.ProduccionTerminada.Add(new ProduccionTerminada
            {
                OrdenProduccionId = orden.Id,
                ProductoId = orden.ProductoAProducirId,
                LoteId = null,
                CantidadProducida = cantidadBase,
                CostoMateriaPrima = costoMateriaPrima,
                CostoManoObra = 0m,
                CostoIndirecto = 0m,
                FechaFinalizacion = Ahora
            });
            orden.CantidadProducida = solicitud.CantidadProducida;
            orden.FechaFin = Ahora;
            await TransicionarAsync(orden, "FINALIZADA", PermisosSistema.ProduccionProducir, token);
            Auditar(seguridad, "OrdenesProduccion", orden.Id, "PRODUCCION_FINALIZADA",
                $"Salida completa de insumos y entrada de {cantidadBase} unidades base; costo de materia prima {costoMateriaPrima}.",
                "PRODUCCION");
            await Contexto.SaveChangesAsync(token);
            return await ObtenerAsync(id, seguridad, token);
        }, token);

    private IQueryable<OrdenProduccionResumenDto> ProyectarResumen(IQueryable<OrdenesProduccion> consulta) =>
        consulta.Select(item => new OrdenProduccionResumenDto(
            item.Id, item.EmpresaId, item.SucursalId, item.Sucursal.Nombre,
            item.BodegaProduccionId, item.BodegaProduccion.Nombre,
            item.ProductoAProducirId, item.ProductoAProducir.Nombre,
            item.RecetaId, item.Receta.Nombre, item.Receta.Version, item.NumeroOrden,
            item.CantidadPlanificada, item.CantidadPlanificadaBase, item.CantidadProducida,
            item.ProductoPresentacionId,
            item.ProductoPresentacion == null
                ? item.ProductoAProducir.UnidadesMedida.Nombre
                : item.ProductoPresentacion.UnidadesMedida.Nombre,
            item.ProductoPresentacion == null
                ? item.ProductoAProducir.UnidadesMedida.Abreviatura
                : item.ProductoPresentacion.UnidadesMedida.Abreviatura,
            item.FactorConversion, item.FechaInicio, item.FechaFin,
            item.EstadoId, item.EstadosFlujo.Codigo, item.ResponsableUsuarioId,
            item.ResponsableUsuario.UserName, Convert.ToBase64String(item.VersionFila))
        {
            UnidadBase = item.ProductoAProducir.UnidadesMedida.Nombre,
            AbreviaturaUnidadBase = item.ProductoAProducir.UnidadesMedida.Abreviatura,
            Presentacion = item.ProductoPresentacion == null ? null : item.ProductoPresentacion.Presentaciones.Nombre
        });

    private async Task<OrdenesProduccion> BuscarOrdenAsync(
        long id, ContextoConfiguracion seguridad, CancellationToken token) =>
        await Alcance(seguridad).Include(item => item.EstadosFlujo)
            .SingleOrDefaultAsync(item => item.Id == id, token)
        ?? throw new RecursoProduccionNoEncontradoException("Orden de producción no encontrada.");

    private async Task ValidarBodegaAsync(
        long bodegaId, ContextoConfiguracion seguridad, CancellationToken token)
    {
        if (!await Contexto.Bodegas.AsNoTracking().AnyAsync(item => item.Id == bodegaId
            && item.EmpresaId == seguridad.EmpresaId && item.SucursalId == seguridad.SucursalId
            && item.Estado.Activo && Operativos.Contains(item.Estado.Nombre), token))
            throw new RecursoProduccionNoEncontradoException(
                "La bodega no pertenece a la sucursal o no está operativa.");
    }

    private async Task ValidarUbicacionAsync(
        OrdenesProduccion orden, long ubicacionId,
        ContextoConfiguracion seguridad, CancellationToken token)
    {
        if (!await Contexto.Ubicaciones.AsNoTracking().AnyAsync(item =>
            item.Id == ubicacionId && item.BodegaId == orden.BodegaProduccionId
            && item.EmpresaId == seguridad.EmpresaId
            && item.Bodegas.SucursalId == seguridad.SucursalId
            && item.Estado.Activo && Operativos.Contains(item.Estado.Nombre)
            && item.Bodegas.Estado.Activo && Operativos.Contains(item.Bodegas.Estado.Nombre), token))
            throw new RecursoProduccionNoEncontradoException(
                "La ubicación no pertenece a la bodega de producción o no está operativa.");
    }

    private async Task<Recetas> CargarRecetaVigenteAsync(
        long id, ContextoConfiguracion seguridad, CancellationToken token)
    {
        var hoy = DateOnly.FromDateTime(Ahora);
        return await Contexto.Recetas.AsNoTracking()
            .Include(item => item.RecetaDetalles)
            .SingleOrDefaultAsync(item => item.Id == id
                && item.EmpresaId == seguridad.EmpresaId
                && item.VigenteHasta == null && item.VigenteDesde <= hoy
                && item.Estado.Activo && Operativos.Contains(item.Estado.Nombre), token)
            ?? throw new ValidacionProduccionException("La receta no existe, no está vigente o no está operativa.");
    }

    private static IReadOnlyList<DetalleOrdenPreparado> PrepararDetalles(
        Recetas receta, decimal cantidadPlanificadaBase)
    {
        if (receta.RendimientoBase <= 0 || receta.RecetaDetalles.Count == 0)
            throw new ConflictoProduccionException("La receta no contiene un rendimiento o ingredientes válidos.");
        var escala = cantidadPlanificadaBase / receta.RendimientoBase;
        var detalles = new List<DetalleOrdenPreparado>();
        foreach (var fila in receta.RecetaDetalles.OrderBy(item => item.ProductoInsumoId))
        {
            var cantidadBase = decimal.Round(fila.CantidadBase * escala, 6, MidpointRounding.AwayFromZero);
            Precision(cantidadBase, "Cantidad planificada base de ingrediente");
            var cantidad = decimal.Round(cantidadBase / fila.FactorConversion, 6, MidpointRounding.AwayFromZero);
            Precision(cantidad, "Cantidad planificada de ingrediente");
            if (cantidad * fila.FactorConversion != cantidadBase)
                throw new ValidacionProduccionException(
                    "La cantidad escalada no se puede representar exactamente con la presentación del ingrediente.");
            detalles.Add(new(fila.ProductoInsumoId, fila.ProductoPresentacionId,
                fila.UnidadMedidaId, cantidad, fila.FactorConversion, cantidadBase));
        }
        return detalles;
    }

    private sealed record DetalleOrdenPreparado(
        long ProductoId, long? ProductoPresentacionId, int UnidadMedidaId,
        decimal Cantidad, decimal Factor, decimal CantidadBase);

    private sealed record ConsumoPreparado(
        long ProductoId, int UnidadBaseId, decimal CantidadBase,
        decimal CostoUnitario, decimal CostoTotal);
}
