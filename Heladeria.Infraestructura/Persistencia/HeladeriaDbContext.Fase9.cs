using Heladeria.Infraestructura.Persistencia.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Heladeria.Infraestructura.Persistencia;

public partial class HeladeriaDbContext
{
    private static void ConfigurarFase9(ModelBuilder modelo)
    {
        modelo.Entity<OrdenesCompra>().Property(x => x.VersionFila).IsRowVersion();
        modelo.Entity<Pagos>().Property(x => x.HuellaSolicitud).HasMaxLength(32);
        modelo.Entity<Pagos>().Property(x => x.Observacion).HasMaxLength(500);
        modelo.Entity<FacturasProveedores>(e => {
            e.Property(x => x.HuellaSolicitud).HasMaxLength(32);
            e.HasIndex(x => new { x.EmpresaId, x.ClaveOperacion }, "UX_FacturasProveedores_ClaveOperacion")
                .IsUnique().HasFilter("[ClaveOperacion] IS NOT NULL");
            e.HasIndex(x => x.OrdenCompraId, "UX_FacturasProveedores_OrdenFase9")
                .IsUnique().HasFilter("[ClaveOperacion] IS NOT NULL AND [OrdenCompraId] IS NOT NULL");
        });
        foreach (var tipo in new[] { typeof(CuentasPorCobrar), typeof(CuentasPorPagar) })
        {
            var e = modelo.Entity(tipo);
            e.Property<byte[]>("VersionFila").IsRowVersion();
            e.Property<DateTime?>("FechaCreacion").HasPrecision(3);
            e.Property<DateTime?>("FechaModificacion").HasPrecision(3);
            e.HasOne(typeof(Sucursales)).WithMany().HasForeignKey("SucursalId").OnDelete(DeleteBehavior.NoAction);
            e.HasOne(typeof(Monedas)).WithMany().HasForeignKey("MonedaId").OnDelete(DeleteBehavior.NoAction);
            e.HasOne(typeof(Usuarios)).WithMany().HasForeignKey("UsuarioCreacionId").OnDelete(DeleteBehavior.NoAction);
            e.HasOne(typeof(Usuarios)).WithMany().HasForeignKey("UsuarioModificacionId").OnDelete(DeleteBehavior.NoAction);
        }
    }
}
