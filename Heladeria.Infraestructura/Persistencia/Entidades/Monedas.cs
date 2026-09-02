using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class Monedas
{
    public int Id { get; set; }

    public int EmpresaId { get; set; }

    public string CodigoISO { get; set; } = null!;

    public string NombreMoneda { get; set; } = null!;

    public string Simbolo { get; set; } = null!;

    public bool EsMonedaBase { get; set; }

    public byte Decimales { get; set; }

    public int EstadoId { get; set; }

    public virtual ICollection<Cotizaciones> Cotizaciones { get; set; } = new List<Cotizaciones>();

    public virtual ICollection<CuentasBancarias> CuentasBancarias { get; set; } = new List<CuentasBancarias>();

    public virtual ICollection<DenominacionesMoneda> DenominacionesMoneda { get; set; } = new List<DenominacionesMoneda>();

    public virtual Empresas Empresa { get; set; } = null!;

    public virtual Estados Estado { get; set; } = null!;

    public virtual ICollection<FacturasProveedores> FacturasProveedores { get; set; } = new List<FacturasProveedores>();

    public virtual ICollection<HistoricoPreciosCompra> HistoricoPreciosCompra { get; set; } = new List<HistoricoPreciosCompra>();

    public virtual ICollection<ListasPrecios> ListasPrecios { get; set; } = new List<ListasPrecios>();

    public virtual ICollection<OrdenesCompra> OrdenesCompra { get; set; } = new List<OrdenesCompra>();

    public virtual ICollection<Pagos> Pagos { get; set; } = new List<Pagos>();

    public virtual ICollection<Proveedores> Proveedores { get; set; } = new List<Proveedores>();

    public virtual ICollection<TiposCambio> TiposCambioMonedaDestino { get; set; } = new List<TiposCambio>();

    public virtual ICollection<TiposCambio> TiposCambioMonedaOrigen { get; set; } = new List<TiposCambio>();

    public virtual ICollection<Ventas> Ventas { get; set; } = new List<Ventas>();
}
