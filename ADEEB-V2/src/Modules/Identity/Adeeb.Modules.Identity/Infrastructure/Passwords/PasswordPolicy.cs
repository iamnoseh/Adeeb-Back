using Adeeb.Modules.Identity.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace Adeeb.Modules.Identity.Infrastructure.Passwords;

public sealed class PasswordPolicy(IOptions<PasswordPolicyOptions> options)
{
    private readonly PasswordPolicyOptions _options = options.Value;

    public bool IsValid(string? password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < _options.MinimumLength)
        {
            return false;
        }

        return (!_options.RequireUppercase || password.Any(char.IsUpper))
            && (!_options.RequireLowercase || password.Any(char.IsLower))
            && (!_options.RequireDigit || password.Any(char.IsDigit));
    }
}
