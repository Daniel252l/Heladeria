namespace Heladeria.Aplicacion.Ventas;

public static class CalculadoraVenta
{
    public const decimal MaximoImporte = 9999999999.99m;
    public static decimal Moneda(decimal valor)
    {
        var redondeado = decimal.Round(valor, 2, MidpointRounding.AwayFromZero);
        if (redondeado < 0 || redondeado > MaximoImporte)
            throw new ValidacionVentasException("El importe excede la capacidad monetaria admitida.");
        return redondeado;
    }

    public static CalculoLineaVenta Calcular(decimal cantidad, decimal precio, decimal descuento,
        bool permiteDescuento, IReadOnlyList<ImpuestoPosDto> impuestos)
    {
        if (cantidad <= 0 || precio <= 0 || precio != decimal.Round(precio, 2)
            || descuento < 0 || descuento > 100 || descuento != decimal.Round(descuento, 2))
            throw new ValidacionVentasException("Cantidad, precio o descuento inválidos.");
        if (descuento > 0 && !permiteDescuento)
            throw new ValidacionVentasException("El producto no permite descuentos.");
        if (impuestos.Any(i => i.Porcentaje < 0 || i.Porcentaje > 100)
            || impuestos.Select(i => i.Id).Distinct().Count() != impuestos.Count)
            throw new ConflictoVentasException("La configuración de impuestos no es válida.");
        if (impuestos.Count(i => i.IncluidoEnPrecio) > 1)
            throw new ConflictoVentasException("La política simple de Fase 7 admite como máximo un impuesto incluido por producto.");
        try
        {
            var bruto = Moneda(cantidad * precio);
            if (bruto == 0) throw new ValidacionVentasException("El importe positivo no se puede representar con dos decimales.");
            var descuentoCatalogo = Moneda(bruto * descuento / 100m);
            var tasaIncluida = impuestos.SingleOrDefault(i => i.IncluidoEnPrecio)?.Porcentaje ?? 0m;
            var subtotalNeto = Moneda(bruto / (1m + tasaIncluida / 100m));
            var baseImponible = Moneda((bruto - descuentoCatalogo) / (1m + tasaIncluida / 100m));
            var descuentoNeto = subtotalNeto - baseImponible;
            var snapshots = impuestos.OrderBy(i => i.Id).Select(i => new ImpuestoVentaDto(i.Id,
                i.Codigo, i.Nombre, i.Porcentaje, i.IncluidoEnPrecio, baseImponible,
                i.IncluidoEnPrecio ? bruto - descuentoCatalogo - baseImponible : Moneda(baseImponible * i.Porcentaje / 100m))).ToArray();
            var totalImpuestos = Moneda(snapshots.Sum(i => i.Monto));
            return new(subtotalNeto, descuentoNeto, descuentoCatalogo, totalImpuestos,
                Moneda(baseImponible + totalImpuestos), snapshots);
        }
        catch (OverflowException) { throw new ValidacionVentasException("El cálculo excede la capacidad decimal admitida."); }
    }
}
public sealed record CalculoLineaVenta(decimal SubtotalNeto, decimal DescuentoNeto,
    decimal DescuentoPrecioCatalogo, decimal Impuesto, decimal Total, IReadOnlyList<ImpuestoVentaDto> Impuestos);
