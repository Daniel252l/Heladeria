namespace Heladeria.Infraestructura.Persistencia.Entidades;

// Extensiones Database First; el script controlado de Fase 9 es la fuente del esquema.
public partial class Ventas
{
    public DateOnly? FechaVencimientoCredito { get; set; }
}
public partial class OrdenesCompra
{
    // NULL conserva la modalidad desconocida de documentos anteriores; no significa contado.
    public bool? EsCredito { get; set; }
    public byte[] VersionFila { get; set; } = [];
}
public partial class Pagos
{
    public byte[]? HuellaSolicitud { get; set; }
    public string? Observacion { get; set; }
}
public partial class FacturasProveedores
{
    public Guid? ClaveOperacion { get; set; }
    public byte[]? HuellaSolicitud { get; set; }
}
public partial class CuentasPorCobrar
{
    public long? SucursalId { get; set; }
    public int? MonedaId { get; set; }
    public int? UsuarioCreacionId { get; set; }
    public DateTime? FechaCreacion { get; set; }
    public int? UsuarioModificacionId { get; set; }
    public DateTime? FechaModificacion { get; set; }
    public byte[] VersionFila { get; set; } = [];
}
public partial class CuentasPorPagar
{
    public long? SucursalId { get; set; }
    public int? MonedaId { get; set; }
    public int? UsuarioCreacionId { get; set; }
    public DateTime? FechaCreacion { get; set; }
    public int? UsuarioModificacionId { get; set; }
    public DateTime? FechaModificacion { get; set; }
    public byte[] VersionFila { get; set; } = [];
}
