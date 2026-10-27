using Bom.Application.Dtos;

namespace Bom.Application.Services;

/// <summary>
/// Login single-admin (lihat <see cref="Bom.Domain.Entities.AppUser"/>) + penerbitan JWT bearer token.
/// </summary>
public interface IAuthService
{
    /// <summary>Null bila username tidak ada atau password salah (controller memetakan ke 401).</summary>
    Task<LoginResponseDto?> LoginAsync(LoginRequestDto request, CancellationToken ct = default);

    /// <summary>Lempar <see cref="BomServiceException"/> (400) bila current password salah.</summary>
    Task ChangePasswordAsync(string username, ChangePasswordRequestDto request, CancellationToken ct = default);
}
