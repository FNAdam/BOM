using Bom.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bom.Infrastructure.Configurations;

public class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.OrderNo)
            .IsRequired()
            .HasMaxLength(40);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.Notes)
            .HasMaxLength(500);

        builder.HasIndex(x => x.OrderNo).IsUnique();

        // Jangan cascade dari supplier; restrict agar supplier tetap terjaga walau masih punya PO.
        builder.HasOne(x => x.Supplier)
            .WithMany()
            .HasForeignKey(x => x.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
