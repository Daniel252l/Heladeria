using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class FacturasProveedores
{
    public long Id { get; set; }

    public int EmpresaId { get; set; }

    public int ProveedorId { get; set; }

    public long? OrdenCompraId { get; set; }

    public string? Serie { get; set; }

    public string NumeroDocumento { get; set; } = null!;

    public DateOnly FechaDocumento { get; set; }

    public DateOnly? FechaVencimiento { get; set; }

    public int MonedaId { get; set; }

    public decimal TipoCambio { get; set; }

    public decimal SubTotal { get; set; }

    public decimal Impuestos { get; set; }

    public decimal Total { get; set; }

    public int EstadoId { get; set; }

    public virtual CuentasPorPagar? CuentasPorPagar { get; set; }

    public virtual Empresas Empresa { get; set; } = null!;

    public virtual Estados Estado { get; set; } = null!;

    public virtual Monedas Moneda { get; set; } = null!;

    public virtual OrdenesCompra? OrdenCompra { get; set; }

    public virtual Proveedores Proveedor { get; set; } = null!;
}
