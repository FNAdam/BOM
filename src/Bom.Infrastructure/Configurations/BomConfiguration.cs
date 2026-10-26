using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BomEntity = Bom.Domain.Entities.Bom;

namespace Bom.Infrastructure.Configurations;

public class BomConfiguration : IEntityTypeConfiguration<BomEntity>
{
    public void Configure(EntityTypeBuilder<BomEntity> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Version)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.OutputQuantity)
            .HasPrecision(18, 6);

        builder.Property(x => x.YieldPercent)
            .HasPrecision(18, 6)
            .HasDefaultValue(1m);

        builder.Property(x => x.Notes)
            .HasMaxLength(500);

        // Satu versi Version unik per produk.
        builder.HasIndex(x => new { x.ProductItemId, x.Version }).IsUnique();

        // Partial unique index: hanya boleh ada satu BOM Active per produk.
        // Kolom snake_case = "status", nilai enum disimpan sebagai teks ('Active').
        builder.HasIndex(x => x.ProductItemId)
            .IsUnique()
            .HasFilter("status = 'Active'");

        // Produk keluaran; restrict agar item tidak terhapus saat masih dipakai BOM.
        builder.HasOne(x => x.ProductItem)
            .WithMany()
            .HasForeignKey(x => x.ProductItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.OutputUom)
            .WithMany()
            .HasForeignKey(x => x.OutputUomId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
