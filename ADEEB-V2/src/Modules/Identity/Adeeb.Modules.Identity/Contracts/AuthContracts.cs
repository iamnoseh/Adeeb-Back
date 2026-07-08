namespace Adeeb.Modules.Identity.Contracts;

public sealed record DeviceRequest(string DeviceId, string DeviceName, string Platform, string? AppVersion);
public sealed record RegisterRequest(string Email, string? PhoneNumber, string Password, string FirstName, string LastName, string? Language, DeviceRequest Device);
public sealed record LoginRequest(string? Identifier, string? Email, string Password, DeviceRequest Device);
public sealed record RefreshTokenRequest(string RefreshToken);
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public sealed record UserResponse(Guid Id, string Email, string? PhoneNumber, string FirstName, string LastName, string PreferredLanguage);
public sealed record TokenResponse(string AccessToken, string RefreshToken, DateTimeOffset AccessTokenExpiresAtUtc);
public sealed record AuthSessionResponse(Guid Id, string DeviceName, string Platform, DateTimeOffset CreatedAtUtc, DateTimeOffset? LastUsedAtUtc, bool IsCurrent);
public sealed record AuthSessionListResponse(IReadOnlyList<AuthSessionResponse> Items);
public sealed record SessionResponse(Guid Id, string DeviceName);
public sealed record AuthResponse(UserResponse User, TokenResponse Tokens, SessionResponse Session);
