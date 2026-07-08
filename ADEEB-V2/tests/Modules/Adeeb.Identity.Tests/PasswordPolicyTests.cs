using Adeeb.Modules.Identity.Infrastructure.Configuration;
using Adeeb.Modules.Identity.Infrastructure.Passwords;
using Microsoft.Extensions.Options;

namespace Adeeb.Identity.Tests;

public sealed class PasswordPolicyTests
{
    [Theory]
    [InlineData("Strong123", true)]
    [InlineData("short1A", false)]
    [InlineData("lowercase123", false)]
    [InlineData("UPPERCASE123", false)]
    [InlineData("NoDigitsHere", false)]
    public void Password_policy_matches_initial_adeeb_rules(string password, bool expected)
    {
        var policy = new PasswordPolicy(Options.Create(new PasswordPolicyOptions()));

        Assert.Equal(expected, policy.IsValid(password));
    }
}
