using Adeeb.Modules.Identity.Infrastructure.Configuration;

namespace Adeeb.Identity.Tests;

public sealed class SecurityOptionsTests
{
    [Theory]
    [InlineData("")]
    [InlineData("short")]
    [InlineData("replace-with-a-secure-32-byte-minimum-secret")]
    [InlineData("your-256-bit-secret")]
    public void Jwt_signing_key_rejects_empty_short_and_known_defaults(string signingKey)
    {
        Assert.False(JwtOptions.IsAllowedSigningKey(signingKey));
    }

    [Fact]
    public void Jwt_signing_key_accepts_non_default_32_character_secret()
    {
        Assert.True(JwtOptions.IsAllowedSigningKey("local-test-secret-32-bytes-minimum"));
    }
}
