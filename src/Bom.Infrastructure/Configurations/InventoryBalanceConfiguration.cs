using Bom.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bom.Infrastructure.Configurations;

public class InventoryBalanceConfiguration : IEntityTypeConfiguration<InventoryBalance>
{
    public void Configure(EntityTypeBuilder<InventoryBalance> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.QtyOnHand)
            .HasPrecision(18, 6)
            .HasDefaultValue(0m);

        builder.Property(x => x.AverageCost)
            .HasPrecision(18, 4)
            .HasDefaultValue(0m);

        // Satu saldo per item.
        builder.HasIndex(x => x.ItemId).IsUnique();

        // Menghapus Item akan menghapus saldonya (cascade).
        builder.HasOne(x => x.Item)
            .WithMany()
            .HasForeignKey(x => x.ItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
