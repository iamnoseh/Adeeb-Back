using Adeeb.SharedKernel.Domain;

namespace Adeeb.Modules.Commerce.Domain.Auditing;

public sealed class AuditLog : Entity
{
    public const int ActionMaxLength = 160;
    public const int ResourceTypeMaxLength = 80;
    public const int ResourceIdMaxLength = 80;
    public const int IpAddressMaxLength = 64;
    public const int UserAgentMaxLength = 512;
    public const int CorrelationIdMaxLength = 128;

    private AuditLog() { }

    public AuditLog(
        Guid id,
        Guid? actorUserId,
        string action,
        string resourceType,
        string resourceId,
        Guid? studentId,
        string? oldValuesJson,
        string? newValuesJson,
        string? ipAddress,
        string? userAgent,
        string? correlationId,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        ActorUserId = actorUserId;
        Action = action;
        ResourceType = resourceType;
        ResourceId = resourceId;
        StudentId = studentId;
        OldValuesJson = oldValuesJson;
        NewValuesJson = newValuesJson;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        CorrelationId = correlationId;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid? ActorUserId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string ResourceType { get; private set; } = string.Empty;
    public string ResourceId { get; private set; } = string.Empty;
    public Guid? StudentId { get; private set; }
    public string? OldValuesJson { get; private set; }
    public string? NewValuesJson { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public string? CorrelationId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
}
