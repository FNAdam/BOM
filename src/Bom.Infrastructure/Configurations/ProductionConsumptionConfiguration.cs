using Bom.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bom.Infrastructure.Configurations;

public class ProductionConsumptionConfiguration : IEntityTypeConfiguration<ProductionConsumption>
{
    public void Configure(EntityTypeBuilder<ProductionConsumption> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.PlannedQty)
            .HasPrecision(18, 6);

        builder.Property(x => x.ActualQty)
            .HasPrecision(18, 6)
            .HasDefaultValue(0m);

        builder.Property(x => x.ActualUnitCost)
            .HasPrecision(18, 4)
            .HasDefaultValue(0m);

        builder.Property(x => x.ActualCost)
            .HasPrecision(18, 4)
            .HasDefaultValue(0m);

        builder.HasIndex(x => x.ProductionOrderId);

        // Menghapus order menghapus baris konsumsinya (cascade).
        builder.HasOne(x => x.ProductionOrder)
            .WithMany(o => o.Consumptions)
            .HasForeignKey(x => x.ProductionOrderId)
            .OnDelete(DeleteBehavior.Cascade);

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
