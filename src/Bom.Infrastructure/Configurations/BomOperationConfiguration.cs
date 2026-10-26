using Bom.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bom.Infrastructure.Configurations;

public class BomOperationConfiguration : IEntityTypeConfiguration<BomOperation>
{
    public void Configure(EntityTypeBuilder<BomOperation> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.LaborCostPerOutputUnit)
            .HasPrecision(18, 4)
            .HasDefaultValue(0m);

        builder.Property(x => x.OverheadRate)
            .HasPrecision(18, 4)
            .HasDefaultValue(0m);

        builder.HasIndex(x => new { x.BomId, x.Sequence });

        builder.HasOne(x => x.Bom)
            .WithMany(b => b.Operations)
            .HasForeignKey(x => x.BomId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
