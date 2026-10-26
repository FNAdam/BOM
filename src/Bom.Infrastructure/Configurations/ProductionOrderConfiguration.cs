using Bom.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bom.Infrastructure.Configurations;

public class ProductionOrderConfiguration : IEntityTypeConfiguration<ProductionOrder>
{
    public void Configure(EntityTypeBuilder<ProductionOrder> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.BatchNo)
            .IsRequired()
            .HasMaxLength(40);

        builder.Property(x => x.PlannedQty)
            .HasPrecision(18, 6);

        builder.Property(x => x.ActualQty)
            .HasPrecision(18, 6);

        // Status enum disimpan sebagai teks (konsisten dengan BOM/Item).
        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.Notes)
            .HasMaxLength(500);

        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.ProductItemId);
        builder.HasIndex(x => x.BatchNo);

        // BOM sumber; restrict agar BOM tidak terhapus saat masih dirujuk order.
        builder.HasOne(x => x.Bom)
            .WithMany()
            .HasForeignKey(x => x.BomId)
            .OnDelete(DeleteBehavior.Restrict);

        // Produk keluaran; restrict agar item tetap terjaga.
        builder.HasOne(x => x.ProductItem)
            .WithMany()
            .HasForeignKey(x => x.ProductItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
