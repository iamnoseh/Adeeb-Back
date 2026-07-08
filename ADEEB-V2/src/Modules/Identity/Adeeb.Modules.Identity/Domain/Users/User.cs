using Adeeb.Application.Abstractions.Localization;
using Adeeb.SharedKernel.Domain;

namespace Adeeb.Modules.Identity.Domain.Users;

public sealed class User : Entity
{
    private User() { }

    public User(
        Guid id,
        string email,
        string normalizedEmail,
        string? phoneNumber,
        string? normalizedPhoneNumber,
        string passwordHash,
        string firstName,
        string lastName,
        SupportedLanguage preferredLanguage,
        DateTimeOffset now)
    {
        Id = id;
        Email = email;
        NormalizedEmail = normalizedEmail;
        PhoneNumber = phoneNumber;
        NormalizedPhoneNumber = normalizedPhoneNumber;
        PasswordHash = passwordHash;
        FirstName = firstName;
        LastName = lastName;
        PreferredLanguage = preferredLanguage;
        Status = UserStatus.Active;
        EmailVerified = false;
        CreatedAtUtc = now;
        UpdatedAtUtc = now;
    }

    public string Email { get; private set; } = string.Empty;
    public string NormalizedEmail { get; private set; } = string.Empty;
    public string? PhoneNumber { get; private set; }
    public string? NormalizedPhoneNumber { get; private set; }
    public string PasswordHash { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public SupportedLanguage PreferredLanguage { get; private set; }
    public UserStatus Status { get; private set; }
    public bool EmailVerified { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public DateTimeOffset? LastLoginAtUtc { get; private set; }

    public void RecordLogin(DateTimeOffset now)
    {
        LastLoginAtUtc = now;
        UpdatedAtUtc = now;
    }

    public void ChangePassword(string passwordHash, DateTimeOffset now)
    {
        PasswordHash = passwordHash;
        UpdatedAtUtc = now;
    }

    public void SetStatus(UserStatus status, DateTimeOffset now)
    {
        Status = status;
        UpdatedAtUtc = now;
    }
}
