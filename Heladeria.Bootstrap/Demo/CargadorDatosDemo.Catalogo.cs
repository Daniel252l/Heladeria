using Heladeria.Aplicacion.Productos;
using Microsoft.EntityFrameworkCore;

namespace Heladeria.Bootstrap.Demo;

public sealed partial class CargadorDatosDemo
{
    private sealed record Articulo(string Clave, string Nombre, string Categoria, string Unidad, decimal Precio,
        decimal Costo, decimal Stock, bool Vendible = false, string? PresentacionCompra = null, decimal FactorCompra = 1);
    private static readonly Articulo[] Articulos =
    [
        new("HEL-CHO","Helado Chocolate de la Casa","HEL","ML",.08m,.05m,12000,true,"LITRO",1000),
        new("HEL-VAI","Helado Vainilla Bourbon","HEL","ML",.08m,.05m,18000,true,"LITRO",1000),
        new("HEL-FRE","Helado Fresa del Huerto","HEL","ML",.08m,.05m,14000,true,"LITRO",1000),
        new("HEL-COO","Helado Cookies & Cream","HEL","ML",.08m,.05m,11000,true,"LITRO",1000),
        new("HEL-PIS","Helado Pistacho Selecto","HEL","ML",.09m,.055m,9000,true,"LITRO",1000),
        new("HEL-CAF","Helado Café de Altura","HEL","ML",.08m,.05m,10000,true,"LITRO",1000),
        new("HEL-MAN","Helado Mango Tropical","HEL","ML",.08m,.05m,15000,true,"LITRO",1000),
        new("HEL-LIM","Helado Limón Fresco","HEL","ML",.08m,.05m,8000,true,"LITRO",1000),
        new("HEL-CAR","Helado Caramelo Dorado","HEL","ML",.08m,.05m,13000,true,"LITRO",1000),
        new("HEL-CHI","Helado Chicle Azul","HEL","ML",.08m,.05m,16000,true,"LITRO",1000),
        new("BEB-COL","Coca-Cola lata 355 ml","BEB","UN",8,4.5m,70,true),
        new("BEB-SPR","Sprite lata 355 ml","BEB","UN",8,4.5m,48,true),
        new("BEB-AGU","Agua pura 600 ml","BEB","UN",6,2.5m,60,true),
        new("BEB-TE","Té frío de durazno","BEB","UN",10,5,24,true),
        new("EMP-V08","Vaso térmico 8 oz","EMP","UN",0,.5m,180),
        new("EMP-V12","Vaso térmico 12 oz","EMP","UN",0,.65m,140),
        new("EMP-V16","Vaso transparente 16 oz","EMP","UN",0,.8m,220),
        new("EMP-CUC","Cuchara para postre","EMP","UN",0,.12m,400),
        new("EMP-SER","Servilleta blanca","EMP","UN",0,.04m,800),
        new("EMP-PAJ","Pajilla para malteada","EMP","UN",0,.08m,300),
        new("EMP-TAP","Tapa para vaso 16 oz","EMP","UN",0,.2m,200),
        new("EMP-COP","Copa para postre","EMP","UN",0,.9m,120),
        new("INS-BAN","Banana seleccionada","INS","UN",0,1.5m,30),
        new("INS-LEC","Leche entera para malteadas","INS","ML",0,.008m,8000,false,"LITRO",1000),
        new("INS-BRO","Brownie artesanal individual","INS","UN",10,5,36,true),
        new("INS-CRE","Crema batida para postres","INS","G",0,.04m,1800),
        new("INS-CER","Cereza para decoración","INS","UN",0,.6m,18),
        new("INS-SCH","Sirope de chocolate intenso","INS","ML",0,.03m,2000,false,"LITRO",1000),
        new("INS-CHI","Chispas de chocolate oscuro","INS","G",0,.06m,1200),
        new("INS-SFR","Sirope de fresa natural","INS","ML",0,.03m,1000,false,"LITRO",1000)
    ];
    private async Task CrearCatalogoAsync(CancellationToken t)
    {
        var categorias = new Dictionary<string, int>();
        foreach (var (clave, nombre) in new[] { ("HEL","Helados de la Casa"),("BEB","Bebidas frías"),
            ("POS","Postres de la Casa"),("ESP","Especialidades"),("INS","Toppings e insumos"),("EMP","Empaques y consumibles") })
        {
            var previa = await db.Categorias.AsNoTracking().SingleOrDefaultAsync(x => x.EmpresaId == s.EmpresaId && x.Nombre == nombre, t);
            if (previa is not null && previa.EstadoId != activo) throw new DemoRechazadaException("Categoría existente inactiva: " + nombre);
            categorias[clave] = previa?.Id ?? (await catalogos.CrearCategoriaAsync(new() {
                Codigo = "DEMO-" + clave, Nombre = nombre, EstadoId = activo,
                Descripcion = "Catálogo de demostración opcional; referencias HELADERIA-DEMO-V1." }, s, t)).Id;
        }
        var unidades = new Dictionary<string, int>();
        foreach (var (clave, nombre, abreviatura, decimales) in new[] {
            ("ML","Mililitro","ml",true),("G","Gramo","g",true),("UN","Unidad","un",false) })
        {
            var previa = await db.UnidadesMedida.AsNoTracking().SingleOrDefaultAsync(x => x.EmpresaId == s.EmpresaId && x.Nombre == nombre, t);
            if (previa is not null && (previa.EstadoId != activo || previa.PermiteDecimales != decimales || decimales && previa.CantidadDecimales < 3))
                throw new DemoRechazadaException("Unidad existente incompatible: " + nombre);
            unidades[clave] = previa?.Id ?? (await catalogos.CrearUnidadAsync(new() {
                Codigo = "DEMO-" + clave, Nombre = nombre, Abreviatura = abreviatura, PermiteDecimales = decimales,
                CantidadDecimales = (byte)(decimales ? 6 : 0), EstadoId = activo }, s, t)).Id;
        }
        var tipos = new Dictionary<bool, int>();
        foreach (var inventario in new[] { true, false })
        {
            var previo = await db.TiposProductos.AsNoTracking().Where(x => x.EmpresaId == s.EmpresaId
                && x.ManejaExistencia == inventario && x.EstadoId == activo).OrderBy(x => x.Id).FirstOrDefaultAsync(t);
            tipos[inventario] = previo?.Id ?? (await catalogos.CrearTipoProductoAsync(new() {
                Codigo = inventario ? "DEMO-INV" : "DEMO-ESP", Nombre = inventario ? "Insumo inventariable" : "Especial preparado",
                ManejaExistencia = inventario, EstadoId = activo }, s, t)).Id;
        }
        var seguimiento = await db.Catalogos.Where(x => x.Codigo == "NINGUNO" && x.ClaseCatalogo.Codigo == "TIPO_SEGUIMIENTO_PRODUCTO"
            && (x.EmpresaId == null || x.EmpresaId == s.EmpresaId) && x.EstadoId == activo).Select(x => x.Id).SingleAsync(t);
        var metodo = await db.Catalogos.Where(x => x.Codigo == "PROMEDIO" && x.ClaseCatalogo.Codigo == "METODO_VALUACION_INVENTARIO"
            && (x.EmpresaId == null || x.EmpresaId == s.EmpresaId) && x.EstadoId == activo).Select(x => x.Id).SingleAsync(t);
        var presentaciones = new Dictionary<string, int>();
        foreach (var (clave, nombre) in new[] { ("BOLA","Bola"),("V120","Vaso pequeño"),("V180","Vaso mediano"),
            ("V250","Vaso grande"),("M500","Medio litro"),("LITRO","Litro") })
        {
            var previa = await db.Presentaciones.AsNoTracking().SingleOrDefaultAsync(x => x.EmpresaId == s.EmpresaId && x.Nombre == nombre, t);
            if (previa is not null && previa.EstadoId != activo) throw new DemoRechazadaException("Presentación existente inactiva: " + nombre);
            presentaciones[clave] = previa?.Id ?? (await catalogos.CrearPresentacionAsync(new() {
                Codigo = "DEMO-" + clave, Nombre = nombre, EstadoId = activo }, s, t)).Id;
        }
        foreach (var a in Articulos)
        {
            var p = await productos.CrearAsync(new() {
                SKU = "DEMO-" + a.Clave, Nombre = a.Nombre, CategoriaId = categorias[a.Categoria], TipoProductoId = tipos[true],
                UnidadBaseId = unidades[a.Unidad], TipoSeguimientoId = seguimiento, MetodoValuacionId = metodo,
                EsComprable = true, EsVendible = a.Vendible, PermiteDescuento = true, PrecioVentaReferencial = a.Vendible ? a.Precio : null,
                Descripcion = "Selección comercial ficticia para demostraciones. HELADERIA-DEMO-V1.", EstadoId = activo }, s, t);
            productoIds[a.Clave] = p.Id;
            var formatos = a.Clave.StartsWith("HEL-", StringComparison.Ordinal)
                ? new[] { ("BOLA",60m,8m),("V120",120m,12m),("V180",180m,18m),("V250",250m,24m),("M500",500m,45m),("LITRO",1000m,80m) }
                : a.PresentacionCompra is not null ? [(a.PresentacionCompra,a.FactorCompra,0m)] : [];
            foreach (var (clave, factor, precio) in formatos)
            {
                var pp = await productos.CrearPresentacionAsync(p.Id, new() {
                    PresentacionId = presentaciones[clave], UnidadMedidaId = unidades["UN"], FactorConversion = factor,
                    PermiteCompra = clave is "LITRO" or "M500", PermiteVenta = a.Vendible, PrecioVentaEstandar = a.Vendible ? precio : null,
                    EstadoId = activo }, s, t);
                presentacionIds[a.Clave + "/" + clave] = pp.Id;
            }
        }
        ComponenteVentaSolicitud C(string clave, decimal cantidad, string? formato = null) => new() {
            ProductoId = productoIds[clave], ProductoPresentacionId = formato is null ? null : presentacionIds[clave + "/" + formato], Cantidad = cantidad };
        var especiales = new (string Clave, string Nombre, string Categoria, decimal Precio, ComponenteVentaSolicitud[] Filas)[] {
            ("ESP-VAC","Vaca Loca de la Casa","ESP",28,[C("HEL-VAI",2,"BOLA"),C("BEB-COL",1),C("EMP-V16",1),C("EMP-CUC",1),C("EMP-SER",1)]),
            ("ESP-BAN","Banana Split Tropical","POS",32,[C("HEL-CHO",1,"BOLA"),C("HEL-VAI",1,"BOLA"),C("HEL-FRE",1,"BOLA"),C("INS-BAN",1),C("INS-CRE",20),C("INS-SCH",10),C("INS-CER",1),C("EMP-COP",1),C("EMP-CUC",1),C("EMP-SER",1)]),
            ("ESP-SUN","Sundae Chocolate Intenso","POS",22,[C("HEL-CHO",2,"BOLA"),C("INS-SCH",15),C("INS-CHI",10),C("EMP-V08",1),C("EMP-CUC",1),C("EMP-SER",1)]),
            ("ESP-MIL","Milkshake Vainilla Bourbon","ESP",28,[C("HEL-VAI",180),C("INS-LEC",150),C("EMP-V16",1),C("EMP-TAP",1),C("EMP-PAJ",1)]),
            ("ESP-BRO","Brownie Tibio con Helado","POS",24,[C("INS-BRO",1),C("HEL-VAI",1,"BOLA"),C("INS-SCH",10),C("EMP-COP",1),C("EMP-CUC",1),C("EMP-SER",1)])
        };
        foreach (var a in especiales)
        {
            var p = await productos.CrearAsync(new() { SKU = "DEMO-" + a.Clave, Nombre = a.Nombre, CategoriaId = categorias[a.Categoria],
                TipoProductoId = tipos[false], UnidadBaseId = unidades["UN"], TipoSeguimientoId = seguimiento, MetodoValuacionId = metodo,
                EsVendible = true, PermiteDescuento = true, PrecioVentaReferencial = a.Precio, EstadoId = activo,
                Descripcion = "Especial compuesto; HELADERIA-DEMO-V1.", Componentes = a.Filas }, s, t);
            productoIds[a.Clave] = p.Id;
        }
        // Configuración comercial simple: permite demostrar el filtro Bajo mínimo sin inventar saldos.
        foreach (var a in Articulos)
        {
            var minimo = a.Clave switch { "INS-CER" => 20m, "BEB-TE" => 30m, "HEL-LIM" => 9000m,
                _ => decimal.Floor(a.Stock / 4) };
            db.ParametrosStockProductos.Add(new() { EmpresaId = s.EmpresaId, ProductoId = productoIds[a.Clave],
                BodegaId = bodega, UbicacionPredeterminadaId = ubicacion, StockMinimo = minimo,
                StockMaximo = a.Stock * 2, PuntoReorden = decimal.Ceiling(minimo * 1.2m),
                CantidadReorden = a.Stock, DiasReposicion = 3, EstadoId = activo,
                FechaCreacion = reloj.GetUtcNow().UtcDateTime, CreadoPorUsuarioId = s.UsuarioId });
        }
        await db.SaveChangesAsync(t);
    }
    private async Task CrearTercerosAsync(CancellationToken t)
    {
        var nombres = new[] { "Distribuidora Frío Norte", "Lácteos del Valle", "Bebidas Centroamericanas",
            "Empaques del Pacífico", "Dulces y Complementos GT" };
        for (var i = 0; i < nombres.Length; i++)
            proveedorIds.Add((await proveedores.GuardarAsync(null, new() { CodigoProveedor = $"DEMO-PROV-{i+1:00}",
                NombreProveedor = nombres[i], Correo = $"proveedor{i+1}@heladeria-demo.invalid", Direccion = "Dirección ficticia de demostración",
                DiasCredito = 30, LimiteCredito = 15000, MonedaId = moneda, EstadoId = activo }, s, t)).Id);
        var consumidores = new[] { "Familia Nube", "Familia Brisa", "Familia Aurora", "Familia Canelo", "Familia Almendra", "Familia Acacia",
            "Cafetería Luna Azul", "Café Jardín de Papel", "Eventos Horizonte", "Oficinas Río Claro", "Repostería Nube Dulce", "Club Mirador" };
        for (var i = 0; i < consumidores.Length; i++)
            clienteIds.Add((await clientes.CrearAsync(new() { CodigoCliente = $"DEMO-CLI-{i+1:00}", NombreCliente = consumidores[i],
                Correo = $"cliente{i+1}@heladeria-demo.invalid", DiasCredito = (short)(i >= 6 ? 15 : 0), LimiteCredito = i >= 6 ? 2000 : 0 }, s, t)).Id);
    }
}
