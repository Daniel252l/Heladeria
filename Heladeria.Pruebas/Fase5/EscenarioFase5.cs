using Heladeria.Aplicacion.Compras;
using Heladeria.Aplicacion.Configuracion;
using Heladeria.Aplicacion.Seguridad.Configuracion;
using Heladeria.Infraestructura.Compras;
using Heladeria.Infraestructura.Fase4;
using Heladeria.Infraestructura.Inventario;
using Heladeria.Infraestructura.Persistencia;
using Heladeria.Infraestructura.Persistencia.Entidades;
using Heladeria.Infraestructura.Seguridad.Bootstrap;
using Heladeria.Pruebas.Fase4;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Heladeria.Pruebas.Fase5;

internal sealed class EscenarioFase5 : IAsyncDisposable
{
    internal static readonly ContextoConfiguracion Seguridad = new(1, "ADMIN", 1, 1, "127.0.0.1", "xUnit");
    internal static readonly IOptions<OpcionesSeguridad> Opciones = Options.Create(new OpcionesSeguridad());
    private readonly EscenarioFase4 _fase4;
    internal EscenarioFase5(EscenarioFase4 fase4, CoordinadorAjustesInventario coordinador)
    {
        _fase4 = fase4; Contexto = fase4.Contexto; Coordinador = coordinador;
        var inventario = new ServicioInventario(Contexto, new AuditoriaFase4(Contexto, TimeProvider.System), Opciones, TimeProvider.System, coordinador);
        Compras = new ServicioCompras(Contexto, Opciones, TimeProvider.System, coordinador, inventario);
        Proveedores = new ServicioProveedores(Contexto, Opciones, TimeProvider.System, coordinador);
        Preparador = new PreparadorFase5(Contexto, Opciones, TimeProvider.System, coordinador);
    }
    internal HeladeriaDbContext Contexto { get; }
    internal ServicioCompras Compras { get; }
    internal ServicioProveedores Proveedores { get; }
    internal PreparadorFase5 Preparador { get; }
    internal CoordinadorAjustesInventario Coordinador { get; }
    internal EscenarioFase4 Fase4 => _fase4;

    internal static async Task<EscenarioFase5> CrearAsync()
    {
        var coordinador = new CoordinadorAjustesInventario();
        var fase4 = await EscenarioFase4.CrearAsync($"fase5-{Guid.NewGuid():N}", EscenarioFase4.RaizCompartida, true, coordinador);
        var escenario = new EscenarioFase5(fase4, coordinador);
        await escenario.SemillarAsync(); return escenario;
    }

    internal async Task SemillarAsync(bool preparar = true)
    {
        Contexto.AddRange(new Modulos { Id = "SEGURIDAD", Nombre = "Seguridad", Icono = "security", Activo = true },
            new Modulos { Id = "INVENTARIO", Nombre = "Inventario", Icono = "inventory_2", Activo = true },
            new AccionesSistema { Id = 1, Codigo = "ADMINISTRAR", Nombre = "Administrar", EstadoId = 1 },
            new Permisos { Id = 1, Codigo = "SEGURIDAD.ADMINISTRAR", ModuloId = "SEGURIDAD", Recurso = "Seguridad", AccionId = 1, EstadoId = 1 },
            new Monedas { Id = 1, EmpresaId = 1, CodigoISO = "GTQ", NombreMoneda = "Quetzal", Simbolo = "Q", EsMonedaBase = true, Decimales = 2, EstadoId = 1 },
            new Monedas { Id = 2, EmpresaId = 2, CodigoISO = "USD", NombreMoneda = "Dólar", Simbolo = "$", EsMonedaBase = true, Decimales = 2, EstadoId = 1 });
        await Contexto.SaveChangesAsync(); Contexto.ChangeTracker.Clear();
        if (preparar) await Preparador.EjecutarAsync(CancellationToken.None);
    }
    internal static GuardarProveedorSolicitud Proveedor(string codigo = "P-NUEVO", string nombre = "Nuevo proveedor", int estado = 1,
        string? nit = null, string? correo = null) => new() { CodigoProveedor = codigo, NombreProveedor = nombre, EstadoId = estado, Nit = nit, Correo = correo };
    internal static GuardarCompraSolicitud Orden(decimal cantidad = 10, decimal precio = 25000, long producto = 1,
        long? presentacion = 1, int proveedor = 1, int moneda = 1, long sucursal = 1, decimal descuento = 0) => new()
    {
        SucursalId = sucursal, ProveedorId = proveedor, MonedaId = moneda, FechaEmision = new(2026, 9, 17),
        Detalles = [new() { ProductoId = producto, ProductoPresentacionId = presentacion, Cantidad = cantidad,
            PrecioUnitario = precio, PorcentajeDescuento = descuento }]
    };
    internal static CrearRecepcionSolicitud Recepcion(long linea, decimal cantidad, Guid? operacion = null, long bodega = 1, long ubicacion = 1) => new()
    {
        ClaveOperacion = operacion ?? Guid.NewGuid(), BodegaId = bodega, UbicacionId = ubicacion,
        Detalles = [new() { OrdenCompraDetalleId = linea, Cantidad = cantidad }]
    };
    internal async Task<CompraConsultaDto> ConfirmarAsync(GuardarCompraSolicitud? solicitud = null)
    {
        var orden = await Compras.GuardarAsync(null, solicitud ?? Orden(), Seguridad, CancellationToken.None);
        return await Compras.CambiarEstadoAsync(orden.Encabezado.Id, "CONFIRMADA", Seguridad, CancellationToken.None);
    }
    public ValueTask DisposeAsync() => _fase4.DisposeAsync();
}
