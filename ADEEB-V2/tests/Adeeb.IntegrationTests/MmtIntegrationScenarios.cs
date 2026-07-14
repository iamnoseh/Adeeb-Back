using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Adeeb.Modules.Mmt.Contracts;
using Adeeb.Modules.Mmt.Domain;
using Adeeb.Modules.Mmt.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Npgsql;

namespace Adeeb.IntegrationTests;

public sealed class MmtIntegrationScenarios(AdeebApiFactory factory) : IClassFixture<AdeebApiFactory>, IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        using var client = factory.CreateClient();
        await factory.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Student_endpoint_returns_only_current_active_published_programs_and_denies_management()
    {
        var year = DateTimeOffset.UtcNow.Year;
        await using (var db = CreateDb())
        {
            var cluster = new MmtCluster(Guid.NewGuid(), "Cluster 2", "C2", null, DateTimeOffset.UtcNow);
            var university = new University(Guid.NewGuid(), "Tajik National University", "TNU", "Dushanbe", UniversityType.Public, null, DateTimeOffset.UtcNow);
            var specialty = new Specialty(Guid.NewGuid(), "LAW", "Law", null, DateTimeOffset.UtcNow);
            db.AddRange(cluster, university, specialty);
            db.AdmissionPrograms.AddRange(
                new(Guid.NewGuid(), university.Id, specialty.Id, cluster.Id, AdmissionType.Budget, StudyForm.FullTime, StudyLanguage.Tajik, year, 50, true, DateTimeOffset.UtcNow),
                new(Guid.NewGuid(), university.Id, specialty.Id, cluster.Id, AdmissionType.Contract, StudyForm.FullTime, StudyLanguage.Tajik, year, 50, false, DateTimeOffset.UtcNow),
                new(Guid.NewGuid(), university.Id, specialty.Id, cluster.Id, AdmissionType.Budget, StudyForm.PartTime, StudyLanguage.Tajik, year - 1, 50, true, DateTimeOffset.UtcNow));
            await db.SaveChangesAsync();
        }

        var anonymous = factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/v2/mmt/admission-programs")).StatusCode);

        var student = factory.CreateClient();
        student.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token());
        var response = await student.GetFromJsonAsync<PagedResponse<AdmissionProgramListItemDto>>("/api/v2/mmt/admission-programs");
        Assert.Single(response!.Items);
        Assert.Equal(HttpStatusCode.Forbidden, (await student.PostAsJsonAsync("/api/v2/admin/mmt/clusters", new CreateMmtClusterDto("Cluster 3", "C3", null))).StatusCode);
    }

    [Fact]
    public async Task PostgreSql_rejects_passing_score_with_more_than_two_decimal_places()
    {
        await using var db = CreateDb();
        var cluster = new MmtCluster(Guid.NewGuid(), "Cluster", $"C{Guid.NewGuid():N}", null, DateTimeOffset.UtcNow);
        var university = new University(Guid.NewGuid(), $"University {Guid.NewGuid():N}", null, "Dushanbe", UniversityType.Public, null, DateTimeOffset.UtcNow);
        var specialty = new Specialty(Guid.NewGuid(), $"S{Guid.NewGuid():N}", "Specialty", null, DateTimeOffset.UtcNow);
        var program = new AdmissionProgram(Guid.NewGuid(), university.Id, specialty.Id, cluster.Id, AdmissionType.Budget, StudyForm.FullTime, StudyLanguage.Tajik, 2026, null, false, DateTimeOffset.UtcNow);
        db.AddRange(cluster, university, specialty, program);
        await db.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO mmt.passing_score_history
                (id, admission_program_id, year, passing_score, created_at_utc, updated_at_utc)
            VALUES
                ({Guid.NewGuid()}, {program.Id}, {2025}, {287.555m}, {DateTimeOffset.UtcNow}, {DateTimeOffset.UtcNow})
            """));
        Assert.Equal(PostgresErrorCodes.CheckViolation, exception.SqlState);
        Assert.Equal("ck_mmt_score_value", exception.ConstraintName);
    }

    private MmtDbContext CreateDb() => new(new DbContextOptionsBuilder<MmtDbContext>().UseNpgsql(factory.ConnectionString).Options);

    private static string Token()
    {
        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes("integration-tests-signing-key-32-bytes")), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            "https://tests.adeeb.tj",
            "adeeb-tests",
            [new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()), new Claim(ClaimTypes.Role, "User"), new Claim("lang", "en-US")],
            DateTime.UtcNow.AddMinutes(-1),
            DateTime.UtcNow.AddMinutes(10),
            credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
