using Bom.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bom.Infrastructure.Configurations;

public class PriceListLineConfiguration : IEntityTypeConfiguration<PriceListLine>
{
    public void Configure(EntityTypeBuilder<PriceListLine> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TierName)
            .IsRequired()
            .HasMaxLength(40);

        builder.Property(x => x.MarginPercent).HasPrecision(18, 6);
        builder.Property(x => x.NetPrice).HasPrecision(18, 4);
        builder.Property(x => x.TaxAmount).HasPrecision(18, 4);
        builder.Property(x => x.GrossPrice).HasPrecision(18, 4);
        builder.Property(x => x.ContributionMargin).HasPrecision(18, 4);

        builder.HasIndex(x => x.PriceListId);

        // Kanal opsional (null untuk Ex-factory / HET); restrict agar kanal tak terhapus saat dipakai.
        builder.HasOne(x => x.SalesChannel)
            .WithMany()
            .HasForeignKey(x => x.SalesChannelId)
            .OnDelete(DeleteBehavior.Restrict);

        // Kemasan; restrict agar kemasan tak terhapus saat masih direferensikan baris harga.
        builder.HasOne(x => x.PackLevel)
            .WithMany()
            .HasForeignKey(x => x.PackLevelId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
