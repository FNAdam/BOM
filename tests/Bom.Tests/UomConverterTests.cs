using Bom.Application.Services;
using Bom.Domain.Entities;
using Bom.Domain.Enums;
using Xunit;

namespace Bom.Tests;

/// <summary>
/// Uji unit murni untuk <see cref="UomConverter"/> (tanpa database).
/// </summary>
public class UomConverterTests
{
    private static readonly Uom Kg = new() { Code = "kg", Type = UomType.Weight, FactorToBase = 1m, IsBase = true };
    private static readonly Uom G = new() { Code = "g", Type = UomType.Weight, FactorToBase = 0.001m };
    private static readonly Uom L = new() { Code = "L", Type = UomType.Volume, FactorToBase = 1m, IsBase = true };
    private static readonly Uom Ml = new() { Code = "ml", Type = UomType.Volume, FactorToBase = 0.001m };
    private static readonly Uom Pcs = new() { Code = "pcs", Type = UomType.Count, FactorToBase = 1m, IsBase = true };

    [Fact]
    public void Convert_KgToG_MultipliesByThousand()
    {
        Assert.Equal(2000m, UomConverter.Convert(2m, Kg, G));
    }

    [Fact]
    public void Convert_GToKg_DividesByThousand()
    {
        Assert.Equal(0.5m, UomConverter.Convert(500m, G, Kg));
    }

    [Fact]
    public void Convert_LToMl_MultipliesByThousand()
    {
        Assert.Equal(1500m, UomConverter.Convert(1.5m, L, Ml));
    }

    [Fact]
    public void Convert_SameUom_ReturnsSameQuantity()
    {
        Assert.Equal(3.25m, UomConverter.Convert(3.25m, Kg, Kg));
    }

    [Fact]
    public void Convert_VolumeToWeight_UsesDensity()
    {
        // 2 L susu dengan densitas 1,03 kg/L = 2,06 kg.
        Assert.Equal(2.06m, UomConverter.Convert(2m, L, Kg, density: 1.03m));
    }

    [Fact]
    public void Convert_WeightToVolume_UsesDensity()
    {
        // 2,06 kg dengan densitas 1,03 kg/L = 2 L.
        Assert.Equal(2m, UomConverter.Convert(2.06m, Kg, L, density: 1.03m));
    }

    [Fact]
    public void Convert_CrossDimensionWithoutDensity_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => UomConverter.Convert(1m, L, Kg));
    }

    [Fact]
    public void Convert_CountCrossDimension_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => UomConverter.Convert(1m, Pcs, Kg, density: 1m));
    }
}
