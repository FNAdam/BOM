using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Bom.Tests;

/// <summary>
/// Uji level-HTTP untuk GET /api/items/next-code: auto-generate kode item berformat
/// "{PREFIX}-0000" berdasarkan tipe (RawMaterial=RW, Packaging=PK, SemiFinished=SF, FinishedGood=FG).
/// </summary>
public class ItemCodeGenerationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ItemCodeGenerationTests(CustomWebApplicationFactory factory) => _factory = factory;

    private static async Task<string> GetNextCodeAsync(HttpClient client, string type)
    {
        var res = await client.GetAsync($"/api/items/next-code?type={type}");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("code").GetString()!;
    }

    [Theory]
    [InlineData("RawMaterial", "RW-")]
    [InlineData("Packaging", "PK-")]
    [InlineData("SemiFinished", "SF-")]
    [InlineData("FinishedGood", "FG-")]
    public async Task NextCode_ReturnsCorrectPrefixPerType(string type, string expectedPrefix)
    {
        var client = _factory.CreateClient();
        var code = await GetNextCodeAsync(client, type);

        Assert.StartsWith(expectedPrefix, code);
        // Bagian setelah prefix harus berupa angka (minimal 4 digit, nol di depan).
        var numberPart = code[expectedPrefix.Length..];
        Assert.True(numberPart.Length >= 4, $"Nomor urut '{numberPart}' seharusnya dipad minimal 4 digit.");
        Assert.True(int.TryParse(numberPart, out _), $"Bagian '{numberPart}' seharusnya numerik.");
    }

    [Fact]
    public async Task NextCode_IncrementsAfterItemWithMatchingPatternIsCreated()
    {
        var client = _factory.CreateClient();

        var first = await GetNextCodeAsync(client, "SemiFinished");

        // Buat item dengan kode hasil saran -> saran berikutnya harus naik satu.
        var createPayload = new
        {
            code = first,
            name = "Item Uji Auto-Code",
            type = "SemiFinished",
            baseUomId = 1, // kg (seed)
            categoryId = (int?)null,
            standardMaterialCost = 0m,
            costingMethod = "Standard",
            density = (decimal?)null,
            isActive = true
        };
        var createRes = await client.PostAsJsonAsync("/api/items", createPayload);
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);

        var second = await GetNextCodeAsync(client, "SemiFinished");

        Assert.NotEqual(first, second);

        var firstNumber = int.Parse(first["SF-".Length..]);
        var secondNumber = int.Parse(second["SF-".Length..]);
        Assert.Equal(firstNumber + 1, secondNumber);
    }

    [Fact]
    public async Task NextCode_IgnoresCodesThatDoNotMatchPrefixPattern()
    {
        // Kode manual lama seperti "CHOC-PWD" (bukan pola "PREFIX-digit") tidak boleh memengaruhi
        // perhitungan nomor urut untuk tipe lain.
        var client = _factory.CreateClient();

        var createPayload = new
        {
            code = "LEGACY-ABC",
            name = "Item Kode Lama",
            type = "RawMaterial",
            baseUomId = 1,
            categoryId = (int?)null,
            standardMaterialCost = 0m,
            costingMethod = "Standard",
            density = (decimal?)null,
            isActive = true
        };
        var createRes = await client.PostAsJsonAsync("/api/items", createPayload);
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);

        // Tidak boleh error / tidak boleh ikut dihitung sebagai nomor urut RW.
        var next = await GetNextCodeAsync(client, "RawMaterial");
        Assert.StartsWith("RW-", next);
    }

    [Fact]
    public async Task NextCode_WithUnknownType_Returns400()
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync("/api/items/next-code?type=TidakValid");

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }
}
