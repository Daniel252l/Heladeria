using Heladeria.Aplicacion.Configuracion;
using Heladeria.Aplicacion.Inventario;
using Heladeria.Aplicacion.Productos;
using Heladeria.Aplicacion.Ventas;
using Heladeria.Infraestructura.Fase4;
using Heladeria.Infraestructura.Inventario;
using Heladeria.Infraestructura.Persistencia.Entidades;
using Microsoft.EntityFrameworkCore;
using VentaEntidad = Heladeria.Infraestructura.Persistencia.Entidades.Ventas;

namespace Heladeria.Infraestructura.VentasPos;

public sealed partial class ServicioVentas
{
    private async Task<ConsumoVenta[]> PrepararConsumosAsync(VentaEntidad venta, ContextoConfiguracion s, CancellationToken t)
    {
        var consumos = new List<ConsumoVenta>();
        var reglas = new ReglasComposicionVenta(Contexto, Operativos, Inventario);
        foreach (var linea in venta.VentasDetalle.OrderBy(x => x.NumeroLinea))
        {
            if (linea.EsCompuesto == true)
            {
                var filas = await Contexto.CombosDetalle.AsNoTracking().Where(x => x.EmpresaId == s.EmpresaId
                    && x.ComboProductoId == linea.ProductoId).OrderBy(x => x.Id).ToArrayAsync(t);
                if (filas.Length == 0) throw new ConflictoVentasException("El producto compuesto ya no tiene componentes. Actualice el borrador.");
                foreach (var fila in filas)
                {
                    decimal cantidad;
                    try { cantidad = fila.Cantidad * linea.CantidadBase!.Value; }
                    catch (OverflowException) { throw new ValidacionVentasException("Cantidad de componente fuera de rango."); }
                    // Composición por unidad base comercial; una presentación de 3 unidades escala tres veces.
                    var r = await reglas.ResolverAsync(fila.ProductoIncluidoId, fila.ProductoPresentacionId, cantidad, s.EmpresaId, t);
                    consumos.Add(new(linea, r.ProductoId, r.Nombre, r.PresentacionId, r.Cantidad, r.CantidadBase, r.UnidadBaseId));
                }
            }
            else if (linea.ManejaExistencia == true)
                consumos.Add(new(linea, linea.ProductoId, $"producto {linea.ProductoId}", linea.ProductoPresentacionId,
                    linea.Cantidad, linea.CantidadBase!.Value, linea.UnidadBaseId!.Value));
        }
        var ordenados = consumos.OrderBy(x => x.ProductoId).ThenBy(x => x.Linea.NumeroLinea).ThenBy(x => x.PresentacionId).ToArray();
        foreach (var grupo in ordenados.GroupBy(x => x.ProductoId))
        {
            await BloquearProductoAsync(grupo.Key, s, t);
            decimal requerido;
            try { requerido = grupo.Sum(x => x.CantidadBase); }
            catch (OverflowException) { throw new ValidacionVentasException("Cantidad acumulada fuera de rango."); }
            Precision(requerido, "Cantidad acumulada");
            var saldo = await Contexto.Existencias.AsNoTracking().SingleOrDefaultAsync(x => x.EmpresaId == s.EmpresaId
                && x.ProductoId == grupo.Key && x.BodegaId == venta.BodegaId && x.UbicacionId == venta.UbicacionId, t);
            if (saldo is null || saldo.Cantidad - saldo.CantidadReservada < requerido)
                throw new StockVentaInsuficienteException($"Inventario insuficiente para completar la venta: {grupo.First().Nombre} (producto {grupo.Key}). Ninguna línea fue descontada.");
            foreach (var consumo in grupo)
            {
                Precision(saldo.CostoPromedio, "Costo unitario base", false);
                consumo.CostoUnitario = saldo.CostoPromedio;
                try { consumo.CostoTotal = decimal.Round(consumo.CantidadBase * consumo.CostoUnitario, 6, MidpointRounding.AwayFromZero); }
                catch (OverflowException) { throw new ValidacionVentasException("Costo del componente fuera de rango."); }
                Precision(consumo.CostoTotal, "Costo del componente", false);
            }
        }
        foreach (var linea in venta.VentasDetalle)
        {
            if (linea.EsCompuesto != true)
            {
                AsignarCosto(linea, ordenados.FirstOrDefault(x => x.Linea == linea)?.CostoUnitario ?? 0);
                continue;
            }
            decimal total;
            try { total = ordenados.Where(x => x.Linea == linea).Sum(x => x.CostoTotal); }
            catch (OverflowException) { throw new ValidacionVentasException("Costo compuesto fuera de rango."); }
            Precision(total, "Costo compuesto", false);
            var unitario = decimal.Round(total / linea.CantidadBase!.Value, 6, MidpointRounding.AwayFromZero);
            Precision(unitario, "Costo unitario compuesto", false);
            if (total > 0 && unitario == 0) throw new ValidacionVentasException("El costo compuesto positivo no se puede representar con seis decimales.");
            linea.CostoUnitario = unitario;
            // El total histórico es la suma de consumos, no el unitario redondeado multiplicado otra vez.
            linea.CostoTotal = total;
        }
        return ordenados;
    }

