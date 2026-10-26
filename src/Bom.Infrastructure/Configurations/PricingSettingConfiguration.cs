using Bom.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bom.Infrastructure.Configurations;

public class PricingSettingConfiguration : IEntityTypeConfiguration<PricingSetting>
{
    public void Configure(EntityTypeBuilder<PricingSetting> builder)
    {
        builder.HasKey(x => x.Id);

        // Singleton: Id tidak di-generate database (selalu 1).
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.DefaultTargetMarginPercent).HasPrecision(18, 6);
        builder.Property(x => x.DefaultTaxPercent).HasPrecision(18, 6);
        builder.Property(x => x.MinMarginPercent).HasPrecision(18, 6);

        builder.Property(x => x.RoundingRule)
            .HasConversion<string>()
            .HasMaxLength(20);
    }
}
