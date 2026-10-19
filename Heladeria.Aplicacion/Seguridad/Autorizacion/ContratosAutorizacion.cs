namespace Heladeria.Aplicacion.Seguridad.Autorizacion;

public interface IServicioPermisos
{
    Task<bool> TienePermisoAsync(
        int usuarioId,
        string rolId,
        string codigoPermiso,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> ObtenerPermisosAsync(
        int usuarioId,
        CancellationToken cancellationToken);
}

public interface IServicioValidacionSesion
{
    Task<bool> EsValidaAsync(
        int usuarioId,
        long sesionId,
        long sucursalId,
        CancellationToken cancellationToken);
}

public interface IServicioAuditoriaSeguridad
{
    Task RegistrarAccesoDenegadoAsync(
        int usuarioId,
        long? sucursalId,
        string recurso,
        string? direccionIp,
        CancellationToken cancellationToken);
}

public static class PermisosSistema
{
    public const string ReportesConsultar = "REPORTES.CONSULTAR";
    public const string AuditoriaConsultar = "AUDITORIA.CONSULTAR";
    public const string CxcConsultar = "CXC.CONSULTAR";
    public const string CxcAdministrar = "CXC.ADMINISTRAR";
    public const string CxcCobrar = "CXC.COBRAR";
    public const string CxpConsultar = "CXP.CONSULTAR";
    public const string CxpAdministrar = "CXP.ADMINISTRAR";
    public const string CxpPagar = "CXP.PAGAR";
    public const string CajaConsultar = "CAJA.CONSULTAR";
    public const string CajaAbrir = "CAJA.ABRIR";
    public const string CajaOperar = "CAJA.OPERAR";
    public const string CajaCerrar = "CAJA.CERRAR";
    public const string CajaAnular = "CAJA.ANULAR";
    public const string CajaMovimientoManual = "CAJA.MOVIMIENTO_MANUAL";
    public const string CajaCobrar = "CAJA.COBRAR";
    public const string VentasConsultar = "VENTAS.CONSULTAR";
    public const string VentasAdministrar = "VENTAS.ADMINISTRAR";
    public const string VentasVender = "VENTAS.VENDER";
    public const string VentasAnular = "VENTAS.ANULAR";
    public const string PosOperar = "POS.OPERAR";
    public const string SeguridadAdministrar = "SEGURIDAD.ADMINISTRAR";
    public const string ConfiguracionAdministrar = "CONFIGURACION.ADMINISTRAR";
    public const string ProductosConsultar = "PRODUCTOS.CONSULTAR";
    public const string ProductosAdministrar = "PRODUCTOS.ADMINISTRAR";
    public const string InventarioConsultar = "INVENTARIO.CONSULTAR";
    public const string InventarioAdministrar = "INVENTARIO.ADMINISTRAR";
    public const string ComprasConsultar = "COMPRAS.CONSULTAR";
    public const string ComprasAdministrar = "COMPRAS.ADMINISTRAR";
    public const string ComprasRecibir = "COMPRAS.RECIBIR";
    public const string ProveedoresConsultar = "PROVEEDORES.CONSULTAR";
    public const string ProveedoresAdministrar = "PROVEEDORES.ADMINISTRAR";
    public const string ProduccionConsultar = "PRODUCCION.CONSULTAR";
    public const string ProduccionAdministrar = "PRODUCCION.ADMINISTRAR";
    public const string ProduccionProducir = "PRODUCCION.PRODUCIR";
    public const string RecetasConsultar = "RECETAS.CONSULTAR";
    public const string RecetasAdministrar = "RECETAS.ADMINISTRAR";
}
