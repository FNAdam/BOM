using Bom.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bom.Infrastructure.Configurations;

public class BomComponentConfiguration : IEntityTypeConfiguration<BomComponent>
{
    public void Configure(EntityTypeBuilder<BomComponent> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Quantity)
            .HasPrecision(18, 6);

        builder.Property(x => x.ScrapPercent)
            .HasPrecision(18, 6)
            .HasDefaultValue(0m);

        builder.Property(x => x.Notes)
            .HasMaxLength(300);

        builder.HasIndex(x => new { x.BomId, x.Sequence });

        // Menghapus BOM menghapus baris komponennya (cascade).
        builder.HasOne(x => x.Bom)
            .WithMany(b => b.Components)
            .HasForeignKey(x => x.BomId)
            .OnDelete(DeleteBehavior.Cascade);

        // Jangan cascade dari item komponen; restrict agar item tetap terjaga.
        builder.HasOne(x => x.ComponentItem)
            .WithMany()
            .HasForeignKey(x => x.ComponentItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Uom)
            .WithMany()
            .HasForeignKey(x => x.UomId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
