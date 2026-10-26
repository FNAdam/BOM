using Bom.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bom.Infrastructure.Configurations;

public class ItemConfiguration : IEntityTypeConfiguration<Item>
{
    public void Configure(EntityTypeBuilder<Item> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(x => x.Type)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.StandardMaterialCost)
            .HasPrecision(18, 4)
            .HasDefaultValue(0m);

        builder.Property(x => x.CostingMethod)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.Density)
            .HasPrecision(18, 6);

        builder.Property(x => x.IsActive)
            .HasDefaultValue(true);

        builder.HasIndex(x => x.Code).IsUnique();

        // Base UOM wajib; jangan cascade dari Uom ke Item.
        builder.HasOne(x => x.BaseUom)
            .WithMany()
            .HasForeignKey(x => x.BaseUomId)
            .OnDelete(DeleteBehavior.Restrict);

        // Kategori opsional; restrict agar kategori tidak terhapus saat masih dipakai.
        builder.HasOne(x => x.Category)
            .WithMany(c => c.Items)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
