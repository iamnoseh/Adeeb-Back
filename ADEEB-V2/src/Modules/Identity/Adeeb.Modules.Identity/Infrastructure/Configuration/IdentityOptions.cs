using System.ComponentModel.DataAnnotations;

namespace Adeeb.Modules.Identity.Infrastructure.Configuration;

public sealed class JwtOptions
{
    private static readonly string[] DisallowedSigningKeys =
    [
        "replace-with-a-secure-32-byte-minimum-secret",
        "your-256-bit-secret",
        "your-super-secret-key",
        "development-secret",
        "default-secret"
    ];

    public const string SectionName = "Jwt";
    [Required] public string Issuer { get; init; } = string.Empty;
    [Required] public string Audience { get; init; } = string.Empty;
    [Required, MinLength(32)] public string SigningKey { get; init; } = string.Empty;
    [Range(1, 60)] public int AccessTokenMinutes { get; init; } = 10;

    public static bool IsAllowedSigningKey(string? signingKey)
    {
        if (string.IsNullOrWhiteSpace(signingKey) || signingKey.Length < 32)
        {
            return false;
        }

        return !DisallowedSigningKeys.Any(x => string.Equals(x, signingKey.Trim(), StringComparison.OrdinalIgnoreCase));
    }
}

public sealed class RefreshTokenOptions
{
    public const string SectionName = "RefreshTokens";
    [Range(1, 365)] public int LifetimeDays { get; init; } = 30;
    [Range(32, 128)] public int TokenBytes { get; init; } = 64;
}

public sealed class PasswordPolicyOptions
{
    public const string SectionName = "PasswordPolicy";
    [Range(8, 256)] public int MinimumLength { get; init; } = 8;
    public bool RequireUppercase { get; init; } = true;
    public bool RequireLowercase { get; init; } = true;
    public bool RequireDigit { get; init; } = true;
}

public sealed class SeedSuperAdminOptions
{
    public const string SectionName = "SeedSuperAdmin";
    public bool Enabled { get; init; }
    [Required, EmailAddress] public string Email { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    [Required, MinLength(8)] public string Password { get; init; } = string.Empty;
    [Required] public string FirstName { get; init; } = "Super";
    [Required] public string LastName { get; init; } = "Admin";
    public string Language { get; init; } = "tg-TJ";
}
