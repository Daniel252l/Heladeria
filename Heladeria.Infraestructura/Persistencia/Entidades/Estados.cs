using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class Estados
{
    public int Id { get; set; }

    public string Nombre { get; set; } = null!;

    public bool Activo { get; set; }

    public int ClaseEstadoId { get; set; }

    public string? ModuloId { get; set; }

    public virtual ICollection<AccionesSistema> AccionesSistema { get; set; } = new List<AccionesSistema>();

    public virtual ICollection<Bancos> Bancos { get; set; } = new List<Bancos>();

    public virtual ICollection<Bodegas> Bodegas { get; set; } = new List<Bodegas>();

    public virtual ICollection<Cajas> Cajas { get; set; } = new List<Cajas>();

    public virtual ICollection<Catalogos> Catalogos { get; set; } = new List<Catalogos>();

    public virtual ICollection<Categorias> Categorias { get; set; } = new List<Categorias>();

    public virtual ClaseEstados ClaseEstado { get; set; } = null!;

    public virtual ICollection<ClaseTipos> ClaseTipos { get; set; } = new List<ClaseTipos>();

    public virtual ICollection<ClasesCatalogos> ClasesCatalogos { get; set; } = new List<ClasesCatalogos>();

    public virtual ICollection<Clientes> ClientesEstado { get; set; } = new List<Clientes>();

    public virtual ICollection<Clientes> ClientesEstadoCredito { get; set; } = new List<Clientes>();

    public virtual ICollection<ClientesFidelidad> ClientesFidelidad { get; set; } = new List<ClientesFidelidad>();

    public virtual ICollection<CodigosBarrasProductos> CodigosBarrasProductos { get; set; } = new List<CodigosBarrasProductos>();

    public virtual ICollection<ConceptosMovimientos> ConceptosMovimientos { get; set; } = new List<ConceptosMovimientos>();

    public virtual ICollection<ContactosClientes> ContactosClientes { get; set; } = new List<ContactosClientes>();

    public virtual ICollection<ContactosProveedores> ContactosProveedores { get; set; } = new List<ContactosProveedores>();

    public virtual ICollection<ConversionesUnidad> ConversionesUnidad { get; set; } = new List<ConversionesUnidad>();

    public virtual ICollection<CuentasBancarias> CuentasBancarias { get; set; } = new List<CuentasBancarias>();

    public virtual ICollection<DenominacionesMoneda> DenominacionesMoneda { get; set; } = new List<DenominacionesMoneda>();

    public virtual ICollection<DireccionesClientes> DireccionesClientes { get; set; } = new List<DireccionesClientes>();

    public virtual ICollection<DocumentosAdjuntos> DocumentosAdjuntos { get; set; } = new List<DocumentosAdjuntos>();

    public virtual ICollection<Empleados> Empleados { get; set; } = new List<Empleados>();

    public virtual ICollection<Empresas> Empresas { get; set; } = new List<Empresas>();

    public virtual ICollection<EstadoTransiciones> EstadoTransiciones { get; set; } = new List<EstadoTransiciones>();

    public virtual ICollection<EstadosFlujo> EstadosFlujo { get; set; } = new List<EstadosFlujo>();

    public virtual ICollection<FacturasProveedores> FacturasProveedores { get; set; } = new List<FacturasProveedores>();

    public virtual ICollection<FlujosEstados> FlujosEstados { get; set; } = new List<FlujosEstados>();

    public virtual ICollection<FormasPago> FormasPago { get; set; } = new List<FormasPago>();

    public virtual ICollection<Impuestos> Impuestos { get; set; } = new List<Impuestos>();

    public virtual ICollection<ListasPrecios> ListasPrecios { get; set; } = new List<ListasPrecios>();

    public virtual ICollection<Marcas> Marcas { get; set; } = new List<Marcas>();

    public virtual Modulos? Modulo { get; set; }

    public virtual ICollection<Monedas> Monedas { get; set; } = new List<Monedas>();

    public virtual ICollection<NivelesFidelidad> NivelesFidelidad { get; set; } = new List<NivelesFidelidad>();

    public virtual ICollection<NotasCredito> NotasCredito { get; set; } = new List<NotasCredito>();

    public virtual ICollection<NotasDebito> NotasDebito { get; set; } = new List<NotasDebito>();

    public virtual ICollection<Pagos> Pagos { get; set; } = new List<Pagos>();

    public virtual ICollection<ParametrosStockProductos> ParametrosStockProductos { get; set; } = new List<ParametrosStockProductos>();

    public virtual ICollection<Permisos> Permisos { get; set; } = new List<Permisos>();

    public virtual ICollection<Presentaciones> Presentaciones { get; set; } = new List<Presentaciones>();

    public virtual ICollection<ProductoImpuestos> ProductoImpuestos { get; set; } = new List<ProductoImpuestos>();

    public virtual ICollection<ProductoPresentaciones> ProductoPresentaciones { get; set; } = new List<ProductoPresentaciones>();

    public virtual ICollection<Productos> Productos { get; set; } = new List<Productos>();

    public virtual ICollection<ProgramasFidelidad> ProgramasFidelidad { get; set; } = new List<ProgramasFidelidad>();

    public virtual ICollection<Promociones> Promociones { get; set; } = new List<Promociones>();

    public virtual ICollection<ProveedorProductos> ProveedorProductos { get; set; } = new List<ProveedorProductos>();

    public virtual ICollection<Proveedores> Proveedores { get; set; } = new List<Proveedores>();

    public virtual ICollection<Recetas> Recetas { get; set; } = new List<Recetas>();

    public virtual ICollection<ReglasAutorizacion> ReglasAutorizacion { get; set; } = new List<ReglasAutorizacion>();

    public virtual ICollection<Reportes> Reportes { get; set; } = new List<Reportes>();

    public virtual ICollection<SeriesDocumentos> SeriesDocumentos { get; set; } = new List<SeriesDocumentos>();

    public virtual ICollection<Sucursales> Sucursales { get; set; } = new List<Sucursales>();

    public virtual ICollection<TipoNotificaciones> TipoNotificaciones { get; set; } = new List<TipoNotificaciones>();

    public virtual ICollection<Tipos> Tipos { get; set; } = new List<Tipos>();

    public virtual ICollection<TiposProductos> TiposProductos { get; set; } = new List<TiposProductos>();

    public virtual ICollection<Ubicaciones> Ubicaciones { get; set; } = new List<Ubicaciones>();

    public virtual ICollection<UnidadesMedida> UnidadesMedida { get; set; } = new List<UnidadesMedida>();

    public virtual ICollection<Usuarios> Usuarios { get; set; } = new List<Usuarios>();

    public virtual ICollection<UsuariosSucursales> UsuariosSucursales { get; set; } = new List<UsuariosSucursales>();
}
