using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Bom.Application.Dtos;
using Bom.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Bom.Tests;

/// <summary>
/// Beda dari test lain (yang pakai <see cref="CustomWebApplicationFactory"/> dengan fake-auth):
/// di sini skema JWT NYATA dari Program.cs tetap dipakai, supaya wiring
/// AddAuthentication/AddJwtBearer/AuthorizeFilter teruji end-to-end, bukan cuma AuthService-nya.
/// </summary>
public class AuthApiTests : IClassFixture<AuthApiTests.RealAuthFactory>
{
    private readonly RealAuthFactory _factory;

    public AuthApiTests(RealAuthFactory factory) => _factory = factory;

    [Fact]
    public async Task ProtectedEndpoint_TanpaToken_Mengembalikan401()
    {
        var client = _factory.CreateClient();

        var res = await client.GetAsync("/api/items");

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Login_PasswordSalah_Mengembalikan401()
    {
        var client = _factory.CreateClient();

        var res = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto("admin", "salah-password"));

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Login_LaluAksesProtectedEndpoint_Berhasil()
    {
        var client = _factory.CreateClient();

        var loginRes = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto("admin", "ChangeMe123!"));
        Assert.Equal(HttpStatusCode.OK, loginRes.StatusCode);

        var session = await loginRes.Content.ReadFromJsonAsync<LoginResponseDto>();
        Assert.NotNull(session);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session!.Token);
        var itemsRes = await client.GetAsync("/api/items");

        Assert.Equal(HttpStatusCode.OK, itemsRes.StatusCode);
    }

    public class RealAuthFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureTestServices(services =>
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

                services.AddDbContext<BomDbContext>(options =>
                    options.UseInMemoryDatabase("auth-api-tests"));
            });
        }

        protected override IHost CreateHost(IHostBuilder builder)
        {
            var host = base.CreateHost(builder);

            using var scope = host.Services.CreateScope();
            scope.ServiceProvider.GetRequiredService<BomDbContext>().Database.EnsureCreated();

            return host;
        }
    }
}
