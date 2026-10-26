using System.Net;
using System.Text.Json;
using Xunit;

namespace Bom.Tests;

/// <summary>
/// Smoke test level-HTTP untuk endpoint dashboard. Memverifikasi rantai DI
/// (IDashboardService -> IPricingService -> ICostingService) dan jalur query (Include/Contains)
/// dapat di-resolve dan berjalan lewat pipeline nyata. Dengan data seed saja (tanpa BOM/price list),
/// laporan kosong.
/// </summary>
public class DashboardApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public DashboardApiTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task GetMargin_WithSeedOnly_Returns200AndEmptyReport()
    {
        var client = _factory.CreateClient();

        var res = await client.GetAsync("/api/dashboard/margin");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        var root = doc.RootElement;

        Assert.Equal(0, root.GetProperty("items").GetArrayLength());
        Assert.Equal(0, root.GetProperty("summary").GetProperty("productCount").GetInt32());
        Assert.Equal(0, root.GetProperty("summary").GetProperty("belowMinCount").GetInt32());
    }
}
