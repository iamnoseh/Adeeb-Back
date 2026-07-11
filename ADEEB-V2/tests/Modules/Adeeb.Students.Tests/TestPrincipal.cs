using System.Security.Claims;

namespace Adeeb.Students.Tests;

internal static class TestPrincipal
{
    public static ClaimsPrincipal ForUser(Guid userId) =>
        new(new ClaimsIdentity([new Claim("sub", userId.ToString())], "Test"));
}
