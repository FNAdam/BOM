using Bom.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bom.Infrastructure.Configurations;

public class UomConfiguration : IEntityTypeConfiguration<Uom>
{
    public void Configure(EntityTypeBuilder<Uom> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Type)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.FactorToBase)
            .HasPrecision(18, 6)
            .HasDefaultValue(1m);

        builder.Property(x => x.IsBase)
            .HasDefaultValue(false);

        builder.HasIndex(x => x.Code).IsUnique();
    }
}
