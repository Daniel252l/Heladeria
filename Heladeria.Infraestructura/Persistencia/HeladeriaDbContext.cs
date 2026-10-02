using System;
using System.Collections.Generic;
using Heladeria.Infraestructura.Persistencia.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Heladeria.Infraestructura.Persistencia;

public partial class HeladeriaDbContext : DbContext
{
    public HeladeriaDbContext(DbContextOptions<HeladeriaDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AccionesSistema> AccionesSistema { get; set; }

    public virtual DbSet<AplicacionesCuentasPorCobrar> AplicacionesCuentasPorCobrar { get; set; }

    public virtual DbSet<AplicacionesCuentasPorPagar> AplicacionesCuentasPorPagar { get; set; }

    public virtual DbSet<ArqueoCajaDetalles> ArqueoCajaDetalles { get; set; }

    public virtual DbSet<ArqueosCaja> ArqueosCaja { get; set; }

    public virtual DbSet<AsistenciaPersonal> AsistenciaPersonal { get; set; }

    public virtual DbSet<Bancos> Bancos { get; set; }

    public virtual DbSet<Bitacoras> Bitacoras { get; set; }

    public virtual DbSet<Bodegas> Bodegas { get; set; }

    public virtual DbSet<Cajas> Cajas { get; set; }

    public virtual DbSet<Catalogos> Catalogos { get; set; }

    public virtual DbSet<Categorias> Categorias { get; set; }

    public virtual DbSet<ClaseEstados> ClaseEstados { get; set; }

    public virtual DbSet<ClaseTipos> ClaseTipos { get; set; }

    public virtual DbSet<ClasesCatalogos> ClasesCatalogos { get; set; }

    public virtual DbSet<Clientes> Clientes { get; set; }

    public virtual DbSet<ClientesFidelidad> ClientesFidelidad { get; set; }

    public virtual DbSet<CodigosBarrasProductos> CodigosBarrasProductos { get; set; }

    public virtual DbSet<CombosDetalle> CombosDetalle { get; set; }

    public virtual DbSet<ConceptosMovimientos> ConceptosMovimientos { get; set; }

    public virtual DbSet<Configuraciones> Configuraciones { get; set; }

    public virtual DbSet<ConsumosProduccion> ConsumosProduccion { get; set; }

    public virtual DbSet<ContactosClientes> ContactosClientes { get; set; }

    public virtual DbSet<ContactosProveedores> ContactosProveedores { get; set; }

    public virtual DbSet<ConteoFisicoDetalles> ConteoFisicoDetalles { get; set; }

    public virtual DbSet<ConteosFisicos> ConteosFisicos { get; set; }

    public virtual DbSet<ConversionesUnidad> ConversionesUnidad { get; set; }

    public virtual DbSet<CorrelativosDocumentos> CorrelativosDocumentos { get; set; }

    public virtual DbSet<CostosProduccion> CostosProduccion { get; set; }

    public virtual DbSet<CostosProductos> CostosProductos { get; set; }

    public virtual DbSet<CotizacionDetalles> CotizacionDetalles { get; set; }

    public virtual DbSet<Cotizaciones> Cotizaciones { get; set; }

    public virtual DbSet<CuentasBancarias> CuentasBancarias { get; set; }

    public virtual DbSet<CuentasPorCobrar> CuentasPorCobrar { get; set; }

    public virtual DbSet<CuentasPorPagar> CuentasPorPagar { get; set; }

    public virtual DbSet<DenominacionesMoneda> DenominacionesMoneda { get; set; }

    public virtual DbSet<Departamentos> Departamentos { get; set; }

    public virtual DbSet<DevolucionVentaDetalles> DevolucionVentaDetalles { get; set; }

    public virtual DbSet<DevolucionesVentas> DevolucionesVentas { get; set; }

    public virtual DbSet<DireccionesClientes> DireccionesClientes { get; set; }

    public virtual DbSet<DocumentosAdjuntos> DocumentosAdjuntos { get; set; }

    public virtual DbSet<Empleados> Empleados { get; set; }

    public virtual DbSet<Empresas> Empresas { get; set; }

    public virtual DbSet<EstadoTransiciones> EstadoTransiciones { get; set; }

    public virtual DbSet<Estados> Estados { get; set; }

    public virtual DbSet<EstadosFlujo> EstadosFlujo { get; set; }

    public virtual DbSet<Existencias> Existencias { get; set; }

    public virtual DbSet<ExistenciasLote> ExistenciasLote { get; set; }

    public virtual DbSet<FacturasProveedores> FacturasProveedores { get; set; }

    public virtual DbSet<FlujosEstados> FlujosEstados { get; set; }

    public virtual DbSet<FormasPago> FormasPago { get; set; }

    public virtual DbSet<HistorialAccesos> HistorialAccesos { get; set; }

    public virtual DbSet<HistoricoCostosProductos> HistoricoCostosProductos { get; set; }

    public virtual DbSet<HistoricoPreciosCompra> HistoricoPreciosCompra { get; set; }

    public virtual DbSet<HistoricoPreciosVenta> HistoricoPreciosVenta { get; set; }

    public virtual DbSet<Impuestos> Impuestos { get; set; }

    public virtual DbSet<Kardex> Kardex { get; set; }

    public virtual DbSet<ListasPrecios> ListasPrecios { get; set; }

    public virtual DbSet<Lotes> Lotes { get; set; }

    public virtual DbSet<Marcas> Marcas { get; set; }

    public virtual DbSet<Modulos> Modulos { get; set; }

    public virtual DbSet<Monedas> Monedas { get; set; }

    public virtual DbSet<MovimientoDetalleSeries> MovimientoDetalleSeries { get; set; }

    public virtual DbSet<MovimientoDetalles> MovimientoDetalles { get; set; }

    public virtual DbSet<Movimientos> Movimientos { get; set; }

    public virtual DbSet<MovimientosBancarios> MovimientosBancarios { get; set; }

    public virtual DbSet<MovimientosCaja> MovimientosCaja { get; set; }

    public virtual DbSet<MovimientosPuntos> MovimientosPuntos { get; set; }

    public virtual DbSet<Municipios> Municipios { get; set; }

    public virtual DbSet<NivelesDeAccesos> NivelesDeAccesos { get; set; }

    public virtual DbSet<NivelesFidelidad> NivelesFidelidad { get; set; }

    public virtual DbSet<NotasCredito> NotasCredito { get; set; }

    public virtual DbSet<NotasDebito> NotasDebito { get; set; }

    public virtual DbSet<Notificaciones> Notificaciones { get; set; }

    public virtual DbSet<OrdenProduccionDetalles> OrdenProduccionDetalles { get; set; }

    public virtual DbSet<OrdenesCompra> OrdenesCompra { get; set; }

    public virtual DbSet<OrdenesCompraDetalle> OrdenesCompraDetalle { get; set; }

    public virtual DbSet<OrdenesProduccion> OrdenesProduccion { get; set; }

    public virtual DbSet<PagoDetalles> PagoDetalles { get; set; }

    public virtual DbSet<Pagos> Pagos { get; set; }

    public virtual DbSet<ParametrosStockProductos> ParametrosStockProductos { get; set; }

    public virtual DbSet<PedidoVentaDetalles> PedidoVentaDetalles { get; set; }

    public virtual DbSet<PedidosVenta> PedidosVenta { get; set; }

    public virtual DbSet<Permisos> Permisos { get; set; }

    public virtual DbSet<PreciosDetalle> PreciosDetalle { get; set; }

    public virtual DbSet<Presentaciones> Presentaciones { get; set; }

    public virtual DbSet<ProduccionTerminada> ProduccionTerminada { get; set; }

    public virtual DbSet<ProductoImpuestos> ProductoImpuestos { get; set; }

    public virtual DbSet<ProductoPresentaciones> ProductoPresentaciones { get; set; }

    public virtual DbSet<Productos> Productos { get; set; }

    public virtual DbSet<ProgramasFidelidad> ProgramasFidelidad { get; set; }

    public virtual DbSet<PromocionCategorias> PromocionCategorias { get; set; }

    public virtual DbSet<PromocionHorarios> PromocionHorarios { get; set; }

    public virtual DbSet<PromocionProductos> PromocionProductos { get; set; }

    public virtual DbSet<PromocionSucursales> PromocionSucursales { get; set; }

    public virtual DbSet<Promociones> Promociones { get; set; }

    public virtual DbSet<ProveedorProductos> ProveedorProductos { get; set; }

    public virtual DbSet<Proveedores> Proveedores { get; set; }

    public virtual DbSet<RecepcionCompraDetalles> RecepcionCompraDetalles { get; set; }

    public virtual DbSet<RecepcionesCompra> RecepcionesCompra { get; set; }

    public virtual DbSet<RecetaDetalles> RecetaDetalles { get; set; }

    public virtual DbSet<Recetas> Recetas { get; set; }

    public virtual DbSet<ReglasAutorizacion> ReglasAutorizacion { get; set; }

    public virtual DbSet<Reportes> Reportes { get; set; }

    public virtual DbSet<ReservaInventarioDetalles> ReservaInventarioDetalles { get; set; }

    public virtual DbSet<ReservasInventario> ReservasInventario { get; set; }

    public virtual DbSet<Roles> Roles { get; set; }

    public virtual DbSet<RolesPermisos> RolesPermisos { get; set; }

    public virtual DbSet<SeriesDocumentos> SeriesDocumentos { get; set; }

    public virtual DbSet<SeriesProductos> SeriesProductos { get; set; }

    public virtual DbSet<SesionesUsuarios> SesionesUsuarios { get; set; }

    public virtual DbSet<SolicitudesAutorizacion> SolicitudesAutorizacion { get; set; }

    public virtual DbSet<Sucursales> Sucursales { get; set; }

    public virtual DbSet<TipoNotificaciones> TipoNotificaciones { get; set; }

    public virtual DbSet<Tipos> Tipos { get; set; }

    public virtual DbSet<TiposCambio> TiposCambio { get; set; }

    public virtual DbSet<TiposProductos> TiposProductos { get; set; }

    public virtual DbSet<TrasladoInventarioDetalles> TrasladoInventarioDetalles { get; set; }

    public virtual DbSet<TrasladosInventario> TrasladosInventario { get; set; }

    public virtual DbSet<TurnosCaja> TurnosCaja { get; set; }

    public virtual DbSet<Ubicaciones> Ubicaciones { get; set; }

    public virtual DbSet<UnidadesMedida> UnidadesMedida { get; set; }

    public virtual DbSet<Usuarios> Usuarios { get; set; }

    public virtual DbSet<UsuariosSucursales> UsuariosSucursales { get; set; }

    public virtual DbSet<VentaImpuestos> VentaImpuestos { get; set; }

    public virtual DbSet<Ventas> Ventas { get; set; }

    public virtual DbSet<VentasDetalle> VentasDetalle { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AccionesSistema>(entity =>
        {
            entity.HasIndex(e => e.Codigo, "UQ_AccionesSistema_Codigo").IsUnique();

            entity.Property(e => e.Codigo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.HasOne(d => d.Estado).WithMany(p => p.AccionesSistema)
                .HasForeignKey(d => d.EstadoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AccionesSistema_Estado");
        });

        modelBuilder.Entity<AplicacionesCuentasPorCobrar>(entity =>
        {
            entity.HasIndex(e => new { e.CuentaPorCobrarId, e.PagoId }, "UQ_AplicacionesCuentasPorCobrar").IsUnique();

            entity.Property(e => e.FechaAplicacion).HasPrecision(3);
            entity.Property(e => e.MontoAplicado).HasColumnType("decimal(12, 2)");

            entity.HasOne(d => d.CuentaPorCobrar).WithMany(p => p.AplicacionesCuentasPorCobrar)
                .HasForeignKey(d => d.CuentaPorCobrarId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AplicacionesCuentasPorCobrar_Cuenta");

            entity.HasOne(d => d.Pago).WithMany(p => p.AplicacionesCuentasPorCobrar)
                .HasForeignKey(d => d.PagoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AplicacionesCuentasPorCobrar_Pago");

            entity.HasOne(d => d.Usuario).WithMany(p => p.AplicacionesCuentasPorCobrar)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AplicacionesCuentasPorCobrar_Usuario");
        });

        modelBuilder.Entity<AplicacionesCuentasPorPagar>(entity =>
        {
            entity.HasIndex(e => new { e.CuentaPorPagarId, e.PagoId }, "UQ_AplicacionesCuentasPorPagar").IsUnique();

            entity.Property(e => e.FechaAplicacion).HasPrecision(3);
            entity.Property(e => e.MontoAplicado).HasColumnType("decimal(12, 2)");

            entity.HasOne(d => d.CuentaPorPagar).WithMany(p => p.AplicacionesCuentasPorPagar)
                .HasForeignKey(d => d.CuentaPorPagarId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AplicacionesCuentasPorPagar_Cuenta");

            entity.HasOne(d => d.Pago).WithMany(p => p.AplicacionesCuentasPorPagar)
                .HasForeignKey(d => d.PagoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AplicacionesCuentasPorPagar_Pago");

            entity.HasOne(d => d.Usuario).WithMany(p => p.AplicacionesCuentasPorPagar)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AplicacionesCuentasPorPagar_Usuario");
        });

        modelBuilder.Entity<ArqueoCajaDetalles>(entity =>
        {
            entity.HasIndex(e => new { e.ArqueoCajaId, e.DenominacionId }, "UQ_ArqueoCajaDetalles").IsUnique();

            entity.Property(e => e.Monto).HasColumnType("decimal(12, 2)");

            entity.HasOne(d => d.ArqueoCaja).WithMany(p => p.ArqueoCajaDetalles)
                .HasForeignKey(d => d.ArqueoCajaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ArqueoCajaDetalles_Arqueo");

            entity.HasOne(d => d.Denominacion).WithMany(p => p.ArqueoCajaDetalles)
                .HasForeignKey(d => d.DenominacionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ArqueoCajaDetalles_Denominacion");
        });

        modelBuilder.Entity<ArqueosCaja>(entity =>
        {
            entity.Property(e => e.Diferencia)
                .HasComputedColumnSql("([MontoContado]-[MontoEsperado])", true)
                .HasColumnType("decimal(13, 2)");
            entity.Property(e => e.Fecha).HasPrecision(3);
            entity.Property(e => e.MontoContado).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.MontoEsperado).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.Observaciones)
                .HasMaxLength(500)
                .IsUnicode(false);

            entity.HasOne(d => d.TurnoCaja).WithMany(p => p.ArqueosCaja)
                .HasForeignKey(d => d.TurnoCajaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ArqueosCaja_Turno");

            entity.HasOne(d => d.Usuario).WithMany(p => p.ArqueosCaja)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ArqueosCaja_Usuario");
        });

        modelBuilder.Entity<AsistenciaPersonal>(entity =>
        {
            entity.Property(e => e.FechaHoraEntrada).HasColumnType("datetime");
            entity.Property(e => e.FechaHoraSalida).HasColumnType("datetime");

            entity.HasOne(d => d.Empleado).WithMany(p => p.AsistenciaPersonal)
                .HasForeignKey(d => d.EmpleadoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Asistencia_Empleado");

            entity.HasOne(d => d.Sucursal).WithMany(p => p.AsistenciaPersonal)
                .HasForeignKey(d => d.SucursalId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Asistencia_Sucursal");

            entity.HasOne(d => d.TipoTurno).WithMany(p => p.AsistenciaPersonal)
                .HasForeignKey(d => d.TipoTurnoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Asistencia_Turno");
        });

        modelBuilder.Entity<Bancos>(entity =>
        {
            entity.HasIndex(e => new { e.EmpresaId, e.Codigo }, "UQ_Bancos_Empresa_Codigo").IsUnique();

            entity.Property(e => e.Codigo)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Nombre)
                .HasMaxLength(150)
                .IsUnicode(false);

            entity.HasOne(d => d.Empresa).WithMany(p => p.Bancos)
                .HasForeignKey(d => d.EmpresaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Bancos_Empresa");

            entity.HasOne(d => d.Estado).WithMany(p => p.Bancos)
                .HasForeignKey(d => d.EstadoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Bancos_Estado");
        });

        modelBuilder.Entity<Bitacoras>(entity =>
        {
            entity.Property(e => e.Accion)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Comentario)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.DireccionIp)
                .HasMaxLength(45)
                .IsUnicode(false);
            entity.Property(e => e.Entidad)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.EntidadId)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Equipo)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.Fecha).HasPrecision(3);
            entity.Property(e => e.ModuloId)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.ValorAnterior).IsUnicode(false);
            entity.Property(e => e.ValorNuevo).IsUnicode(false);

            entity.HasOne(d => d.Empresa).WithMany(p => p.Bitacoras)
                .HasForeignKey(d => d.EmpresaId)
                .HasConstraintName("FK_Bitacoras_Empresa");

            entity.HasOne(d => d.Modulo).WithMany(p => p.Bitacoras)
                .HasForeignKey(d => d.ModuloId)
                .HasConstraintName("FK_Bitacoras_Modulo");

            entity.HasOne(d => d.Sucursal).WithMany(p => p.Bitacoras)
                .HasForeignKey(d => d.SucursalId)
                .HasConstraintName("FK_Bitacoras_Sucursal");

            entity.HasOne(d => d.Usuario).WithMany(p => p.Bitacoras)
                .HasForeignKey(d => d.UsuarioId)
                .HasConstraintName("FK_Bitacoras_Usuario");
        });

        modelBuilder.Entity<Bodegas>(entity =>
        {
            entity.HasIndex(e => new { e.EmpresaId, e.Codigo }, "UQ_Bodegas_Empresa_Codigo").IsUnique();

            entity.HasIndex(e => new { e.EmpresaId, e.Id }, "UQ_Bodegas_Empresa_Id").IsUnique();

            entity.HasIndex(e => new { e.EmpresaId, e.SucursalId, e.Id }, "UQ_Bodegas_Empresa_Sucursal_Id").IsUnique();

            entity.HasIndex(e => new { e.EmpresaId, e.SucursalId, e.Nombre }, "UQ_Bodegas_Empresa_Sucursal_Nombre").IsUnique();

            entity.Property(e => e.Codigo)
                .HasMaxLength(25)
                .IsUnicode(false);
            entity.Property(e => e.Descripcion)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.FechaCreacion).HasPrecision(3);
            entity.Property(e => e.FechaModificacion).HasPrecision(3);
            entity.Property(e => e.Nombre)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.VersionFila)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.CreadoPorUsuario).WithMany(p => p.BodegasCreadoPorUsuario)
                .HasForeignKey(d => d.CreadoPorUsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Bodegas_CreadoPor");

            entity.HasOne(d => d.Empresa).WithMany(p => p.Bodegas)
                .HasForeignKey(d => d.EmpresaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Bodegas_Empresa");

            entity.HasOne(d => d.Estado).WithMany(p => p.Bodegas)
                .HasForeignKey(d => d.EstadoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Bodegas_Estado");

            entity.HasOne(d => d.ModificadoPorUsuario).WithMany(p => p.BodegasModificadoPorUsuario)
                .HasForeignKey(d => d.ModificadoPorUsuarioId)
                .HasConstraintName("FK_Bodegas_ModificadoPor");

            entity.HasOne(d => d.Sucursal).WithMany(p => p.Bodegas)
                .HasForeignKey(d => d.SucursalId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Bodegas_Sucursal");

            entity.HasOne(d => d.BodegasNavigation).WithMany(p => p.InverseBodegasNavigation)
                .HasPrincipalKey(p => new { p.EmpresaId, p.Id })
                .HasForeignKey(d => new { d.EmpresaId, d.BodegaPadreId })
                .HasConstraintName("FK_Bodegas_Padre");
        });

        modelBuilder.Entity<Cajas>(entity =>
        {
            entity.HasIndex(e => new { e.SucursalId, e.Codigo }, "UQ_Cajas_Sucursal_Codigo").IsUnique();

            entity.Property(e => e.Codigo)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.HasOne(d => d.Empresa).WithMany(p => p.Cajas)
                .HasForeignKey(d => d.EmpresaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Cajas_Empresa");

            entity.HasOne(d => d.Estado).WithMany(p => p.Cajas)
                .HasForeignKey(d => d.EstadoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Cajas_Estado");

            entity.HasOne(d => d.Sucursal).WithMany(p => p.Cajas)
                .HasForeignKey(d => d.SucursalId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Cajas_Sucursal");
        });

        modelBuilder.Entity<Catalogos>(entity =>
        {
            entity.HasIndex(e => new { e.ClaseCatalogoId, e.EmpresaId, e.Codigo }, "UQ_Catalogos_Clase_Empresa_Codigo").IsUnique();

            entity.HasIndex(e => new { e.ClaseCatalogoId, e.EmpresaId, e.Nombre }, "UQ_Catalogos_Clase_Empresa_Nombre").IsUnique();

            entity.HasIndex(e => new { e.ClaseCatalogoId, e.Id }, "UQ_Catalogos_Clase_Id").IsUnique();

            entity.Property(e => e.Codigo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Descripcion)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.FechaCreacion).HasPrecision(3);
            entity.Property(e => e.FechaModificacion).HasPrecision(3);
            entity.Property(e => e.Nombre)
                .HasMaxLength(150)
                .IsUnicode(false);

            entity.HasOne(d => d.ClaseCatalogo).WithMany(p => p.Catalogos)
                .HasForeignKey(d => d.ClaseCatalogoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Catalogos_Clase");

            entity.HasOne(d => d.CreadoPorUsuario).WithMany(p => p.CatalogosCreadoPorUsuario)
                .HasForeignKey(d => d.CreadoPorUsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Catalogos_CreadoPor");

            entity.HasOne(d => d.Empresa).WithMany(p => p.Catalogos)
                .HasForeignKey(d => d.EmpresaId)
                .HasConstraintName("FK_Catalogos_Empresa");

            entity.HasOne(d => d.Estado).WithMany(p => p.Catalogos)
                .HasForeignKey(d => d.EstadoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Catalogos_Estado");

            entity.HasOne(d => d.ModificadoPorUsuario).WithMany(p => p.CatalogosModificadoPorUsuario)
                .HasForeignKey(d => d.ModificadoPorUsuarioId)
                .HasConstraintName("FK_Catalogos_ModificadoPor");

            entity.HasOne(d => d.CatalogosNavigation).WithMany(p => p.InverseCatalogosNavigation)
                .HasPrincipalKey(p => new { p.ClaseCatalogoId, p.Id })
                .HasForeignKey(d => new { d.ClaseCatalogoId, d.CatalogoPadreId })
                .HasConstraintName("FK_Catalogos_Padre");
        });

        modelBuilder.Entity<Categorias>(entity =>
        {
            entity.HasIndex(e => new { e.EmpresaId, e.Codigo }, "UQ_Categorias_Empresa_Codigo").IsUnique();

            entity.HasIndex(e => new { e.EmpresaId, e.Id }, "UQ_Categorias_Empresa_Id").IsUnique();

            entity.HasIndex(e => new { e.EmpresaId, e.Nombre }, "UQ_Categorias_Empresa_Nombre").IsUnique();

            entity.Property(e => e.Codigo)
                .HasMaxLength(25)
                .IsUnicode(false);
            entity.Property(e => e.Descripcion)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.FechaCreacion).HasPrecision(3);
            entity.Property(e => e.FechaModificacion).HasPrecision(3);
            entity.Property(e => e.Nombre)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.VersionFila)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.CreadoPorUsuario).WithMany(p => p.CategoriasCreadoPorUsuario)
                .HasForeignKey(d => d.CreadoPorUsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Categorias_CreadoPor");

            entity.HasOne(d => d.Empresa).WithMany(p => p.Categorias)
                .HasForeignKey(d => d.EmpresaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Categorias_Empresa");

            entity.HasOne(d => d.Estado).WithMany(p => p.Categorias)
                .HasForeignKey(d => d.EstadoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Categorias_Estado");

            entity.HasOne(d => d.ModificadoPorUsuario).WithMany(p => p.CategoriasModificadoPorUsuario)
                .HasForeignKey(d => d.ModificadoPorUsuarioId)
                .HasConstraintName("FK_Categorias_ModificadoPor");

            entity.HasOne(d => d.CategoriasNavigation).WithMany(p => p.InverseCategoriasNavigation)
                .HasPrincipalKey(p => new { p.EmpresaId, p.Id })
                .HasForeignKey(d => new { d.EmpresaId, d.CategoriaPadreId })
                .HasConstraintName("FK_Categorias_Padre");
        });

        modelBuilder.Entity<ClaseEstados>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Nombre)
                .HasMaxLength(200)
                .IsUnicode(false);
        });

        modelBuilder.Entity<ClaseTipos>(entity =>
        {
            entity.HasIndex(e => e.Nombre, "UQ_ClaseTipos_Nombre").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.HasOne(d => d.Estado).WithMany(p => p.ClaseTipos)
                .HasForeignKey(d => d.EstadoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ClaseTipos_Estado");
        });

        modelBuilder.Entity<ClasesCatalogos>(entity =>
        {
            entity.HasIndex(e => e.Codigo, "UQ_ClasesCatalogos_Codigo").IsUnique();

            entity.HasIndex(e => e.Nombre, "UQ_ClasesCatalogos_Nombre").IsUnique();

            entity.Property(e => e.Codigo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Descripcion)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.FechaCreacion).HasPrecision(3);
            entity.Property(e => e.FechaModificacion).HasPrecision(3);
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.HasOne(d => d.CreadoPorUsuario).WithMany(p => p.ClasesCatalogosCreadoPorUsuario)
                .HasForeignKey(d => d.CreadoPorUsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ClasesCatalogos_CreadoPor");

            entity.HasOne(d => d.Estado).WithMany(p => p.ClasesCatalogos)
                .HasForeignKey(d => d.EstadoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ClasesCatalogos_Estado");

            entity.HasOne(d => d.ModificadoPorUsuario).WithMany(p => p.ClasesCatalogosModificadoPorUsuario)
                .HasForeignKey(d => d.ModificadoPorUsuarioId)
                .HasConstraintName("FK_ClasesCatalogos_ModificadoPor");
        });

        modelBuilder.Entity<Clientes>(entity =>
        {
            entity.HasIndex(e => new { e.EmpresaId, e.CodigoCliente }, "UQ_ClientesCodigo").IsUnique();

            entity.Property(e => e.CodigoCliente)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Correo)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.LimiteCredito).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.Nit)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.NombreCliente)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.Telefono)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.Empresa).WithMany(p => p.Clientes)
                .HasForeignKey(d => d.EmpresaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Clientes_Empresa");

            entity.HasOne(d => d.EstadoCredito).WithMany(p => p.ClientesEstadoCredito)
                .HasForeignKey(d => d.EstadoCreditoId)
                .HasConstraintName("FK_Clientes_EstadoCredito");

            entity.HasOne(d => d.Estado).WithMany(p => p.ClientesEstado)
                .HasForeignKey(d => d.EstadoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Clientes_Estado");

            entity.HasOne(d => d.ListaPrecio).WithMany(p => p.Clientes)
                .HasForeignKey(d => d.ListaPrecioId)
                .HasConstraintName("FK_Clientes_ListaPrecio");
        });

        modelBuilder.Entity<ClientesFidelidad>(entity =>
        {
            entity.HasIndex(e => new { e.ClienteId, e.ProgramaId }, "UQ_ClientesFidelidad").IsUnique();

            entity.HasOne(d => d.Cliente).WithMany(p => p.ClientesFidelidad)
                .HasForeignKey(d => d.ClienteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ClientesFidelidad_Cliente");

            entity.HasOne(d => d.Estado).WithMany(p => p.ClientesFidelidad)
                .HasForeignKey(d => d.EstadoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ClientesFidelidad_Estado");

            entity.HasOne(d => d.Nivel).WithMany(p => p.ClientesFidelidad)
                .HasForeignKey(d => d.NivelId)
                .HasConstraintName("FK_ClientesFidelidad_Nivel");

            entity.HasOne(d => d.Programa).WithMany(p => p.ClientesFidelidad)
                .HasForeignKey(d => d.ProgramaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ClientesFidelidad_Programa");
        });

        modelBuilder.Entity<CodigosBarrasProductos>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_CodigosBarrasProducto");

            entity.HasIndex(e => new { e.EmpresaId, e.CodigoBarras }, "UQ_CodigosBarras_Empresa_Codigo").IsUnique();

            entity.Property(e => e.CodigoBarras)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.FechaCreacion).HasPrecision(3);

            entity.HasOne(d => d.CreadoPorUsuario).WithMany(p => p.CodigosBarrasProductos)
                .HasForeignKey(d => d.CreadoPorUsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CodigosBarras_CreadoPor");

            entity.HasOne(d => d.Empresa).WithMany(p => p.CodigosBarrasProductos)
                .HasForeignKey(d => d.EmpresaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CodigosBarrasProductos_Empresa");

            entity.HasOne(d => d.Estado).WithMany(p => p.CodigosBarrasProductos)
                .HasForeignKey(d => d.EstadoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CodigosBarrasProductos_Estado");

            entity.HasOne(d => d.TipoCodigoBarras).WithMany(p => p.CodigosBarrasProductos)
                .HasForeignKey(d => d.TipoCodigoBarrasId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CodigosBarras_Tipo");

            entity.HasOne(d => d.Productos).WithMany(p => p.CodigosBarrasProductos)
                .HasPrincipalKey(p => new { p.EmpresaId, p.Id })
                .HasForeignKey(d => new { d.EmpresaId, d.ProductoId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CodigosBarras_Producto");

            entity.HasOne(d => d.ProductoPresentaciones).WithMany(p => p.CodigosBarrasProductos)
                .HasPrincipalKey(p => new { p.EmpresaId, p.ProductoId, p.Id })
                .HasForeignKey(d => new { d.EmpresaId, d.ProductoId, d.ProductoPresentacionId })
                .HasConstraintName("FK_CodigosBarras_Presentacion");
        });

        modelBuilder.Entity<CombosDetalle>(entity =>
        {
            entity.Property(e => e.Cantidad).HasDefaultValue(1);

            entity.HasOne(d => d.Empresa).WithMany(p => p.CombosDetalle)
                .HasForeignKey(d => d.EmpresaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CombosDetalle_Empresa");

            entity.HasOne(d => d.Productos).WithMany(p => p.CombosDetalleProductos)
                .HasPrincipalKey(p => new { p.EmpresaId, p.Id })
                .HasForeignKey(d => new { d.EmpresaId, d.ComboProductoId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Combos_ProductoPadre");

            entity.HasOne(d => d.ProductosNavigation).WithMany(p => p.CombosDetalleProductosNavigation)
                .HasPrincipalKey(p => new { p.EmpresaId, p.Id })
                .HasForeignKey(d => new { d.EmpresaId, d.ProductoIncluidoId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Combos_ProductoHijo");
        });

        modelBuilder.Entity<ConceptosMovimientos>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_ConceptosMovimiento");

            entity.HasIndex(e => new { e.EmpresaId, e.Codigo }, "UQ_Conceptos_Empresa_Codigo").IsUnique();

            entity.HasIndex(e => new { e.EmpresaId, e.Id }, "UQ_Conceptos_Empresa_Id").IsUnique();

            entity.HasIndex(e => new { e.EmpresaId, e.Nombre }, "UQ_Conceptos_Empresa_Nombre").IsUnique();

            entity.Property(e => e.Codigo)
                .HasMaxLength(25)
                .IsUnicode(false);
            entity.Property(e => e.FechaCreacion).HasPrecision(3);
            entity.Property(e => e.FechaModificacion).HasPrecision(3);
            entity.Property(e => e.Nombre)
                .HasMaxLength(120)
                .IsUnicode(false);
            entity.Property(e => e.TipoOperacion)
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.VersionFila)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.CreadoPorUsuario).WithMany(p => p.ConceptosMovimientosCreadoPorUsuario)
                .HasForeignKey(d => d.CreadoPorUsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ConceptosMovimientos_CreadoPor");

            entity.HasOne(d => d.Empresa).WithMany(p => p.ConceptosMovimientos)
                .HasForeignKey(d => d.EmpresaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ConceptosMovimientos_Empresa");

            entity.HasOne(d => d.Estado).WithMany(p => p.ConceptosMovimientos)
                .HasForeignKey(d => d.EstadoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ConceptosMovimientos_Estado");

            entity.HasOne(d => d.ModificadoPorUsuario).WithMany(p => p.ConceptosMovimientosModificadoPorUsuario)
                .HasForeignKey(d => d.ModificadoPorUsuarioId)
                .HasConstraintName("FK_ConceptosMovimientos_ModificadoPor");
        });

        modelBuilder.Entity<Configuraciones>(entity =>
        {
            entity.HasIndex(e => new { e.ModuloId, e.TipoConfiguracionId, e.Descripcion }, "UQ_Configuraciones_ModuloId_Descripcion").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Descripcion)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.ModuloId)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.PorDefecto)
                .HasMaxLength(300)
                .IsUnicode(false);

            entity.HasOne(d => d.Modulo).WithMany(p => p.Configuraciones)
                .HasForeignKey(d => d.ModuloId)
                .HasConstraintName("FK_Configuraciones_ModuloId");

            entity.HasOne(d => d.TipoConfiguracion).WithMany(p => p.ConfiguracionesTipoConfiguracion)
                .HasForeignKey(d => d.TipoConfiguracionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Configuraciones_TipoConfiguracionId");

            entity.HasOne(d => d.TipoValor).WithMany(p => p.ConfiguracionesTipoValor)
                .HasForeignKey(d => d.TipoValorId)
                .HasConstraintName("FK_Configuraciones_TipoValorId");
        });

        modelBuilder.Entity<ConsumosProduccion>(entity =>
        {
            entity.Property(e => e.CantidadConsumida).HasColumnType("decimal(19, 6)");
            entity.Property(e => e.CostoUnitario).HasColumnType("decimal(19, 6)");
            entity.Property(e => e.FechaConsumo).HasPrecision(3);

            entity.HasOne(d => d.Lote).WithMany(p => p.ConsumosProduccion)
                .HasForeignKey(d => d.LoteId)
                .HasConstraintName("FK_ConsumosProduccion_Lote");

            entity.HasOne(d => d.OrdenProduccion).WithMany(p => p.ConsumosProduccion)
                .HasForeignKey(d => d.OrdenProduccionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ConsumosProduccion_Orden");

            entity.HasOne(d => d.Producto).WithMany(p => p.ConsumosProduccion)
                .HasForeignKey(d => d.ProductoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ConsumosProduccion_Producto");

            entity.HasOne(d => d.UnidadMedida).WithMany(p => p.ConsumosProduccion)
                .HasForeignKey(d => d.UnidadMedidaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ConsumosProduccion_Unidad");
        });

        modelBuilder.Entity<ContactosClientes>(entity =>
        {
            entity.Property(e => e.Cargo)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Correo)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Nombre)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.Telefono)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.Cliente).WithMany(p => p.ContactosClientes)
                .HasForeignKey(d => d.ClienteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ContactosClientes_Cliente");

            entity.HasOne(d => d.Estado).WithMany(p => p.ContactosClientes)
                .HasForeignKey(d => d.EstadoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ContactosClientes_Estado");
        });

        modelBuilder.Entity<ContactosProveedores>(entity =>
        {
            entity.Property(e => e.Cargo)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Correo)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Nombre)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.Telefono)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.Estado).WithMany(p => p.ContactosProveedores)
                .HasForeignKey(d => d.EstadoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ContactosProveedores_Estado");

            entity.HasOne(d => d.Proveedor).WithMany(p => p.ContactosProveedores)
                .HasForeignKey(d => d.ProveedorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ContactosProveedores_Proveedor");
        });

        modelBuilder.Entity<ConteoFisicoDetalles>(entity =>
        {
            entity.HasIndex(e => new { e.EmpresaId, e.ConteoFisicoId, e.NumeroLinea }, "UQ_ConteoFisicoDetalles_Linea").IsUnique();

            entity.Property(e => e.CantidadFinal).HasColumnType("decimal(19, 6)");
            entity.Property(e => e.CantidadPrimerConteo).HasColumnType("decimal(19, 6)");
            entity.Property(e => e.CantidadReconteo).HasColumnType("decimal(19, 6)");
            entity.Property(e => e.Diferencia)
                .HasComputedColumnSql("([CantidadFinal]-[ExistenciaSistema])", true)
                .HasColumnType("decimal(20, 6)");
            entity.Property(e => e.ExistenciaSistema).HasColumnType("decimal(19, 6)");
            entity.Property(e => e.FechaConteo).HasPrecision(3);
            entity.Property(e => e.Observaciones)
                .HasMaxLength(500)
                .IsUnicode(false);

            entity.HasOne(d => d.ContadoPorUsuario).WithMany(p => p.ConteoFisicoDetalles)
                .HasForeignKey(d => d.ContadoPorUsuarioId)
                .HasConstraintName("FK_ConteoFisicoDetalles_ContadoPor");

            entity.HasOne(d => d.Empresa).WithMany(p => p.ConteoFisicoDetalles)
                .HasForeignKey(d => d.EmpresaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ConteoFisicoDetalles_Empresa");

            entity.HasOne(d => d.Lote).WithMany(p => p.ConteoFisicoDetalles)
                .HasForeignKey(d => d.LoteId)
                .HasConstraintName("FK_ConteoFisicoDetalles_Lote");

            entity.HasOne(d => d.Producto).WithMany(p => p.ConteoFisicoDetalles)
                .HasForeignKey(d => d.ProductoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ConteoFisicoDetalles_Producto");

            entity.HasOne(d => d.SerieProducto).WithMany(p => p.ConteoFisicoDetalles)
                .HasForeignKey(d => d.SerieProductoId)
                .HasConstraintName("FK_ConteoFisicoDetalles_Serie");

            entity.HasOne(d => d.Ubicacion).WithMany(p => p.ConteoFisicoDetalles)
                .HasForeignKey(d => d.UbicacionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ConteoFisicoDetalles_Ubicacion");

            entity.HasOne(d => d.ConteosFisicos).WithMany(p => p.ConteoFisicoDetalles)
                .HasPrincipalKey(p => new { p.EmpresaId, p.Id })
                .HasForeignKey(d => new { d.EmpresaId, d.ConteoFisicoId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ConteoFisicoDetalles_Conteo");
        });

        modelBuilder.Entity<ConteosFisicos>(entity =>
        {
            entity.HasIndex(e => new { e.EmpresaId, e.CodigoConteo }, "UQ_ConteosFisicos_Empresa_Codigo").IsUnique();

            entity.HasIndex(e => new { e.EmpresaId, e.Id }, "UQ_ConteosFisicos_Empresa_Id").IsUnique();

            entity.Property(e => e.CodigoConteo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.FechaCierre).HasPrecision(3);
            entity.Property(e => e.FechaCorte).HasPrecision(3);
            entity.Property(e => e.FechaInicio).HasPrecision(3);
            entity.Property(e => e.Observaciones)
                .HasMaxLength(1000)
                .IsUnicode(false);
            entity.Property(e => e.VersionFila)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.Bodega).WithMany(p => p.ConteosFisicos)
                .HasForeignKey(d => d.BodegaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ConteosFisicos_Bodega");

            entity.HasOne(d => d.CerradoPorUsuario).WithMany(p => p.ConteosFisicosCerradoPorUsuario)
                .HasForeignKey(d => d.CerradoPorUsuarioId)
                .HasConstraintName("FK_ConteosFisicos_CerradoPor");

            entity.HasOne(d => d.CreadoPorUsuario).WithMany(p => p.ConteosFisicosCreadoPorUsuario)
                .HasForeignKey(d => d.CreadoPorUsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ConteosFisicos_CreadoPor");

            entity.HasOne(d => d.Empresa).WithMany(p => p.ConteosFisicos)
                .HasForeignKey(d => d.EmpresaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ConteosFisicos_Empresa");

            entity.HasOne(d => d.IniciadoPorUsuario).WithMany(p => p.ConteosFisicosIniciadoPorUsuario)
                .HasForeignKey(d => d.IniciadoPorUsuarioId)
                .HasConstraintName("FK_ConteosFisicos_IniciadoPor");

            entity.HasOne(d => d.MovimientoAjuste).WithMany(p => p.ConteosFisicos)
                .HasForeignKey(d => d.MovimientoAjusteId)
                .HasConstraintName("FK_ConteosFisicos_MovimientoAjuste");

            entity.HasOne(d => d.Sucursal).WithMany(p => p.ConteosFisicos)
                .HasForeignKey(d => d.SucursalId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ConteosFisicos_Sucursal");

            entity.HasOne(d => d.TipoConteo).WithMany(p => p.ConteosFisicos)
                .HasForeignKey(d => d.TipoConteoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ConteosFisicos_Tipo");

            entity.HasOne(d => d.EstadosFlujo).WithMany(p => p.ConteosFisicos)
                .HasPrincipalKey(p => new { p.FlujoEstadoId, p.Id })
                .HasForeignKey(d => new { d.FlujoEstadoId, d.EstadoId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ConteosFisicos_Estado");
        });

        modelBuilder.Entity<ConversionesUnidad>(entity =>
        {
            entity.HasIndex(e => new { e.EmpresaId, e.UnidadOrigenId, e.UnidadDestinoId }, "UQ_ConversionesUnidad").IsUnique();

            entity.Property(e => e.Factor).HasColumnType("decimal(19, 8)");

            entity.HasOne(d => d.Empresa).WithMany(p => p.ConversionesUnidad)
                .HasForeignKey(d => d.EmpresaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ConversionesUnidad_Empresa");

            entity.HasOne(d => d.Estado).WithMany(p => p.ConversionesUnidad)
                .HasForeignKey(d => d.EstadoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ConversionesUnidad_Estado");

            entity.HasOne(d => d.UnidadDestino).WithMany(p => p.ConversionesUnidadUnidadDestino)
                .HasForeignKey(d => d.UnidadDestinoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ConversionesUnidad_Destino");

            entity.HasOne(d => d.UnidadOrigen).WithMany(p => p.ConversionesUnidadUnidadOrigen)
                .HasForeignKey(d => d.UnidadOrigenId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ConversionesUnidad_Origen");
        });

        modelBuilder.Entity<CorrelativosDocumentos>(entity =>
        {
            entity.HasIndex(e => e.SerieDocumentoId, "UQ_CorrelativosDocumentos_Serie").IsUnique();

            entity.Property(e => e.FechaModificacion).HasPrecision(3);
            entity.Property(e => e.VersionFila)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.SerieDocumento).WithOne(p => p.CorrelativosDocumentos)
                .HasForeignKey<CorrelativosDocumentos>(d => d.SerieDocumentoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CorrelativosDocumentos_Serie");
        });

        modelBuilder.Entity<CostosProduccion>(entity =>
        {
            entity.Property(e => e.Descripcion)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.FechaRegistro).HasPrecision(3);
            entity.Property(e => e.Monto).HasColumnType("decimal(19, 6)");

            entity.HasOne(d => d.OrdenProduccion).WithMany(p => p.CostosProduccion)
                .HasForeignKey(d => d.OrdenProduccionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CostosProduccion_Orden");

            entity.HasOne(d => d.TipoCosto).WithMany(p => p.CostosProduccion)
                .HasForeignKey(d => d.TipoCostoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CostosProduccion_Tipo");

            entity.HasOne(d => d.Usuario).WithMany(p => p.CostosProduccion)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CostosProduccion_Usuario");
        });

        modelBuilder.Entity<CostosProductos>(entity =>
        {
            entity.HasIndex(e => new { e.EmpresaId, e.ProductoId }, "UQ_CostosProductos_Empresa_Producto").IsUnique();

            entity.Property(e => e.CostoActual).HasColumnType("decimal(19, 6)");
            entity.Property(e => e.FechaCalculo).HasPrecision(3);

            entity.HasOne(d => d.Empresa).WithMany(p => p.CostosProductos)
                .HasForeignKey(d => d.EmpresaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CostosProductos_Empresa");

            entity.HasOne(d => d.MetodoValuacion).WithMany(p => p.CostosProductos)
                .HasForeignKey(d => d.MetodoValuacionId)
                .HasConstraintName("FK_CostosProductos_Metodo");

            entity.HasOne(d => d.Producto).WithMany(p => p.CostosProductos)
                .HasForeignKey(d => d.ProductoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CostosProductos_Producto");
        });

        modelBuilder.Entity<CotizacionDetalles>(entity =>
        {
            entity.HasIndex(e => new { e.CotizacionId, e.NumeroLinea }, "UQ_CotizacionDetalles_Linea").IsUnique();

            entity.Property(e => e.Cantidad).HasColumnType("decimal(19, 6)");
            entity.Property(e => e.Descuento).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.Impuesto).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.PrecioUnitario).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.TotalLinea)
                .HasComputedColumnSql("(([Cantidad]*[PrecioUnitario]-[Descuento])+[Impuesto])", true)
                .HasColumnType("decimal(34, 8)");

            entity.HasOne(d => d.Cotizacion).WithMany(p => p.CotizacionDetalles)
                .HasForeignKey(d => d.CotizacionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CotizacionDetalles_Cotizacion");

            entity.HasOne(d => d.Producto).WithMany(p => p.CotizacionDetalles)
                .HasForeignKey(d => d.ProductoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CotizacionDetalles_Producto");

            entity.HasOne(d => d.ProductoPresentacion).WithMany(p => p.CotizacionDetalles)
                .HasForeignKey(d => d.ProductoPresentacionId)
                .HasConstraintName("FK_CotizacionDetalles_Presentacion");
        });

        modelBuilder.Entity<Cotizaciones>(entity =>
        {
            entity.HasIndex(e => new { e.EmpresaId, e.NumeroCotizacion }, "UQ_Cotizaciones_Numero").IsUnique();

            entity.Property(e => e.Fecha).HasPrecision(3);
            entity.Property(e => e.NumeroCotizacion)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Total).HasColumnType("decimal(12, 2)");

            entity.HasOne(d => d.Cliente).WithMany(p => p.Cotizaciones)
                .HasForeignKey(d => d.ClienteId)
                .HasConstraintName("FK_Cotizaciones_Cliente");

            entity.HasOne(d => d.CreadoPorUsuario).WithMany(p => p.Cotizaciones)
                .HasForeignKey(d => d.CreadoPorUsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Cotizaciones_Usuario");

            entity.HasOne(d => d.Empresa).WithMany(p => p.Cotizaciones)
                .HasForeignKey(d => d.EmpresaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Cotizaciones_Empresa");

            entity.HasOne(d => d.Moneda).WithMany(p => p.Cotizaciones)
                .HasForeignKey(d => d.MonedaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Cotizaciones_Moneda");

            entity.HasOne(d => d.Sucursal).WithMany(p => p.Cotizaciones)
                .HasForeignKey(d => d.SucursalId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Cotizaciones_Sucursal");

            entity.HasOne(d => d.EstadosFlujo).WithMany(p => p.Cotizaciones)
                .HasPrincipalKey(p => new { p.FlujoEstadoId, p.Id })
                .HasForeignKey(d => new { d.FlujoEstadoId, d.EstadoId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Cotizaciones_Estado");
        });

        modelBuilder.Entity<CuentasBancarias>(entity =>
        {
            entity.HasIndex(e => new { e.BancoId, e.NumeroCuenta }, "UQ_CuentasBancarias_Numero").IsUnique();

            entity.Property(e => e.NombreCuenta)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.NumeroCuenta)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.Banco).WithMany(p => p.CuentasBancarias)
                .HasForeignKey(d => d.BancoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CuentasBancarias_Banco");

            entity.HasOne(d => d.Estado).WithMany(p => p.CuentasBancarias)
                .HasForeignKey(d => d.EstadoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CuentasBancarias_Estado");

            entity.HasOne(d => d.Moneda).WithMany(p => p.CuentasBancarias)
                .HasForeignKey(d => d.MonedaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CuentasBancarias_Moneda");

            entity.HasOne(d => d.TipoCuenta).WithMany(p => p.CuentasBancarias)
                .HasForeignKey(d => d.TipoCuentaId)
                .HasConstraintName("FK_CuentasBancarias_Tipo");
        });

        modelBuilder.Entity<CuentasPorCobrar>(entity =>
        {
            entity.HasIndex(e => e.VentaId, "UQ_CuentasPorCobrar_Venta").IsUnique();

            entity.Property(e => e.MontoOriginal).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.SaldoPendiente).HasColumnType("decimal(12, 2)");

            entity.HasOne(d => d.Cliente).WithMany(p => p.CuentasPorCobrar)
                .HasForeignKey(d => d.ClienteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CuentasPorCobrar_Cliente");

            entity.HasOne(d => d.Empresa).WithMany(p => p.CuentasPorCobrar)
                .HasForeignKey(d => d.EmpresaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CuentasPorCobrar_Empresa");

            entity.HasOne(d => d.Venta).WithOne(p => p.CuentasPorCobrar)
                .HasForeignKey<CuentasPorCobrar>(d => d.VentaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CuentasPorCobrar_Venta");

            entity.HasOne(d => d.EstadosFlujo).WithMany(p => p.CuentasPorCobrar)
                .HasPrincipalKey(p => new { p.FlujoEstadoId, p.Id })
                .HasForeignKey(d => new { d.FlujoEstadoId, d.EstadoId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CuentasPorCobrar_Estado");
        });

        modelBuilder.Entity<CuentasPorPagar>(entity =>
        {
            entity.HasIndex(e => e.FacturaProveedorId, "UQ_CuentasPorPagar_Factura").IsUnique();

            entity.Property(e => e.MontoOriginal).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.SaldoPendiente).HasColumnType("decimal(12, 2)");

            entity.HasOne(d => d.Empresa).WithMany(p => p.CuentasPorPagar)
                .HasForeignKey(d => d.EmpresaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CuentasPorPagar_Empresa");

            entity.HasOne(d => d.FacturaProveedor).WithOne(p => p.CuentasPorPagar)
                .HasForeignKey<CuentasPorPagar>(d => d.FacturaProveedorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CuentasPorPagar_Factura");

            entity.HasOne(d => d.Proveedor).WithMany(p => p.CuentasPorPagar)
                .HasForeignKey(d => d.ProveedorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CuentasPorPagar_Proveedor");

            entity.HasOne(d => d.EstadosFlujo).WithMany(p => p.CuentasPorPagar)
                .HasPrincipalKey(p => new { p.FlujoEstadoId, p.Id })
                .HasForeignKey(d => new { d.FlujoEstadoId, d.EstadoId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CuentasPorPagar_Estado");
        });

        modelBuilder.Entity<DenominacionesMoneda>(entity =>
        {
            entity.HasIndex(e => new { e.MonedaId, e.Valor }, "UQ_DenominacionesMoneda").IsUnique();

            entity.Property(e => e.Valor).HasColumnType("decimal(12, 2)");

            entity.HasOne(d => d.Estado).WithMany(p => p.DenominacionesMoneda)
                .HasForeignKey(d => d.EstadoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DenominacionesMoneda_Estado");

            entity.HasOne(d => d.Moneda).WithMany(p => p.DenominacionesMoneda)
                .HasForeignKey(d => d.MonedaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DenominacionesMoneda_Moneda");

            entity.HasOne(d => d.Tipo).WithMany(p => p.DenominacionesMoneda)
                .HasForeignKey(d => d.TipoId)
                .HasConstraintName("FK_DenominacionesMoneda_Tipo");
        });

        modelBuilder.Entity<Departamentos>(entity =>
        {
            entity.HasIndex(e => e.Nombre, "UQ_Departamentos_Nombre").IsUnique();

            entity.Property(e => e.Codigo)
                .HasMaxLength(2)
                .IsUnicode(false);
            entity.Property(e => e.Nombre)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<DevolucionVentaDetalles>(entity =>
        {
            entity.Property(e => e.Cantidad).HasColumnType("decimal(19, 6)");
            entity.Property(e => e.Monto).HasColumnType("decimal(12, 2)");

            entity.HasOne(d => d.DevolucionVenta).WithMany(p => p.DevolucionVentaDetalles)
                .HasForeignKey(d => d.DevolucionVentaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DevolucionVentaDetalles_Devolucion");

            entity.HasOne(d => d.VentaDetalle).WithMany(p => p.DevolucionVentaDetalles)
                .HasForeignKey(d => d.VentaDetalleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DevolucionVentaDetalles_VentaDetalle");
        });

        modelBuilder.Entity<DevolucionesVentas>(entity =>
        {
            entity.HasIndex(e => new { e.EmpresaId, e.NumeroDevolucion }, "UQ_DevolucionesVentas_Numero").IsUnique();

            entity.Property(e => e.Fecha).HasPrecision(3);
            entity.Property(e => e.NumeroDevolucion)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Total).HasColumnType("decimal(12, 2)");

            entity.HasOne(d => d.Empresa).WithMany(p => p.DevolucionesVentas)
                .HasForeignKey(d => d.EmpresaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DevolucionesVentas_Empresa");

            entity.HasOne(d => d.Motivo).WithMany(p => p.DevolucionesVentas)
                .HasForeignKey(d => d.MotivoId)
                .HasConstraintName("FK_DevolucionesVentas_Motivo");

            entity.HasOne(d => d.Usuario).WithMany(p => p.DevolucionesVentas)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DevolucionesVentas_Usuario");

            entity.HasOne(d => d.Venta).WithMany(p => p.DevolucionesVentas)
                .HasForeignKey(d => d.VentaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DevolucionesVentas_Venta");

            entity.HasOne(d => d.EstadosFlujo).WithMany(p => p.DevolucionesVentas)
                .HasPrincipalKey(p => new { p.FlujoEstadoId, p.Id })
                .HasForeignKey(d => new { d.FlujoEstadoId, d.EstadoId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DevolucionesVentas_Estado");
        });

        modelBuilder.Entity<DireccionesClientes>(entity =>
        {
            entity.Property(e => e.Direccion)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.Referencia)
                .HasMaxLength(250)
                .IsUnicode(false);

            entity.HasOne(d => d.Cliente).WithMany(p => p.DireccionesClientes)
                .HasForeignKey(d => d.ClienteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DireccionesClientes_Cliente");

            entity.HasOne(d => d.Estado).WithMany(p => p.DireccionesClientes)
                .HasForeignKey(d => d.EstadoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DireccionesClientes_Estado");

            entity.HasOne(d => d.Municipio).WithMany(p => p.DireccionesClientes)
                .HasForeignKey(d => d.MunicipioId)
                .HasConstraintName("FK_DireccionesClientes_Municipio");

            entity.HasOne(d => d.TipoDireccion).WithMany(p => p.DireccionesClientes)
                .HasForeignKey(d => d.TipoDireccionId)
                .HasConstraintName("FK_DireccionesClientes_Tipo");
        });

        modelBuilder.Entity<DocumentosAdjuntos>(entity =>
        {
            entity.Property(e => e.Entidad)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.FechaCarga).HasPrecision(3);
            entity.Property(e => e.ModuloId)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.NombreArchivo)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.RutaArchivo)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.TipoMime)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.HasOne(d => d.Empresa).WithMany(p => p.DocumentosAdjuntos)
                .HasForeignKey(d => d.EmpresaId)
                .HasConstraintName("FK_DocumentosAdjuntos_Empresa");

            entity.HasOne(d => d.Estado).WithMany(p => p.DocumentosAdjuntos)
                .HasForeignKey(d => d.EstadoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DocumentosAdjuntos_Estado");

            entity.HasOne(d => d.Modulo).WithMany(p => p.DocumentosAdjuntos)
                .HasForeignKey(d => d.ModuloId)
                .HasConstraintName("FK_DocumentosAdjuntos_Modulo");

            entity.HasOne(d => d.Usuario).WithMany(p => p.DocumentosAdjuntos)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_DocumentosAdjuntos_Usuario");
        });

        modelBuilder.Entity<Empleados>(entity =>
        {
            entity.HasIndex(e => new { e.EmpresaId, e.CodigoEmpleado }, "UQ_Empleados_Codigo").IsUnique();

            entity.Property(e => e.Apellidos)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.CodigoEmpleado)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Dpi)
                .HasMaxLength(25)
                .IsUnicode(false);
            entity.Property(e => e.Nombres)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.SalarioBase).HasColumnType("decimal(12, 2)");

            entity.HasOne(d => d.Cargo).WithMany(p => p.Empleados)
                .HasForeignKey(d => d.CargoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Empleados_Cargo");

            entity.HasOne(d => d.Empresa).WithMany(p => p.Empleados)
                .HasForeignKey(d => d.EmpresaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Empleados_Empresa");

            entity.HasOne(d => d.Estado).WithMany(p => p.Empleados)
                .HasForeignKey(d => d.EstadoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Empleados_Estado");

            entity.HasOne(d => d.Sucursal).WithMany(p => p.Empleados)
                .HasForeignKey(d => d.SucursalId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Empleados_Sucursal");

            entity.HasOne(d => d.Usuario).WithMany(p => p.Empleados)
                .HasForeignKey(d => d.UsuarioId)
                .HasConstraintName("FK_Empleados_Usuario");
        });

        modelBuilder.Entity<Empresas>(entity =>
        {
            entity.HasIndex(e => e.Email, "UQ_Empresas_Email").IsUnique();

            entity.HasIndex(e => e.Nit, "UQ_Empresas_Nit").IsUnique();

            entity.Property(e => e.Direccion)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.Dpi)
                .HasMaxLength(25)
                .IsUnicode(false);
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Eslogan)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.Icono)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Imagen)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.ImagenReportes)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Nit)
                .HasMaxLength(25)
                .IsUnicode(false);
            entity.Property(e => e.NombreComercial)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.RazonSocial)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.RoleId)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Telefono)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Url)
                .HasMaxLength(150)
                .IsUnicode(false);

            entity.HasOne(d => d.Cliente).WithMany(p => p.Empresas)
                .HasForeignKey(d => d.ClienteId)
                .HasConstraintName("FK_Empresas_ClienteId");

            entity.HasOne(d => d.Estado).WithMany(p => p.Empresas)
                .HasForeignKey(d => d.EstadoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Empresas_Estado");

            entity.HasOne(d => d.Role).WithMany(p => p.Empresas)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Empresas_RoleId");
        });

        modelBuilder.Entity<EstadoTransiciones>(entity =>
        {
            entity.HasIndex(e => new { e.FlujoEstadoId, e.EstadoOrigenId, e.EstadoDestinoId }, "UQ_EstadoTransiciones_Flujo_Origen_Destino").IsUnique();

            entity.Property(e => e.FechaCreacion).HasPrecision(3);
            entity.Property(e => e.FechaModificacion).HasPrecision(3);
            entity.Property(e => e.PermisoCodigo)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.VersionFila)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.CreadoPorUsuario).WithMany(p => p.EstadoTransicionesCreadoPorUsuario)
                .HasForeignKey(d => d.CreadoPorUsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EstadoTransiciones_CreadoPor");

            entity.HasOne(d => d.Estado).WithMany(p => p.EstadoTransiciones)
                .HasForeignKey(d => d.EstadoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EstadoTransiciones_Estado");

            entity.HasOne(d => d.FlujoEstado).WithMany(p => p.EstadoTransiciones)
                .HasForeignKey(d => d.FlujoEstadoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EstadoTransiciones_Flujo");

            entity.HasOne(d => d.ModificadoPorUsuario).WithMany(p => p.EstadoTransicionesModificadoPorUsuario)
                .HasForeignKey(d => d.ModificadoPorUsuarioId)
                .HasConstraintName("FK_EstadoTransiciones_ModificadoPor");

            entity.HasOne(d => d.EstadosFlujo).WithMany(p => p.EstadoTransicionesEstadosFlujo)
                .HasPrincipalKey(p => new { p.FlujoEstadoId, p.Id })
                .HasForeignKey(d => new { d.FlujoEstadoId, d.EstadoDestinoId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EstadoTransiciones_Destino");

            entity.HasOne(d => d.EstadosFlujoNavigation).WithMany(p => p.EstadoTransicionesEstadosFlujoNavigation)
                .HasPrincipalKey(p => new { p.FlujoEstadoId, p.Id })
                .HasForeignKey(d => new { d.FlujoEstadoId, d.EstadoOrigenId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EstadoTransiciones_Origen");
        });

        modelBuilder.Entity<Estados>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.ModuloId)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.HasOne(d => d.ClaseEstado).WithMany(p => p.Estados)
                .HasForeignKey(d => d.ClaseEstadoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Estados_ClaseEstadoId");

            entity.HasOne(d => d.Modulo).WithMany(p => p.Estados)
                .HasForeignKey(d => d.ModuloId)
                .HasConstraintName("FK_Estados_ModuloId");
        });

        modelBuilder.Entity<EstadosFlujo>(entity =>
        {
            entity.HasIndex(e => new { e.FlujoEstadoId, e.Codigo }, "UQ_EstadosFlujo_Flujo_Codigo").IsUnique();

            entity.HasIndex(e => new { e.FlujoEstadoId, e.Id }, "UQ_EstadosFlujo_Flujo_Id").IsUnique();

            entity.HasIndex(e => new { e.FlujoEstadoId, e.Nombre }, "UQ_EstadosFlujo_Flujo_Nombre").IsUnique();

            entity.Property(e => e.Codigo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Descripcion)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.FechaCreacion).HasPrecision(3);
            entity.Property(e => e.FechaModificacion).HasPrecision(3);
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.VersionFila)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.CreadoPorUsuario).WithMany(p => p.EstadosFlujoCreadoPorUsuario)
                .HasForeignKey(d => d.CreadoPorUsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EstadosFlujo_CreadoPor");

            entity.HasOne(d => d.Estado).WithMany(p => p.EstadosFlujo)
                .HasForeignKey(d => d.EstadoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EstadosFlujo_Estado");

            entity.HasOne(d => d.FlujoEstado).WithMany(p => p.EstadosFlujo)
                .HasForeignKey(d => d.FlujoEstadoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EstadosFlujo_Flujo");

            entity.HasOne(d => d.ModificadoPorUsuario).WithMany(p => p.EstadosFlujoModificadoPorUsuario)
                .HasForeignKey(d => d.ModificadoPorUsuarioId)
                .HasConstraintName("FK_EstadosFlujo_ModificadoPor");
        });

        modelBuilder.Entity<Existencias>(entity =>
        {
            entity.HasIndex(e => new { e.EmpresaId, e.Id }, "UQ_Existencias_Empresa_Id").IsUnique();

            entity.HasIndex(e => new { e.EmpresaId, e.ProductoId, e.BodegaId, e.UbicacionId }, "UQ_Existencias_Producto_Bodega_Ubicacion").IsUnique();

            entity.HasIndex(e => new { e.EmpresaId, e.ProductoId, e.Id }, "UQ_Existencias_Producto_Id").IsUnique();

            entity.Property(e => e.Cantidad).HasColumnType("decimal(19, 6)");
            entity.Property(e => e.CantidadDisponible)
                .HasComputedColumnSql("([Cantidad]-[CantidadReservada])", true)
                .HasColumnType("decimal(20, 6)");
            entity.Property(e => e.CantidadReservada).HasColumnType("decimal(19, 6)");
            entity.Property(e => e.CostoPromedio).HasColumnType("decimal(19, 6)");
            entity.Property(e => e.UltimoMovimiento).HasPrecision(3);
            entity.Property(e => e.VersionFila)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.Empresa).WithMany(p => p.Existencias)
                .HasForeignKey(d => d.EmpresaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Existencias_Empresa");

            entity.HasOne(d => d.Bodegas).WithMany(p => p.Existencias)
                .HasPrincipalKey(p => new { p.EmpresaId, p.Id })
                .HasForeignKey(d => new { d.EmpresaId, d.BodegaId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Existencias_Bodega");

            entity.HasOne(d => d.Productos).WithMany(p => p.Existencias)
                .HasPrincipalKey(p => new { p.EmpresaId, p.Id })
                .HasForeignKey(d => new { d.EmpresaId, d.ProductoId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Existencias_Producto");

            entity.HasOne(d => d.Ubicaciones).WithMany(p => p.Existencias)
                .HasPrincipalKey(p => new { p.EmpresaId, p.BodegaId, p.Id })
                .HasForeignKey(d => new { d.EmpresaId, d.BodegaId, d.UbicacionId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Existencias_Ubicacion");
        });

        modelBuilder.Entity<ExistenciasLote>(entity =>
        {
            entity.HasIndex(e => new { e.EmpresaId, e.ExistenciaId, e.LoteId }, "UQ_ExistenciasLote_Existencia_Lote").IsUnique();

            entity.Property(e => e.Cantidad).HasColumnType("decimal(19, 6)");
            entity.Property(e => e.CantidadDisponible)
                .HasComputedColumnSql("([Cantidad]-[CantidadReservada])", true)
                .HasColumnType("decimal(20, 6)");
            entity.Property(e => e.CantidadReservada).HasColumnType("decimal(19, 6)");
            entity.Property(e => e.UltimoMovimiento).HasPrecision(3);
            entity.Property(e => e.VersionFila)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.Empresa).WithMany(p => p.ExistenciasLote)
                .HasForeignKey(d => d.EmpresaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ExistenciasLote_Empresa");

            entity.HasOne(d => d.Existencias).WithMany(p => p.ExistenciasLote)
                .HasPrincipalKey(p => new { p.EmpresaId, p.ProductoId, p.Id })
                .HasForeignKey(d => new { d.EmpresaId, d.ProductoId, d.ExistenciaId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ExistenciasLote_Existencia");

            entity.HasOne(d => d.Lotes).WithMany(p => p.ExistenciasLote)
                .HasPrincipalKey(p => new { p.EmpresaId, p.ProductoId, p.Id })
                .HasForeignKey(d => new { d.EmpresaId, d.ProductoId, d.LoteId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ExistenciasLote_Lote");
        });

        modelBuilder.Entity<FacturasProveedores>(entity =>
        {
            entity.HasIndex(e => new { e.EmpresaId, e.ProveedorId, e.Serie, e.NumeroDocumento }, "UQ_FacturasProveedores_Documento").IsUnique();

            entity.Property(e => e.Impuestos).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.NumeroDocumento)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Serie)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.SubTotal).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.TipoCambio).HasColumnType("decimal(19, 8)");
            entity.Property(e => e.Total).HasColumnType("decimal(12, 2)");

            entity.HasOne(d => d.Empresa).WithMany(p => p.FacturasProveedores)
                .HasForeignKey(d => d.EmpresaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FacturasProveedores_Empresa");

            entity.HasOne(d => d.Estado).WithMany(p => p.FacturasProveedores)
                .HasForeignKey(d => d.EstadoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FacturasProveedores_Estado");

            entity.HasOne(d => d.Moneda).WithMany(p => p.FacturasProveedores)
                .HasForeignKey(d => d.MonedaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FacturasProveedores_Moneda");

            entity.HasOne(d => d.OrdenCompra).WithMany(p => p.FacturasProveedores)
                .HasForeignKey(d => d.OrdenCompraId)
                .HasConstraintName("FK_FacturasProveedores_Orden");

            entity.HasOne(d => d.Proveedor).WithMany(p => p.FacturasProveedores)
                .HasForeignKey(d => d.ProveedorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FacturasProveedores_Proveedor");
        });

        modelBuilder.Entity<FlujosEstados>(entity =>
        {
            entity.HasIndex(e => e.Codigo, "UQ_FlujosEstados_Codigo").IsUnique();

            entity.HasIndex(e => e.Nombre, "UQ_FlujosEstados_Nombre").IsUnique();

            entity.Property(e => e.Codigo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Descripcion)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.FechaCreacion).HasPrecision(3);
            entity.Property(e => e.FechaModificacion).HasPrecision(3);
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.HasOne(d => d.CreadoPorUsuario).WithMany(p => p.FlujosEstadosCreadoPorUsuario)
                .HasForeignKey(d => d.CreadoPorUsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FlujosEstados_CreadoPor");

            entity.HasOne(d => d.Estado).WithMany(p => p.FlujosEstados)
                .HasForeignKey(d => d.EstadoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FlujosEstados_Estado");

            entity.HasOne(d => d.ModificadoPorUsuario).WithMany(p => p.FlujosEstadosModificadoPorUsuario)
                .HasForeignKey(d => d.ModificadoPorUsuarioId)
                .HasConstraintName("FK_FlujosEstados_ModificadoPor");
        });

        modelBuilder.Entity<FormasPago>(entity =>
        {
            entity.HasIndex(e => new { e.EmpresaId, e.Codigo }, "UQ_FormasPago_Empresa_Codigo").IsUnique();

            entity.Property(e => e.Codigo)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Nombre)
                .HasMaxLength(80)
                .IsUnicode(false);

            entity.HasOne(d => d.Empresa).WithMany(p => p.FormasPago)
                .HasForeignKey(d => d.EmpresaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FormasPago_Empresa");

            entity.HasOne(d => d.Estado).WithMany(p => p.FormasPago)
                .HasForeignKey(d => d.EstadoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FormasPago_Estado");
        });

        modelBuilder.Entity<HistorialAccesos>(entity =>
        {
            entity.Property(e => e.DireccionIp)
                .HasMaxLength(45)
                .IsUnicode(false);
            entity.Property(e => e.Fecha).HasPrecision(3);
            entity.Property(e => e.Motivo)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.UserName)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.Usuario).WithMany(p => p.HistorialAccesos)
                .HasForeignKey(d => d.UsuarioId)
                .HasConstraintName("FK_HistorialAccesos_Usuario");
        });

        modelBuilder.Entity<HistoricoCostosProductos>(entity =>
        {
            entity.Property(e => e.CostoAnterior).HasColumnType("decimal(19, 6)");
            entity.Property(e => e.CostoNuevo).HasColumnType("decimal(19, 6)");
            entity.Property(e => e.FechaCambio).HasPrecision(3);
            entity.Property(e => e.OrigenCambio)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.Empresa).WithMany(p => p.HistoricoCostosProductos)
                .HasForeignKey(d => d.EmpresaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_HistoricoCostosProductos_Empresa");

            entity.HasOne(d => d.Producto).WithMany(p => p.HistoricoCostosProductos)
                .HasForeignKey(d => d.ProductoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_HistoricoCostosProductos_Producto");

            entity.HasOne(d => d.Usuario).WithMany(p => p.HistoricoCostosProductos)
                .HasForeignKey(d => d.UsuarioId)
                .HasConstraintName("FK_HistoricoCostosProductos_Usuario");
        });

        modelBuilder.Entity<HistoricoPreciosCompra>(entity =>
        {
            entity.Property(e => e.Fecha).HasPrecision(3);
            entity.Property(e => e.PrecioCompra).HasColumnType("decimal(12, 2)");

            entity.HasOne(d => d.Empresa).WithMany(p => p.HistoricoPreciosCompra)
                .HasForeignKey(d => d.EmpresaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_HistoricoPreciosCompra_Empresa");

            entity.HasOne(d => d.Moneda).WithMany(p => p.HistoricoPreciosCompra)
                .HasForeignKey(d => d.MonedaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_HistoricoPreciosCompra_Moneda");

            entity.HasOne(d => d.Producto).WithMany(p => p.HistoricoPreciosCompra)
                .HasForeignKey(d => d.ProductoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_HistoricoPreciosCompra_Producto");

            entity.HasOne(d => d.Proveedor).WithMany(p => p.HistoricoPreciosCompra)
                .HasForeignKey(d => d.ProveedorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_HistoricoPreciosCompra_Proveedor");

            entity.HasOne(d => d.RecepcionCompra).WithMany(p => p.HistoricoPreciosCompra)
                .HasForeignKey(d => d.RecepcionCompraId)
                .HasConstraintName("FK_HistoricoPreciosCompra_Recepcion");
        });

        modelBuilder.Entity<HistoricoPreciosVenta>(entity =>
        {
            entity.Property(e => e.FechaCambio).HasPrecision(3);
            entity.Property(e => e.PrecioAnterior).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.PrecioNuevo).HasColumnType("decimal(12, 2)");

            entity.HasOne(d => d.Empresa).WithMany(p => p.HistoricoPreciosVenta)
                .HasForeignKey(d => d.EmpresaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_HistoricoPreciosVenta_Empresa");

            entity.HasOne(d => d.Producto).WithMany(p => p.HistoricoPreciosVenta)
                .HasForeignKey(d => d.ProductoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_HistoricoPreciosVenta_Producto");

            entity.HasOne(d => d.ProductoPresentacion).WithMany(p => p.HistoricoPreciosVenta)
                .HasForeignKey(d => d.ProductoPresentacionId)
                .HasConstraintName("FK_HistoricoPreciosVenta_Presentacion");

            entity.HasOne(d => d.Usuario).WithMany(p => p.HistoricoPreciosVenta)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_HistoricoPreciosVenta_Usuario");
        });

        modelBuilder.Entity<Impuestos>(entity =>
        {
            entity.HasIndex(e => new { e.EmpresaId, e.Codigo }, "UQ_Impuestos_Empresa_Codigo").IsUnique();

            entity.Property(e => e.Codigo)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Porcentaje).HasColumnType("decimal(9, 6)");

            entity.HasOne(d => d.Empresa).WithMany(p => p.Impuestos)
                .HasForeignKey(d => d.EmpresaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Impuestos_Empresa");

            entity.HasOne(d => d.Estado).WithMany(p => p.Impuestos)
                .HasForeignKey(d => d.EstadoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Impuestos_Estado");
        });

        modelBuilder.Entity<Kardex>(entity =>
        {
            entity.HasIndex(e => new { e.MovimientoDetalleId, e.BodegaId, e.TipoAfectacion }, "UQ_Kardex_Detalle_Bodega_Afectacion").IsUnique();

            entity.Property(e => e.Cantidad).HasColumnType("decimal(19, 6)");
            entity.Property(e => e.CostoTotal)
                .HasComputedColumnSql("([Cantidad]*[CostoUnitario])", true)
                .HasColumnType("decimal(38, 11)");
            entity.Property(e => e.CostoUnitario).HasColumnType("decimal(19, 6)");
            entity.Property(e => e.FechaMovimiento).HasPrecision(3);
            entity.Property(e => e.FechaRegistro).HasPrecision(3);
            entity.Property(e => e.SaldoCantidad).HasColumnType("decimal(19, 6)");
            entity.Property(e => e.SaldoCosto).HasColumnType("decimal(19, 6)");
            entity.Property(e => e.TipoAfectacion)
                .HasMaxLength(10)
                .IsUnicode(false);

            entity.HasOne(d => d.Empresa).WithMany(p => p.Kardex)
                .HasForeignKey(d => d.EmpresaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Kardex_Empresa");

            entity.HasOne(d => d.Bodegas).WithMany(p => p.Kardex)
                .HasPrincipalKey(p => new { p.EmpresaId, p.Id })
                .HasForeignKey(d => new { d.EmpresaId, d.BodegaId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Kardex_Bodega");

            entity.HasOne(d => d.MovimientoDetalles).WithMany(p => p.Kardex)
                .HasPrincipalKey(p => new { p.EmpresaId, p.Id })
                .HasForeignKey(d => new { d.EmpresaId, d.MovimientoDetalleId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Kardex_MovimientoDetalle");

            entity.HasOne(d => d.Productos).WithMany(p => p.Kardex)
                .HasPrincipalKey(p => new { p.EmpresaId, p.Id })
                .HasForeignKey(d => new { d.EmpresaId, d.ProductoId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Kardex_Producto");

            entity.HasOne(d => d.Ubicaciones).WithMany(p => p.Kardex)
                .HasPrincipalKey(p => new { p.EmpresaId, p.Id })
                .HasForeignKey(d => new { d.EmpresaId, d.UbicacionId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Kardex_Ubicacion");

            entity.HasOne(d => d.Lotes).WithMany(p => p.Kardex)
                .HasPrincipalKey(p => new { p.EmpresaId, p.ProductoId, p.Id })
                .HasForeignKey(d => new { d.EmpresaId, d.ProductoId, d.LoteId })
                .HasConstraintName("FK_Kardex_Lote");
        });

        modelBuilder.Entity<ListasPrecios>(entity =>
        {
            entity.HasIndex(e => new { e.EmpresaId, e.Codigo }, "UQ_ListasPrecios_Codigo").IsUnique();

            entity.Property(e => e.Codigo)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.HasOne(d => d.AplicaSucursal).WithMany(p => p.ListasPrecios)
                .HasForeignKey(d => d.AplicaSucursalId)
                .HasConstraintName("FK_ListasPrecios_Sucursal");

            entity.HasOne(d => d.CanalVenta).WithMany(p => p.ListasPrecios)
                .HasForeignKey(d => d.CanalVentaId)
                .HasConstraintName("FK_ListasPrecios_Canal");

            entity.HasOne(d => d.Empresa).WithMany(p => p.ListasPrecios)
                .HasForeignKey(d => d.EmpresaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ListasPrecios_Empresa");

            entity.HasOne(d => d.Estado).WithMany(p => p.ListasPrecios)
                .HasForeignKey(d => d.EstadoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ListasPrecios_Estado");

            entity.HasOne(d => d.Moneda).WithMany(p => p.ListasPrecios)
                .HasForeignKey(d => d.MonedaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ListasPrecios_Moneda");
        });

        modelBuilder.Entity<Lotes>(entity =>
        {
            entity.HasIndex(e => new { e.EmpresaId, e.Id }, "UQ_Lotes_Empresa_Id").IsUnique();

            entity.HasIndex(e => new { e.EmpresaId, e.ProductoId, e.Id }, "UQ_Lotes_Producto_Id").IsUnique();

            entity.HasIndex(e => new { e.EmpresaId, e.ProductoId, e.NumeroLote }, "UQ_Lotes_Producto_Numero").IsUnique();

            entity.Property(e => e.FechaCreacion).HasPrecision(3);
            entity.Property(e => e.NumeroLote)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Observaciones)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.VersionFila)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.CreadoPorUsuario).WithMany(p => p.Lotes)
                .HasForeignKey(d => d.CreadoPorUsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Lotes_CreadoPor");

            entity.HasOne(d => d.Empresa).WithMany(p => p.Lotes)
                .HasForeignKey(d => d.EmpresaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Lotes_Empresa");

            entity.HasOne(d => d.Productos).WithMany(p => p.Lotes)
                .HasPrincipalKey(p => new { p.EmpresaId, p.Id })
                .HasForeignKey(d => new { d.EmpresaId, d.ProductoId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Lotes_Producto");

            entity.HasOne(d => d.EstadosFlujo).WithMany(p => p.Lotes)
                .HasPrincipalKey(p => new { p.FlujoEstadoId, p.Id })
                .HasForeignKey(d => new { d.FlujoEstadoId, d.EstadoId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Lotes_Estado");
        });

        modelBuilder.Entity<Marcas>(entity =>
        {
            entity.HasIndex(e => new { e.EmpresaId, e.Codigo }, "UQ_Marcas_Empresa_Codigo").IsUnique();

            entity.HasIndex(e => new { e.EmpresaId, e.Id }, "UQ_Marcas_Empresa_Id").IsUnique();

            entity.HasIndex(e => new { e.EmpresaId, e.Nombre }, "UQ_Marcas_Empresa_Nombre").IsUnique();

            entity.Property(e => e.Codigo)
                .HasMaxLength(25)
                .IsUnicode(false);
            entity.Property(e => e.Descripcion)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.FechaCreacion).HasPrecision(3);
            entity.Property(e => e.FechaModificacion).HasPrecision(3);
            entity.Property(e => e.Nombre)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.VersionFila)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.CreadoPorUsuario).WithMany(p => p.MarcasCreadoPorUsuario)
                .HasForeignKey(d => d.CreadoPorUsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Marcas_CreadoPor");

            entity.HasOne(d => d.Empresa).WithMany(p => p.Marcas)
                .HasForeignKey(d => d.EmpresaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Marcas_Empresa");

            entity.HasOne(d => d.Estado).WithMany(p => p.Marcas)
                .HasForeignKey(d => d.EstadoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Marcas_Estado");

            entity.HasOne(d => d.ModificadoPorUsuario).WithMany(p => p.MarcasModificadoPorUsuario)
                .HasForeignKey(d => d.ModificadoPorUsuarioId)
                .HasConstraintName("FK_Marcas_ModificadoPor");
        });

        modelBuilder.Entity<Modulos>(entity =>
        {
            entity.Property(e => e.Id)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Icono)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Nombre)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Monedas>(entity =>
        {
            entity.HasIndex(e => new { e.EmpresaId, e.CodigoISO }, "UQ_Monedas_Empresa_Codigo").IsUnique();

            entity.Property(e => e.CodigoISO)
                .HasMaxLength(3)
                .IsUnicode(false);
            entity.Property(e => e.NombreMoneda)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Simbolo)
                .HasMaxLength(5)
                .IsUnicode(false);

            entity.HasOne(d => d.Empresa).WithMany(p => p.Monedas)
                .HasForeignKey(d => d.EmpresaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Monedas_Empresa");

            entity.HasOne(d => d.Estado).WithMany(p => p.Monedas)
                .HasForeignKey(d => d.EstadoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Monedas_Estado");
        });

        modelBuilder.Entity<MovimientoDetalleSeries>(entity =>
        {
            entity.HasIndex(e => new { e.EmpresaId, e.MovimientoDetalleId, e.SerieProductoId }, "UQ_MovimientoDetalleSeries").IsUnique();

            entity.HasOne(d => d.Empresa).WithMany(p => p.MovimientoDetalleSeries)
                .HasForeignKey(d => d.EmpresaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MovimientoDetalleSeries_Empresa");

            entity.HasOne(d => d.MovimientoDetalles).WithMany(p => p.MovimientoDetalleSeries)
                .HasPrincipalKey(p => new { p.EmpresaId, p.Id })
                .HasForeignKey(d => new { d.EmpresaId, d.MovimientoDetalleId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MovimientoDetalleSeries_Detalle");

            entity.HasOne(d => d.SeriesProductos).WithMany(p => p.MovimientoDetalleSeries)
                .HasPrincipalKey(p => new { p.EmpresaId, p.Id })
                .HasForeignKey(d => new { d.EmpresaId, d.SerieProductoId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MovimientoDetalleSeries_Serie");
        });

        modelBuilder.Entity<MovimientoDetalles>(entity =>
        {
            entity.HasIndex(e => new { e.EmpresaId, e.Id }, "UQ_MovimientoDetalles_Empresa_Id").IsUnique();

            entity.HasIndex(e => new { e.EmpresaId, e.MovimientoId, e.NumeroLinea }, "UQ_MovimientoDetalles_Linea").IsUnique();

            entity.Property(e => e.Cantidad).HasColumnType("decimal(19, 6)");
            entity.Property(e => e.CantidadBase).HasColumnType("decimal(19, 6)");
            entity.Property(e => e.CostoTotal)
                .HasComputedColumnSql("([CantidadBase]*[CostoUnitario])", true)
                .HasColumnType("decimal(38, 11)");
            entity.Property(e => e.CostoUnitario).HasColumnType("decimal(19, 6)");
            entity.Property(e => e.FactorConversion).HasColumnType("decimal(19, 6)");
            entity.Property(e => e.Observaciones)
                .HasMaxLength(500)
                .IsUnicode(false);

            entity.HasOne(d => d.Empresa).WithMany(p => p.MovimientoDetalles)
                .HasForeignKey(d => d.EmpresaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MovimientoDetalles_Empresa");

            entity.HasOne(d => d.Movimientos).WithMany(p => p.MovimientoDetalles)
                .HasPrincipalKey(p => new { p.EmpresaId, p.Id })
                .HasForeignKey(d => new { d.EmpresaId, d.MovimientoId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MovimientoDetalles_Movimiento");

            entity.HasOne(d => d.Productos).WithMany(p => p.MovimientoDetalles)
                .HasPrincipalKey(p => new { p.EmpresaId, p.Id })
                .HasForeignKey(d => new { d.EmpresaId, d.ProductoId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MovimientoDetalles_Producto");

            entity.HasOne(d => d.Ubicaciones).WithMany(p => p.MovimientoDetallesUbicaciones)
                .HasPrincipalKey(p => new { p.EmpresaId, p.Id })
                .HasForeignKey(d => new { d.EmpresaId, d.UbicacionDestinoId })
                .HasConstraintName("FK_MovimientoDetalles_UbicacionDestino");

            entity.HasOne(d => d.UbicacionesNavigation).WithMany(p => p.MovimientoDetallesUbicacionesNavigation)
                .HasPrincipalKey(p => new { p.EmpresaId, p.Id })
                .HasForeignKey(d => new { d.EmpresaId, d.UbicacionOrigenId })
                .HasConstraintName("FK_MovimientoDetalles_UbicacionOrigen");

            entity.HasOne(d => d.UnidadesMedida).WithMany(p => p.MovimientoDetalles)
                .HasPrincipalKey(p => new { p.EmpresaId, p.Id })
                .HasForeignKey(d => new { d.EmpresaId, d.UnidadMedidaId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MovimientoDetalles_Unidad");

            entity.HasOne(d => d.Lotes).WithMany(p => p.MovimientoDetalles)
                .HasPrincipalKey(p => new { p.EmpresaId, p.ProductoId, p.Id })
                .HasForeignKey(d => new { d.EmpresaId, d.ProductoId, d.LoteId })
                .HasConstraintName("FK_MovimientoDetalles_Lote");

            entity.HasOne(d => d.ProductoPresentaciones).WithMany(p => p.MovimientoDetalles)
                .HasPrincipalKey(p => new { p.EmpresaId, p.ProductoId, p.Id })
                .HasForeignKey(d => new { d.EmpresaId, d.ProductoId, d.ProductoPresentacionId })
                .HasConstraintName("FK_MovimientoDetalles_Presentacion");
        });

        modelBuilder.Entity<Movimientos>(entity =>
        {
            entity.HasIndex(e => new { e.EmpresaId, e.SerieDocumento, e.NumeroDocumento }, "UQ_Movimientos_Documento").IsUnique();

            entity.HasIndex(e => new { e.EmpresaId, e.Id }, "UQ_Movimientos_Empresa_Id").IsUnique();

            entity.Property(e => e.ComentarioAnulacion)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.FechaAnulacion).HasPrecision(3);
            entity.Property(e => e.FechaAplicacion).HasPrecision(3);
            entity.Property(e => e.FechaCreacion).HasPrecision(3);
            entity.Property(e => e.FechaMovimiento).HasPrecision(3);
            entity.Property(e => e.ModuloOrigen)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Observaciones)
                .HasMaxLength(1000)
                .IsUnicode(false);
            entity.Property(e => e.ReferenciaExterna)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.SerieDocumento)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.AnuladoPorUsuario).WithMany(p => p.MovimientosAnuladoPorUsuario)
                .HasForeignKey(d => d.AnuladoPorUsuarioId)
                .HasConstraintName("FK_Movimientos_AnuladoPor");

            entity.HasOne(d => d.AplicadoPorUsuario).WithMany(p => p.MovimientosAplicadoPorUsuario)
                .HasForeignKey(d => d.AplicadoPorUsuarioId)
                .HasConstraintName("FK_Movimientos_AplicadoPor");

            entity.HasOne(d => d.CreadoPorUsuario).WithMany(p => p.MovimientosCreadoPorUsuario)
                .HasForeignKey(d => d.CreadoPorUsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Movimientos_CreadoPor");

            entity.HasOne(d => d.Empresa).WithMany(p => p.Movimientos)
                .HasForeignKey(d => d.EmpresaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Movimientos_Empresa");

            entity.HasOne(d => d.MotivoAnulacion).WithMany(p => p.Movimientos)
                .HasForeignKey(d => d.MotivoAnulacionId)
                .HasConstraintName("FK_Movimientos_MotivoAnulacion");

            entity.HasOne(d => d.Sucursal).WithMany(p => p.Movimientos)
                .HasForeignKey(d => d.SucursalId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Movimientos_Sucursal");

            entity.HasOne(d => d.Bodegas).WithMany(p => p.MovimientosBodegas)
                .HasPrincipalKey(p => new { p.EmpresaId, p.Id })
                .HasForeignKey(d => new { d.EmpresaId, d.BodegaDestinoId })
                .HasConstraintName("FK_Movimientos_BodegaDestino");

            entity.HasOne(d => d.BodegasNavigation).WithMany(p => p.MovimientosBodegasNavigation)
                .HasPrincipalKey(p => new { p.EmpresaId, p.Id })
                .HasForeignKey(d => new { d.EmpresaId, d.BodegaOrigenId })
                .HasConstraintName("FK_Movimientos_BodegaOrigen");

            entity.HasOne(d => d.ConceptosMovimientos).WithMany(p => p.Movimientos)
                .HasPrincipalKey(p => new { p.EmpresaId, p.Id })
                .HasForeignKey(d => new { d.EmpresaId, d.ConceptoMovimientoId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Movimientos_Concepto");

            entity.HasOne(d => d.EstadosFlujo).WithMany(p => p.Movimientos)
                .HasPrincipalKey(p => new { p.FlujoEstadoId, p.Id })
                .HasForeignKey(d => new { d.FlujoEstadoId, d.EstadoId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Movimientos_Estado");
        });

        modelBuilder.Entity<MovimientosBancarios>(entity =>
        {
            entity.Property(e => e.Descripcion)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.Fecha).HasPrecision(3);
            entity.Property(e => e.Monto).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.Referencia)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.HasOne(d => d.CuentaBancaria).WithMany(p => p.MovimientosBancarios)
                .HasForeignKey(d => d.CuentaBancariaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MovimientosBancarios_Cuenta");

            entity.HasOne(d => d.Pago).WithMany(p => p.MovimientosBancarios)
                .HasForeignKey(d => d.PagoId)
                .HasConstraintName("FK_MovimientosBancarios_Pago");

            entity.HasOne(d => d.TipoMovimiento).WithMany(p => p.MovimientosBancarios)
                .HasForeignKey(d => d.TipoMovimientoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MovimientosBancarios_Tipo");

            entity.HasOne(d => d.Usuario).WithMany(p => p.MovimientosBancarios)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MovimientosBancarios_Usuario");
        });

        modelBuilder.Entity<MovimientosCaja>(entity =>
        {
            entity.Property(e => e.Descripcion)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.Fecha).HasPrecision(3);
            entity.Property(e => e.Monto).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.Referencia)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.HasOne(d => d.FormaPago).WithMany(p => p.MovimientosCaja)
                .HasForeignKey(d => d.FormaPagoId)
                .HasConstraintName("FK_MovimientosCaja_FormaPago");

            entity.HasOne(d => d.TipoMovimiento).WithMany(p => p.MovimientosCaja)
                .HasForeignKey(d => d.TipoMovimientoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MovimientosCaja_Tipo");

            entity.HasOne(d => d.TurnoCaja).WithMany(p => p.MovimientosCaja)
                .HasForeignKey(d => d.TurnoCajaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MovimientosCaja_Turno");

            entity.HasOne(d => d.Usuario).WithMany(p => p.MovimientosCaja)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MovimientosCaja_Usuario");
        });

        modelBuilder.Entity<MovimientosPuntos>(entity =>
        {
            entity.Property(e => e.Descripcion)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.FechaMovimiento).HasPrecision(3);

            entity.HasOne(d => d.ClienteFidelidad).WithMany(p => p.MovimientosPuntos)
                .HasForeignKey(d => d.ClienteFidelidadId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MovimientosPuntos_ClienteFidelidad");

            entity.HasOne(d => d.TipoMovimiento).WithMany(p => p.MovimientosPuntos)
                .HasForeignKey(d => d.TipoMovimientoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MovimientosPuntos_Tipo");

            entity.HasOne(d => d.Venta).WithMany(p => p.MovimientosPuntos)
                .HasForeignKey(d => d.VentaId)
                .HasConstraintName("FK_MovimientosPuntos_Venta");
        });

        modelBuilder.Entity<Municipios>(entity =>
        {
            entity.HasIndex(e => new { e.DepartamentoId, e.Nombre }, "UQ_Municipios_DepartamentoId_Nombre").IsUnique();

            entity.Property(e => e.Codigo)
                .HasMaxLength(4)
                .IsUnicode(false);
            entity.Property(e => e.Nombre)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.Departamento).WithMany(p => p.Municipios)
                .HasForeignKey(d => d.DepartamentoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Municipios_DepartamentoId");
        });

        modelBuilder.Entity<NivelesDeAccesos>(entity =>
        {
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .IsUnicode(false);
        });

        modelBuilder.Entity<NivelesFidelidad>(entity =>
        {
            entity.HasIndex(e => new { e.ProgramaId, e.Nombre }, "UQ_NivelesFidelidad").IsUnique();

            entity.Property(e => e.Multiplicador).HasColumnType("decimal(9, 4)");
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.HasOne(d => d.Estado).WithMany(p => p.NivelesFidelidad)
                .HasForeignKey(d => d.EstadoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_NivelesFidelidad_Estado");

            entity.HasOne(d => d.Programa).WithMany(p => p.NivelesFidelidad)
                .HasForeignKey(d => d.ProgramaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_NivelesFidelidad_Programa");
        });

        modelBuilder.Entity<NotasCredito>(entity =>
        {
            entity.HasIndex(e => new { e.EmpresaId, e.NumeroNota }, "UQ_NotasCredito_Numero").IsUnique();

            entity.Property(e => e.Fecha).HasPrecision(3);
            entity.Property(e => e.Monto).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.Motivo)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.NumeroNota)
                .HasMaxLength(30)
                .IsUnicode(false);

            entity.HasOne(d => d.Cliente).WithMany(p => p.NotasCredito)
                .HasForeignKey(d => d.ClienteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_NotasCredito_Cliente");

            entity.HasOne(d => d.Empresa).WithMany(p => p.NotasCredito)
                .HasForeignKey(d => d.EmpresaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_NotasCredito_Empresa");

            entity.HasOne(d => d.Estado).WithMany(p => p.NotasCredito)
                .HasForeignKey(d => d.EstadoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_NotasCredito_Estado");

            entity.HasOne(d => d.Venta).WithMany(p => p.NotasCredito)
                .HasForeignKey(d => d.VentaId)
                .HasConstraintName("FK_NotasCredito_Venta");
        });

        modelBuilder.Entity<NotasDebito>(entity =>
        {
            entity.HasIndex(e => new { e.EmpresaId, e.NumeroNota }, "UQ_NotasDebito_Numero").IsUnique();

            entity.Property(e => e.Fecha).HasPrecision(3);
            entity.Property(e => e.Monto).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.Motivo)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.NumeroNota)
                .HasMaxLength(30)
                .IsUnicode(false);

            entity.HasOne(d => d.Cliente).WithMany(p => p.NotasDebito)
                .HasForeignKey(d => d.ClienteId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_NotasDebito_Cliente");

            entity.HasOne(d => d.Empresa).WithMany(p => p.NotasDebito)
                .HasForeignKey(d => d.EmpresaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_NotasDebito_Empresa");

            entity.HasOne(d => d.Estado).WithMany(p => p.NotasDebito)
                .HasForeignKey(d => d.EstadoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_NotasDebito_Estado");

            entity.HasOne(d => d.Venta).WithMany(p => p.NotasDebito)
                .HasForeignKey(d => d.VentaId)
                .HasConstraintName("FK_NotasDebito_Venta");
        });

        modelBuilder.Entity<Notificaciones>(entity =>
        {
            entity.Property(e => e.Comentario)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.Enlace)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.TipoNotificacionId)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.Sucursal).WithMany(p => p.Notificaciones)
                .HasForeignKey(d => d.SucursalId)
                .HasConstraintName("FK_Notificaciones_SucursalId");

            entity.HasOne(d => d.TipoNotificacion).WithMany(p => p.Notificaciones)
                .HasForeignKey(d => d.TipoNotificacionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Notificaciones_TipoNotificacionId");
        });

        modelBuilder.Entity<OrdenProduccionDetalles>(entity =>
        {
            entity.HasIndex(e => new { e.OrdenProduccionId, e.ProductoInsumoId }, "UQ_OrdenProduccionDetalles").IsUnique();

            entity.Property(e => e.CantidadPlanificada).HasColumnType("decimal(19, 6)");
            entity.Property(e => e.CantidadPlanificadaBase).HasColumnType("decimal(19, 6)");
            entity.Property(e => e.CostoPlanificado).HasColumnType("decimal(19, 6)");
            entity.Property(e => e.FactorConversion).HasColumnType("decimal(19, 6)");

            entity.HasOne(d => d.OrdenProduccion).WithMany(p => p.OrdenProduccionDetalles)
                .HasForeignKey(d => d.OrdenProduccionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrdenProduccionDetalles_Orden");

            entity.HasOne(d => d.ProductoInsumo).WithMany(p => p.OrdenProduccionDetalles)
                .HasForeignKey(d => d.ProductoInsumoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrdenProduccionDetalles_Producto");

            entity.HasOne(d => d.ProductoPresentacion).WithMany()
                .HasForeignKey(d => d.ProductoPresentacionId)
                .HasConstraintName("FK_OrdenProduccionDetalles_ProductoPresentacion");

            entity.HasOne(d => d.UnidadMedida).WithMany(p => p.OrdenProduccionDetalles)
                .HasForeignKey(d => d.UnidadMedidaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrdenProduccionDetalles_Unidad");
        });

        modelBuilder.Entity<OrdenesCompra>(entity =>
        {
            entity.HasIndex(e => new { e.EmpresaId, e.NumeroOrden }, "UQ_OrdenesCompra_Numero").IsUnique();

            entity.Property(e => e.FechaEmision).HasPrecision(3);
            entity.Property(e => e.FechaEntregaEsperada).HasPrecision(3);
            entity.Property(e => e.Impuestos).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.NumeroOrden)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.SubTotal).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.TipoCambio).HasColumnType("decimal(19, 8)");
            entity.Property(e => e.TotalEstimado).HasColumnType("decimal(12, 2)");

            entity.HasOne(d => d.Empresa).WithMany(p => p.OrdenesCompra)
                .HasForeignKey(d => d.EmpresaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrdenesCompra_Empresa");

            entity.HasOne(d => d.Moneda).WithMany(p => p.OrdenesCompra)
                .HasForeignKey(d => d.MonedaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrdenesCompra_Moneda");

            entity.HasOne(d => d.Proveedor).WithMany(p => p.OrdenesCompra)
                .HasForeignKey(d => d.ProveedorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrdenesCompra_Proveedor");

            entity.HasOne(d => d.Sucursal).WithMany(p => p.OrdenesCompra)
                .HasForeignKey(d => d.SucursalId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrdenesCompra_Sucursal");

            entity.HasOne(d => d.UsuarioCreacion).WithMany(p => p.OrdenesCompra)
                .HasForeignKey(d => d.UsuarioCreacionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrdenesCompra_Usuario");

            entity.HasOne(d => d.EstadosFlujo).WithMany(p => p.OrdenesCompra)
                .HasPrincipalKey(p => new { p.FlujoEstadoId, p.Id })
                .HasForeignKey(d => new { d.FlujoEstadoId, d.EstadoId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrdenesCompra_Estado");
        });

        modelBuilder.Entity<OrdenesCompraDetalle>(entity =>
        {
            entity.HasIndex(e => new { e.OrdenCompraId, e.NumeroLinea }, "UQ_OrdenesCompraDetalle_Linea").IsUnique();

            entity.Property(e => e.Cantidad).HasColumnType("decimal(19, 6)");
            entity.Property(e => e.Descuento).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.Impuesto).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.PrecioUnitario).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.TotalLinea)
                .HasComputedColumnSql("(([Cantidad]*[PrecioUnitario]-[Descuento])+[Impuesto])", true)
                .HasColumnType("decimal(34, 8)");

            entity.HasOne(d => d.OrdenCompra).WithMany(p => p.OrdenesCompraDetalle)
                .HasForeignKey(d => d.OrdenCompraId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrdenesCompraDetalle_Orden");

            entity.HasOne(d => d.Producto).WithMany(p => p.OrdenesCompraDetalle)
                .HasForeignKey(d => d.ProductoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrdenesCompraDetalle_Producto");

            entity.HasOne(d => d.ProductoPresentacion).WithMany(p => p.OrdenesCompraDetalle)
                .HasForeignKey(d => d.ProductoPresentacionId)
                .HasConstraintName("FK_OrdenesCompraDetalle_Presentacion");
        });

        modelBuilder.Entity<OrdenesProduccion>(entity =>
        {
            entity.HasIndex(e => new { e.EmpresaId, e.NumeroOrden }, "UQ_OrdenesProduccion_Numero").IsUnique();

            entity.Property(e => e.CantidadPlanificada).HasColumnType("decimal(19, 6)");
            entity.Property(e => e.CantidadPlanificadaBase).HasColumnType("decimal(19, 6)");
            entity.Property(e => e.CantidadProducida).HasColumnType("decimal(19, 6)");
            entity.Property(e => e.FactorConversion).HasColumnType("decimal(19, 6)");
            entity.Property(e => e.FechaFin).HasPrecision(3);
            entity.Property(e => e.FechaInicio).HasPrecision(3);
            entity.Property(e => e.NumeroOrden)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.VersionFila)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.BodegaProduccion).WithMany(p => p.OrdenesProduccion)
                .HasForeignKey(d => d.BodegaProduccionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrdenesProduccion_Bodega");

            entity.HasOne(d => d.Empresa).WithMany(p => p.OrdenesProduccion)
                .HasForeignKey(d => d.EmpresaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrdenesProduccion_Empresa");

            entity.HasOne(d => d.ProductoAProducir).WithMany(p => p.OrdenesProduccion)
                .HasForeignKey(d => d.ProductoAProducirId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrdenesProduccion_Producto");

            entity.HasOne(d => d.ProductoPresentacion).WithMany()
                .HasForeignKey(d => d.ProductoPresentacionId)
                .HasConstraintName("FK_OrdenesProduccion_ProductoPresentacion");

            entity.HasOne(d => d.Receta).WithMany(p => p.OrdenesProduccion)
                .HasForeignKey(d => d.RecetaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrdenesProduccion_Receta");

            entity.HasOne(d => d.ResponsableUsuario).WithMany(p => p.OrdenesProduccion)
                .HasForeignKey(d => d.ResponsableUsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrdenesProduccion_Responsable");

            entity.HasOne(d => d.Sucursal).WithMany(p => p.OrdenesProduccion)
                .HasForeignKey(d => d.SucursalId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrdenesProduccion_Sucursal");

            entity.HasOne(d => d.EstadosFlujo).WithMany(p => p.OrdenesProduccion)
                .HasPrincipalKey(p => new { p.FlujoEstadoId, p.Id })
                .HasForeignKey(d => new { d.FlujoEstadoId, d.EstadoId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_OrdenesProduccion_Estado");
        });

        modelBuilder.Entity<PagoDetalles>(entity =>
        {
            entity.Property(e => e.Monto).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.Referencia)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.HasOne(d => d.Banco).WithMany(p => p.PagoDetalles)
                .HasForeignKey(d => d.BancoId)
                .HasConstraintName("FK_PagoDetalles_Banco");

            entity.HasOne(d => d.CuentaBancaria).WithMany(p => p.PagoDetalles)
                .HasForeignKey(d => d.CuentaBancariaId)
                .HasConstraintName("FK_PagoDetalles_CuentaBancaria");

            entity.HasOne(d => d.FormaPago).WithMany(p => p.PagoDetalles)
                .HasForeignKey(d => d.FormaPagoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PagoDetalles_FormaPago");

            entity.HasOne(d => d.Pago).WithMany(p => p.PagoDetalles)
                .HasForeignKey(d => d.PagoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PagoDetalles_Pago");
        });

        modelBuilder.Entity<Pagos>(entity =>
        {
            entity.Property(e => e.Fecha).HasPrecision(3);
            entity.Property(e => e.MontoTotal).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.TipoCambio).HasColumnType("decimal(19, 8)");

            entity.HasOne(d => d.Cliente).WithMany(p => p.Pagos)
                .HasForeignKey(d => d.ClienteId)
                .HasConstraintName("FK_Pagos_Cliente");

            entity.HasOne(d => d.Empresa).WithMany(p => p.Pagos)
                .HasForeignKey(d => d.EmpresaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Pagos_Empresa");

            entity.HasOne(d => d.Estado).WithMany(p => p.Pagos)
                .HasForeignKey(d => d.EstadoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Pagos_Estado");

            entity.HasOne(d => d.Moneda).WithMany(p => p.Pagos)
                .HasForeignKey(d => d.MonedaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Pagos_Moneda");

            entity.HasOne(d => d.Proveedor).WithMany(p => p.Pagos)
                .HasForeignKey(d => d.ProveedorId)
                .HasConstraintName("FK_Pagos_Proveedor");

            entity.HasOne(d => d.Sucursal).WithMany(p => p.Pagos)
                .HasForeignKey(d => d.SucursalId)
                .HasConstraintName("FK_Pagos_Sucursal");

            entity.HasOne(d => d.TipoPagoOperacion).WithMany(p => p.Pagos)
                .HasForeignKey(d => d.TipoPagoOperacionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Pagos_TipoOperacion");

            entity.HasOne(d => d.TurnoCaja).WithMany(p => p.Pagos)
                .HasForeignKey(d => d.TurnoCajaId)
                .HasConstraintName("FK_Pagos_TurnoCaja");

            entity.HasOne(d => d.Usuario).WithMany(p => p.Pagos)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Pagos_Usuario");
        });

        modelBuilder.Entity<ParametrosStockProductos>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_ParametrosStockProducto");

            entity.HasIndex(e => new { e.EmpresaId, e.ProductoId, e.BodegaId }, "UQ_Parametros_Producto_Bodega").IsUnique();

            entity.Property(e => e.CantidadReorden).HasColumnType("decimal(19, 6)");
            entity.Property(e => e.FechaCreacion).HasPrecision(3);
            entity.Property(e => e.FechaModificacion).HasPrecision(3);
            entity.Property(e => e.PuntoReorden).HasColumnType("decimal(19, 6)");
            entity.Property(e => e.StockMaximo).HasColumnType("decimal(19, 6)");
            entity.Property(e => e.StockMinimo).HasColumnType("decimal(19, 6)");
            entity.Property(e => e.VersionFila)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.CreadoPorUsuario).WithMany(p => p.ParametrosStockProductosCreadoPorUsuario)
                .HasForeignKey(d => d.CreadoPorUsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ParametrosStockProductos_CreadoPor");

            entity.HasOne(d => d.Empresa).WithMany(p => p.ParametrosStockProductos)
                .HasForeignKey(d => d.EmpresaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ParametrosStockProductos_Empresa");

            entity.HasOne(d => d.Estado).WithMany(p => p.ParametrosStockProductos)
                .HasForeignKey(d => d.EstadoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ParametrosStockProductos_Estado");

            entity.HasOne(d => d.ModificadoPorUsuario).WithMany(p => p.ParametrosStockProductosModificadoPorUsuario)
                .HasForeignKey(d => d.ModificadoPorUsuarioId)
                .HasConstraintName("FK_ParametrosStockProductos_ModificadoPor");

            entity.HasOne(d => d.Bodegas).WithMany(p => p.ParametrosStockProductos)
                .HasPrincipalKey(p => new { p.EmpresaId, p.Id })
                .HasForeignKey(d => new { d.EmpresaId, d.BodegaId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Parametros_Bodega");

            entity.HasOne(d => d.Productos).WithMany(p => p.ParametrosStockProductos)
                .HasPrincipalKey(p => new { p.EmpresaId, p.Id })
                .HasForeignKey(d => new { d.EmpresaId, d.ProductoId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Parametros_Producto");

            entity.HasOne(d => d.Ubicaciones).WithMany(p => p.ParametrosStockProductos)
                .HasPrincipalKey(p => new { p.EmpresaId, p.Id })
                .HasForeignKey(d => new { d.EmpresaId, d.UbicacionPredeterminadaId })
                .HasConstraintName("FK_Parametros_Ubicacion");
        });

        modelBuilder.Entity<PedidoVentaDetalles>(entity =>
        {
            entity.HasIndex(e => new { e.PedidoVentaId, e.NumeroLinea }, "UQ_PedidoVentaDetalles_Linea").IsUnique();

            entity.Property(e => e.Cantidad).HasColumnType("decimal(19, 6)");
            entity.Property(e => e.Descuento).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.Impuesto).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.PrecioUnitario).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.TotalLinea)
                .HasComputedColumnSql("(([Cantidad]*[PrecioUnitario]-[Descuento])+[Impuesto])", true)
                .HasColumnType("decimal(34, 8)");

            entity.HasOne(d => d.PedidoVenta).WithMany(p => p.PedidoVentaDetalles)
                .HasForeignKey(d => d.PedidoVentaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PedidoVentaDetalles_Pedido");

            entity.HasOne(d => d.Producto).WithMany(p => p.PedidoVentaDetalles)
                .HasForeignKey(d => d.ProductoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PedidoVentaDetalles_Producto");

            entity.HasOne(d => d.ProductoPresentacion).WithMany(p => p.PedidoVentaDetalles)
                .HasForeignKey(d => d.ProductoPresentacionId)
                .HasConstraintName("FK_PedidoVentaDetalles_Presentacion");
        });

        modelBuilder.Entity<PedidosVenta>(entity =>
        {
            entity.HasIndex(e => new { e.EmpresaId, e.NumeroPedido }, "UQ_PedidosVenta_Numero").IsUnique();

            entity.Property(e => e.FechaEntrega).HasPrecision(3);
            entity.Property(e => e.FechaPedido).HasPrecision(3);
            entity.Property(e => e.NumeroPedido)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Total).HasColumnType("decimal(12, 2)");

            entity.HasOne(d => d.Cliente).WithMany(p => p.PedidosVenta)
                .HasForeignKey(d => d.ClienteId)
                .HasConstraintName("FK_PedidosVenta_Cliente");

            entity.HasOne(d => d.Cotizacion).WithMany(p => p.PedidosVenta)
                .HasForeignKey(d => d.CotizacionId)
                .HasConstraintName("FK_PedidosVenta_Cotizacion");

            entity.HasOne(d => d.CreadoPorUsuario).WithMany(p => p.PedidosVenta)
                .HasForeignKey(d => d.CreadoPorUsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PedidosVenta_Usuario");

            entity.HasOne(d => d.Empresa).WithMany(p => p.PedidosVenta)
                .HasForeignKey(d => d.EmpresaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PedidosVenta_Empresa");

            entity.HasOne(d => d.Sucursal).WithMany(p => p.PedidosVenta)
                .HasForeignKey(d => d.SucursalId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PedidosVenta_Sucursal");

            entity.HasOne(d => d.EstadosFlujo).WithMany(p => p.PedidosVenta)
                .HasPrincipalKey(p => new { p.FlujoEstadoId, p.Id })
                .HasForeignKey(d => new { d.FlujoEstadoId, d.EstadoId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PedidosVenta_Estado");
        });

        modelBuilder.Entity<Permisos>(entity =>
        {
            entity.HasIndex(e => e.Codigo, "UQ_Permisos_Codigo").IsUnique();

            entity.HasIndex(e => new { e.ModuloId, e.AccionId, e.Recurso }, "UQ_Permisos_Modulo_Accion_Recurso").IsUnique();

            entity.Property(e => e.Codigo)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.ModuloId)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Recurso)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.HasOne(d => d.Accion).WithMany(p => p.Permisos)
                .HasForeignKey(d => d.AccionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Permisos_Accion");

            entity.HasOne(d => d.Estado).WithMany(p => p.Permisos)
                .HasForeignKey(d => d.EstadoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Permisos_Estado");

            entity.HasOne(d => d.Modulo).WithMany(p => p.Permisos)
                .HasForeignKey(d => d.ModuloId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Permisos_Modulo");
        });

        modelBuilder.Entity<PreciosDetalle>(entity =>
        {
            entity.HasIndex(e => new { e.ListaPrecioId, e.ProductoId, e.ProductoPresentacionId }, "UQ_PreciosDetalle").IsUnique();

            entity.Property(e => e.PrecioVenta).HasColumnType("decimal(12, 2)");

            entity.HasOne(d => d.ListaPrecio).WithMany(p => p.PreciosDetalle)
                .HasForeignKey(d => d.ListaPrecioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PreciosDetalle_Lista");

            entity.HasOne(d => d.Producto).WithMany(p => p.PreciosDetalle)
                .HasForeignKey(d => d.ProductoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PreciosDetalle_Producto");

            entity.HasOne(d => d.ProductoPresentacion).WithMany(p => p.PreciosDetalle)
                .HasForeignKey(d => d.ProductoPresentacionId)
                .HasConstraintName("FK_PreciosDetalle_ProductoPresentacion");
        });

        modelBuilder.Entity<Presentaciones>(entity =>
        {
            entity.HasIndex(e => new { e.EmpresaId, e.Codigo }, "UQ_Presentaciones_Empresa_Codigo").IsUnique();

            entity.HasIndex(e => new { e.EmpresaId, e.Id }, "UQ_Presentaciones_Empresa_Id").IsUnique();

            entity.HasIndex(e => new { e.EmpresaId, e.Nombre }, "UQ_Presentaciones_Empresa_Nombre").IsUnique();

            entity.Property(e => e.Codigo)
                .HasMaxLength(25)
                .IsUnicode(false);
            entity.Property(e => e.Descripcion)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.FechaCreacion).HasPrecision(3);
            entity.Property(e => e.FechaModificacion).HasPrecision(3);
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.VersionFila)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.CreadoPorUsuario).WithMany(p => p.PresentacionesCreadoPorUsuario)
                .HasForeignKey(d => d.CreadoPorUsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Presentaciones_CreadoPor");

            entity.HasOne(d => d.Empresa).WithMany(p => p.Presentaciones)
                .HasForeignKey(d => d.EmpresaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Presentaciones_Empresa");

            entity.HasOne(d => d.Estado).WithMany(p => p.Presentaciones)
                .HasForeignKey(d => d.EstadoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Presentaciones_Estado");

            entity.HasOne(d => d.ModificadoPorUsuario).WithMany(p => p.PresentacionesModificadoPorUsuario)
                .HasForeignKey(d => d.ModificadoPorUsuarioId)
                .HasConstraintName("FK_Presentaciones_ModificadoPor");
        });

        modelBuilder.Entity<ProduccionTerminada>(entity =>
        {
            entity.HasIndex(e => e.OrdenProduccionId, "UQ_ProduccionTerminada_Orden").IsUnique();

            entity.Property(e => e.CantidadProducida).HasColumnType("decimal(19, 6)");
            entity.Property(e => e.CostoIndirecto).HasColumnType("decimal(19, 6)");
            entity.Property(e => e.CostoManoObra).HasColumnType("decimal(19, 6)");
            entity.Property(e => e.CostoMateriaPrima).HasColumnType("decimal(19, 6)");
            entity.Property(e => e.CostoTotal)
                .HasComputedColumnSql("(([CostoMateriaPrima]+[CostoManoObra])+[CostoIndirecto])", true)
                .HasColumnType("decimal(21, 6)");
            entity.Property(e => e.FechaFinalizacion).HasPrecision(3);

            entity.HasOne(d => d.Lote).WithMany(p => p.ProduccionTerminada)
                .HasForeignKey(d => d.LoteId)
                .HasConstraintName("FK_ProduccionTerminada_Lote");

            entity.HasOne(d => d.OrdenProduccion).WithMany(p => p.ProduccionTerminada)
                .HasForeignKey(d => d.OrdenProduccionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProduccionTerminada_Orden");

            entity.HasOne(d => d.Producto).WithMany(p => p.ProduccionTerminada)
                .HasForeignKey(d => d.ProductoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProduccionTerminada_Producto");
        });

        modelBuilder.Entity<ProductoImpuestos>(entity =>
        {
            entity.HasIndex(e => new { e.ProductoId, e.ImpuestoId }, "UQ_ProductoImpuestos").IsUnique();

            entity.HasOne(d => d.Estado).WithMany(p => p.ProductoImpuestos)
                .HasForeignKey(d => d.EstadoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProductoImpuestos_Estado");

            entity.HasOne(d => d.Impuesto).WithMany(p => p.ProductoImpuestos)
                .HasForeignKey(d => d.ImpuestoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProductoImpuestos_Impuesto");

            entity.HasOne(d => d.Producto).WithMany(p => p.ProductoImpuestos)
                .HasForeignKey(d => d.ProductoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProductoImpuestos_Producto");
        });

        modelBuilder.Entity<ProductoPresentaciones>(entity =>
        {
            entity.HasIndex(e => new { e.EmpresaId, e.Id }, "UQ_ProductoPresentaciones_Empresa_Id").IsUnique();

            entity.HasIndex(e => new { e.EmpresaId, e.ProductoId, e.Id }, "UQ_ProductoPresentaciones_Producto_Id").IsUnique();

            entity.HasIndex(e => new { e.EmpresaId, e.ProductoId, e.PresentacionId }, "UQ_ProductoPresentaciones_Producto_Presentacion").IsUnique();

            entity.Property(e => e.FactorConversion).HasColumnType("decimal(19, 6)");
            entity.Property(e => e.FechaCreacion).HasPrecision(3);
            entity.Property(e => e.FechaModificacion).HasPrecision(3);
            entity.Property(e => e.PrecioVentaEstandar).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.VersionFila)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.CreadoPorUsuario).WithMany(p => p.ProductoPresentacionesCreadoPorUsuario)
                .HasForeignKey(d => d.CreadoPorUsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProductoPresentaciones_CreadoPor");

            entity.HasOne(d => d.Empresa).WithMany(p => p.ProductoPresentaciones)
                .HasForeignKey(d => d.EmpresaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProductoPresentaciones_Empresa");

            entity.HasOne(d => d.Estado).WithMany(p => p.ProductoPresentaciones)
                .HasForeignKey(d => d.EstadoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProductoPresentaciones_Estado");

            entity.HasOne(d => d.ModificadoPorUsuario).WithMany(p => p.ProductoPresentacionesModificadoPorUsuario)
                .HasForeignKey(d => d.ModificadoPorUsuarioId)
                .HasConstraintName("FK_ProductoPresentaciones_ModificadoPor");

            entity.HasOne(d => d.Presentaciones).WithMany(p => p.ProductoPresentaciones)
                .HasPrincipalKey(p => new { p.EmpresaId, p.Id })
                .HasForeignKey(d => new { d.EmpresaId, d.PresentacionId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProductoPresentaciones_Presentacion");

            entity.HasOne(d => d.Productos).WithMany(p => p.ProductoPresentaciones)
                .HasPrincipalKey(p => new { p.EmpresaId, p.Id })
                .HasForeignKey(d => new { d.EmpresaId, d.ProductoId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProductoPresentaciones_Producto");

            entity.HasOne(d => d.UnidadesMedida).WithMany(p => p.ProductoPresentaciones)
                .HasPrincipalKey(p => new { p.EmpresaId, p.Id })
                .HasForeignKey(d => new { d.EmpresaId, d.UnidadMedidaId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProductoPresentaciones_Unidad");
        });

        modelBuilder.Entity<Productos>(entity =>
        {
            entity.HasIndex(e => new { e.EmpresaId, e.Id }, "UQ_Productos_Empresa_Id").IsUnique();

            entity.HasIndex(e => new { e.EmpresaId, e.SKU }, "UQ_Productos_Empresa_SKU").IsUnique();

            entity.Property(e => e.Descripcion)
                .HasMaxLength(1000)
                .IsUnicode(false);
            entity.Property(e => e.FechaCreacion).HasPrecision(3);
            entity.Property(e => e.FechaModificacion).HasPrecision(3);
            entity.Property(e => e.Imagen)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.Modelo)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Nombre)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.Peso).HasColumnType("decimal(19, 6)");
            entity.Property(e => e.PrecioVentaReferencial).HasColumnType("decimal(10, 2)");
            entity.Property(e => e.SKU)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.VersionFila)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.CreadoPorUsuario).WithMany(p => p.ProductosCreadoPorUsuario)
                .HasForeignKey(d => d.CreadoPorUsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Productos_CreadoPor");

            entity.HasOne(d => d.Empresa).WithMany(p => p.Productos)
                .HasForeignKey(d => d.EmpresaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Productos_Empresa");

            entity.HasOne(d => d.Estado).WithMany(p => p.Productos)
                .HasForeignKey(d => d.EstadoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Productos_Estado");

            entity.HasOne(d => d.MetodoValuacion).WithMany(p => p.ProductosMetodoValuacion)
                .HasForeignKey(d => d.MetodoValuacionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Productos_MetodoValuacion");

            entity.HasOne(d => d.ModificadoPorUsuario).WithMany(p => p.ProductosModificadoPorUsuario)
                .HasForeignKey(d => d.ModificadoPorUsuarioId)
                .HasConstraintName("FK_Productos_ModificadoPor");

            entity.HasOne(d => d.TipoSeguimiento).WithMany(p => p.ProductosTipoSeguimiento)
                .HasForeignKey(d => d.TipoSeguimientoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Productos_TipoSeguimiento");

            entity.HasOne(d => d.Categorias).WithMany(p => p.Productos)
                .HasPrincipalKey(p => new { p.EmpresaId, p.Id })
                .HasForeignKey(d => new { d.EmpresaId, d.CategoriaId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Productos_Categoria");

            entity.HasOne(d => d.Marcas).WithMany(p => p.Productos)
                .HasPrincipalKey(p => new { p.EmpresaId, p.Id })
                .HasForeignKey(d => new { d.EmpresaId, d.MarcaId })
                .HasConstraintName("FK_Productos_Marca");

            entity.HasOne(d => d.TiposProductos).WithMany(p => p.Productos)
                .HasPrincipalKey(p => new { p.EmpresaId, p.Id })
                .HasForeignKey(d => new { d.EmpresaId, d.TipoProductoId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Productos_Tipo");

            entity.HasOne(d => d.UnidadesMedida).WithMany(p => p.Productos)
                .HasPrincipalKey(p => new { p.EmpresaId, p.Id })
                .HasForeignKey(d => new { d.EmpresaId, d.UnidadBaseId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Productos_UnidadBase");
        });

        modelBuilder.Entity<ProgramasFidelidad>(entity =>
        {
            entity.HasIndex(e => new { e.EmpresaId, e.Codigo }, "UQ_ProgramasFidelidad_Empresa_Codigo").IsUnique();

            entity.Property(e => e.Codigo)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.MontoPorPunto).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.ValorPunto).HasColumnType("decimal(12, 4)");

            entity.HasOne(d => d.Empresa).WithMany(p => p.ProgramasFidelidad)
                .HasForeignKey(d => d.EmpresaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProgramasFidelidad_Empresa");

            entity.HasOne(d => d.Estado).WithMany(p => p.ProgramasFidelidad)
                .HasForeignKey(d => d.EstadoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProgramasFidelidad_Estado");
        });

        modelBuilder.Entity<PromocionCategorias>(entity =>
        {
            entity.HasIndex(e => new { e.PromocionId, e.CategoriaId }, "UQ_PromocionCategorias").IsUnique();

            entity.HasOne(d => d.Categoria).WithMany(p => p.PromocionCategorias)
                .HasForeignKey(d => d.CategoriaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PromocionCategorias_Categoria");

            entity.HasOne(d => d.Promocion).WithMany(p => p.PromocionCategorias)
                .HasForeignKey(d => d.PromocionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PromocionCategorias_Promocion");
        });

        modelBuilder.Entity<PromocionHorarios>(entity =>
        {
            entity.HasIndex(e => new { e.PromocionId, e.DiaSemana, e.HoraInicio }, "UQ_PromocionHorarios").IsUnique();

            entity.HasOne(d => d.Promocion).WithMany(p => p.PromocionHorarios)
                .HasForeignKey(d => d.PromocionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PromocionHorarios_Promocion");
        });

        modelBuilder.Entity<PromocionProductos>(entity =>
        {
            entity.HasIndex(e => new { e.PromocionId, e.ProductoId }, "UQ_PromocionProductos").IsUnique();

            entity.HasOne(d => d.Producto).WithMany(p => p.PromocionProductos)
                .HasForeignKey(d => d.ProductoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PromocionProductos_Producto");

            entity.HasOne(d => d.Promocion).WithMany(p => p.PromocionProductos)
                .HasForeignKey(d => d.PromocionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PromocionProductos_Promocion");
        });

        modelBuilder.Entity<PromocionSucursales>(entity =>
        {
            entity.HasIndex(e => new { e.PromocionId, e.SucursalId }, "UQ_PromocionSucursales").IsUnique();

            entity.HasOne(d => d.Promocion).WithMany(p => p.PromocionSucursales)
                .HasForeignKey(d => d.PromocionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PromocionSucursales_Promocion");

            entity.HasOne(d => d.Sucursal).WithMany(p => p.PromocionSucursales)
                .HasForeignKey(d => d.SucursalId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PromocionSucursales_Sucursal");
        });

        modelBuilder.Entity<Promociones>(entity =>
        {
            entity.HasIndex(e => new { e.EmpresaId, e.Codigo }, "UQ_Promociones_Empresa_Codigo").IsUnique();

            entity.Property(e => e.CantidadMinima).HasColumnType("decimal(19, 6)");
            entity.Property(e => e.Codigo)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.DescuentoMonto).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.DescuentoPorcentaje).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.FechaFin).HasPrecision(3);
            entity.Property(e => e.FechaInicio).HasPrecision(3);
            entity.Property(e => e.MontoMinimo).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.Nombre)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.PrecioEspecial).HasColumnType("decimal(12, 2)");

            entity.HasOne(d => d.Empresa).WithMany(p => p.Promociones)
                .HasForeignKey(d => d.EmpresaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Promociones_Empresa");

            entity.HasOne(d => d.Estado).WithMany(p => p.Promociones)
                .HasForeignKey(d => d.EstadoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Promociones_Estado");

            entity.HasOne(d => d.TipoPromocion).WithMany(p => p.Promociones)
                .HasForeignKey(d => d.TipoPromocionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Promociones_Tipo");
        });

        modelBuilder.Entity<ProveedorProductos>(entity =>
        {
            entity.HasIndex(e => new { e.ProveedorId, e.ProductoId }, "UQ_ProveedorProductos").IsUnique();

            entity.Property(e => e.CodigoProveedor)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PrecioUltimaCompra).HasColumnType("decimal(12, 2)");

            entity.HasOne(d => d.Estado).WithMany(p => p.ProveedorProductos)
                .HasForeignKey(d => d.EstadoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProveedorProductos_Estado");

            entity.HasOne(d => d.Producto).WithMany(p => p.ProveedorProductos)
                .HasForeignKey(d => d.ProductoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProveedorProductos_Producto");

            entity.HasOne(d => d.Proveedor).WithMany(p => p.ProveedorProductos)
                .HasForeignKey(d => d.ProveedorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ProveedorProductos_Proveedor");
        });

        modelBuilder.Entity<Proveedores>(entity =>
        {
            entity.HasIndex(e => new { e.EmpresaId, e.CodigoProveedor }, "UQ_ProveedoresCodigo").IsUnique();

            entity.Property(e => e.CodigoProveedor)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Correo)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Direccion)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.LimiteCredito).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.Nit)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.NombreProveedor)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.Telefono)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.Empresa).WithMany(p => p.Proveedores)
                .HasForeignKey(d => d.EmpresaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Proveedores_Empresa");

            entity.HasOne(d => d.Estado).WithMany(p => p.Proveedores)
                .HasForeignKey(d => d.EstadoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Proveedores_Estado");

            entity.HasOne(d => d.Moneda).WithMany(p => p.Proveedores)
                .HasForeignKey(d => d.MonedaId)
                .HasConstraintName("FK_Proveedores_Moneda");
        });

        modelBuilder.Entity<RecepcionCompraDetalles>(entity =>
        {
            entity.Property(e => e.CantidadRecibida).HasColumnType("decimal(19, 6)");
            entity.Property(e => e.CostoUnitario).HasColumnType("decimal(19, 6)");

            entity.HasOne(d => d.Lote).WithMany(p => p.RecepcionCompraDetalles)
                .HasForeignKey(d => d.LoteId)
                .HasConstraintName("FK_RecepcionCompraDetalles_Lote");

            entity.HasOne(d => d.OrdenCompraDetalle).WithMany(p => p.RecepcionCompraDetalles)
                .HasForeignKey(d => d.OrdenCompraDetalleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RecepcionCompraDetalles_OrdenDetalle");

            entity.HasOne(d => d.Producto).WithMany(p => p.RecepcionCompraDetalles)
                .HasForeignKey(d => d.ProductoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RecepcionCompraDetalles_Producto");

            entity.HasOne(d => d.RecepcionCompra).WithMany(p => p.RecepcionCompraDetalles)
                .HasForeignKey(d => d.RecepcionCompraId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RecepcionCompraDetalles_Recepcion");
        });

        modelBuilder.Entity<RecepcionesCompra>(entity =>
        {
            entity.HasIndex(e => new { e.EmpresaId, e.NumeroRecepcion }, "UQ_RecepcionesCompra_Numero").IsUnique();

            entity.Property(e => e.FechaRecepcion).HasPrecision(3);
            entity.Property(e => e.NumeroRecepcion)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Observaciones)
                .HasMaxLength(500)
                .IsUnicode(false);

            entity.HasOne(d => d.Empresa).WithMany(p => p.RecepcionesCompra)
                .HasForeignKey(d => d.EmpresaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RecepcionesCompra_Empresa");

            entity.HasOne(d => d.OrdenCompra).WithMany(p => p.RecepcionesCompra)
                .HasForeignKey(d => d.OrdenCompraId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RecepcionesCompra_Orden");

            entity.HasOne(d => d.RecibidoPorUsuario).WithMany(p => p.RecepcionesCompra)
                .HasForeignKey(d => d.RecibidoPorUsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RecepcionesCompra_Usuario");

            entity.HasOne(d => d.Sucursal).WithMany(p => p.RecepcionesCompra)
                .HasForeignKey(d => d.SucursalId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RecepcionesCompra_Sucursal");

            entity.HasOne(d => d.EstadosFlujo).WithMany(p => p.RecepcionesCompra)
                .HasPrincipalKey(p => new { p.FlujoEstadoId, p.Id })
                .HasForeignKey(d => new { d.FlujoEstadoId, d.EstadoId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RecepcionesCompra_Estado");
        });

        modelBuilder.Entity<RecetaDetalles>(entity =>
        {
            entity.HasIndex(e => new { e.RecetaId, e.ProductoInsumoId }, "UQ_RecetaDetalles").IsUnique();

            entity.Property(e => e.CantidadRequerida).HasColumnType("decimal(19, 6)");
            entity.Property(e => e.CantidadBase).HasColumnType("decimal(19, 6)");
            entity.Property(e => e.FactorConversion).HasColumnType("decimal(19, 6)");
            entity.Property(e => e.Observacion)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.PorcentajeDesperdicioTecnico).HasColumnType("decimal(5, 2)");

            entity.HasOne(d => d.ProductoInsumo).WithMany(p => p.RecetaDetalles)
                .HasForeignKey(d => d.ProductoInsumoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RecetaDetalles_Producto");

            entity.HasOne(d => d.ProductoPresentacion).WithMany()
                .HasForeignKey(d => d.ProductoPresentacionId)
                .HasConstraintName("FK_RecetaDetalles_ProductoPresentacion");

            entity.HasOne(d => d.Receta).WithMany(p => p.RecetaDetalles)
                .HasForeignKey(d => d.RecetaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RecetaDetalles_Receta");

            entity.HasOne(d => d.UnidadMedida).WithMany(p => p.RecetaDetalles)
                .HasForeignKey(d => d.UnidadMedidaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RecetaDetalles_Unidad");
        });

        modelBuilder.Entity<Recetas>(entity =>
        {
            entity.HasIndex(e => new { e.EmpresaId, e.Codigo, e.Version }, "UQ_Recetas_Empresa_Codigo_Version").IsUnique();

            entity.HasIndex(e => new { e.EmpresaId, e.ProductoTerminadoId }, "UQ_Recetas_Empresa_Producto_Vigente")
                .IsUnique()
                .HasFilter("[VigenteHasta] IS NULL");

            entity.Property(e => e.Codigo)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Nombre)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.FactorConversion).HasColumnType("decimal(19, 6)");
            entity.Property(e => e.Rendimiento).HasColumnType("decimal(19, 6)");
            entity.Property(e => e.RendimientoBase).HasColumnType("decimal(19, 6)");
            entity.Property(e => e.VersionFila)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.Empresa).WithMany(p => p.Recetas)
                .HasForeignKey(d => d.EmpresaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Recetas_Empresa");

            entity.HasOne(d => d.Estado).WithMany(p => p.Recetas)
                .HasForeignKey(d => d.EstadoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Recetas_Estado");

            entity.HasOne(d => d.ProductoTerminado).WithMany(p => p.Recetas)
                .HasForeignKey(d => d.ProductoTerminadoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Recetas_ProductoTerminado");

            entity.HasOne(d => d.ProductoPresentacionRendimiento).WithMany()
                .HasForeignKey(d => d.ProductoPresentacionRendimientoId)
                .HasConstraintName("FK_Recetas_ProductoPresentacionRendimiento");

            entity.HasOne(d => d.UnidadRendimiento).WithMany(p => p.Recetas)
                .HasForeignKey(d => d.UnidadRendimientoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Recetas_UnidadRendimiento");
        });

        modelBuilder.Entity<ReglasAutorizacion>(entity =>
        {
            entity.HasIndex(e => new { e.EmpresaId, e.Codigo }, "UQ_ReglasAutorizacion_Empresa_Codigo").IsUnique();

            entity.Property(e => e.Codigo)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ModuloId)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Nombre)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.RoleAutorizadorId)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.ValorUmbral).HasColumnType("decimal(19, 6)");

            entity.HasOne(d => d.Empresa).WithMany(p => p.ReglasAutorizacion)
                .HasForeignKey(d => d.EmpresaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ReglasAutorizacion_Empresa");

            entity.HasOne(d => d.Estado).WithMany(p => p.ReglasAutorizacion)
                .HasForeignKey(d => d.EstadoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ReglasAutorizacion_Estado");

            entity.HasOne(d => d.Modulo).WithMany(p => p.ReglasAutorizacion)
                .HasForeignKey(d => d.ModuloId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ReglasAutorizacion_Modulo");

            entity.HasOne(d => d.RoleAutorizador).WithMany(p => p.ReglasAutorizacion)
                .HasForeignKey(d => d.RoleAutorizadorId)
                .HasConstraintName("FK_ReglasAutorizacion_Role");

            entity.HasOne(d => d.TipoRegla).WithMany(p => p.ReglasAutorizacion)
                .HasForeignKey(d => d.TipoReglaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ReglasAutorizacion_Tipo");
        });

        modelBuilder.Entity<Reportes>(entity =>
        {
            entity.Property(e => e.Id)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Comentario)
                .HasMaxLength(1000)
                .IsUnicode(false);
            entity.Property(e => e.Descripcion)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.ModuloId)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.Cliente).WithMany(p => p.Reportes)
                .HasForeignKey(d => d.ClienteId)
                .HasConstraintName("FK_Reportes_ClienteId");

            entity.HasOne(d => d.Estado).WithMany(p => p.Reportes)
                .HasForeignKey(d => d.EstadoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Reportes_Estado");

            entity.HasOne(d => d.Modulo).WithMany(p => p.Reportes)
                .HasForeignKey(d => d.ModuloId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Reportes_ModuloId");
        });

        modelBuilder.Entity<ReservaInventarioDetalles>(entity =>
        {
            entity.HasIndex(e => new { e.EmpresaId, e.ReservaId, e.NumeroLinea }, "UQ_ReservaInventarioDetalles_Linea").IsUnique();

            entity.Property(e => e.Cantidad).HasColumnType("decimal(19, 6)");
            entity.Property(e => e.CantidadAplicada).HasColumnType("decimal(19, 6)");
            entity.Property(e => e.CantidadLiberada).HasColumnType("decimal(19, 6)");
            entity.Property(e => e.VersionFila)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.Bodega).WithMany(p => p.ReservaInventarioDetalles)
                .HasForeignKey(d => d.BodegaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ReservaInventarioDetalles_Bodega");

            entity.HasOne(d => d.Empresa).WithMany(p => p.ReservaInventarioDetalles)
                .HasForeignKey(d => d.EmpresaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ReservaInventarioDetalles_Empresa");

            entity.HasOne(d => d.Lote).WithMany(p => p.ReservaInventarioDetalles)
                .HasForeignKey(d => d.LoteId)
                .HasConstraintName("FK_ReservaInventarioDetalles_Lote");

            entity.HasOne(d => d.Producto).WithMany(p => p.ReservaInventarioDetalles)
                .HasForeignKey(d => d.ProductoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ReservaInventarioDetalles_Producto");

            entity.HasOne(d => d.SerieProducto).WithMany(p => p.ReservaInventarioDetalles)
                .HasForeignKey(d => d.SerieProductoId)
                .HasConstraintName("FK_ReservaInventarioDetalles_Serie");

            entity.HasOne(d => d.Ubicacion).WithMany(p => p.ReservaInventarioDetalles)
                .HasForeignKey(d => d.UbicacionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ReservaInventarioDetalles_Ubicacion");

            entity.HasOne(d => d.ReservasInventario).WithMany(p => p.ReservaInventarioDetalles)
                .HasPrincipalKey(p => new { p.EmpresaId, p.Id })
                .HasForeignKey(d => new { d.EmpresaId, d.ReservaId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ReservaInventarioDetalles_Reserva");
        });

        modelBuilder.Entity<ReservasInventario>(entity =>
        {
            entity.HasIndex(e => new { e.EmpresaId, e.CodigoReserva }, "UQ_ReservasInventario_Empresa_Codigo").IsUnique();

            entity.HasIndex(e => new { e.EmpresaId, e.Id }, "UQ_ReservasInventario_Empresa_Id").IsUnique();

            entity.Property(e => e.CodigoReserva)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.FechaExpiracion).HasPrecision(3);
            entity.Property(e => e.FechaLiberacion).HasPrecision(3);
            entity.Property(e => e.FechaReserva).HasPrecision(3);
            entity.Property(e => e.ModuloOrigen)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Observaciones)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.VersionFila)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.CreadoPorUsuario).WithMany(p => p.ReservasInventarioCreadoPorUsuario)
                .HasForeignKey(d => d.CreadoPorUsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ReservasInventario_CreadoPor");

            entity.HasOne(d => d.Empresa).WithMany(p => p.ReservasInventario)
                .HasForeignKey(d => d.EmpresaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ReservasInventario_Empresa");

            entity.HasOne(d => d.LiberadoPorUsuario).WithMany(p => p.ReservasInventarioLiberadoPorUsuario)
                .HasForeignKey(d => d.LiberadoPorUsuarioId)
                .HasConstraintName("FK_ReservasInventario_LiberadoPor");

            entity.HasOne(d => d.Sucursal).WithMany(p => p.ReservasInventario)
                .HasForeignKey(d => d.SucursalId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ReservasInventario_Sucursal");

            entity.HasOne(d => d.EstadosFlujo).WithMany(p => p.ReservasInventario)
                .HasPrincipalKey(p => new { p.FlujoEstadoId, p.Id })
                .HasForeignKey(d => new { d.FlujoEstadoId, d.EstadoId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ReservasInventario_Estado");
        });

        modelBuilder.Entity<Roles>(entity =>
        {
            entity.HasIndex(e => e.Nombre, "UQ_Roles_Nombre").IsUnique();

            entity.Property(e => e.Id)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Nombre)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.NivelDeAcceso).WithMany(p => p.Roles)
                .HasForeignKey(d => d.NivelDeAccesoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Roles_NivelDeAccesoId");

            entity.HasOne(d => d.Tipo).WithMany(p => p.Roles)
                .HasForeignKey(d => d.TipoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Roles_TipoId");
        });

        modelBuilder.Entity<RolesPermisos>(entity =>
        {
            entity.HasIndex(e => new { e.RoleId, e.PermisoId }, "UQ_RolesPermisos_Role_Permiso").IsUnique();

            entity.Property(e => e.RoleId)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.Permiso).WithMany(p => p.RolesPermisos)
                .HasForeignKey(d => d.PermisoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RolesPermisos_Permiso");

            entity.HasOne(d => d.Role).WithMany(p => p.RolesPermisos)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RolesPermisos_Role");
        });

        modelBuilder.Entity<SeriesDocumentos>(entity =>
        {
            entity.HasIndex(e => new { e.EmpresaId, e.SucursalId, e.TipoDocumentoId, e.Serie }, "UQ_SeriesDocumentos").IsUnique();

            entity.Property(e => e.Prefijo)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Serie)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.Empresa).WithMany(p => p.SeriesDocumentos)
                .HasForeignKey(d => d.EmpresaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SeriesDocumentos_Empresa");

            entity.HasOne(d => d.Estado).WithMany(p => p.SeriesDocumentos)
                .HasForeignKey(d => d.EstadoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SeriesDocumentos_Estado");

            entity.HasOne(d => d.Sucursal).WithMany(p => p.SeriesDocumentos)
                .HasForeignKey(d => d.SucursalId)
                .HasConstraintName("FK_SeriesDocumentos_Sucursal");

            entity.HasOne(d => d.TipoDocumento).WithMany(p => p.SeriesDocumentos)
                .HasForeignKey(d => d.TipoDocumentoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SeriesDocumentos_TipoDocumento");
        });

        modelBuilder.Entity<SeriesProductos>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_SeriesProducto");

            entity.HasIndex(e => new { e.EmpresaId, e.Id }, "UQ_Series_Empresa_Id").IsUnique();

            entity.HasIndex(e => new { e.EmpresaId, e.ProductoId, e.Id }, "UQ_Series_Producto_Id").IsUnique();

            entity.HasIndex(e => new { e.EmpresaId, e.ProductoId, e.NumeroSerie }, "UQ_Series_Producto_Numero").IsUnique();

            entity.Property(e => e.FechaCreacion).HasPrecision(3);
            entity.Property(e => e.FechaIngreso).HasPrecision(3);
            entity.Property(e => e.FechaSalida).HasPrecision(3);
            entity.Property(e => e.NumeroSerie)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.VersionFila)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.CreadoPorUsuario).WithMany(p => p.SeriesProductos)
                .HasForeignKey(d => d.CreadoPorUsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SeriesProductos_CreadoPor");

            entity.HasOne(d => d.Empresa).WithMany(p => p.SeriesProductos)
                .HasForeignKey(d => d.EmpresaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SeriesProductos_Empresa");

            entity.HasOne(d => d.Bodegas).WithMany(p => p.SeriesProductos)
                .HasPrincipalKey(p => new { p.EmpresaId, p.Id })
                .HasForeignKey(d => new { d.EmpresaId, d.BodegaId })
                .HasConstraintName("FK_Series_Bodega");

            entity.HasOne(d => d.Productos).WithMany(p => p.SeriesProductos)
                .HasPrincipalKey(p => new { p.EmpresaId, p.Id })
                .HasForeignKey(d => new { d.EmpresaId, d.ProductoId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Series_Producto");

            entity.HasOne(d => d.Ubicaciones).WithMany(p => p.SeriesProductos)
                .HasPrincipalKey(p => new { p.EmpresaId, p.Id })
                .HasForeignKey(d => new { d.EmpresaId, d.UbicacionId })
                .HasConstraintName("FK_Series_Ubicacion");

            entity.HasOne(d => d.EstadosFlujo).WithMany(p => p.SeriesProductos)
                .HasPrincipalKey(p => new { p.FlujoEstadoId, p.Id })
                .HasForeignKey(d => new { d.FlujoEstadoId, d.EstadoId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Series_Estado");

            entity.HasOne(d => d.Lotes).WithMany(p => p.SeriesProductos)
                .HasPrincipalKey(p => new { p.EmpresaId, p.ProductoId, p.Id })
                .HasForeignKey(d => new { d.EmpresaId, d.ProductoId, d.LoteId })
                .HasConstraintName("FK_Series_Lote");
        });

        modelBuilder.Entity<SesionesUsuarios>(entity =>
        {
            entity.Property(e => e.DireccionIp)
                .HasMaxLength(45)
                .IsUnicode(false);
            entity.Property(e => e.Dispositivo)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.FechaFin).HasPrecision(3);
            entity.Property(e => e.FechaInicio).HasPrecision(3);
            entity.Property(e => e.TokenIdentificador)
                .HasMaxLength(200)
                .IsUnicode(false);

            entity.HasOne(d => d.Usuario).WithMany(p => p.SesionesUsuarios)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SesionesUsuarios_Usuario");
        });

        modelBuilder.Entity<SolicitudesAutorizacion>(entity =>
        {
            entity.Property(e => e.ComentarioResolucion)
                .HasMaxLength(500)
                .IsUnicode(false);
            entity.Property(e => e.Entidad)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.FechaResolucion).HasPrecision(3);
            entity.Property(e => e.FechaSolicitud).HasPrecision(3);
            entity.Property(e => e.Motivo)
                .HasMaxLength(500)
                .IsUnicode(false);

            entity.HasOne(d => d.AutorizadoPorUsuario).WithMany(p => p.SolicitudesAutorizacionAutorizadoPorUsuario)
                .HasForeignKey(d => d.AutorizadoPorUsuarioId)
                .HasConstraintName("FK_SolicitudesAutorizacion_AutorizadoPor");

            entity.HasOne(d => d.ReglaAutorizacion).WithMany(p => p.SolicitudesAutorizacion)
                .HasForeignKey(d => d.ReglaAutorizacionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SolicitudesAutorizacion_Regla");

            entity.HasOne(d => d.SolicitadoPorUsuario).WithMany(p => p.SolicitudesAutorizacionSolicitadoPorUsuario)
                .HasForeignKey(d => d.SolicitadoPorUsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SolicitudesAutorizacion_SolicitadoPor");

            entity.HasOne(d => d.EstadosFlujo).WithMany(p => p.SolicitudesAutorizacion)
                .HasPrincipalKey(p => new { p.FlujoEstadoId, p.Id })
                .HasForeignKey(d => new { d.FlujoEstadoId, d.EstadoId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SolicitudesAutorizacion_Estado");
        });

        modelBuilder.Entity<Sucursales>(entity =>
        {
            entity.Property(e => e.Codigo)
                .HasMaxLength(25)
                .IsUnicode(false);
            entity.Property(e => e.CodigoNombre)
                .HasMaxLength(175)
                .IsUnicode(false);
            entity.Property(e => e.Direccion)
                .HasMaxLength(250)
                .IsUnicode(false);
            entity.Property(e => e.DireccionCorta)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Imagen)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ImagenReportes)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Nombre)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.RepresentanteLegal)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Telefono)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.HasOne(d => d.Cliente).WithMany(p => p.Sucursales)
                .HasForeignKey(d => d.ClienteId)
                .HasConstraintName("FK_Sucursales_ClienteId");

            entity.HasOne(d => d.Departamento).WithMany(p => p.Sucursales)
                .HasForeignKey(d => d.DepartamentoId)
                .HasConstraintName("FK_Sucursales_DepartamentoId");

            entity.HasOne(d => d.Empresa).WithMany(p => p.Sucursales)
                .HasForeignKey(d => d.EmpresaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Sucursales_EmpresaId");

            entity.HasOne(d => d.Estado).WithMany(p => p.Sucursales)
                .HasForeignKey(d => d.EstadoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Sucursales_Estado");

            entity.HasOne(d => d.Municipio).WithMany(p => p.Sucursales)
                .HasForeignKey(d => d.MunicipioId)
                .HasConstraintName("FK_Sucursales_MunicipioId");

            entity.HasOne(d => d.Proveedor).WithMany(p => p.Sucursales)
                .HasForeignKey(d => d.ProveedorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Sucursales_ProveedorId");

            entity.HasOne(d => d.SucursalCentral).WithMany(p => p.InverseSucursalCentral)
                .HasForeignKey(d => d.SucursalCentralId)
                .HasConstraintName("FK_Sucursales_SucursalCentralId");

            entity.HasOne(d => d.SucursalTransporte).WithMany(p => p.InverseSucursalTransporte)
                .HasForeignKey(d => d.SucursalTransporteId)
                .HasConstraintName("FK_Sucursales_SucursalTransporteId");
        });

        modelBuilder.Entity<TipoNotificaciones>(entity =>
        {
            entity.HasIndex(e => e.TipoId, "UQ_TipoNotificaciones_TipoId").IsUnique();

            entity.Property(e => e.Id)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Descripcion)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.ModuloId)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.Estado).WithMany(p => p.TipoNotificaciones)
                .HasForeignKey(d => d.EstadoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TipoNotificaciones_Estado");

            entity.HasOne(d => d.Modulo).WithMany(p => p.TipoNotificaciones)
                .HasForeignKey(d => d.ModuloId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TipoNotificaciones_ModuloId");

            entity.HasOne(d => d.Tipo).WithOne(p => p.TipoNotificaciones)
                .HasForeignKey<TipoNotificaciones>(d => d.TipoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TipoNotificaciones_TipoId");
        });

        modelBuilder.Entity<Tipos>(entity =>
        {
            entity.HasIndex(e => new { e.ClaseTipoId, e.Nombre }, "UQ_Tipos_Nombre_ClaseTipoId").IsUnique();

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.ModuloId)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .IsUnicode(false);

            entity.HasOne(d => d.ClaseTipo).WithMany(p => p.Tipos)
                .HasForeignKey(d => d.ClaseTipoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Tipos_ClaseTipoId");

            entity.HasOne(d => d.Estado).WithMany(p => p.Tipos)
                .HasForeignKey(d => d.EstadoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Tipos_Estado");

            entity.HasOne(d => d.Modulo).WithMany(p => p.Tipos)
                .HasForeignKey(d => d.ModuloId)
                .HasConstraintName("FK_Tipos_ModuloId");
        });

        modelBuilder.Entity<TiposCambio>(entity =>
        {
            entity.Property(e => e.Fecha).HasPrecision(3);
            entity.Property(e => e.TasaCompra).HasColumnType("decimal(19, 8)");
            entity.Property(e => e.TasaVenta).HasColumnType("decimal(19, 8)");

            entity.HasOne(d => d.Empresa).WithMany(p => p.TiposCambio)
                .HasForeignKey(d => d.EmpresaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TiposCambio_Empresa");

            entity.HasOne(d => d.MonedaDestino).WithMany(p => p.TiposCambioMonedaDestino)
                .HasForeignKey(d => d.MonedaDestinoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TiposCambio_MonedaDestino");

            entity.HasOne(d => d.MonedaOrigen).WithMany(p => p.TiposCambioMonedaOrigen)
                .HasForeignKey(d => d.MonedaOrigenId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TiposCambio_MonedaOrigen");

            entity.HasOne(d => d.Usuario).WithMany(p => p.TiposCambio)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TiposCambio_Usuario");
        });

        modelBuilder.Entity<TiposProductos>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_TiposProducto");

            entity.HasIndex(e => new { e.EmpresaId, e.Codigo }, "UQ_TiposProducto_Empresa_Codigo").IsUnique();

            entity.HasIndex(e => new { e.EmpresaId, e.Id }, "UQ_TiposProducto_Empresa_Id").IsUnique();

            entity.HasIndex(e => new { e.EmpresaId, e.Nombre }, "UQ_TiposProducto_Empresa_Nombre").IsUnique();

            entity.Property(e => e.Codigo)
                .HasMaxLength(25)
                .IsUnicode(false);
            entity.Property(e => e.FechaCreacion).HasPrecision(3);
            entity.Property(e => e.FechaModificacion).HasPrecision(3);
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.VersionFila)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.CreadoPorUsuario).WithMany(p => p.TiposProductosCreadoPorUsuario)
                .HasForeignKey(d => d.CreadoPorUsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TiposProducto_CreadoPor");

            entity.HasOne(d => d.Empresa).WithMany(p => p.TiposProductos)
                .HasForeignKey(d => d.EmpresaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TiposProducto_Empresa");

            entity.HasOne(d => d.Estado).WithMany(p => p.TiposProductos)
                .HasForeignKey(d => d.EstadoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TiposProductos_Estado");

            entity.HasOne(d => d.ModificadoPorUsuario).WithMany(p => p.TiposProductosModificadoPorUsuario)
                .HasForeignKey(d => d.ModificadoPorUsuarioId)
                .HasConstraintName("FK_TiposProducto_ModificadoPor");
        });

        modelBuilder.Entity<TrasladoInventarioDetalles>(entity =>
        {
            entity.Property(e => e.Cantidad).HasColumnType("decimal(19, 6)");

            entity.HasOne(d => d.Lote).WithMany(p => p.TrasladoInventarioDetalles)
                .HasForeignKey(d => d.LoteId)
                .HasConstraintName("FK_TrasladoInventarioDetalles_Lote");

            entity.HasOne(d => d.Producto).WithMany(p => p.TrasladoInventarioDetalles)
                .HasForeignKey(d => d.ProductoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TrasladoInventarioDetalles_Producto");

            entity.HasOne(d => d.Traslado).WithMany(p => p.TrasladoInventarioDetalles)
                .HasForeignKey(d => d.TrasladoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TrasladoInventarioDetalles_Traslado");

            entity.HasOne(d => d.UnidadMedida).WithMany(p => p.TrasladoInventarioDetalles)
                .HasForeignKey(d => d.UnidadMedidaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TrasladoInventarioDetalles_Unidad");
        });

        modelBuilder.Entity<TrasladosInventario>(entity =>
        {
            entity.HasIndex(e => new { e.EmpresaId, e.NumeroTraslado }, "UQ_TrasladosInventario_Numero").IsUnique();

            entity.Property(e => e.FechaTraslado).HasPrecision(3);
            entity.Property(e => e.NumeroTraslado)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.Observaciones)
                .HasMaxLength(500)
                .IsUnicode(false);

            entity.HasOne(d => d.BodegaDestino).WithMany(p => p.TrasladosInventarioBodegaDestino)
                .HasForeignKey(d => d.BodegaDestinoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TrasladosInventario_BodegaDestino");

            entity.HasOne(d => d.BodegaOrigen).WithMany(p => p.TrasladosInventarioBodegaOrigen)
                .HasForeignKey(d => d.BodegaOrigenId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TrasladosInventario_BodegaOrigen");

            entity.HasOne(d => d.CreadoPorUsuario).WithMany(p => p.TrasladosInventario)
                .HasForeignKey(d => d.CreadoPorUsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TrasladosInventario_CreadoPor");

            entity.HasOne(d => d.Empresa).WithMany(p => p.TrasladosInventario)
                .HasForeignKey(d => d.EmpresaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TrasladosInventario_Empresa");

            entity.HasOne(d => d.SucursalDestino).WithMany(p => p.TrasladosInventarioSucursalDestino)
                .HasForeignKey(d => d.SucursalDestinoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TrasladosInventario_SucursalDestino");

            entity.HasOne(d => d.SucursalOrigen).WithMany(p => p.TrasladosInventarioSucursalOrigen)
                .HasForeignKey(d => d.SucursalOrigenId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TrasladosInventario_SucursalOrigen");

            entity.HasOne(d => d.EstadosFlujo).WithMany(p => p.TrasladosInventario)
                .HasPrincipalKey(p => new { p.FlujoEstadoId, p.Id })
                .HasForeignKey(d => new { d.FlujoEstadoId, d.EstadoId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TrasladosInventario_Estado");
        });

        modelBuilder.Entity<TurnosCaja>(entity =>
        {
            entity.Property(e => e.Diferencia)
                .HasComputedColumnSql("(case when [MontoEsperado] IS NULL OR [MontoFisico] IS NULL then NULL else [MontoFisico]-[MontoEsperado] end)", true)
                .HasColumnType("decimal(13, 2)");
            entity.Property(e => e.FechaApertura).HasPrecision(3);
            entity.Property(e => e.FechaCierre).HasPrecision(3);
            entity.Property(e => e.MontoEsperado).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.MontoFisico).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.MontoInicial).HasColumnType("decimal(12, 2)");

            entity.HasOne(d => d.Caja).WithMany(p => p.TurnosCaja)
                .HasForeignKey(d => d.CajaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TurnosCaja_Caja");

            entity.HasOne(d => d.Usuario).WithMany(p => p.TurnosCaja)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TurnosCaja_Usuario");

            entity.HasOne(d => d.EstadosFlujo).WithMany(p => p.TurnosCaja)
                .HasPrincipalKey(p => new { p.FlujoEstadoId, p.Id })
                .HasForeignKey(d => new { d.FlujoEstadoId, d.EstadoId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TurnosCaja_Estado");
        });

        modelBuilder.Entity<Ubicaciones>(entity =>
        {
            entity.HasIndex(e => new { e.EmpresaId, e.BodegaId, e.Codigo }, "UQ_Ubicaciones_Empresa_Bodega_Codigo").IsUnique();

            entity.HasIndex(e => new { e.EmpresaId, e.BodegaId, e.Id }, "UQ_Ubicaciones_Empresa_Bodega_Id").IsUnique();

            entity.HasIndex(e => new { e.EmpresaId, e.Id }, "UQ_Ubicaciones_Empresa_Id").IsUnique();

            entity.Property(e => e.Codigo)
                .HasMaxLength(40)
                .IsUnicode(false);
            entity.Property(e => e.FechaCreacion).HasPrecision(3);
            entity.Property(e => e.FechaModificacion).HasPrecision(3);
            entity.Property(e => e.Nombre)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.VersionFila)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.CreadoPorUsuario).WithMany(p => p.UbicacionesCreadoPorUsuario)
                .HasForeignKey(d => d.CreadoPorUsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Ubicaciones_CreadoPor");

            entity.HasOne(d => d.Empresa).WithMany(p => p.Ubicaciones)
                .HasForeignKey(d => d.EmpresaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Ubicaciones_Empresa");

            entity.HasOne(d => d.Estado).WithMany(p => p.Ubicaciones)
                .HasForeignKey(d => d.EstadoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Ubicaciones_Estado");

            entity.HasOne(d => d.ModificadoPorUsuario).WithMany(p => p.UbicacionesModificadoPorUsuario)
                .HasForeignKey(d => d.ModificadoPorUsuarioId)
                .HasConstraintName("FK_Ubicaciones_ModificadoPor");

            entity.HasOne(d => d.TipoUbicacion).WithMany(p => p.Ubicaciones)
                .HasForeignKey(d => d.TipoUbicacionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Ubicaciones_Tipo");

            entity.HasOne(d => d.Bodegas).WithMany(p => p.Ubicaciones)
                .HasPrincipalKey(p => new { p.EmpresaId, p.Id })
                .HasForeignKey(d => new { d.EmpresaId, d.BodegaId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Ubicaciones_Bodega");

            entity.HasOne(d => d.UbicacionesNavigation).WithMany(p => p.InverseUbicacionesNavigation)
                .HasPrincipalKey(p => new { p.EmpresaId, p.Id })
                .HasForeignKey(d => new { d.EmpresaId, d.UbicacionPadreId })
                .HasConstraintName("FK_Ubicaciones_Padre");
        });

        modelBuilder.Entity<UnidadesMedida>(entity =>
        {
            entity.HasIndex(e => new { e.EmpresaId, e.Codigo }, "UQ_Unidades_Empresa_Codigo").IsUnique();

            entity.HasIndex(e => new { e.EmpresaId, e.Id }, "UQ_Unidades_Empresa_Id").IsUnique();

            entity.HasIndex(e => new { e.EmpresaId, e.Nombre }, "UQ_Unidades_Empresa_Nombre").IsUnique();

            entity.Property(e => e.Abreviatura)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Codigo)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.FechaCreacion).HasPrecision(3);
            entity.Property(e => e.FechaModificacion).HasPrecision(3);
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.VersionFila)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.CreadoPorUsuario).WithMany(p => p.UnidadesMedidaCreadoPorUsuario)
                .HasForeignKey(d => d.CreadoPorUsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UnidadesMedida_CreadoPor");

            entity.HasOne(d => d.Empresa).WithMany(p => p.UnidadesMedida)
                .HasForeignKey(d => d.EmpresaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UnidadesMedida_Empresa");

            entity.HasOne(d => d.Estado).WithMany(p => p.UnidadesMedida)
                .HasForeignKey(d => d.EstadoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UnidadesMedida_Estado");

            entity.HasOne(d => d.ModificadoPorUsuario).WithMany(p => p.UnidadesMedidaModificadoPorUsuario)
                .HasForeignKey(d => d.ModificadoPorUsuarioId)
                .HasConstraintName("FK_UnidadesMedida_ModificadoPor");
        });

        modelBuilder.Entity<Usuarios>(entity =>
        {
            entity.HasIndex(e => e.Email, "UQ_Usuarios_Email").IsUnique();

            entity.HasIndex(e => e.IdentityId, "UQ_Usuarios_IdentityId").IsUnique();

            entity.HasIndex(e => e.UserName, "UQ_Usuarios_UserName").IsUnique();

            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.IdentityId).HasMaxLength(128);
            entity.Property(e => e.Imagen)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PasswordTemporal)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.RoleId)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.Soundex)
                .HasMaxLength(300)
                .IsUnicode(false);
            entity.Property(e => e.UserName)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.Estado).WithMany(p => p.Usuarios)
                .HasForeignKey(d => d.EstadoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Usuarios_Estado");

            entity.HasOne(d => d.Role).WithMany(p => p.Usuarios)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Usuarios_RoleId");

            entity.HasOne(d => d.TipoInicioSesion).WithMany(p => p.Usuarios)
                .HasForeignKey(d => d.TipoInicioSesionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Usuarios_TipoInicioSesionId");
        });

        modelBuilder.Entity<UsuariosSucursales>(entity =>
        {
            entity.HasIndex(e => new { e.UsuarioId, e.SucursalId }, "UQ_UsuariosSucursales").IsUnique();

            entity.HasOne(d => d.Estado).WithMany(p => p.UsuariosSucursales)
                .HasForeignKey(d => d.EstadoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UsuariosSucursales_Estado");

            entity.HasOne(d => d.Sucursal).WithMany(p => p.UsuariosSucursales)
                .HasForeignKey(d => d.SucursalId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UsuariosSucursales_Sucursal");

            entity.HasOne(d => d.Usuario).WithMany(p => p.UsuariosSucursales)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UsuariosSucursales_Usuario");
        });

        modelBuilder.Entity<VentaImpuestos>(entity =>
        {
            entity.HasIndex(e => new { e.VentaId, e.ImpuestoId }, "UQ_VentaImpuestos").IsUnique();

            entity.Property(e => e.BaseImponible).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.Monto).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.Porcentaje).HasColumnType("decimal(9, 6)");

            entity.HasOne(d => d.Impuesto).WithMany(p => p.VentaImpuestos)
                .HasForeignKey(d => d.ImpuestoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_VentaImpuestos_Impuesto");

            entity.HasOne(d => d.Venta).WithMany(p => p.VentaImpuestos)
                .HasForeignKey(d => d.VentaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_VentaImpuestos_Venta");
        });

        modelBuilder.Entity<Ventas>(entity =>
        {
            entity.HasIndex(e => new { e.EmpresaId, e.NumeroFactura }, "UQ_VentasFactura").IsUnique();

            entity.Property(e => e.Descuento).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.FechaVenta).HasPrecision(3);
            entity.Property(e => e.Impuestos).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.NumeroFactura)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.SubTotal).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.TipoCambio).HasColumnType("decimal(19, 8)");
            entity.Property(e => e.TotalVenta).HasColumnType("decimal(12, 2)");

            entity.HasOne(d => d.Cliente).WithMany(p => p.Ventas)
                .HasForeignKey(d => d.ClienteId)
                .HasConstraintName("FK_Ventas_Cliente");

            entity.HasOne(d => d.Empresa).WithMany(p => p.Ventas)
                .HasForeignKey(d => d.EmpresaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Ventas_Empresa");

            entity.HasOne(d => d.Moneda).WithMany(p => p.Ventas)
                .HasForeignKey(d => d.MonedaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Ventas_Moneda");

            entity.HasOne(d => d.PedidoVenta).WithMany(p => p.Ventas)
                .HasForeignKey(d => d.PedidoVentaId)
                .HasConstraintName("FK_Ventas_Pedido");

            entity.HasOne(d => d.Sucursal).WithMany(p => p.Ventas)
                .HasForeignKey(d => d.SucursalId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Ventas_Sucursal");

            entity.HasOne(d => d.TurnoCaja).WithMany(p => p.Ventas)
                .HasForeignKey(d => d.TurnoCajaId)
                .HasConstraintName("FK_Ventas_TurnoCaja");

            entity.HasOne(d => d.UsuarioCreacion).WithMany(p => p.Ventas)
                .HasForeignKey(d => d.UsuarioCreacionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Ventas_Usuario");

            entity.HasOne(d => d.EstadosFlujo).WithMany(p => p.Ventas)
                .HasPrincipalKey(p => new { p.FlujoEstadoId, p.Id })
                .HasForeignKey(d => new { d.FlujoEstadoId, d.EstadoId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Ventas_Estado");
        });

        modelBuilder.Entity<VentasDetalle>(entity =>
        {
            entity.HasIndex(e => new { e.VentaId, e.NumeroLinea }, "UQ_VentasDetalle_Linea").IsUnique();

            entity.Property(e => e.Cantidad).HasColumnType("decimal(19, 6)");
            entity.Property(e => e.CostoUnitario).HasColumnType("decimal(19, 6)");
            entity.Property(e => e.Impuesto).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.MontoDescuento).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.PorcentajeDescuento).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.PrecioUnitario).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.SubTotal)
                .HasComputedColumnSql("(([Cantidad]*[PrecioUnitario]-[MontoDescuento])+[Impuesto])", true)
                .HasColumnType("decimal(34, 8)");

            entity.HasOne(d => d.Producto).WithMany(p => p.VentasDetalle)
                .HasForeignKey(d => d.ProductoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_VentasDetalle_Producto");

            entity.HasOne(d => d.ProductoPresentacion).WithMany(p => p.VentasDetalle)
                .HasForeignKey(d => d.ProductoPresentacionId)
                .HasConstraintName("FK_VentasDetalle_Presentacion");

            entity.HasOne(d => d.Venta).WithMany(p => p.VentasDetalle)
                .HasForeignKey(d => d.VentaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_VentasDetalle_Venta");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
