using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Adeeb.Modules.Identity.Contracts;
using Adeeb.Modules.Students.Contracts;
using Adeeb.Modules.Students.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Adeeb.IntegrationTests;

public sealed class StudentsIntegrationScenarios(AdeebApiFactory factory) : IClassFixture<AdeebApiFactory>, IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task InitializeAsync()
    {
        using var client = factory.CreateClient();
        await factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Registration_provisions_student_persona()
    {
        var client = factory.CreateClient();
        var auth = await RegisterAsync(client, "student-register@adeeb.tj");

        using var request = Authenticated(HttpMethod.Get, "/api/v2/students/me", auth.Tokens.AccessToken);
        var response = await client.SendAsync(request);
        var student = await ReadJsonAsync<StudentResponse>(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(Guid.Parse(ReadClaim(auth.Tokens.AccessToken, JwtRegisteredClaimNames.Sub)), student.IdentityUserId);
        Assert.NotEqual(student.IdentityUserId, student.StudentId);
        Assert.Equal("Active", student.Status);
        Assert.Equal("NotStarted", student.OnboardingState);
    }

    [Fact]
    public async Task Authenticated_identity_without_student_returns_not_found()
    {
        var client = factory.CreateClient();
        var token = CreateAccessToken(Guid.NewGuid());

        using var request = Authenticated(HttpMethod.Get, "/api/v2/students/me", token);
        var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("student.not_found", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Anonymous_self_access_returns_unauthorized()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v2/students/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_activity_access_returns_unauthorized()
    {
        var client = factory.CreateClient();

        var calendar = await client.GetAsync("/api/v2/students/me/activity/calendar");
        var visit = await client.PostAsJsonAsync(
            "/api/v2/students/me/activity/visit",
            new StudentActivityVisitRequest("Asia/Dushanbe"));

        Assert.Equal(HttpStatusCode.Unauthorized, calendar.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, visit.StatusCode);
    }

    [Fact]
    public async Task User_cannot_modify_another_student_profile_through_self_endpoint()
    {
        var client = factory.CreateClient();
        var first = await RegisterAsync(client, "student-a@adeeb.tj");
        var second = await RegisterAsync(client, "student-b@adeeb.tj");
        var firstStudent = await GetMeAsync(client, first.Tokens.AccessToken);
        var secondStudentBefore = await GetMeAsync(client, second.Tokens.AccessToken);

        using var patch = Authenticated(HttpMethod.Patch, "/api/v2/students/me/profile", first.Tokens.AccessToken);
        patch.Content = JsonContent.Create(new UpdateStudentProfileRequest("User A", null, null, null, null, null, null));
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(patch)).StatusCode);

        var secondStudentAfter = await GetMeAsync(client, second.Tokens.AccessToken);
        Assert.NotEqual(firstStudent.StudentId, secondStudentAfter.StudentId);
        Assert.Equal(secondStudentBefore.Profile.DisplayName, secondStudentAfter.Profile.DisplayName);
    }

    [Fact]
    public async Task Duplicate_and_concurrent_provisioning_return_one_student()
    {
        var client = factory.CreateClient();
        var identityUserId = Guid.NewGuid();
        var token = CreateAccessToken(identityUserId);

        var responses = await Task.WhenAll(
            ProvisionAsync(client, token),
            ProvisionAsync(client, token));
        Assert.All(responses, x => Assert.Equal(HttpStatusCode.OK, x.StatusCode));

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<StudentsDbContext>();
        Assert.Equal(1, db.Students.Count(x => x.IdentityUserId == identityUserId));
    }

    [Fact]
    public async Task Concurrent_activity_visits_are_idempotent_and_calendar_is_owned_by_student()
    {
        var client = factory.CreateClient();
        var auth = await RegisterAsync(client, "student-activity@adeeb.tj");
        var student = await GetMeAsync(client, auth.Tokens.AccessToken);

        var responses = await Task.WhenAll(
            VisitAsync(client, auth.Tokens.AccessToken, "Asia/Dushanbe"),
            VisitAsync(client, auth.Tokens.AccessToken, "Asia/Dushanbe"));
        Assert.All(responses, response => Assert.Equal(HttpStatusCode.OK, response.StatusCode));

        using var calendarRequest = Authenticated(HttpMethod.Get, "/api/v2/students/me/activity/calendar", auth.Tokens.AccessToken);
        var calendarResponse = await client.SendAsync(calendarRequest);
        var calendar = await ReadJsonAsync<StudentActivityCalendarResponse>(calendarResponse);

        Assert.Equal(1, calendar.ActiveDaysInMonth);
        Assert.Equal(1, calendar.CurrentStreak);
        Assert.Single(calendar.Days);
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<StudentsDbContext>();
        Assert.Equal(1, db.DailyActivities.Count(x => x.StudentId == student.StudentId));
    }

    [Fact]
    public async Task Profile_and_status_persist_in_students_schema()
    {
        var client = factory.CreateClient();
        var auth = await RegisterAsync(client, "student-persist@adeeb.tj");

        using var patch = Authenticated(HttpMethod.Patch, "/api/v2/students/me/profile", auth.Tokens.AccessToken);
        patch.Content = JsonContent.Create(new UpdateStudentProfileRequest("Persisted", null, null, "Sughd", "Khujand", "School 1", 8));
        var patched = await ReadJsonAsync<StudentResponse>(await client.SendAsync(patch));

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<StudentsDbContext>();
        var stored = db.Students.Single(x => x.Id == patched.StudentId);
        db.Entry(stored).Reference(x => x.Profile).Load();

        Assert.Equal("Persisted", stored.Profile.DisplayName);
        Assert.Equal("Sughd", stored.Profile.Region);
        Assert.Equal((short)8, stored.Profile.Grade);
    }

    private static async Task<AuthResponse> RegisterAsync(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync("/api/v2/auth/register", AdeebApiFactory.RegisterRequest(email));
        return await ReadJsonAsync<AuthResponse>(response);
    }

    private static async Task<StudentResponse> GetMeAsync(HttpClient client, string token)
    {
        using var request = Authenticated(HttpMethod.Get, "/api/v2/students/me", token);
        return await ReadJsonAsync<StudentResponse>(await client.SendAsync(request));
    }

    private static async Task<HttpResponseMessage> ProvisionAsync(HttpClient client, string token)
    {
        using var request = Authenticated(HttpMethod.Post, "/api/v2/students/me/provision", token);
        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> VisitAsync(HttpClient client, string token, string timeZoneId)
    {
        using var request = Authenticated(HttpMethod.Post, "/api/v2/students/me/activity/visit", token);
        request.Content = JsonContent.Create(new StudentActivityVisitRequest(timeZoneId));
        return await client.SendAsync(request);
    }

    private static HttpRequestMessage Authenticated(HttpMethod method, string path, string token)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    private static async Task<T> ReadJsonAsync<T>(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<T>(body, JsonOptions)
            ?? throw new InvalidOperationException($"Could not deserialize response: {body}");
    }

    private static string ReadClaim(string token, string type) =>
        new JwtSecurityTokenHandler().ReadJwtToken(token).Claims.Single(x => x.Type == type).Value;

    private static string CreateAccessToken(Guid userId)
    {
        var now = DateTimeOffset.UtcNow;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("integration-tests-signing-key-32-bytes"));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: "https://tests.adeeb.tj",
            audience: "adeeb-tests",
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim("sid", Guid.NewGuid().ToString()),
                new Claim("lang", "tg-TJ"),
                new Claim(ClaimTypes.Role, "User")
            ],
            notBefore: now.UtcDateTime,
            expires: now.AddMinutes(10).UtcDateTime,
            signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
