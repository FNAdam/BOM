using Bom.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bom.Infrastructure.Configurations;

public class UomConversionConfiguration : IEntityTypeConfiguration<UomConversion>
{
    public void Configure(EntityTypeBuilder<UomConversion> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Factor)
            .HasPrecision(18, 6);

        builder.HasOne(x => x.FromUom)
            .WithMany()
            .HasForeignKey(x => x.FromUomId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ToUom)
            .WithMany()
            .HasForeignKey(x => x.ToUomId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.FromUomId, x.ToUomId }).IsUnique();
    }
}
