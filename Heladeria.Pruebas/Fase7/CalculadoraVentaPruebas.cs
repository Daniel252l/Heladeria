using Heladeria.Aplicacion.Ventas;

namespace Heladeria.Pruebas.Fase7;

public sealed class CalculadoraVentaPruebas
{
    [Theory]
    [InlineData(2, 25, 0, 50, 0, 50)]
    [InlineData(2, 25, 10, 50, 5, 45)]
    [InlineData(2, 25, 100, 50, 50, 0)]
    [InlineData(0.5, 24.5, 5, 12.25, 0.61, 11.64)]
    public void CalculoDecimal(decimal cantidad, decimal precio, decimal porcentaje, decimal subtotal, decimal descuento, decimal total)
    {
        var r = CalculadoraVenta.Calcular(cantidad, precio, porcentaje, true, []);
        Assert.Equal(subtotal, r.SubtotalNeto); Assert.Equal(descuento, r.DescuentoNeto); Assert.Equal(total, r.Total);
    }
    [Theory]
    [InlineData(false, 100, 10, 10.8, 100.8)]
    [InlineData(true, 112, 11.2, 12, 100.8)]
    public void ImpuestoIncluidoOAgregadoSinDobleCobro(bool incluido, decimal precio, decimal descuentoCatalogo, decimal tasa, decimal total)
    {
        var r = CalculadoraVenta.Calcular(1, precio, 10, true, [new(1, "IVA", "Impuesto", 12, incluido)]);
        Assert.Equal(descuentoCatalogo, r.DescuentoPrecioCatalogo);
        Assert.Equal(10.8m, r.Impuesto);
        Assert.Equal(total, r.Total);
        Assert.Equal(r.SubtotalNeto - r.DescuentoNeto + r.Impuesto, r.Total);
        Assert.True(tasa > 0);
    }
    [Theory]
    [InlineData(-1, 25, 0, true)]
    [InlineData(0, 25, 0, true)]
    [InlineData(1, 0, 0, true)]
    [InlineData(1, -1, 0, true)]
    [InlineData(1, 25, -1, true)]
    [InlineData(1, 25, 101, true)]
    [InlineData(1, 25, 1, false)]
    [InlineData(1, 25.001, 0, true)]
    [InlineData(1, 25, 0.001, true)]
    public void RechazaValoresInvalidos(decimal cantidad, decimal precio, decimal descuento, bool permitido) =>
        Assert.Throws<ValidacionVentasException>(() => CalculadoraVenta.Calcular(cantidad, precio, descuento, permitido, []));
    [Fact]
    public void RechazaImpuestosIncluidosAmbiguos() => Assert.Throws<ConflictoVentasException>(() =>
        CalculadoraVenta.Calcular(1, 112, 0, true, [new(1, "IVA", "IVA", 12, true), new(2, "OTRO", "Otro", 5, true)]));
    [Fact]
    public void MultiplesImpuestosNoCompuestos()
    {
        var r = CalculadoraVenta.Calcular(1, 112, 0, true, [new(1, "IVA", "IVA", 12, true), new(2, "OTRO", "Otro", 5, false)]);
        Assert.Equal(100, r.SubtotalNeto); Assert.Equal(17, r.Impuesto); Assert.Equal(117, r.Total);
    }
    [Fact]
    public void RechazaOverflowMonetario() => Assert.Throws<ValidacionVentasException>(() =>
        CalculadoraVenta.Calcular(9999999999999m, 9999999999m, 0, true, []));
}
