using Bom.Domain.Entities;
using Bom.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using BomEntity = Bom.Domain.Entities.Bom;

namespace Bom.Infrastructure.Persistence;

/// <summary>
/// DbContext utama aplikasi BOM. Konfigurasi entity dimuat dari assembly ini,
/// dan snake_case naming convention di-set di layer komposisi (Program.cs / DesignTimeFactory).
/// </summary>
public class BomDbContext : DbContext
{
    public BomDbContext(DbContextOptions<BomDbContext> options) : base(options)
    {
    }

    public DbSet<Item> Items => Set<Item>();
    public DbSet<ItemCategory> ItemCategories => Set<ItemCategory>();
    public DbSet<Uom> Uoms => Set<Uom>();
    public DbSet<UomConversion> UomConversions => Set<UomConversion>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<ItemSupplierPrice> ItemSupplierPrices => Set<ItemSupplierPrice>();
    public DbSet<BomEntity> Boms => Set<BomEntity>();
    public DbSet<BomComponent> BomComponents => Set<BomComponent>();
    public DbSet<BomOperation> BomOperations => Set<BomOperation>();
    public DbSet<CostSnapshot> CostSnapshots => Set<CostSnapshot>();
    public DbSet<InventoryBalance> InventoryBalances => Set<InventoryBalance>();
    public DbSet<ProductionOrder> ProductionOrders => Set<ProductionOrder>();
    public DbSet<ProductionConsumption> ProductionConsumptions => Set<ProductionConsumption>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<SalesChannel> SalesChannels => Set<SalesChannel>();
    public DbSet<PackLevel> PackLevels => Set<PackLevel>();
    public DbSet<PricingSetting> PricingSettings => Set<PricingSetting>();
    public DbSet<PriceList> PriceLists => Set<PriceList>();
    public DbSet<PriceListLine> PriceListLines => Set<PriceListLine>();
    public DbSet<AppUser> AppUsers => Set<AppUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Terapkan seluruh IEntityTypeConfiguration di assembly Infrastructure.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BomDbContext).Assembly);

        // Data awal (master data).
        SeedData.Apply(modelBuilder);
    }
}
