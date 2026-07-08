using Adeeb.Modules.Identity.Application;

namespace Adeeb.Identity.Tests;

public sealed class PhoneIdentifierTests
{
    [Theory]
    [InlineData("+992 900 00 00 00", "+992900000000")]
    [InlineData("900-00-00-00", "900000000")]
    [InlineData("123", null)]
    public void Phone_numbers_are_normalized_for_login_and_uniqueness(string input, string? expected)
    {
        Assert.Equal(expected, Validation.NormalizePhoneNumber(input));
    }
}
