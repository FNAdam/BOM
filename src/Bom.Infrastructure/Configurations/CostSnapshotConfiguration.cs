using Bom.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bom.Infrastructure.Configurations;

public class CostSnapshotConfiguration : IEntityTypeConfiguration<CostSnapshot>
{
    public void Configure(EntityTypeBuilder<CostSnapshot> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.MaterialCost).HasPrecision(18, 4);
        builder.Property(x => x.LaborCost).HasPrecision(18, 4);
        builder.Property(x => x.OverheadCost).HasPrecision(18, 4);
        builder.Property(x => x.TotalCost).HasPrecision(18, 4);

        // DateOnly -> kolom date (didukung Npgsql).

        // Riwayat snapshot per item, terurut tanggal.
        builder.HasIndex(x => new { x.ItemId, x.AsOfDate });

        // Menghapus Item akan menghapus riwayat snapshot terkait (cascade).
        builder.HasOne(x => x.Item)
            .WithMany()
            .HasForeignKey(x => x.ItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.OutputUom)
            .WithMany()
            .HasForeignKey(x => x.OutputUomId)
            .OnDelete(DeleteBehavior.Restrict);

        // BOM sumber opsional; jika BOM dihapus, biarkan snapshot dengan referensi null.
        builder.HasOne(x => x.SourceBom)
            .WithMany()
            .HasForeignKey(x => x.SourceBomId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
