using Bom.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bom.Infrastructure.Configurations;

public class ItemSupplierPriceConfiguration : IEntityTypeConfiguration<ItemSupplierPrice>
{
    public void Configure(EntityTypeBuilder<ItemSupplierPrice> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Price)
            .HasPrecision(18, 4);

        builder.Property(x => x.Currency)
            .IsRequired()
            .HasMaxLength(3)
            .HasDefaultValue("IDR");

        builder.Property(x => x.MinOrderQty)
            .HasPrecision(18, 4)
            .HasDefaultValue(0m);

        // DateOnly? -> kolom date di PostgreSQL (didukung Npgsql).

        // Menghapus Item akan menghapus daftar harga terkait (cascade).
        builder.HasOne(x => x.Item)
            .WithMany(i => i.SupplierPrices)
            .HasForeignKey(x => x.ItemId)
            .OnDelete(DeleteBehavior.Cascade);

        // Jangan cascade dari Supplier ke harga.
        builder.HasOne(x => x.Supplier)
            .WithMany(s => s.Prices)
            .HasForeignKey(x => x.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Uom)
            .WithMany()
            .HasForeignKey(x => x.UomId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
