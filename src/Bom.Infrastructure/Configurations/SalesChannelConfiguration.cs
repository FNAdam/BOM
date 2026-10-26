using Bom.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bom.Infrastructure.Configurations;

public class SalesChannelConfiguration : IEntityTypeConfiguration<SalesChannel>
{
    public void Configure(EntityTypeBuilder<SalesChannel> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(60);

        builder.Property(x => x.DefaultMarginPercent)
            .HasPrecision(18, 6);

        builder.Property(x => x.IsActive)
            .HasDefaultValue(true);

        // Kode kanal unik.
        builder.HasIndex(x => x.Code).IsUnique();
    }
}
