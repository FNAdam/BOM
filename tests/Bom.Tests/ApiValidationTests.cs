using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Bom.Tests;

/// <summary>
/// Uji level-HTTP lewat pipeline MVC nyata (WebApplicationFactory + DbContext InMemory).
/// Menjadi regression guard untuk bug "atribut validasi bertarget <c>[property:]</c>" yang dulu
/// membuat semua create balas 500, dan membuktikan request invalid balas 400 ValidationProblemDetails.
/// </summary>
public class ApiValidationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ApiValidationTests(CustomWebApplicationFactory factory) => _factory = factory;

    private static string UniqueCode(string prefix, int maxLen)
    {
        var raw = $"{prefix}{Guid.NewGuid():N}";
        return raw.Length <= maxLen ? raw : raw[..maxLen];
    }

    // --------------------------------------------------- POST /api/items (valid) -> 201

    [Fact]
    public async Task CreateItem_WithValidPayload_Returns201()
    {
        var client = _factory.CreateClient();
        var payload = new
        {
            code = UniqueCode("IT", 30),
            name = "Item Uji Valid",
            type = "RawMaterial",
            baseUomId = 1, // kg (dari seed HasData)
            categoryId = (int?)null,
            standardMaterialCost = 1000m,
            costingMethod = "Standard",
            density = (decimal?)null,
            isActive = true
        };

        var res = await client.PostAsJsonAsync("/api/items", payload);

        // Dulu GAGAL (500) karena DTO record memakai [property: Required] dst. Sekarang harus 201.
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        Assert.True(doc.RootElement.GetProperty("id").GetInt32() > 0);
    }

    // --------------------------------------------------- POST /api/items (invalid) -> 400

    [Fact]
    public async Task CreateItem_WithInvalidPayload_Returns400ValidationProblem()
    {
        var client = _factory.CreateClient();
        var payload = new
        {
            code = "",      // melanggar Required
            name = "Tanpa Kode",
            type = "RawMaterial",
            baseUomId = 0,  // melanggar Range(1, ...)
            categoryId = (int?)null,
            standardMaterialCost = 0m,
            costingMethod = "Standard",
            density = (decimal?)null,
            isActive = true
        };

        var res = await client.PostAsJsonAsync("/api/items", payload);

        // Harus 400 (ValidationProblemDetails), BUKAN 500.
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        Assert.True(
            doc.RootElement.TryGetProperty("errors", out _),
            "Respons 400 harus berbentuk ValidationProblemDetails (punya properti 'errors').");
    }

    // ------------------------------------- POST /api/items density=0 -> 400 (FluentValidation saja)

    [Fact]
    public async Task CreateItem_WithZeroDensity_Returns400_ProvesFluentValidationActive()
    {
        // Density tidak dibatasi DataAnnotations; hanya FluentValidation (Density > 0 bila diisi).
        // 400 di sini hanya mungkin bila auto-validation FluentValidation benar-benar aktif.
        var client = _factory.CreateClient();
        var payload = new
        {
            code = UniqueCode("ITD", 30),
            name = "Item Density Nol",
            type = "RawMaterial",
            baseUomId = 1,
            categoryId = (int?)null,
            standardMaterialCost = 0m,
            costingMethod = "Standard",
            density = 0m,
            isActive = true
        };

        var res = await client.PostAsJsonAsync("/api/items", payload);

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);

        // SharpGrip harus menghasilkan bentuk ValidationProblemDetails yang sama (punya 'errors'),
        // sehingga client.ts (extractErrorMessage) dapat memparsnya seperti error DataAnnotations.
        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        Assert.True(
            doc.RootElement.TryGetProperty("errors", out _),
            "Respons 400 FluentValidation harus berbentuk ValidationProblemDetails (punya properti 'errors').");
    }

    // --------------------------------------------------- POST /api/uoms (valid) -> 201

    [Fact]
    public async Task CreateUom_WithValidPayload_Returns201()
    {
        var client = _factory.CreateClient();
        var payload = new
        {
            code = UniqueCode("U", 10),
            name = "Satuan Uji",
            type = "Count",
            factorToBase = 1m,
            isBase = false
        };

        var res = await client.PostAsJsonAsync("/api/uoms", payload);

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
    }
}
