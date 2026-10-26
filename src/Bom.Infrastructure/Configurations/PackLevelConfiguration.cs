using Bom.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bom.Infrastructure.Configurations;

public class PackLevelConfiguration : IEntityTypeConfiguration<PackLevel>
{
    public void Configure(EntityTypeBuilder<PackLevel> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(40);

        builder.Property(x => x.QtyPerBase)
            .HasPrecision(18, 6);

        builder.Property(x => x.Barcode)
            .HasMaxLength(40);

        // Nama kemasan unik per produk.
        builder.HasIndex(x => new { x.FinishedItemId, x.Name }).IsUnique();

        // Produk pemilik; restrict agar item tidak terhapus saat masih punya kemasan.
        builder.HasOne(x => x.FinishedItem)
            .WithMany()
            .HasForeignKey(x => x.FinishedItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
