using Bom.Domain.Entities;
using Bom.Domain.Enums;

namespace Bom.Application.Services;

/// <summary>
/// Konversi kuantitas antar satuan ukur. Murni (tanpa EF / I/O) sehingga mudah diuji.
///
/// Sedimensi: <c>qty * from.FactorToBase / to.FactorToBase</c>.
/// Lintas Volume &lt;-&gt; Weight memakai densitas (massa-base per volume-base, mis. kg per L):
/// Volume-&gt;Weight = volBase * density; Weight-&gt;Volume = wtBase / density.
/// Konversi lintas dimensi yang melibatkan Count, atau tanpa densitas, dilempar sebagai error.
/// </summary>
public static class UomConverter
{
    public static decimal Convert(decimal qty, Uom from, Uom to, decimal? density = null)
    {
        ArgumentNullException.ThrowIfNull(from);
        ArgumentNullException.ThrowIfNull(to);

        // Kuantitas dalam satuan dasar dimensi asal.
        var fromBase = qty * from.FactorToBase;

        // Sedimensi: cukup rasio faktor.
        if (from.Type == to.Type)
            return fromBase / to.FactorToBase;

        // Lintas dimensi hanya didukung antara Weight dan Volume via densitas.
        if (from.Type == UomType.Volume && to.Type == UomType.Weight)
        {
            var d = RequireDensity(density, from, to);
            var weightBase = fromBase * d; // (volume base) * (massa/volume) = massa base
            return weightBase / to.FactorToBase;
        }

        if (from.Type == UomType.Weight && to.Type == UomType.Volume)
        {
            var d = RequireDensity(density, from, to);
            var volumeBase = fromBase / d; // (massa base) / (massa/volume) = volume base
            return volumeBase / to.FactorToBase;
        }

        throw new InvalidOperationException(
            $"Konversi lintas-dimensi dari '{from.Code}' ({from.Type}) ke '{to.Code}' ({to.Type}) tidak didukung.");
    }

    private static decimal RequireDensity(decimal? density, Uom from, Uom to)
    {
        if (density is not decimal d || d <= 0)
            throw new InvalidOperationException(
                $"Konversi '{from.Code}' ({from.Type}) ke '{to.Code}' ({to.Type}) membutuhkan densitas > 0 (kg per L).");
        return d;
    }
}
