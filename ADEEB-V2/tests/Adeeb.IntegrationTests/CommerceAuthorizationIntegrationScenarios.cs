using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Adeeb.Application.Abstractions.Authorization;
using Adeeb.Modules.Identity.Contracts;
using Adeeb.Modules.Identity.Infrastructure.Persistence;
using Adeeb.Modules.Commerce.Contracts;
using Adeeb.Modules.Commerce.Domain.Payments;
using Adeeb.Modules.Commerce.Domain.Tariffs;
using Adeeb.Modules.Commerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Adeeb.IntegrationTests;

public sealed class CommerceAuthorizationIntegrationScenarios(AdeebApiFactory factory) : IClassFixture<AdeebApiFactory>, IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    public async Task InitializeAsync()
    {
        using var client = factory.CreateClient();
        await factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Commerce_security_matrix_enforces_finance_support_and_content_boundaries()
    {
        var anonymous = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/v2/admin/commerce/payment-receipts")).StatusCode);

        var content = Client("ContentAdmin");
        Assert.Equal(HttpStatusCode.Forbidden, (await content.GetAsync("/api/v2/admin/commerce/payment-receipts")).StatusCode);

        var support = Client("SupportAdmin", Permissions.Commerce.ViewPaymentReceipts);
        Assert.Equal(HttpStatusCode.OK, (await support.GetAsync("/api/v2/admin/commerce/payment-receipts")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await support.PostAsJsonAsync(
            $"/api/v2/admin/commerce/payment-receipts/{Guid.NewGuid()}/reject",
            new ReviewPaymentReceiptRequest("no"))).StatusCode);

        var student = Client("User");
        Assert.Equal(HttpStatusCode.Forbidden, (await student.GetAsync("/api/v2/admin/commerce/payment-receipts")).StatusCode);

        var receiptId = await SeedPendingReceiptAsync();
        var finance = Client("FinanceAdmin", Permissions.Commerce.ReviewPaymentReceipts);
        var approved = await finance.PostAsJsonAsync(
            $"/api/v2/admin/commerce/payment-receipts/{receiptId}/approve",
            new ReviewPaymentReceiptRequest("verified"));
        Assert.Equal(HttpStatusCode.OK, approved.StatusCode);

        await using var db = CreateDb();
        Assert.Equal(PaymentReceiptStatus.Approved, (await db.PaymentReceipts.AsNoTracking().SingleAsync(x => x.Id == receiptId)).Status);
        Assert.Equal(1, await db.AuditLogs.CountAsync(x => x.ResourceId == receiptId.ToString()));
    }

    [Fact]
    public async Task Persisted_roles_drive_login_refresh_permissions_and_http_authorization()
    {
        var anonymous = factory.CreateClient();
        var user = await RegisterAsync(anonymous, "plain-commerce@adeeb.tj");
        var financeBeforeRoleChange = await RegisterAsync(anonymous, "finance-commerce@adeeb.tj");
        var content = await RegisterAsync(anonymous, "content-commerce@adeeb.tj");
        await SetRoleAsync(financeBeforeRoleChange.User.Id, "FinanceAdmin");
        await SetRoleAsync(content.User.Id, "ContentAdmin");

        var financeLogin = await LoginAsync(anonymous, financeBeforeRoleChange.User.Email);
        var contentLogin = await LoginAsync(anonymous, content.User.Email);
        var refreshedFinance = await RefreshAsync(anonymous, financeBeforeRoleChange.Tokens.RefreshToken);
        var expectedFinancePermissions = Permissions.Commerce.All.OrderBy(x => x).ToArray();

        Assert.Empty(PermissionsFrom(user.Tokens.AccessToken));
        Assert.Empty(PermissionsFrom(financeBeforeRoleChange.Tokens.AccessToken));
        Assert.Equal(expectedFinancePermissions, PermissionsFrom(financeLogin.Tokens.AccessToken).OrderBy(x => x));
        Assert.Equal(expectedFinancePermissions, PermissionsFrom(refreshedFinance.Tokens.AccessToken).OrderBy(x => x));
        Assert.DoesNotContain(Permissions.QuestionBank.Manage, PermissionsFrom(financeLogin.Tokens.AccessToken));
        Assert.DoesNotContain(Permissions.Commerce.ReviewPaymentReceipts, PermissionsFrom(contentLogin.Tokens.AccessToken));

        var userClient = AuthenticatedClient(user.Tokens.AccessToken);
        Assert.Equal(HttpStatusCode.Forbidden, (await userClient.GetAsync("/api/v2/admin/commerce/payment-receipts")).StatusCode);

        var receiptId = await SeedPendingReceiptAsync();
        var contentClient = AuthenticatedClient(contentLogin.Tokens.AccessToken);
        Assert.Equal(HttpStatusCode.Forbidden, (await contentClient.PostAsJsonAsync(
            $"/api/v2/admin/commerce/payment-receipts/{receiptId}/approve",
            new ReviewPaymentReceiptRequest("forbidden"))).StatusCode);

        var financeClient = AuthenticatedClient(financeLogin.Tokens.AccessToken);
        Assert.Equal(HttpStatusCode.OK, (await financeClient.PostAsJsonAsync(
            $"/api/v2/admin/commerce/payment-receipts/{receiptId}/approve",
            new ReviewPaymentReceiptRequest("verified"))).StatusCode);
    }

    private async Task<Guid> SeedPendingReceiptAsync()
    {
        await using var db = CreateDb();
        await db.Database.MigrateAsync();
        var now = DateTimeOffset.Parse("2026-07-13T08:00:00Z");
        var tariff = new CommerceTariff(Guid.NewGuid(), "Premium", 25, "TJS", 30, "qr.webp", now);
        var receipt = new PaymentReceipt(
            Guid.NewGuid(),
            Guid.NewGuid(),
            tariff.Id,
            tariff.Name,
            tariff.Price,
            tariff.Currency,
            tariff.DurationDays,
            "commerce/payment-receipts/test/security.webp",
            $"security-{Guid.NewGuid():N}",
            now);
        db.AddRange(tariff, receipt);
        await db.SaveChangesAsync();
        return receipt.Id;
    }

    private HttpClient Client(string role, params string[] permissions)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token(role, permissions));
        return client;
    }

    private HttpClient AuthenticatedClient(string accessToken)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    private async Task SetRoleAsync(Guid userId, string role)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE identity.users SET role = {role} WHERE id = {userId}");
    }

    private static async Task<AuthResponse> RegisterAsync(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/api/v2/auth/register", AdeebApiFactory.RegisterRequest(email));
        return await ReadAsync<AuthResponse>(response);
    }

    private static async Task<AuthResponse> LoginAsync(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/api/v2/auth/login", AdeebApiFactory.LoginRequest(email));
        return await ReadAsync<AuthResponse>(response);
    }

    private static async Task<AuthResponse> RefreshAsync(HttpClient client, string refreshToken)
    {
        var response = await client.PostAsJsonAsync("/api/v2/auth/refresh", new RefreshTokenRequest(refreshToken));
        return await ReadAsync<AuthResponse>(response);
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<T>(content, JsonOptions)
            ?? throw new InvalidOperationException($"Could not deserialize response: {content}");
    }

    private static IReadOnlyList<string> PermissionsFrom(string token) =>
        new JwtSecurityTokenHandler().ReadJwtToken(token).Claims
            .Where(x => x.Type == AdeebClaimNames.Permission)
            .Select(x => x.Value)
            .ToList();

    private static string Token(string role, IEnumerable<string> permissions)
    {
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes("integration-tests-signing-key-32-bytes")),
            SecurityAlgorithms.HmacSha256);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
            new(ClaimTypes.Role, role),
            new("lang", "en-US")
        };
        claims.AddRange(permissions.Select(x => new Claim(AdeebClaimNames.Permission, x)));
        var token = new JwtSecurityToken(
            "https://tests.adeeb.tj",
            "adeeb-tests",
            claims,
            DateTime.UtcNow.AddMinutes(-1),
            DateTime.UtcNow.AddMinutes(10),
            credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private CommerceDbContext CreateDb() => new(new DbContextOptionsBuilder<CommerceDbContext>()
        .UseNpgsql(factory.ConnectionString)
        .Options);
}
