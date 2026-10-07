using Heladeria.Infraestructura.Persistencia.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Heladeria.Infraestructura.Persistencia;

public partial class HeladeriaDbContext
{
    private static void ConfigurarFase8(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TurnosCaja>(e => {
            e.Property(x => x.VersionFila).IsRowVersion();
            e.Property(x => x.FechaModificacion).HasPrecision(3);
            e.Property(x => x.NumeroApertura).HasMaxLength(30).IsUnicode(false);
            e.Property(x => x.NumeroCierre).HasMaxLength(30).IsUnicode(false);
            e.Property(x => x.TotalIngresos).HasColumnType("decimal(12,2)");
            e.Property(x => x.TotalEgresos).HasColumnType("decimal(12,2)");
            e.HasOne(x => x.Moneda).WithMany().HasForeignKey(x => x.MonedaId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne<Usuarios>().WithMany().HasForeignKey(x => x.UsuarioCierreId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne<ArqueosCaja>().WithMany().HasForeignKey(x => x.ArqueoCierreId).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => x.CajaId, "UX_TurnosCaja_CajaAbierta").IsUnique().HasFilter("[FechaCierre] IS NULL");
        });
        modelBuilder.Entity<Pagos>(e => {
            e.HasOne(x => x.Venta).WithMany().HasForeignKey(x => x.VentaId).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => x.VentaId, "UX_Pagos_Venta").IsUnique().HasFilter("[VentaId] IS NOT NULL");
            e.HasIndex(x => new { x.EmpresaId, x.ClaveOperacion }, "UX_Pagos_ClaveOperacion").IsUnique().HasFilter("[ClaveOperacion] IS NOT NULL");
        });
        modelBuilder.Entity<PagoDetalles>(e => e.Property(x => x.MontoRecibido).HasColumnType("decimal(12,2)"));
        modelBuilder.Entity<MovimientosCaja>(e => {
            e.Property(x => x.CodigoConcepto).HasMaxLength(30).IsUnicode(false);
            e.Property(x => x.NumeroDocumento).HasMaxLength(30).IsUnicode(false);
            e.HasOne(x => x.PagoDetalle).WithMany().HasForeignKey(x => x.PagoDetalleId).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => x.PagoDetalleId, "UX_MovimientosCaja_PagoDetalle").IsUnique().HasFilter("[PagoDetalleId] IS NOT NULL");
        });
        modelBuilder.Entity<ArqueosCaja>().Property(x => x.VersionTurno).HasMaxLength(8);
    }
}
