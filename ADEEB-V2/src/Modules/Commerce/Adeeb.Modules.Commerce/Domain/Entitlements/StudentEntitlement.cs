using Adeeb.SharedKernel.Domain;

namespace Adeeb.Modules.Commerce.Domain.Entitlements;

public sealed class StudentEntitlement : Entity
{
    private StudentEntitlement() { }

    public StudentEntitlement(
        Guid id,
        Guid studentId,
        CommerceEntitlementKind kind,
        CommerceEntitlementSource source,
        DateTimeOffset startsAtUtc,
        DateTimeOffset? expiresAtUtc,
        string idempotencyKey,
        DateTimeOffset now)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Entitlement id is required.", nameof(id));
        }

        if (studentId == Guid.Empty)
        {
            throw new ArgumentException("Student id is required.", nameof(studentId));
        }

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new ArgumentException("Idempotency key is required.", nameof(idempotencyKey));
        }

        if (expiresAtUtc is not null && expiresAtUtc <= startsAtUtc)
        {
            throw new ArgumentException("Expiration must be after start.", nameof(expiresAtUtc));
        }

        Id = id;
        StudentId = studentId;
        Kind = kind;
        Source = source;
        Status = CommerceEntitlementStatus.Active;
        StartsAtUtc = startsAtUtc;
        ExpiresAtUtc = expiresAtUtc;
        IdempotencyKey = idempotencyKey.Trim();
        CreatedAtUtc = now;
        UpdatedAtUtc = now;
    }

    public Guid StudentId { get; private set; }
    public CommerceEntitlementKind Kind { get; private set; }
    public CommerceEntitlementStatus Status { get; private set; }
    public CommerceEntitlementSource Source { get; private set; }
    public DateTimeOffset StartsAtUtc { get; private set; }
    public DateTimeOffset? ExpiresAtUtc { get; private set; }
    public string IdempotencyKey { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public bool IsActiveAt(DateTimeOffset now) =>
        Status == CommerceEntitlementStatus.Active &&
        StartsAtUtc <= now &&
        (ExpiresAtUtc is null || ExpiresAtUtc > now);

    public void Revoke(DateTimeOffset now)
    {
        if (Status == CommerceEntitlementStatus.Revoked)
        {
            return;
        }

        Status = CommerceEntitlementStatus.Revoked;
        UpdatedAtUtc = now;
    }
}
