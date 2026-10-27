using Bom.Domain.Entities;
using Bom.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Bom.Infrastructure.Seed;

/// <summary>
/// Master data awal (HasData) untuk kategori item dan satuan ukur dasar.
/// </summary>
public static class SeedData
{
    public static void Apply(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ItemCategory>().HasData(
            new ItemCategory { Id = 1, Name = "Bahan Baku" },
            new ItemCategory { Id = 2, Name = "Kemasan" },
            new ItemCategory { Id = 3, Name = "Semi-finished" },
            new ItemCategory { Id = 4, Name = "Produk Jadi" });

        modelBuilder.Entity<Uom>().HasData(
            new Uom { Id = 1, Code = "kg", Name = "Kilogram", Type = UomType.Weight, FactorToBase = 1m, IsBase = true },
            new Uom { Id = 2, Code = "g", Name = "Gram", Type = UomType.Weight, FactorToBase = 0.001m, IsBase = false },
            new Uom { Id = 3, Code = "L", Name = "Liter", Type = UomType.Volume, FactorToBase = 1m, IsBase = true },
            new Uom { Id = 4, Code = "ml", Name = "Mililiter", Type = UomType.Volume, FactorToBase = 0.001m, IsBase = false },
            new Uom { Id = 5, Code = "pcs", Name = "Pieces", Type = UomType.Count, FactorToBase = 1m, IsBase = true });

        // Kanal penjualan default (Fase 6). Ex-factory bukan kanal (harga dasar); HET = kanal terakhir + PPN.
        modelBuilder.Entity<SalesChannel>().HasData(
            new SalesChannel { Id = 1, Code = "DIST", Name = "Distributor", DefaultMarginPercent = 0.12m, Sequence = 1, IsActive = true },
            new SalesChannel { Id = 2, Code = "RETAIL", Name = "Retail", DefaultMarginPercent = 0.20m, Sequence = 2, IsActive = true });

        // Setelan harga singleton (Fase 6): target margin 45%, PPN 11%, margin minimum 0, tanpa pembulatan.
        modelBuilder.Entity<PricingSetting>().HasData(
            new PricingSetting
            {
                Id = 1,
                DefaultTargetMarginPercent = 0.45m,
                DefaultTaxPercent = 0.11m,
                MinMarginPercent = 0m,
                RoundingRule = RoundingRule.None
            });

        // Akun admin default. Password: "ChangeMe123!" (hash di bawah dihasilkan sekali lewat
        // PasswordHasher<object>, bukan secret runtime — ganti via endpoint ganti password
        // setelah login pertama kali).
        modelBuilder.Entity<AppUser>().HasData(
            new AppUser
            {
                Id = 1,
                Username = "admin",
                PasswordHash = "AQAAAAIAAYagAAAAEPNxzwDIXdQyUWljdwwrKOUZEPXwJVzxXFv2KgwPOT0XSk6LN6RWN8cZ9leKubX9qw==",
                CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            });
    }
}
