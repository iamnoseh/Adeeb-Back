using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Adeeb.Modules.Identity.Contracts;
using Adeeb.Modules.Identity.Domain.Users;
using Adeeb.Modules.Identity.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Adeeb.IntegrationTests;

public sealed class IdentityIntegrationScenarios(AdeebApiFactory factory) : IClassFixture<AdeebApiFactory>, IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task InitializeAsync()
    {
        using var client = factory.CreateClient();
        await factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Jwt_principal_exists_before_localization_reads_language_claim()
    {
        var client = factory.CreateClient();
        var auth = await RegisterAsync(client, "culture-ru@adeeb.tj", language: "ru-RU");

        using var request = new HttpRequestMessage(HttpMethod.Get, "/__test/culture");
        request.Headers.Authorization = Bearer(auth.Tokens.AccessToken);

        var response = await client.SendAsync(request);
        var body = await ReadJsonAsync<TestCultureResponse>(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("ru-RU", body.Culture);
        Assert.Equal("ru-RU", body.UiCulture);
        Assert.Equal("ru-RU", body.LangClaim);
    }

    [Fact]
    public async Task Language_precedence_uses_header_then_claim_then_accept_language_then_default()
    {
        var client = factory.CreateClient();
        var auth = await RegisterAsync(client, "culture-precedence@adeeb.tj", language: "ru-RU");

        using var headerRequest = new HttpRequestMessage(HttpMethod.Get, "/__test/culture");
        headerRequest.Headers.Authorization = Bearer(auth.Tokens.AccessToken);
        headerRequest.Headers.Add("X-Adeeb-Language", "tg-TJ");
        var header = await ReadJsonAsync<TestCultureResponse>(await client.SendAsync(headerRequest));
        Assert.Equal("tg-TJ", header.Culture);

        using var invalidHeaderRequest = new HttpRequestMessage(HttpMethod.Get, "/__test/culture");
        invalidHeaderRequest.Headers.Authorization = Bearer(auth.Tokens.AccessToken);
        invalidHeaderRequest.Headers.Add("X-Adeeb-Language", "invalid");
        var invalidHeader = await ReadJsonAsync<TestCultureResponse>(await client.SendAsync(invalidHeaderRequest));
        Assert.Equal("ru-RU", invalidHeader.Culture);

        using var acceptRequest = new HttpRequestMessage(HttpMethod.Get, "/__test/culture");
        acceptRequest.Headers.AcceptLanguage.ParseAdd("en-US");
        var accept = await ReadJsonAsync<TestCultureResponse>(await client.SendAsync(acceptRequest));
        Assert.Equal("en-US", accept.Culture);

        var fallback = await ReadJsonAsync<TestCultureResponse>(await client.GetAsync("/__test/culture"));
        Assert.Equal("tg-TJ", fallback.Culture);
    }

    [Fact]
    public async Task Jwt_principal_exists_before_authenticated_rate_limit_partitioning()
    {
        var client = factory.CreateClient();
        var first = await RegisterAsync(client, "rate-one@adeeb.tj");
        var second = await RegisterAsync(client, "rate-two@adeeb.tj");

        for (var i = 0; i < 3; i++)
        {
            Assert.Equal(HttpStatusCode.OK, (await PostRateLimitedAsync(client, first.Tokens.AccessToken)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await PostRateLimitedAsync(client, second.Tokens.AccessToken)).StatusCode);
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, (await PostRateLimitedAsync(client, first.Tokens.AccessToken)).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await PostRateLimitedAsync(client, second.Tokens.AccessToken)).StatusCode);
    }

    [Fact]
    public async Task Refresh_uses_current_preferred_language_for_new_access_token()
    {
        var client = factory.CreateClient();
        var auth = await RegisterAsync(client, "language-refresh@adeeb.tj", language: "tg-TJ");
        Assert.Equal("tg-TJ", ReadClaim(auth.Tokens.AccessToken, "lang"));

        using var changeRequest = new HttpRequestMessage(HttpMethod.Put, "/api/v2/auth/me/language");
        changeRequest.Headers.Authorization = Bearer(auth.Tokens.AccessToken);
        changeRequest.Content = JsonContent.Create(new ChangePreferredLanguageRequest("ru-RU"));
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(changeRequest)).StatusCode);

        var refreshed = await RefreshAsync(client, auth.Tokens.RefreshToken);

        Assert.Equal("tg-TJ", ReadClaim(auth.Tokens.AccessToken, "lang"));
        Assert.Equal("ru-RU", ReadClaim(refreshed.Tokens.AccessToken, "lang"));
    }

    [Fact]
    public async Task Refresh_rotation_reuse_revokes_family_and_rejects_old_token()
    {
        var client = factory.CreateClient();
        var auth = await RegisterAsync(client, "refresh-reuse@adeeb.tj");

        var rotated = await RefreshAsync(client, auth.Tokens.RefreshToken);
        var reuse = await client.PostAsJsonAsync("/api/v2/auth/refresh", new RefreshTokenRequest(auth.Tokens.RefreshToken));
        var activeAfterReuse = await client.PostAsJsonAsync("/api/v2/auth/refresh", new RefreshTokenRequest(rotated.Tokens.RefreshToken));

        Assert.NotEqual(HttpStatusCode.OK, reuse.StatusCode);
        Assert.NotEqual(HttpStatusCode.OK, activeAfterReuse.StatusCode);
    }

    [Fact]
    public async Task Refresh_rejects_expired_revoked_and_blocked_user_sessions()
    {
        var client = factory.CreateClient();
        var expired = await RegisterAsync(client, "expired-refresh@adeeb.tj");
        var revoked = await RegisterAsync(client, "revoked-refresh@adeeb.tj");
        var blocked = await RegisterAsync(client, "blocked-refresh@adeeb.tj");

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var expiredHash = Hash(expired.Tokens.RefreshToken);
        var revokedHash = Hash(revoked.Tokens.RefreshToken);
        var blockedUserId = Guid.Parse(ReadClaim(blocked.Tokens.AccessToken, JwtRegisteredClaimNames.Sub));

        var expiredSession = db.AuthSessions.Single(x => x.RefreshTokenHash == expiredHash);
        typeof(Adeeb.Modules.Identity.Domain.Sessions.AuthSession)
            .GetProperty(nameof(expiredSession.ExpiresAtUtc))!
            .SetValue(expiredSession, DateTimeOffset.UtcNow.AddMinutes(-1));

        var revokedSession = db.AuthSessions.Single(x => x.RefreshTokenHash == revokedHash);
        revokedSession.Revoke(DateTimeOffset.UtcNow, "test_revoked");

        var blockedUser = db.Users.Single(x => x.Id == blockedUserId);
        blockedUser.SetStatus(UserStatus.Blocked, DateTimeOffset.UtcNow);
        await db.SaveChangesAsync();

        Assert.NotEqual(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/v2/auth/refresh", new RefreshTokenRequest(expired.Tokens.RefreshToken))).StatusCode);
        Assert.NotEqual(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/v2/auth/refresh", new RefreshTokenRequest(revoked.Tokens.RefreshToken))).StatusCode);
        Assert.NotEqual(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/v2/auth/refresh", new RefreshTokenRequest(blocked.Tokens.RefreshToken))).StatusCode);
    }

    [Fact]
    public async Task Concurrent_registration_maps_unique_email_to_stable_domain_error()
    {
        var client = factory.CreateClient();
        var email = "concurrent-email@adeeb.tj";
        var attempts = await Task.WhenAll(
            client.PostAsJsonAsync("/api/v2/auth/register", AdeebApiFactory.RegisterRequest(email)),
            client.PostAsJsonAsync("/api/v2/auth/register", AdeebApiFactory.RegisterRequest(email)));

        Assert.Single(attempts, x => x.StatusCode == HttpStatusCode.OK);
        Assert.Single(attempts, x => x.StatusCode == HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Health_live_is_process_only_and_ready_checks_databases()
    {
        var client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/live")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/ready")).StatusCode);
    }

    private static async Task<AuthResponse> RegisterAsync(HttpClient client, string email, string language = "tg-TJ")
    {
        var response = await client.PostAsJsonAsync("/api/v2/auth/register", AdeebApiFactory.RegisterRequest(email, language));
        return await ReadJsonAsync<AuthResponse>(response);
    }

    private static async Task<AuthResponse> RefreshAsync(HttpClient client, string refreshToken)
    {
        var response = await client.PostAsJsonAsync("/api/v2/auth/refresh", new RefreshTokenRequest(refreshToken));
        return await ReadJsonAsync<AuthResponse>(response);
    }

    private static async Task<HttpResponseMessage> PostRateLimitedAsync(HttpClient client, string accessToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/__test/rate-limit-auth");
        request.Headers.Authorization = Bearer(accessToken);
        return await client.SendAsync(request);
    }

    private static async Task<T> ReadJsonAsync<T>(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<T>(body, JsonOptions)
            ?? throw new InvalidOperationException($"Could not deserialize response: {body}");
    }

    private static AuthenticationHeaderValue Bearer(string token) => new("Bearer", token);

    private static string ReadClaim(string token, string type) =>
        new JwtSecurityTokenHandler().ReadJwtToken(token).Claims.Single(x => x.Type == type).Value;

    private static string Hash(string token)
    {
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private sealed record TestCultureResponse(string Culture, string UiCulture, string? LangClaim, string? Sub);
}
