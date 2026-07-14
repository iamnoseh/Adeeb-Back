using Adeeb.Application.Abstractions.Time;
using Adeeb.Modules.Commerce.Application.Auditing;
using Adeeb.Modules.Commerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Adeeb.Commerce.Tests;

public sealed class CommerceAuditTests
{
    [Fact]
    public async Task Audit_records_actor_correlation_and_redacts_sensitive_fields()
    {
        await using var db = new CommerceDbContext(new DbContextOptionsBuilder<CommerceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        var actorId = Guid.NewGuid();
        var writer = new CommerceAuditWriter(db, new AuditContext(actorId), new FixedClock());

        writer.Write(
            CommerceAuditActions.ReceiptApproved,
            "PaymentReceipt",
            Guid.NewGuid(),
            Guid.NewGuid(),
            new Dictionary<string, object?>
            {
                ["status"] = "Pending",
                ["authorizationHeader"] = "Bearer secret",
                ["refreshToken"] = "secret",
                ["receiptImageObjectKey"] = "private/key"
            },
            new Dictionary<string, object?> { ["status"] = "Approved" });
        await db.SaveChangesAsync();

        var audit = await db.AuditLogs.SingleAsync();
        Assert.Equal(actorId, audit.ActorUserId);
        Assert.Equal("correlation-1", audit.CorrelationId);
        Assert.Equal("127.0.0.1", audit.IpAddress);
        Assert.Contains("Pending", audit.OldValuesJson, StringComparison.Ordinal);
        Assert.DoesNotContain("secret", audit.OldValuesJson, StringComparison.Ordinal);
        Assert.DoesNotContain("private/key", audit.OldValuesJson, StringComparison.Ordinal);
    }

    private sealed class AuditContext(Guid actorId) : ICommerceAuditContext
    {
        public CommerceAuditActor Current => new(actorId, "127.0.0.1", "test-agent", "correlation-1");
    }

    private sealed class FixedClock : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => DateTimeOffset.Parse("2026-07-13T08:00:00Z");
        public DateTimeOffset DushanbeNow => ToDushanbeTime(UtcNow);
        public DateTimeOffset ToDushanbeTime(DateTimeOffset utc) => utc.ToOffset(TimeSpan.FromHours(5));
    }
}
