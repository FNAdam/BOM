using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Bom.Infrastructure.Persistence;

/// <summary>
/// Factory design-time agar perintah `dotnet ef` (mis. migrations add) dapat membuat
/// DbContext tanpa menjalankan host API. Tidak melakukan koneksi ke DB saat scaffolding migration.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<BomDbContext>
{
    // Default disamakan dengan appsettings.json (ConnectionStrings:Default).
    private const string DefaultConnection =
        "Host=localhost;Port=5432;Database=bom;Username=bom;Password=bompass";

    public BomDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__Default") ?? DefaultConnection;

        var options = new DbContextOptionsBuilder<BomDbContext>()
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        return new BomDbContext(options);
    }
}
