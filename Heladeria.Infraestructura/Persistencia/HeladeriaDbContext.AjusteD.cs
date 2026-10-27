using Heladeria.Infraestructura.Persistencia.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Heladeria.Infraestructura.Persistencia;

public partial class HeladeriaDbContext
{
    private static void ConfigurarAjusteD(ModelBuilder modelo)
    {
        modelo.Entity<CombosDetalle>(e =>
        {
            e.Property(x => x.Cantidad).HasColumnType("decimal(19,6)").HasDefaultValue(1m);
            e.HasOne(x => x.ProductoPresentacion).WithMany()
                .HasForeignKey(x => new { x.EmpresaId, x.ProductoIncluidoId, x.ProductoPresentacionId })
                .HasPrincipalKey(x => new { x.EmpresaId, x.ProductoId, x.Id }).OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_CombosDetalle_Presentacion");
            e.HasIndex(x => new { x.EmpresaId, x.ComboProductoId, x.ProductoIncluidoId, x.ProductoPresentacionId },
                "UQ_CombosDetalle_Componente").IsUnique().HasFilter(null);
        });
        modelo.Entity<MovimientoDetalles>(e =>
        {
            e.HasOne(x => x.VentaDetalle).WithMany().HasForeignKey(x => x.VentaDetalleId)
                .OnDelete(DeleteBehavior.NoAction).HasConstraintName("FK_MovimientoDetalles_VentaDetalle");
            e.HasOne<UnidadesMedida>().WithMany().HasForeignKey(x => new { x.EmpresaId, x.UnidadBaseId })
                .HasPrincipalKey(x => new { x.EmpresaId, x.Id }).OnDelete(DeleteBehavior.NoAction)
                .HasConstraintName("FK_MovimientoDetalles_UnidadBase");
            e.HasIndex(x => x.VentaDetalleId, "IX_MovimientoDetalles_VentaDetalle");
        });
    }
}
