using Bom.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Bom.Tests;

/// <summary>
/// Host uji level-HTTP: menjalankan pipeline MVC nyata (routing, model binding, filter validasi)
/// tetapi mengganti <see cref="BomDbContext"/> dari Npgsql ke provider InMemory, sehingga test
/// berjalan tanpa Postgres. Data master minimal (Uom 1..5, kategori, kanal, setelan harga) disemai
/// dari HasData lewat <c>EnsureCreated()</c> setelah host dibangun.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    // Nama tetap (bukan Guid di dalam lambda): seluruh scope pada host ini memakai store yang sama.
    private const string DatabaseName = "bom-api-tests";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Lingkungan selain Development agar middleware yang bergantung DB (Swagger gen) tidak ikut.
        builder.UseEnvironment("Testing");

        // ConfigureTestServices dijamin berjalan SETELAH ConfigureServices milik Program.cs,
        // sehingga registrasi Npgsql dapat dilepas dan diganti InMemory.
        builder.ConfigureTestServices(services =>
        {
            RemoveDbContextRegistrations(services);

            services.AddDbContext<BomDbContext>(options =>
                options.UseInMemoryDatabase(DatabaseName));
        });

        // Redam noise log provider InMemory saat test (mis. peringatan transaksi diabaikan).
        builder.ConfigureLogging(logging => logging.SetMinimumLevel(LogLevel.Warning));
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        // Seed master data (HasData) ke store InMemory host ini.
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BomDbContext>();
        db.Database.EnsureCreated();

        return host;
    }

    /// <summary>
    /// Lepas seluruh registrasi DbContext berbasis Npgsql: opsi, konfigurasi opsi (EF 9+), dan context
    /// itu sendiri. Tanpa ini, menambah provider InMemory memicu "multiple database providers registered".
    /// </summary>
    private static void RemoveDbContextRegistrations(IServiceCollection services)
    {
        var toRemove = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<BomDbContext>)
                || d.ServiceType == typeof(DbContextOptions)
                || d.ServiceType == typeof(BomDbContext)
                || (d.ServiceType.IsGenericType
                    && d.ServiceType.GetGenericTypeDefinition().Name.StartsWith("IDbContextOptionsConfiguration")))
            .ToList();

        foreach (var descriptor in toRemove)
            services.Remove(descriptor);
    }
}
