using Heladeria.Infraestructura.Persistencia.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Heladeria.Infraestructura.Persistencia;

public partial class HeladeriaDbContext
{
    public virtual DbSet<VentaDetalleImpuestos> VentaDetalleImpuestos => Set<VentaDetalleImpuestos>();

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        ConfigurarTiempo(modelBuilder);
        ConfigurarFase8(modelBuilder);
        ConfigurarFase9(modelBuilder);
        ConfigurarAjusteD(modelBuilder);
        modelBuilder.Entity<Ventas>(e =>
        {
            e.Property(x => x.VersionFila).IsRowVersion();
            e.Property(x => x.FechaModificacion).HasPrecision(3);
            e.Property(x => x.FechaCompletada).HasPrecision(3);
            e.Property(x => x.FechaAnulacion).HasPrecision(3);
            e.Property(x => x.MotivoAnulacion).HasMaxLength(500).IsUnicode(false);
            e.HasIndex(x => new { x.EmpresaId, x.ClaveOperacion }, "UX_Ventas_ClaveOperacion")
                .IsUnique().HasFilter("[ClaveOperacion] IS NOT NULL");
            e.HasOne<Bodegas>().WithMany().HasForeignKey(x => x.BodegaId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne<Ubicaciones>().WithMany().HasForeignKey(x => x.UbicacionId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne<Usuarios>().WithMany().HasForeignKey(x => x.ModificadoPorUsuarioId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne<Usuarios>().WithMany().HasForeignKey(x => x.CompletadoPorUsuarioId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne<Usuarios>().WithMany().HasForeignKey(x => x.AnuladoPorUsuarioId).OnDelete(DeleteBehavior.NoAction);
        });
        modelBuilder.Entity<VentasDetalle>(e =>
        {
            foreach (var nombre in new[] { "FactorConversion", "CantidadBase", "CostoTotal" }) e.Property(nombre).HasColumnType("decimal(19,6)");
            foreach (var nombre in new[] { "SubtotalNeto", "DescuentoPrecioCatalogo", "TotalLinea" }) e.Property(nombre).HasColumnType("decimal(12,2)");
            e.Property(x => x.FuentePrecio).HasMaxLength(30).IsUnicode(false);
            e.HasOne(x => x.UnidadMedida).WithMany().HasForeignKey(x => x.UnidadMedidaId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(x => x.UnidadBase).WithMany().HasForeignKey(x => x.UnidadBaseId).OnDelete(DeleteBehavior.NoAction);
        });
        modelBuilder.Entity<VentaDetalleImpuestos>(e =>
        {
            e.ToTable("VentaDetalleImpuestos");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.VentaDetalleId, x.ImpuestoId }, "UQ_VentaDetalleImpuestos").IsUnique();
            e.Property(x => x.Codigo).HasMaxLength(20).IsUnicode(false);
            e.Property(x => x.Nombre).HasMaxLength(100).IsUnicode(false);
            e.Property(x => x.Porcentaje).HasColumnType("decimal(9,6)");
            e.Property(x => x.BaseImponible).HasColumnType("decimal(12,2)");
            e.Property(x => x.Monto).HasColumnType("decimal(12,2)");
            e.HasOne(x => x.VentaDetalle).WithMany(x => x.ImpuestosSnapshot).HasForeignKey(x => x.VentaDetalleId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne<Impuestos>().WithMany().HasForeignKey(x => x.ImpuestoId).OnDelete(DeleteBehavior.NoAction);
        });
    }
}
