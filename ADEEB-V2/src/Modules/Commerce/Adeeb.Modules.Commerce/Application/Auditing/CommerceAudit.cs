using System.Text.Json;
using Adeeb.Application.Abstractions.Time;
using Adeeb.Modules.Commerce.Domain.Auditing;
using Adeeb.Modules.Commerce.Infrastructure.Persistence;

namespace Adeeb.Modules.Commerce.Application.Auditing;

public static class CommerceAuditActions
{
    public const string TariffCreated = "commerce.tariff.created";
    public const string TariffUpdated = "commerce.tariff.updated";
    public const string TariffArchived = "commerce.tariff.archived";
    public const string ReceiptSubmitted = "commerce.receipt.submitted";
    public const string ReceiptApproved = "commerce.receipt.approved";
    public const string ReceiptRejected = "commerce.receipt.rejected";
    public const string ReceiptImageAccessed = "commerce.receipt.image_accessed";
    public const string EntitlementGranted = "commerce.entitlement.granted";
    public const string EntitlementRevoked = "commerce.entitlement.revoked";
}

public sealed record CommerceAuditActor(
    Guid? UserId,
    string? IpAddress,
    string? UserAgent,
    string? CorrelationId);

public interface ICommerceAuditContext
{
    CommerceAuditActor Current { get; }
}

public interface ICommerceAuditWriter
{
    void Write(
        string action,
        string resourceType,
        Guid resourceId,
        Guid? studentId = null,
        IReadOnlyDictionary<string, object?>? oldValues = null,
        IReadOnlyDictionary<string, object?>? newValues = null);
}

public sealed class CommerceAuditWriter(
    CommerceDbContext db,
    ICommerceAuditContext context,
    IDateTimeProvider clock) : ICommerceAuditWriter
{
    public void Write(
        string action,
        string resourceType,
        Guid resourceId,
        Guid? studentId = null,
        IReadOnlyDictionary<string, object?>? oldValues = null,
        IReadOnlyDictionary<string, object?>? newValues = null)
    {
        var actor = context.Current;
        db.AuditLogs.Add(new AuditLog(
            Guid.NewGuid(),
            actor.UserId,
            action,
            resourceType,
            resourceId.ToString(),
            studentId,
            Serialize(oldValues),
            Serialize(newValues),
            Trim(actor.IpAddress, AuditLog.IpAddressMaxLength),
            Trim(actor.UserAgent, AuditLog.UserAgentMaxLength),
            Trim(actor.CorrelationId, AuditLog.CorrelationIdMaxLength),
            clock.UtcNow));
    }

    private static string? Serialize(IReadOnlyDictionary<string, object?>? values)
    {
        if (values is null)
        {
            return null;
        }

        var sanitized = values
            .Where(x => !IsSensitive(x.Key))
            .ToDictionary(x => x.Key, x => x.Value);
        return sanitized.Count == 0 ? null : JsonSerializer.Serialize(sanitized);
    }

    private static bool IsSensitive(string key) =>
        key.Contains("password", StringComparison.OrdinalIgnoreCase) ||
        key.Contains("token", StringComparison.OrdinalIgnoreCase) ||
        key.Contains("authorization", StringComparison.OrdinalIgnoreCase) ||
        key.Contains("connection", StringComparison.OrdinalIgnoreCase) ||
        key.Contains("image", StringComparison.OrdinalIgnoreCase) ||
        key.Contains("objectKey", StringComparison.OrdinalIgnoreCase) ||
        key.Contains("card", StringComparison.OrdinalIgnoreCase) ||
        key.Contains("account", StringComparison.OrdinalIgnoreCase);

    private static string? Trim(string? value, int maxLength) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim()[..Math.Min(value.Trim().Length, maxLength)];
}

internal sealed class NullCommerceAuditWriter : ICommerceAuditWriter
{
    public void Write(
        string action,
        string resourceType,
        Guid resourceId,
        Guid? studentId = null,
        IReadOnlyDictionary<string, object?>? oldValues = null,
        IReadOnlyDictionary<string, object?>? newValues = null)
    {
    }
}
