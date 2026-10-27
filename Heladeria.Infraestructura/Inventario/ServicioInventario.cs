using System.Data;
using Heladeria.Aplicacion.Configuracion;
using Heladeria.Aplicacion.Inventario;
using Heladeria.Aplicacion.Productos;
using Heladeria.Aplicacion.Seguridad.Configuracion;
using Heladeria.Infraestructura.Persistencia;
using Heladeria.Infraestructura.Persistencia.Entidades;
using Heladeria.Infraestructura.Fase4;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;

namespace Heladeria.Infraestructura.Inventario;

public sealed class ServicioInventario(
    HeladeriaDbContext contexto,
    IAuditoriaFase4 auditoria,
    IOptions<OpcionesSeguridad> opciones,
    TimeProvider timeProvider,
    CoordinadorAjustesInventario coordinador)
    : ServicioFase4Base(contexto, auditoria, opciones, timeProvider), IServicioInventario
{
    private const string FlujoMovimientos = "MOVIMIENTOS_INVENTARIO";
    private const string EstadoAplicado = "APLICADO";
    private const string SerieAjuste = "AJUSTE";
    private const decimal MaximoDecimal19Con6 = 9999999999999.999999m;

    public async Task<IReadOnlyList<AlmacenInventarioDto>> ListarAlmacenesAsync(
        ContextoConfiguracion seguridad,
        CancellationToken token) => await Contexto.Ubicaciones.AsNoTracking()
        .Where(item => item.EmpresaId == seguridad.EmpresaId
            && item.Estado.Activo
            && EstadosOperativos.Contains(item.Estado.Nombre)
            && item.Bodegas.Estado.Activo
            && EstadosOperativos.Contains(item.Bodegas.Estado.Nombre)
            && item.Bodegas.Sucursal.Estado.Activo
            && EstadosOperativos.Contains(item.Bodegas.Sucursal.Estado.Nombre)
            && item.Bodegas.Sucursal.Empresa.Estado.Activo
            && EstadosOperativos.Contains(item.Bodegas.Sucursal.Empresa.Estado.Nombre)
            && Contexto.UsuariosSucursales.Any(asignacion =>
                asignacion.UsuarioId == seguridad.UsuarioId
                && asignacion.SucursalId == item.Bodegas.SucursalId
                && asignacion.Estado.Activo
                && EstadosOperativos.Contains(asignacion.Estado.Nombre)))
        .OrderBy(item => item.Bodegas.Sucursal.Nombre)
        .ThenByDescending(item => item.Bodegas.EsPrincipal)
        .ThenBy(item => item.Bodegas.Nombre)
        .ThenBy(item => item.Nombre)
        .Select(item => new AlmacenInventarioDto(
            item.Bodegas.SucursalId,
            item.Bodegas.Sucursal.Nombre,
            item.BodegaId,
            item.Bodegas.Nombre,
            item.Id,
            item.Nombre,
            item.Bodegas.EsPrincipal))
        .ToArrayAsync(token);

    public async Task<RespuestaPaginada<ExistenciaConsultaDto>> ListarExistenciasAsync(
        FiltroExistenciasSolicitud filtro,
        ContextoConfiguracion seguridad,
        CancellationToken token)
    {
        var sucursalId = filtro.SucursalId ?? seguridad.SucursalId;
        await ValidarSucursalAccesibleAsync(sucursalId, seguridad, token);
        var consulta = Contexto.Existencias.AsNoTracking()
            .Where(item => item.EmpresaId == seguridad.EmpresaId
                && item.Bodegas.SucursalId == sucursalId);
        if (filtro.ProductoId.HasValue)
            consulta = consulta.Where(item => item.ProductoId == filtro.ProductoId.Value);
        if (filtro.CategoriaId.HasValue)
            consulta = consulta.Where(item => item.Productos.CategoriaId == filtro.CategoriaId.Value);
        var busqueda = NormalizarOpcional(filtro.Busqueda);
        if (busqueda is not null)
        {
            var termino = busqueda.ToUpperInvariant();
            consulta = consulta.Where(item => item.Productos.SKU.ToUpper().Contains(termino) || item.Productos.Nombre.ToUpper().Contains(termino));
        }
        if (filtro.SoloExistenciaPositiva == true)
            consulta = consulta.Where(item => item.Cantidad - item.CantidadReservada > 0);
        if (filtro.SoloBajoMinimo == true)
            consulta = consulta.Where(item => item.Productos.ParametrosStockProductos.Any(parametro =>
                parametro.BodegaId == item.BodegaId
                && parametro.Estado.Activo
                && EstadosOperativos.Contains(parametro.Estado.Nombre)
                && item.Cantidad - item.CantidadReservada <= parametro.StockMinimo));

        var total = await consulta.CountAsync(token);
        var elementos = await Paginar(
                consulta.OrderBy(item => item.Productos.Nombre)
                    .ThenBy(item => item.Bodegas.Nombre)
                    .ThenBy(item => item.Ubicaciones.Nombre),
                filtro)
            .Select(item => new ExistenciaConsultaDto(
                item.Id,
                item.EmpresaId,
                item.Bodegas.SucursalId,
                item.Bodegas.Sucursal.Nombre,
                item.BodegaId,
                item.Bodegas.Nombre,
                item.UbicacionId,
                item.Ubicaciones.Nombre,
                item.ProductoId,
                item.Productos.SKU,
                item.Productos.Nombre,
                item.Productos.CategoriaId,
                item.Productos.Categorias.Nombre,
                item.Productos.UnidadBaseId,
                item.Productos.UnidadesMedida.Nombre,
                item.Cantidad,
                item.CantidadReservada,
                item.Cantidad - item.CantidadReservada,
                item.CostoPromedio,
                item.Productos.ParametrosStockProductos
                    .Where(parametro => parametro.BodegaId == item.BodegaId
                        && parametro.Estado.Activo
                        && EstadosOperativos.Contains(parametro.Estado.Nombre))
                    .Select(parametro => (decimal?)parametro.StockMinimo)
                    .FirstOrDefault(),
                item.Productos.ParametrosStockProductos.Any(parametro =>
                    parametro.BodegaId == item.BodegaId
                    && parametro.Estado.Activo
                    && EstadosOperativos.Contains(parametro.Estado.Nombre)
                    && item.Cantidad - item.CantidadReservada <= parametro.StockMinimo),
                item.UltimoMovimiento,
                Convert.ToBase64String(item.VersionFila)))
            .ToArrayAsync(token);
        return new(elementos, filtro.Pagina, filtro.TamanioPagina, total);
    }

    public async Task<AjusteInventarioResultadoDto> AjustarAsync(
        AjusteInventarioSolicitud solicitud,
        ContextoConfiguracion seguridad,
        CancellationToken token)
    {
        var tipo = NormalizarCodigo(solicitud.Tipo);
        if (tipo is not ("ENTRADA" or "SALIDA"))
            throw new ValidacionFase4Exception("El ajuste debe ser de ENTRADA o SALIDA.");
        if (solicitud.Cantidad <= 0 || solicitud.Cantidad > MaximoDecimal19Con6)
            throw new ValidacionFase4Exception("La cantidad debe ser positiva y caber en decimal(19,6).");
        if (solicitud.CostoUnitario is < 0 or > MaximoDecimal19Con6)
            throw new ValidacionFase4Exception("El costo debe caber en decimal(19,6) y no puede ser negativo.");
        var clave = seguridad.EmpresaId.ToString(System.Globalization.CultureInfo.InvariantCulture);
        await using var bloqueoLocal = await coordinador.AdquirirAsync(clave, token);
        if (Contexto.ChangeTracker.HasChanges()) throw new ConflictoFase4Exception("El contexto contiene cambios pendientes.");
        Contexto.ChangeTracker.Clear();
        await using var transaccion = await IniciarTransaccionAsync(token);
        try
        {
            await TomarBloqueoSqlAsync(clave, token);
            var resultado = await AplicarMovimientoAsync(
                solicitud,
                seguridad,
                UnidadCostoInventario.UnidadSolicitada,
                token);
            if (transaccion is not null) await transaccion.CommitAsync(token);
            return resultado;
        }
        catch (Exception error)
        {
            if (transaccion is not null) await transaccion.RollbackAsync(CancellationToken.None);
            Contexto.ChangeTracker.Clear();
            if (error is Microsoft.Data.SqlClient.SqlException { Number: 51002 or 1205 or 1222 })
                throw new ConflictoFase4Exception("Otra operación está modificando el inventario. Reintente.");
            throw;
        }
    }

    // Núcleo compartido. El documento automático es dueño de la transacción/bloqueo;
    // no se inicia otra operación y así compras/producción comparten exactamente el mismo motor.
    internal async Task<AjusteInventarioResultadoDto> AplicarMovimientoAsync(
        AjusteInventarioSolicitud solicitud, ContextoConfiguracion seguridad,
        UnidadCostoInventario unidadCosto, CancellationToken token,
        OrigenMovimientoInventario? origen = null)
    {
        var tipo = NormalizarCodigo(solicitud.Tipo);

        await ValidarSucursalAccesibleAsync(solicitud.SucursalId, seguridad, token);
        var ubicacion = await Contexto.Ubicaciones.AsNoTracking()
            .Where(item => item.Id == solicitud.UbicacionId
                && item.BodegaId == solicitud.BodegaId
                && item.EmpresaId == seguridad.EmpresaId
                && item.Bodegas.SucursalId == solicitud.SucursalId
                && item.Bodegas.Estado.Activo
                && EstadosOperativos.Contains(item.Bodegas.Estado.Nombre)
                && item.Estado.Activo
                && EstadosOperativos.Contains(item.Estado.Nombre))
            .Select(item => new
            {
                Bodega = item.Bodegas.Nombre,
                Ubicacion = item.Nombre
            })
            .SingleOrDefaultAsync(token)
            ?? throw new RecursoFase4NoEncontradoException(
                "La bodega o ubicación no existe dentro de la sucursal autorizada.");

        var producto = await Contexto.Productos.AsNoTracking()
            .Where(item => item.Id == solicitud.ProductoId
                && item.EmpresaId == seguridad.EmpresaId
                && item.Estado.Activo
                && EstadosOperativos.Contains(item.Estado.Nombre)
                && item.TiposProductos.ManejaExistencia
                && item.TiposProductos.Estado.Activo
                && EstadosOperativos.Contains(item.TiposProductos.Estado.Nombre))
            .Select(item => new
            {
                item.Id,
                item.UnidadBaseId,
                UnidadBase = item.UnidadesMedida.Nombre,
                item.UnidadesMedida.PermiteDecimales,
                item.UnidadesMedida.CantidadDecimales
            })
            .SingleOrDefaultAsync(token)
            ?? throw new ValidacionFase4Exception(
                "El producto no existe, está inactivo o no maneja existencia.");

        var conversion = await ConvertirCantidadAsync(producto.Id, solicitud.ProductoPresentacionId,
            solicitud.Cantidad, seguridad.EmpresaId, token);
        var cantidadBase = conversion.CantidadBase;

        var existencia = await Contexto.Existencias.SingleOrDefaultAsync(item =>
            item.EmpresaId == seguridad.EmpresaId
            && item.ProductoId == solicitud.ProductoId
            && item.BodegaId == solicitud.BodegaId
            && item.UbicacionId == solicitud.UbicacionId, token);
        var saldoAnterior = existencia?.Cantidad ?? 0m;
        var disponible = existencia is null ? 0m : existencia.Cantidad - existencia.CantidadReservada;
        if (tipo == "SALIDA" && disponible < cantidadBase)
            throw new ExistenciaInsuficienteException(
                $"Existencia insuficiente. Disponible: {disponible}; requerido: {cantidadBase}.");

        var concepto = await Contexto.ConceptosMovimientos.AsNoTracking()
            .Where(item => item.EmpresaId == seguridad.EmpresaId
                && item.Codigo == (origen == null ? $"AJUSTE-{tipo}" : origen.ConceptoCodigo)
                && item.TipoOperacion == tipo
                && item.EsAutomatico == (origen != null)
                && (origen == null || item.RequiereReferencia)
                && item.Estado.Activo
                && EstadosOperativos.Contains(item.Estado.Nombre))
            .Select(item => new { item.Id, item.AfectaCosto, item.PermiteCostoCero, item.RequiereReferencia })
            .SingleOrDefaultAsync(token)
            ?? throw new ConflictoFase4Exception(
                "La preparación de Fase 4 no contiene el concepto requerido para el ajuste.");
        if (concepto.RequiereReferencia && string.IsNullOrWhiteSpace(solicitud.Referencia))
            throw new ValidacionFase4Exception("El concepto requiere una referencia.");

        var estadoFlujo = await Contexto.EstadosFlujo.AsNoTracking()
            .Where(item => item.FlujoEstado.Codigo == FlujoMovimientos
                && item.Codigo == EstadoAplicado
                && item.Estado.Activo
                && EstadosOperativos.Contains(item.Estado.Nombre))
            .Select(item => new { item.Id, item.FlujoEstadoId })
            .SingleOrDefaultAsync(token)
            ?? throw new ConflictoFase4Exception(
                "La preparación de Fase 4 no contiene el estado aplicado para movimientos.");

        decimal costoUnitarioBase;
        decimal costoPromedioNuevo;
        if (tipo == "ENTRADA")
        {
            if (concepto.AfectaCosto && !solicitud.CostoUnitario.HasValue)
                throw new ValidacionFase4Exception("El costo unitario es obligatorio para este ajuste de entrada.");
            costoUnitarioBase = solicitud.CostoUnitario.HasValue
                ? ResolverCostoUnitarioBase(solicitud.CostoUnitario.Value, conversion.Factor, unidadCosto)
                : existencia?.CostoPromedio ?? 0m;
            if (!concepto.PermiteCostoCero && costoUnitarioBase == 0)
                throw new ValidacionFase4Exception("El concepto de ajuste no permite costo cero.");
            costoPromedioNuevo = concepto.AfectaCosto
                ? CalcularCostoPromedio(saldoAnterior, existencia?.CostoPromedio ?? 0m, cantidadBase, costoUnitarioBase)
                : existencia?.CostoPromedio ?? costoUnitarioBase;
        }
        else
        {
            costoUnitarioBase = existencia?.CostoPromedio ?? 0m;
            costoPromedioNuevo = costoUnitarioBase;
        }

        var saldoNuevo = tipo == "ENTRADA"
            ? saldoAnterior + cantidadBase
            : saldoAnterior - cantidadBase;
        if (saldoNuevo < 0)
            throw new ExistenciaInsuficienteException("El ajuste produciría inventario negativo.");
        if (saldoNuevo > MaximoDecimal19Con6)
            throw new ValidacionFase4Exception("El saldo resultante excede la precisión de Existencias.Cantidad.");
        ValidarProductoDecimal19Con6(
            cantidadBase,
            costoUnitarioBase,
            "El costo total del movimiento excede la precisión admitida.");
        var saldoCosto = ValidarProductoDecimal19Con6(
            saldoNuevo,
            costoPromedioNuevo,
            "El saldo valorizado excede la precisión de Kardex.SaldoCosto.");

        var numero = origen?.Numero ?? (await Contexto.Movimientos
            .Where(item => item.EmpresaId == seguridad.EmpresaId && item.SerieDocumento == SerieAjuste)
            .Select(item => (long?)item.NumeroDocumento)
            .MaxAsync(token) ?? 0) + 1;
        var ahora = AhoraUtc;
        var movimiento = new Movimientos
        {
            EmpresaId = seguridad.EmpresaId,
            SucursalId = solicitud.SucursalId,
            ConceptoMovimientoId = concepto.Id,
            BodegaOrigenId = tipo == "SALIDA" ? solicitud.BodegaId : null,
            BodegaDestinoId = tipo == "ENTRADA" ? solicitud.BodegaId : null,
            SerieDocumento = origen?.Serie ?? SerieAjuste,
            NumeroDocumento = numero,
            FechaMovimiento = ahora,
            FlujoEstadoId = estadoFlujo.FlujoEstadoId,
            EstadoId = estadoFlujo.Id,
            ReferenciaExterna = NormalizarOpcional(solicitud.Referencia),
            ModuloOrigen = origen?.ModuloOrigen ?? "AJUSTE_MANUAL",
            DocumentoOrigenId = origen?.DocumentoId,
            Observaciones = NormalizarOpcional(solicitud.Observaciones),
            AplicadoPorUsuarioId = seguridad.UsuarioId,
            FechaAplicacion = ahora,
            FechaCreacion = ahora,
            CreadoPorUsuarioId = seguridad.UsuarioId
        };
        Contexto.Movimientos.Add(movimiento);
        await GuardarCambiosAsync(token);

        var detalle = new MovimientoDetalles
        {
            EmpresaId = seguridad.EmpresaId,
            MovimientoId = movimiento.Id,
            NumeroLinea = 1,
            ProductoId = solicitud.ProductoId,
            ProductoPresentacionId = solicitud.ProductoPresentacionId,
            UnidadMedidaId = conversion.UnidadEntradaId,
            UbicacionOrigenId = tipo == "SALIDA" ? solicitud.UbicacionId : null,
            UbicacionDestinoId = tipo == "ENTRADA" ? solicitud.UbicacionId : null,
            Cantidad = solicitud.Cantidad,
            FactorConversion = conversion.Factor,
            CantidadBase = cantidadBase,
            CostoUnitario = costoUnitarioBase,
            VentaDetalleId = origen?.VentaDetalleId,
            UnidadBaseId = origen?.UnidadBaseId,
            Observaciones = NormalizarOpcional(solicitud.Observaciones)
        };
        Contexto.MovimientoDetalles.Add(detalle);

        if (existencia is null)
        {
            existencia = new Existencias
            {
                EmpresaId = seguridad.EmpresaId,
                ProductoId = solicitud.ProductoId,
                BodegaId = solicitud.BodegaId,
                UbicacionId = solicitud.UbicacionId,
                CantidadReservada = 0,
                VersionFila = []
            };
            Contexto.Existencias.Add(existencia);
        }
        existencia.Cantidad = saldoNuevo;
        existencia.CostoPromedio = costoPromedioNuevo;
        existencia.UltimoMovimiento = ahora;
        await GuardarCambiosAsync(token);

        var kardex = new Kardex
        {
            EmpresaId = seguridad.EmpresaId,
            ProductoId = solicitud.ProductoId,
            BodegaId = solicitud.BodegaId,
            UbicacionId = solicitud.UbicacionId,
            MovimientoDetalleId = detalle.Id,
            FechaMovimiento = ahora,
            TipoAfectacion = tipo,
            Cantidad = cantidadBase,
            CostoUnitario = costoUnitarioBase,
            SaldoCantidad = saldoNuevo,
            SaldoCosto = saldoCosto,
            FechaRegistro = ahora
        };
        Contexto.Kardex.Add(kardex);
        if (tipo == "ENTRADA" && concepto.AfectaCosto)
            await ActualizarCostoProductoAsync(seguridad, solicitud.ProductoId, costoPromedioNuevo, ahora, token, origen);
        Auditoria.Registrar(
            seguridad,
            "Movimientos",
            movimiento.Id.ToString(),
            origen?.AccionAuditoria ?? $"AJUSTE_{tipo}",
            $"Movimiento de {cantidadBase} unidades base en {ubicacion.Bodega}/{ubicacion.Ubicacion}.");
        await GuardarCambiosAsync(token);

        return new(
            movimiento.Id,
            detalle.Id,
            existencia.Id,
            tipo,
            solicitud.Cantidad,
            conversion.Factor,
            cantidadBase,
            saldoAnterior,
            saldoNuevo,
            costoUnitarioBase,
            producto.UnidadBase);
    }

    private async Task ValidarSucursalAccesibleAsync(
        long sucursalId,
        ContextoConfiguracion seguridad,
        CancellationToken token)
    {
        if (!await Contexto.UsuariosSucursales.AsNoTracking().AnyAsync(item =>
                item.UsuarioId == seguridad.UsuarioId
                && item.SucursalId == sucursalId
                && item.Sucursal.EmpresaId == seguridad.EmpresaId
                && item.Estado.Activo
                && EstadosOperativos.Contains(item.Estado.Nombre)
                && item.Sucursal.Estado.Activo
                && EstadosOperativos.Contains(item.Sucursal.Estado.Nombre)
                && item.Sucursal.Empresa.Estado.Activo
                && EstadosOperativos.Contains(item.Sucursal.Empresa.Estado.Nombre),
                token))
            throw new RecursoFase4NoEncontradoException("La sucursal no existe dentro del alcance autorizado.");
    }

    private async Task<ConversionAjuste> ResolverConversionAsync(
        AjusteInventarioSolicitud solicitud,
        long productoId,
        int unidadBaseId,
        int empresaId,
        CancellationToken token)
    {
        if (!solicitud.ProductoPresentacionId.HasValue)
        {
            var unidad = await Contexto.UnidadesMedida.AsNoTracking()
                .Where(item => item.Id == unidadBaseId && item.EmpresaId == empresaId)
                .Select(item => new { item.PermiteDecimales, item.CantidadDecimales })
                .SingleAsync(token);
            return new(unidadBaseId, 1m, unidad.PermiteDecimales, unidad.CantidadDecimales);
        }

        return await Contexto.ProductoPresentaciones.AsNoTracking()
            .Where(item => item.Id == solicitud.ProductoPresentacionId
                && item.ProductoId == productoId
                && item.EmpresaId == empresaId
                && item.Estado.Activo
                && EstadosOperativos.Contains(item.Estado.Nombre)
                && item.UnidadesMedida.Estado.Activo
                && EstadosOperativos.Contains(item.UnidadesMedida.Estado.Nombre))
            .Select(item => new ConversionAjuste(
                item.UnidadMedidaId,
                item.FactorConversion,
                item.UnidadesMedida.PermiteDecimales,
                item.UnidadesMedida.CantidadDecimales))
            .SingleOrDefaultAsync(token)
            ?? throw new ValidacionFase4Exception("La presentación no pertenece al producto o no está activa.");
    }

    private async Task ActualizarCostoProductoAsync(
        ContextoConfiguracion seguridad,
        long productoId,
        decimal costoNuevo,
        DateTime ahora,
        CancellationToken token,
        OrigenMovimientoInventario? origen = null)
    {
        var costo = Contexto.CostosProductos.Local.FirstOrDefault(item =>
            item.EmpresaId == seguridad.EmpresaId && item.ProductoId == productoId)
            ?? await Contexto.CostosProductos.SingleOrDefaultAsync(item =>
                item.EmpresaId == seguridad.EmpresaId && item.ProductoId == productoId,
                token);
        var costoAnterior = costo?.CostoActual;
        if (costo is null)
        {
            costo = new CostosProductos
            {
                EmpresaId = seguridad.EmpresaId,
                ProductoId = productoId
            };
            Contexto.CostosProductos.Add(costo);
        }
        costo.CostoActual = costoNuevo;
        costo.FechaCalculo = ahora;
        if (origen is not null)
            costo.MetodoValuacionId = await Contexto.Productos.Where(item => item.Id == productoId
                && item.EmpresaId == seguridad.EmpresaId).Select(item => item.MetodoValuacionId).SingleAsync(token);
        if (costoAnterior != costoNuevo)
        {
            Contexto.HistoricoCostosProductos.Add(new HistoricoCostosProductos
            {
                EmpresaId = seguridad.EmpresaId,
                ProductoId = productoId,
                CostoAnterior = costoAnterior,
                CostoNuevo = costoNuevo,
                FechaCambio = ahora,
                OrigenCambio = origen?.OrigenCosto ?? "AJUSTE_INVENTARIO",
                DocumentoOrigenId = origen?.DocumentoId,
                UsuarioId = seguridad.UsuarioId
            });
        }
    }

    internal async Task<ConversionInventario> ConvertirCantidadAsync(long productoId, long? presentacionId,
        decimal cantidad, int empresaId, CancellationToken token)
    {
        if (cantidad <= 0 || cantidad > MaximoDecimal19Con6)
            throw new ValidacionFase4Exception("La cantidad debe ser positiva y caber en decimal(19,6).");
        var unidad = await Contexto.Productos.AsNoTracking().Where(item => item.Id == productoId && item.EmpresaId == empresaId)
            .Select(item => new { item.UnidadBaseId, item.UnidadesMedida.CantidadDecimales, item.UnidadesMedida.PermiteDecimales })
            .SingleAsync(token);
        var conversion = await ResolverConversionAsync(new AjusteInventarioSolicitud { ProductoPresentacionId = presentacionId },
            productoId, unidad.UnidadBaseId, empresaId, token);
        ValidarPrecision(cantidad, conversion.DecimalesEntrada, conversion.PermiteDecimalesEntrada, "cantidad solicitada");
        decimal cantidadBase;
        try { cantidadBase = cantidad * conversion.Factor; }
        catch (OverflowException) { throw new ValidacionFase4Exception("La cantidad convertida excede la capacidad admitida."); }
        if (cantidadBase > MaximoDecimal19Con6) throw new ValidacionFase4Exception("La cantidad base excede decimal(19,6).");
        ValidarPrecision(cantidadBase, unidad.CantidadDecimales, unidad.PermiteDecimales, "cantidad base");
        return new(conversion.UnidadEntradaId, conversion.Factor, cantidadBase);
    }

    private async Task<IDbContextTransaction?> IniciarTransaccionAsync(CancellationToken token) =>
        Contexto.Database.IsRelational()
            ? await Contexto.Database.BeginTransactionAsync(IsolationLevel.Serializable, token)
            : null;

    private async Task TomarBloqueoSqlAsync(string clave, CancellationToken token)
    {
        if (!Contexto.Database.IsSqlServer())
            return;
        await Contexto.Database.ExecuteSqlInterpolatedAsync(
            $"""
            DECLARE @resultado int;
            EXEC @resultado = sys.sp_getapplock
                @Resource = {"Heladeria.Inventario." + clave},
                @LockMode = N'Exclusive',
                @LockOwner = N'Transaction',
                @LockTimeout = 10000;
            IF @resultado < 0
                THROW 51002, 'No fue posible reservar la existencia solicitada.', 1;
            """,
            token);
    }

    private static decimal CalcularCostoPromedio(
        decimal cantidadAnterior,
        decimal costoAnterior,
        decimal cantidadEntrada,
        decimal costoEntrada)
    {
        var cantidadTotal = cantidadAnterior + cantidadEntrada;
        return cantidadTotal == 0
            ? 0
            : decimal.Round(
                (cantidadAnterior * costoAnterior + cantidadEntrada * costoEntrada) / cantidadTotal,
                6,
                MidpointRounding.AwayFromZero);
    }

    private static decimal ResolverCostoUnitarioBase(
        decimal costo,
        decimal factorConversion,
        UnidadCostoInventario unidadCosto)
    {
        decimal costoSinRedondear;
        try
        {
            costoSinRedondear = unidadCosto switch
            {
                UnidadCostoInventario.UnidadSolicitada => costo / factorConversion,
                UnidadCostoInventario.UnidadBase => costo,
                _ => throw new ArgumentOutOfRangeException(nameof(unidadCosto), unidadCosto, null)
            };
        }
        catch (OverflowException)
        {
            throw new ValidacionFase4Exception("El costo unitario base excede la capacidad admitida.");
        }
        catch (DivideByZeroException)
        {
            throw new ValidacionFase4Exception("El factor de conversión debe ser mayor que cero.");
        }

        var costoUnitarioBase = decimal.Round(costoSinRedondear, 6, MidpointRounding.AwayFromZero);
        if (costoSinRedondear > 0 && costoUnitarioBase == 0)
            throw new ValidacionFase4Exception(
                "El costo positivo convertido no se puede representar en decimal(19,6); no se registrará como costo cero.");
        if (costoUnitarioBase < 0 || costoUnitarioBase > MaximoDecimal19Con6)
            throw new ValidacionFase4Exception("El costo unitario base debe caber en decimal(19,6) y no puede ser negativo.");
        return costoUnitarioBase;
    }

    private static decimal ValidarProductoDecimal19Con6(
        decimal valorA,
        decimal valorB,
        string mensaje)
    {
        decimal resultado;
        try
        {
            resultado = decimal.Round(valorA * valorB, 6, MidpointRounding.AwayFromZero);
        }
        catch (OverflowException)
        {
            throw new ValidacionFase4Exception(mensaje);
        }

        if (resultado > MaximoDecimal19Con6)
            throw new ValidacionFase4Exception(mensaje);
        return resultado;
    }

    private static void ValidarPrecision(decimal cantidad, byte decimales, bool permiteDecimales, string campo)
    {
        if (!permiteDecimales && cantidad != decimal.Truncate(cantidad))
            throw new ValidacionFase4Exception($"La {campo} no permite decimales.");
        if (cantidad != decimal.Round(cantidad, decimales, MidpointRounding.AwayFromZero))
            throw new ValidacionFase4Exception($"La {campo} supera la precisión permitida de {decimales} decimales.");
    }

    private sealed record ConversionAjuste(
        int UnidadEntradaId,
        decimal Factor,
        bool PermiteDecimalesEntrada,
        byte DecimalesEntrada);
}

internal sealed record OrigenMovimientoInventario(
    long DocumentoId,
    string Serie,
    long Numero,
    string ModuloOrigen,
    string ConceptoCodigo,
    string AccionAuditoria,
    string OrigenCosto,
    long? VentaDetalleId = null,
    int? UnidadBaseId = null);
internal sealed record ConversionInventario(int UnidadEntradaId, decimal Factor, decimal CantidadBase);
internal enum UnidadCostoInventario
{
    UnidadSolicitada,
    UnidadBase
}
