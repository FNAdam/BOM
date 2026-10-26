using Bom.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bom.Infrastructure.Configurations;

public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.HasKey(x => x.Id);

        // Tipe enum disimpan sebagai teks (mis. 'ProductionReceipt').
        builder.Property(x => x.Type)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(x => x.Qty)
            .HasPrecision(18, 6);

        builder.Property(x => x.UnitCost)
            .HasPrecision(18, 4);

        builder.Property(x => x.RefType)
            .HasMaxLength(40);

        builder.Property(x => x.Notes)
            .HasMaxLength(300);

        // Riwayat per item terurut tanggal.
        builder.HasIndex(x => new { x.ItemId, x.MovementDate });

        // Buku besar tidak boleh hilang saat item dihapus; restrict.
        builder.HasOne(x => x.Item)
            .WithMany()
            .HasForeignKey(x => x.ItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
