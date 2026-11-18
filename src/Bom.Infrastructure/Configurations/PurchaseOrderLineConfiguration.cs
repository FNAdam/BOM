using Bom.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bom.Infrastructure.Configurations;

public class PurchaseOrderLineConfiguration : IEntityTypeConfiguration<PurchaseOrderLine>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderLine> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.QtyOrdered).HasPrecision(18, 6);
        builder.Property(x => x.UnitPrice).HasPrecision(18, 4);
        builder.Property(x => x.QtyReceived).HasPrecision(18, 6).HasDefaultValue(0m);

        // Menghapus PO menghapus baris-barisnya (cascade).
        builder.HasOne(x => x.PurchaseOrder)
            .WithMany(p => p.Lines)
            .HasForeignKey(x => x.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // Jangan cascade dari item/uom; restrict agar master data tetap terjaga.
        builder.HasOne(x => x.Item)
            .WithMany()
            .HasForeignKey(x => x.ItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Uom)
            .WithMany()
            .HasForeignKey(x => x.UomId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
