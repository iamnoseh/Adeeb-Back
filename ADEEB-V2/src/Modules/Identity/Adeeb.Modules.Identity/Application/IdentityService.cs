using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Adeeb.Application.Abstractions.Localization;
using Adeeb.Application.Abstractions.Time;
using Adeeb.Modules.Identity.Contracts;
using Adeeb.Modules.Identity.Domain.Sessions;
using Adeeb.Modules.Identity.Domain.Users;
using Adeeb.Modules.Identity.Infrastructure.Authentication;
using Adeeb.Modules.Identity.Infrastructure.Configuration;
using Adeeb.Modules.Identity.Infrastructure.Passwords;
using Adeeb.Modules.Identity.Infrastructure.Persistence;
using Adeeb.SharedKernel.Results;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Adeeb.Modules.Identity.Application;

public sealed class IdentityService(
    IdentityDbContext db,
    PasswordHasher<User> passwordHasher,
    PasswordPolicy passwordPolicy,
    IRefreshTokenGenerator refreshTokenGenerator,
    IAccessTokenGenerator jwtTokenGenerator,
    IDateTimeProvider clock,
    IOptions<RefreshTokenOptions> refreshOptions,
    ILogger<IdentityService> logger)
{
    private readonly RefreshTokenOptions _refreshOptions = refreshOptions.Value;

    public async Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, ClientContext client, CancellationToken cancellationToken)
    {
        var validation = Validation.ValidateRegister(request, passwordPolicy);
        if (validation.IsFailure)
        {
            return Result<AuthResponse>.ValidationFailure(validation.ValidationErrors!);
        }

        var normalizedEmail = NormalizeEmail(request.Email);
        if (await db.Users.AnyAsync(x => x.NormalizedEmail == normalizedEmail, cancellationToken))
        {
            return Result<AuthResponse>.Failure(IdentityErrors.EmailAlreadyExists);
        }

        var normalizedPhoneNumber = Validation.NormalizePhoneNumber(request.PhoneNumber);
        if (normalizedPhoneNumber is not null &&
            await db.Users.AnyAsync(x => x.NormalizedPhoneNumber == normalizedPhoneNumber, cancellationToken))
        {
            return Result<AuthResponse>.Failure(IdentityErrors.PhoneAlreadyExists);
        }

        SupportedLanguageExtensions.TryParseCulture(request.Language, out var language);
        var now = clock.UtcNow;
        var user = new User(
            Guid.NewGuid(),
            request.Email.Trim(),
            normalizedEmail,
            request.PhoneNumber?.Trim(),
            normalizedPhoneNumber,
            string.Empty,
            request.FirstName.Trim(),
            request.LastName.Trim(),
            language,
            now);
        user.ChangePassword(passwordHasher.HashPassword(user, request.Password), now);

        var (session, rawRefreshToken) = CreateSession(user.Id, Guid.NewGuid(), ResolveDevice(request.Device, client), client, now);
        db.Users.Add(user);
        db.AuthSessions.Add(session);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (PostgresExceptionHelper.IsUniqueViolation(ex, UserDatabaseConstraints.NormalizedEmailUnique))
        {
            return Result<AuthResponse>.Failure(IdentityErrors.EmailAlreadyExists);
        }
        catch (DbUpdateException ex) when (PostgresExceptionHelper.IsUniqueViolation(ex, UserDatabaseConstraints.NormalizedPhoneNumberUnique))
        {
            return Result<AuthResponse>.Failure(IdentityErrors.PhoneAlreadyExists);
        }
        logger.LogInformation("auth.register.succeeded user_id={UserId} session_id={SessionId}", user.Id, session.Id);
        return Result<AuthResponse>.Success(CreateAuthResponse(user, session, rawRefreshToken, now));
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, ClientContext client, CancellationToken cancellationToken)
    {
        var validation = Validation.ValidateLogin(request);
        if (validation.IsFailure)
        {
            return Result<AuthResponse>.ValidationFailure(validation.ValidationErrors!);
        }

        var identifier = request.Identifier ?? request.Email ?? string.Empty;
        var user = await FindUserByIdentifierAsync(identifier, cancellationToken);
        if (user is null)
        {
            logger.LogWarning("auth.login.failed reason=unknown_user");
            return Result<AuthResponse>.Failure(IdentityErrors.InvalidCredentials);
        }

        var passwordResult = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (passwordResult == PasswordVerificationResult.Failed)
        {
            logger.LogWarning("auth.login.failed user_id={UserId} reason=bad_password", user.Id);
            return Result<AuthResponse>.Failure(IdentityErrors.InvalidCredentials);
        }

        if (user.Status != UserStatus.Active)
        {
            logger.LogWarning("auth.login.failed user_id={UserId} reason=status status={Status}", user.Id, user.Status);
            return Result<AuthResponse>.Failure(IdentityErrors.AccountBlocked);
        }

        var now = clock.UtcNow;
        var (session, rawRefreshToken) = CreateSession(user.Id, Guid.NewGuid(), ResolveDevice(request.Device, client), client, now);
        user.RecordLogin(now);
        db.AuthSessions.Add(session);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("auth.login.succeeded user_id={UserId} session_id={SessionId}", user.Id, session.Id);
        return Result<AuthResponse>.Success(CreateAuthResponse(user, session, rawRefreshToken, now));
    }

    public async Task<Result<AuthResponse>> RefreshAsync(RefreshTokenRequest request, ClientContext client, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return Result<AuthResponse>.Failure(IdentityErrors.InvalidRefreshToken);
        }

        var now = clock.UtcNow;
        var tokenHash = refreshTokenGenerator.Hash(request.RefreshToken);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var session = await db.AuthSessions
            .FromSqlInterpolated($"SELECT * FROM identity.auth_sessions WHERE refresh_token_hash = {tokenHash} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);

        if (session is null)
        {
            logger.LogWarning("auth.refresh.failed reason=unknown_token");
            return Result<AuthResponse>.Failure(IdentityErrors.InvalidRefreshToken);
        }

        if (session.RevokedAtUtc is not null && session.ReplacedBySessionId is not null)
        {
            await RevokeFamilyAsync(session.UserId, session.FamilyId, now, "token_reuse_detected", cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            logger.LogCritical("auth.refresh.reuse_detected user_id={UserId} family_id={FamilyId}", session.UserId, session.FamilyId);
            return Result<AuthResponse>.Failure(IdentityErrors.InvalidRefreshToken);
        }

        if (!session.IsActive(now))
        {
            logger.LogWarning("auth.refresh.failed session_id={SessionId} reason=inactive", session.Id);
            return Result<AuthResponse>.Failure(IdentityErrors.InvalidRefreshToken);
        }

        var user = await db.Users.SingleOrDefaultAsync(x => x.Id == session.UserId, cancellationToken);
        if (user is null || user.Status != UserStatus.Active)
        {
            return Result<AuthResponse>.Failure(IdentityErrors.InvalidRefreshToken);
        }

        var replacementDevice = new DeviceRequest(session.DeviceId, session.DeviceName, session.Platform, session.AppVersion);
        var (replacement, rawRefreshToken) = CreateSession(user.Id, session.FamilyId, replacementDevice, client, now);
        session.RotateTo(replacement.Id, now, client.IpAddress);
        db.AuthSessions.Add(replacement);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation("auth.refresh.succeeded user_id={UserId} old_session_id={OldSessionId} session_id={SessionId}", user.Id, session.Id, replacement.Id);
        return Result<AuthResponse>.Success(CreateAuthResponse(user, replacement, rawRefreshToken, now));
    }

    public async Task<Result> LogoutAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var sessionId = GetSessionId(principal);
        if (sessionId is null)
        {
            return Result.Failure(IdentityErrors.InvalidCredentials);
        }

        var session = await db.AuthSessions.SingleOrDefaultAsync(x => x.Id == sessionId.Value, cancellationToken);
        session?.Revoke(clock.UtcNow, "logout");
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("auth.session.revoked session_id={SessionId} reason=logout", sessionId);
        return Result.Success();
    }

    public async Task<Result> LogoutAllAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        if (userId is null)
        {
            return Result.Failure(IdentityErrors.InvalidCredentials);
        }

        await RevokeUserSessionsAsync(userId.Value, clock.UtcNow, "logout_all", cancellationToken);
        logger.LogInformation("auth.logout_all user_id={UserId}", userId);
        return Result.Success();
    }

    public async Task<Result<AuthSessionListResponse>> GetSessionsAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        var currentSessionId = GetSessionId(principal);
        if (userId is null || currentSessionId is null)
        {
            return Result<AuthSessionListResponse>.Failure(IdentityErrors.InvalidCredentials);
        }

        var now = clock.UtcNow;
        var sessions = await db.AuthSessions
            .Where(x => x.UserId == userId.Value && x.RevokedAtUtc == null && x.ExpiresAtUtc > now)
            .OrderByDescending(x => x.LastUsedAtUtc ?? x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var response = sessions
            .Select(x => new AuthSessionResponse(
                x.Id,
                x.DeviceName,
                x.Platform,
                x.CreatedAtUtc,
                clock.ToDushanbeTime(x.CreatedAtUtc),
                x.LastUsedAtUtc,
                x.LastUsedAtUtc is null ? null : clock.ToDushanbeTime(x.LastUsedAtUtc.Value),
                x.Id == currentSessionId.Value))
            .ToList();

        return Result<AuthSessionListResponse>.Success(new AuthSessionListResponse(response));
    }

    public async Task<Result> RevokeSessionAsync(ClaimsPrincipal principal, Guid sessionId, CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        if (userId is null)
        {
            return Result.Failure(IdentityErrors.InvalidCredentials);
        }

        var session = await db.AuthSessions.SingleOrDefaultAsync(x => x.Id == sessionId, cancellationToken);
        if (session is null)
        {
            return Result.Failure(IdentityErrors.NotFound);
        }

        if (session.UserId != userId.Value)
        {
            return Result.Failure(IdentityErrors.Forbidden);
        }

        session.Revoke(clock.UtcNow, "user_revoked_session");
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("auth.session.revoked user_id={UserId} session_id={SessionId}", userId, sessionId);
        return Result.Success();
    }

    public async Task<Result<UserResponse>> GetCurrentUserAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var userId = GetUserId(principal);
        if (userId is null)
        {
            return Result<UserResponse>.Failure(IdentityErrors.InvalidCredentials);
        }

        var user = await db.Users.SingleOrDefaultAsync(x => x.Id == userId.Value, cancellationToken);
        return user is null
            ? Result<UserResponse>.Failure(IdentityErrors.InvalidCredentials)
            : Result<UserResponse>.Success(ToUserResponse(user));
    }

    public async Task<Result<UserResponse>> ChangePreferredLanguageAsync(ClaimsPrincipal principal, ChangePreferredLanguageRequest request, CancellationToken cancellationToken)
    {
        var validation = Validation.ValidateChangePreferredLanguage(request);
        if (validation.IsFailure)
        {
            return Result<UserResponse>.ValidationFailure(validation.ValidationErrors!);
        }

        var userId = GetUserId(principal);
        if (userId is null)
        {
            return Result<UserResponse>.Failure(IdentityErrors.InvalidCredentials);
        }

        var user = await db.Users.SingleOrDefaultAsync(x => x.Id == userId.Value, cancellationToken);
        if (user is null)
        {
            return Result<UserResponse>.Failure(IdentityErrors.InvalidCredentials);
        }

        SupportedLanguageExtensions.TryParseCulture(request.Language, out var language);
        user.ChangePreferredLanguage(language, clock.UtcNow);
        await db.SaveChangesAsync(cancellationToken);
        return Result<UserResponse>.Success(ToUserResponse(user));
    }

    public async Task<Result> ChangePasswordAsync(ClaimsPrincipal principal, ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var validation = Validation.ValidateChangePassword(request, passwordPolicy);
        if (validation.IsFailure)
        {
            return Result.ValidationFailure(validation.ValidationErrors!);
        }

        var userId = GetUserId(principal);
        var currentSessionId = GetSessionId(principal);
        if (userId is null || currentSessionId is null)
        {
            return Result.Failure(IdentityErrors.InvalidCredentials);
        }

        var user = await db.Users.SingleOrDefaultAsync(x => x.Id == userId.Value, cancellationToken);
        if (user is null)
        {
            return Result.Failure(IdentityErrors.InvalidCredentials);
        }

        var passwordResult = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword);
        if (passwordResult == PasswordVerificationResult.Failed)
        {
            return Result.Failure(IdentityErrors.InvalidCredentials);
        }

        var now = clock.UtcNow;
        user.ChangePassword(passwordHasher.HashPassword(user, request.NewPassword), now);

        var otherSessions = await db.AuthSessions
            .Where(x => x.UserId == user.Id && x.Id != currentSessionId.Value && x.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);
        foreach (var session in otherSessions)
        {
            session.Revoke(now, "password_changed");
        }

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("auth.password.changed user_id={UserId} surviving_session_id={SessionId}", user.Id, currentSessionId);
        return Result.Success();
    }

    private AuthResponse CreateAuthResponse(User user, AuthSession session, string refreshToken, DateTimeOffset now)
    {
        var accessToken = jwtTokenGenerator.Generate(user, session, now);
        return new AuthResponse(
            ToUserResponse(user),
            new TokenResponse(accessToken.Token, refreshToken, accessToken.ExpiresAtUtc, clock.ToDushanbeTime(accessToken.ExpiresAtUtc)),
            new SessionResponse(session.Id, session.DeviceName));
    }

    private (AuthSession Session, string RawToken) CreateSession(Guid userId, Guid familyId, DeviceRequest device, ClientContext client, DateTimeOffset now)
    {
        var rawRefreshToken = refreshTokenGenerator.Generate();
        var hash = refreshTokenGenerator.Hash(rawRefreshToken);
        var session = new AuthSession(
            Guid.NewGuid(),
            userId,
            familyId,
            device.DeviceId.Trim(),
            device.DeviceName.Trim(),
            device.Platform.Trim().ToLowerInvariant(),
            device.AppVersion?.Trim(),
            hash,
            now,
            now.AddDays(_refreshOptions.LifetimeDays),
            client.IpAddress,
            client.UserAgent);

        return (session, rawRefreshToken);
    }

    private static UserResponse ToUserResponse(User user) =>
        new(user.Id, user.Email, user.PhoneNumber, user.FirstName, user.LastName, user.PreferredLanguage.ToCultureCode(), user.Role.ToString());

    private static DeviceRequest ResolveDevice(DeviceRequest? device, ClientContext client)
    {
        if (device is not null)
        {
            return device;
        }

        var platform = NormalizeHeaderValue(client.DevicePlatform) ?? DetectPlatform(client.UserAgent);
        var deviceName = NormalizeHeaderValue(client.DeviceName) ?? DetectDeviceName(client.UserAgent, platform);
        var appVersion = NormalizeHeaderValue(client.AppVersion);
        var source = NormalizeHeaderValue(client.DeviceId)
            ?? $"{client.IpAddress ?? "unknown"}|{client.UserAgent ?? "unknown"}|{platform}";
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source))).ToLowerInvariant()[..32];

        return new DeviceRequest(
            $"auto-{hash}",
            deviceName,
            platform,
            appVersion);
    }

    private static string? NormalizeHeaderValue(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string DetectPlatform(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return "unknown";
        }

        var ua = userAgent.ToLowerInvariant();
        if (ua.Contains("android", StringComparison.Ordinal))
        {
            return "android";
        }

        if (ua.Contains("iphone", StringComparison.Ordinal) || ua.Contains("ipad", StringComparison.Ordinal) || ua.Contains("ios", StringComparison.Ordinal))
        {
            return "ios";
        }

        return "web";
    }

    private static string DetectDeviceName(string? userAgent, string platform)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return platform == "unknown" ? "Unknown device" : $"{platform} client";
        }

        var ua = userAgent.ToLowerInvariant();
        var browser = ua switch
        {
            var x when x.Contains("edg/", StringComparison.Ordinal) => "Edge",
            var x when x.Contains("chrome/", StringComparison.Ordinal) => "Chrome",
            var x when x.Contains("firefox/", StringComparison.Ordinal) => "Firefox",
            var x when x.Contains("safari/", StringComparison.Ordinal) => "Safari",
            _ => "Browser"
        };

        if (ua.Contains("swagger", StringComparison.Ordinal))
        {
            return "Swagger UI";
        }

        if (platform == "android")
        {
            return $"{browser} on Android";
        }

        if (platform == "ios")
        {
            return $"{browser} on iOS";
        }

        if (ua.Contains("windows", StringComparison.Ordinal))
        {
            return $"{browser} on Windows";
        }

        if (ua.Contains("mac os", StringComparison.Ordinal) || ua.Contains("macintosh", StringComparison.Ordinal))
        {
            return $"{browser} on macOS";
        }

        return $"{browser} on Web";
    }

    private async Task<User?> FindUserByIdentifierAsync(string identifier, CancellationToken cancellationToken)
    {
        if (identifier.Contains('@', StringComparison.Ordinal))
        {
            var normalizedEmail = NormalizeEmail(identifier);
            return await db.Users.SingleOrDefaultAsync(x => x.NormalizedEmail == normalizedEmail, cancellationToken);
        }

        var normalizedPhone = Validation.NormalizePhoneNumber(identifier);
        return normalizedPhone is null
            ? null
            : await db.Users.SingleOrDefaultAsync(x => x.NormalizedPhoneNumber == normalizedPhone, cancellationToken);
    }

    private async Task RevokeFamilyAsync(Guid userId, Guid familyId, DateTimeOffset now, string reason, CancellationToken cancellationToken)
    {
        var sessions = await db.AuthSessions
            .Where(x => x.UserId == userId && x.FamilyId == familyId && x.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);
        foreach (var session in sessions)
        {
            session.Revoke(now, reason);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task RevokeUserSessionsAsync(Guid userId, DateTimeOffset now, string reason, CancellationToken cancellationToken)
    {
        var sessions = await db.AuthSessions
            .Where(x => x.UserId == userId && x.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);
        foreach (var session in sessions)
        {
            session.Revoke(now, reason);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static string NormalizeEmail(string email) => email.Trim().ToUpperInvariant();

    private static Guid? GetUserId(ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub"), out var userId)
            ? userId
            : null;

    private static Guid? GetSessionId(ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirstValue("sid"), out var sessionId) ? sessionId : null;
}

public sealed record ClientContext(
    string? IpAddress,
    string? UserAgent,
    string? DeviceId,
    string? DeviceName,
    string? DevicePlatform,
    string? AppVersion);
