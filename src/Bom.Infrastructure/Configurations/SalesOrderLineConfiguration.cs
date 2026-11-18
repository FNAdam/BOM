using Bom.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bom.Infrastructure.Configurations;

public class SalesOrderLineConfiguration : IEntityTypeConfiguration<SalesOrderLine>
{
    public void Configure(EntityTypeBuilder<SalesOrderLine> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Qty).HasPrecision(18, 6);
        builder.Property(x => x.UnitPrice).HasPrecision(18, 4);

        // Menghapus pesanan menghapus baris-barisnya (cascade).
        builder.HasOne(x => x.SalesOrder)
            .WithMany(o => o.Lines)
            .HasForeignKey(x => x.SalesOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // Jangan cascade dari produk/kemasan; restrict agar master data tetap terjaga.
        builder.HasOne(x => x.ProductItem)
            .WithMany()
            .HasForeignKey(x => x.ProductItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.PackLevel)
            .WithMany()
            .HasForeignKey(x => x.PackLevelId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
