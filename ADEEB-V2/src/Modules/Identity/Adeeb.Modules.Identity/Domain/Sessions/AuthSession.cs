using Adeeb.SharedKernel.Domain;

namespace Adeeb.Modules.Identity.Domain.Sessions;

public sealed class AuthSession : Entity
{
    private AuthSession() { }

    public AuthSession(
        Guid id,
        Guid userId,
        Guid familyId,
        string deviceId,
        string deviceName,
        string platform,
        string? appVersion,
        string refreshTokenHash,
        DateTimeOffset now,
        DateTimeOffset expiresAtUtc,
        string? ipAddress,
        string? userAgent)
    {
        Id = id;
        UserId = userId;
        FamilyId = familyId;
        DeviceId = deviceId;
        DeviceName = deviceName;
        Platform = platform;
        AppVersion = appVersion;
        RefreshTokenHash = refreshTokenHash;
        CreatedAtUtc = now;
        ExpiresAtUtc = expiresAtUtc;
        LastUsedAtUtc = now;
        CreatedByIp = ipAddress;
        LastUsedIp = ipAddress;
        UserAgent = userAgent;
    }

    public Guid UserId { get; private set; }
    public Guid FamilyId { get; private set; }
    public string DeviceId { get; private set; } = string.Empty;
    public string DeviceName { get; private set; } = string.Empty;
    public string Platform { get; private set; } = string.Empty;
    public string? AppVersion { get; private set; }
    public string RefreshTokenHash { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset? LastUsedAtUtc { get; private set; }
    public DateTimeOffset? RotatedAtUtc { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public string? RevokeReason { get; private set; }
    public Guid? ReplacedBySessionId { get; private set; }
    public string? CreatedByIp { get; private set; }
    public string? LastUsedIp { get; private set; }
    public string? UserAgent { get; private set; }

    public bool IsActive(DateTimeOffset now) => RevokedAtUtc is null && ExpiresAtUtc > now;

    public void RotateTo(Guid replacementSessionId, DateTimeOffset now, string? ipAddress)
    {
        RotatedAtUtc = now;
        RevokedAtUtc = now;
        RevokeReason = "rotated";
        ReplacedBySessionId = replacementSessionId;
        LastUsedAtUtc = now;
        LastUsedIp = ipAddress;
    }

    public void Revoke(DateTimeOffset now, string reason)
    {
        if (RevokedAtUtc is not null)
        {
            return;
        }

        RevokedAtUtc = now;
        RevokeReason = reason;
    }
}
