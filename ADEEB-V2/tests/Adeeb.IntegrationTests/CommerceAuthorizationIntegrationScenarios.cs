using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Adeeb.Application.Abstractions.Authorization;
using Adeeb.Modules.Commerce.Contracts;
using Adeeb.Modules.Commerce.Domain.Payments;
using Adeeb.Modules.Commerce.Domain.Tariffs;
using Adeeb.Modules.Commerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Adeeb.IntegrationTests;

public sealed class CommerceAuthorizationIntegrationScenarios(AdeebApiFactory factory) : IClassFixture<AdeebApiFactory>
{
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
