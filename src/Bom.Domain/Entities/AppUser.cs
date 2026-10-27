namespace Bom.Domain.Entities;

/// <summary>
/// Akun login aplikasi. Skala kecil (single-admin): tidak ada role/registrasi,
/// cuma username + password hash supaya UI tidak publik diakses siapa saja.
/// </summary>
public class AppUser
{
    public int Id { get; set; }

    public string Username { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }
}
