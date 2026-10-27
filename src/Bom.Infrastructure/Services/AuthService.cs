using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Bom.Application.Dtos;
using Bom.Application.Services;
using Bom.Domain.Entities;
using Bom.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Bom.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly BomDbContext _db;
    private readonly IConfiguration _config;
    private readonly PasswordHasher<AppUser> _hasher = new();

    public AuthService(BomDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto request, CancellationToken ct = default)
    {
        var user = await _db.AppUsers.FirstOrDefaultAsync(u => u.Username == request.Username, ct);
        if (user is null)
            return null;

        var verify = _hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verify == PasswordVerificationResult.Failed)
            return null;

        var (token, expiresAtUtc) = CreateToken(user.Username);
        return new LoginResponseDto(token, user.Username, expiresAtUtc);
    }

    public async Task ChangePasswordAsync(string username, ChangePasswordRequestDto request, CancellationToken ct = default)
    {
        var user = await _db.AppUsers.FirstOrDefaultAsync(u => u.Username == username, ct)
            ?? throw BomServiceException.NotFound("User tidak ditemukan.");

        var verify = _hasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword);
        if (verify == PasswordVerificationResult.Failed)
            throw BomServiceException.BadRequest("Password saat ini salah.");

        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 8)
            throw BomServiceException.BadRequest("Password baru minimal 8 karakter.");

        user.PasswordHash = _hasher.HashPassword(user, request.NewPassword);
        await _db.SaveChangesAsync(ct);
    }

    private (string Token, DateTime ExpiresAtUtc) CreateToken(string username)
    {
        var key = _config["Jwt:Key"]
            ?? "dev-only-insecure-signing-key-change-me-in-production-32chars+";
        var expiresInHours = double.TryParse(_config["Jwt:ExpiresInHours"], out var hours) ? hours : 168; // 7 hari

        var expiresAtUtc = DateTime.UtcNow.AddHours(expiresInHours);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, username),
            new Claim(ClaimTypes.Name, username),
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAtUtc);
    }
}
