using Bom.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bom.Infrastructure.Configurations;

public class SalesOrderConfiguration : IEntityTypeConfiguration<SalesOrder>
{
    public void Configure(EntityTypeBuilder<SalesOrder> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.OrderNo)
            .IsRequired()
            .HasMaxLength(40);

        builder.Property(x => x.CustomerName)
            .HasMaxLength(150);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.TaxPercent)
            .HasPrecision(18, 6)
            .HasDefaultValue(0m);

        builder.Property(x => x.Notes)
            .HasMaxLength(500);

        builder.HasIndex(x => x.OrderNo).IsUnique();

        // Jangan cascade dari kanal; restrict agar kanal tetap terjaga walau masih dipakai pesanan.
        builder.HasOne(x => x.SalesChannel)
            .WithMany()
            .HasForeignKey(x => x.SalesChannelId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
