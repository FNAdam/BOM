using Bom.Domain.Entities;
using Bom.Domain.Enums;
using Xunit;

namespace Bom.Tests;

/// <summary>
/// Smoke test dasar untuk memastikan layer Domain terkompilasi dan default value benar.
/// </summary>
public class DomainSmokeTests
{
    [Fact]
    public void Item_Defaults_AreSensible()
    {
        var item = new Item
        {
            Code = "RM-001",
            Name = "Gula Pasir",
            Type = ItemType.RawMaterial,
            BaseUomId = 1
        };

        Assert.True(item.IsActive);
        Assert.Equal(CostingMethod.Standard, item.CostingMethod);
        Assert.Equal(0m, item.StandardMaterialCost);
        Assert.Null(item.Category);
    }

    [Fact]
    public void Uom_FactorToBase_DefaultsToOne()
    {
        var uom = new Uom { Code = "kg", Name = "Kilogram", Type = UomType.Weight };

        Assert.Equal(1m, uom.FactorToBase);
    }
}
