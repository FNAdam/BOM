using Bom.Application.Dtos;
using Bom.Application.Services;
using Bom.Infrastructure.Persistence;
using Bom.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Bom.Tests;

/// <summary>
/// Uji AuthService dengan DbContext InMemory. Admin default (username "admin",
/// password "ChangeMe123!") datang dari HasData (lihat SeedData) lewat EnsureCreated().
/// </summary>
public class AuthServiceTests
{
    private static AuthService NewService(out BomDbContext ctx)
    {
        var options = new DbContextOptionsBuilder<BomDbContext>()
            .UseInMemoryDatabase($"auth-tests-{Guid.NewGuid()}")
            .Options;

        ctx = new BomDbContext(options);
        ctx.Database.EnsureCreated();

        var config = new ConfigurationBuilder().Build();
        return new AuthService(ctx, config);
    }

    [Fact]
    public async Task Login_DenganKredensialBenar_MengembalikanToken()
    {
        var service = NewService(out _);

        var result = await service.LoginAsync(new LoginRequestDto("admin", "ChangeMe123!"));

        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result!.Token));
        Assert.Equal("admin", result.Username);
        Assert.True(result.ExpiresAtUtc > DateTime.UtcNow);
    }

    [Fact]
    public async Task Login_PasswordSalah_MengembalikanNull()
    {
        var service = NewService(out _);

        var result = await service.LoginAsync(new LoginRequestDto("admin", "salah-password"));

        Assert.Null(result);
    }

    [Fact]
    public async Task Login_UsernameTidakDikenal_MengembalikanNull()
    {
        var service = NewService(out _);

        var result = await service.LoginAsync(new LoginRequestDto("tidak-ada", "apapun"));

        Assert.Null(result);
    }

    [Fact]
    public async Task ChangePassword_DenganCurrentBenar_PasswordBaruBisaLogin()
    {
        var service = NewService(out _);

        await service.ChangePasswordAsync("admin", new ChangePasswordRequestDto("ChangeMe123!", "PasswordBaru123!"));

        Assert.Null(await service.LoginAsync(new LoginRequestDto("admin", "ChangeMe123!")));
        Assert.NotNull(await service.LoginAsync(new LoginRequestDto("admin", "PasswordBaru123!")));
    }

    [Fact]
    public async Task ChangePassword_CurrentSalah_MelemparBomServiceException()
    {
        var service = NewService(out _);

        await Assert.ThrowsAsync<BomServiceException>(() =>
            service.ChangePasswordAsync("admin", new ChangePasswordRequestDto("salah", "PasswordBaru123!")));
    }
}
