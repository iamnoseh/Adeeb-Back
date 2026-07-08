using Adeeb.Modules.Identity.Domain.Sessions;

namespace Adeeb.Identity.Tests;

public sealed class AuthSessionTests
{
    [Fact]
    public void Rotated_session_records_lineage_for_reuse_detection()
    {
        var now = DateTimeOffset.UtcNow;
        var replacementId = Guid.NewGuid();
        var session = new AuthSession(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "device-1",
            "Samsung S24",
            "android",
            "1.0.0",
            "hash",
            now,
            now.AddDays(30),
            "127.0.0.1",
            "test");

        session.RotateTo(replacementId, now.AddMinutes(1), "127.0.0.2");

        Assert.NotNull(session.RevokedAtUtc);
        Assert.Equal("rotated", session.RevokeReason);
        Assert.Equal(replacementId, session.ReplacedBySessionId);
    }
}
