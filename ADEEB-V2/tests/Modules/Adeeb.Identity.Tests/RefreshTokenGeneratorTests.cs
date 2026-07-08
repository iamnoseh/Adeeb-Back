using Adeeb.Modules.Identity.Infrastructure.Authentication;
using Adeeb.Modules.Identity.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace Adeeb.Identity.Tests;

public sealed class RefreshTokenGeneratorTests
{
    [Fact]
    public void Generate_returns_url_safe_high_entropy_tokens()
    {
        var generator = new RefreshTokenGenerator(Options.Create(new RefreshTokenOptions()));

        var first = generator.Generate();
        var second = generator.Generate();

        Assert.NotEqual(first, second);
        Assert.DoesNotContain("+", first);
        Assert.DoesNotContain("/", first);
        Assert.DoesNotContain("=", first);
        Assert.True(first.Length > 64);
    }

    [Fact]
    public void Hash_is_stable_and_does_not_expose_raw_token()
    {
        var generator = new RefreshTokenGenerator(Options.Create(new RefreshTokenOptions()));
        const string token = "secret-refresh-token";

        var first = generator.Hash(token);
        var second = generator.Hash(token);

        Assert.Equal(first, second);
        Assert.DoesNotContain(token, first);
        Assert.Equal(64, first.Length);
    }
}
