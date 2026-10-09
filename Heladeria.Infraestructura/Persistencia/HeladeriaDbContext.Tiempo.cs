using Heladeria.Infraestructura.Persistencia.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Heladeria.Infraestructura.Persistencia;

public partial class HeladeriaDbContext
{
    private static void ConfigurarTiempo(ModelBuilder modelo)
    {
        // Solo entidades con semántica UTC verificada en los escritores de las fases implementadas.
        // datetime2 no conserva Kind: restaurarlo al materializar NO desplaza ni reescribe el dato.
        // No incluir fechas civiles (OrdenesCompra, vigencias DateOnly) ni módulos aún no implementados.
        Type[] entidadesUtc = [typeof(Ventas), typeof(RecepcionesCompra), typeof(HistoricoPreciosCompra),
            typeof(OrdenesProduccion), typeof(ConsumosProduccion), typeof(ProduccionTerminada), typeof(CostosProduccion),
            typeof(Movimientos), typeof(Kardex), typeof(Existencias), typeof(HistoricoCostosProductos), typeof(CostosProductos),
            typeof(TurnosCaja), typeof(Pagos), typeof(MovimientosCaja), typeof(ArqueosCaja),
            typeof(CuentasPorCobrar), typeof(CuentasPorPagar), typeof(AplicacionesCuentasPorCobrar), typeof(AplicacionesCuentasPorPagar),
            typeof(Bitacoras), typeof(HistorialAccesos), typeof(SesionesUsuarios), typeof(CorrelativosDocumentos),
            typeof(Productos), typeof(Categorias), typeof(Marcas), typeof(TiposProductos), typeof(UnidadesMedida),
            typeof(Presentaciones), typeof(ProductoPresentaciones), typeof(CodigosBarrasProductos),
            typeof(ParametrosStockProductos), typeof(Bodegas), typeof(Ubicaciones), typeof(ClasesCatalogos),
            typeof(Catalogos), typeof(ConceptosMovimientos), typeof(FlujosEstados), typeof(EstadosFlujo), typeof(EstadoTransiciones)];
        var conversor = new ValueConverter<DateTime, DateTime>(
            valor => GuardarUtc(valor), valor => DateTime.SpecifyKind(valor, DateTimeKind.Utc));
        foreach (var entidad in entidadesUtc)
        foreach (var propiedad in modelo.Entity(entidad).Metadata.GetProperties())
            if ((Nullable.GetUnderlyingType(propiedad.ClrType) ?? propiedad.ClrType) == typeof(DateTime))
                propiedad.SetValueConverter(conversor);
    }

    private static DateTime GuardarUtc(DateTime valor) => valor.Kind == DateTimeKind.Local
        ? throw new InvalidOperationException("Un timestamp persistido no puede depender de la zona local del servidor.")
        : valor;
}