    private async Task RevertirCompuestosAsync(VentaEntidad venta, string motivo, ContextoConfiguracion s, CancellationToken t)
    {
        var lineas = venta.VentasDetalle.Where(x => x.EsCompuesto == true).ToArray();
        if (lineas.Length == 0) return;
        var ids = lineas.Select(x => x.Id).ToArray();
        var originales = await Contexto.MovimientoDetalles.AsNoTracking().Where(x => x.EmpresaId == s.EmpresaId
            && x.VentaDetalleId.HasValue && ids.Contains(x.VentaDetalleId.Value)
            && x.Movimientos.ModuloOrigen == "VENTAS" && x.Movimientos.DocumentoOrigenId == venta.Id
            && x.Movimientos.SucursalId == s.SucursalId && x.Movimientos.ConceptosMovimientos.Codigo == "VENTA-SALIDA")
            .OrderBy(x => x.ProductoId).ThenBy(x => x.Id).ToArrayAsync(t);
        foreach (var linea in lineas)
        {
            var filas = originales.Where(x => x.VentaDetalleId == linea.Id).ToArray();
            if (filas.Length == 0 || filas.Sum(x => decimal.Round(x.CantidadBase * x.CostoUnitario, 6, MidpointRounding.AwayFromZero)) != linea.CostoTotal)
                throw new ConflictoVentasException("El consumo histórico del producto compuesto está incompleto. No es seguro reconstruirlo desde la composición actual.");
        }
        foreach (var original in originales)
        {
            await BloquearProductoAsync(original.ProductoId, s, t);
            if (!original.UnidadBaseId.HasValue || !await Contexto.Productos.AnyAsync(x => x.Id == original.ProductoId
                && x.EmpresaId == s.EmpresaId && x.UnidadBaseId == original.UnidadBaseId, t))
                throw new ConflictoVentasException("La unidad base del consumo histórico no permite una reversión segura.");
            var doc = await DocumentoAsync("VA", s, t);
            var observacion = $"Anulación venta {venta.Id}, consumo original {original.Id}: {motivo}";
            await Inventario.AplicarMovimientoAsync(new AjusteInventarioSolicitud {
                SucursalId = s.SucursalId, BodegaId = venta.BodegaId!.Value, UbicacionId = original.UbicacionOrigenId!.Value,
                ProductoId = original.ProductoId, Cantidad = original.CantidadBase, CostoUnitario = original.CostoUnitario,
                Tipo = "ENTRADA", Referencia = venta.NumeroFactura, Observaciones = observacion[..Math.Min(500, observacion.Length)]
            }, s, UnidadCostoInventario.UnidadBase, t, new(venta.Id, doc.Serie, doc.Numero, "VENTAS",
                "VENTA-ANULACION-ENTRADA", "VENTA_INVENTARIO_REVERTIDO", "ANULACION_VENTA", original.VentaDetalleId, original.UnidadBaseId));
        }
    }

    private sealed class ConsumoVenta(VentasDetalle linea, long productoId, string nombre, long? presentacionId,
        decimal cantidad, decimal cantidadBase, int unidadBaseId)
    {
        public VentasDetalle Linea { get; } = linea;
        public long ProductoId { get; } = productoId;
        public string Nombre { get; } = nombre;
        public long? PresentacionId { get; } = presentacionId;
        public decimal Cantidad { get; } = cantidad;
        public decimal CantidadBase { get; } = cantidadBase;
        public int UnidadBaseId { get; } = unidadBaseId;
        public decimal CostoUnitario { get; set; }
        public decimal CostoTotal { get; set; }
    }
}
