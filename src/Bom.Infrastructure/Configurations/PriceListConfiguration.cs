using Bom.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bom.Infrastructure.Configurations;

public class PriceListConfiguration : IEntityTypeConfiguration<PriceList>
{
    public void Configure(EntityTypeBuilder<PriceList> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.PricingMethod)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.CostBasis)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.TaxMode)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.TargetPercent).HasPrecision(18, 6);
        builder.Property(x => x.TaxPercent).HasPrecision(18, 6);
        builder.Property(x => x.ManualBasePrice).HasPrecision(18, 4);
        builder.Property(x => x.FloorCost).HasPrecision(18, 4);

        builder.Property(x => x.Notes).HasMaxLength(500);

        // Partial unique index: hanya boleh ada satu price list Active per produk.
        // Kolom snake_case = "status", nilai enum disimpan sebagai teks ('Active').
        builder.HasIndex(x => x.ProductItemId)
            .IsUnique()
            .HasFilter("status = 'Active'");

        // Produk yang diberi harga; restrict agar tidak terhapus saat masih dipakai.
        builder.HasOne(x => x.ProductItem)
            .WithMany()
            .HasForeignKey(x => x.ProductItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Lines)
            .WithOne(l => l.PriceList!)
            .HasForeignKey(l => l.PriceListId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
