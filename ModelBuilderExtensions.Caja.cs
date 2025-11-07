// Data/ModelBuilderExtensions.Caja.cs
using LaOriginalBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace LaOriginalBackend.Data
{
    public static class ModelBuilderExtensionsCaja
    {
        public static void ConfigureCaja(this ModelBuilder mb)
        {
            mb.Entity<CajaApertura>(e =>
            {
                e.ToTable("CajaAperturas");
                e.HasKey(x => x.Id);
                e.Property(x => x.Codigo).HasMaxLength(20);
                e.Property(x => x.CajeroNombre).HasMaxLength(120);
                e.Property(x => x.ObservacionesApertura).HasMaxLength(200);
                e.Property(x => x.ObservacionesCierre).HasMaxLength(200);
                e.Property(x => x.MontoInicial).HasColumnType("decimal(18,2)");
                e.Property(x => x.MontoCierreDeclarado).HasColumnType("decimal(18,2)");
            });

            mb.Entity<CajaMovimiento>(e =>
            {
                e.ToTable("CajaMovimientos");
                e.HasKey(x => x.Id);
                e.Property(x => x.Monto).HasColumnType("decimal(18,2)");
                e.Property(x => x.Concepto).HasMaxLength(120);
                e.Property(x => x.Observaciones).HasMaxLength(200);
                e.Property(x => x.Documento).HasMaxLength(40);

                e.HasOne(x => x.CajaApertura)
                    .WithMany(a => a.Movimientos)
                    .HasForeignKey(x => x.CajaAperturaId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
