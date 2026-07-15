using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Adeeb.Modules.Mmt.Contracts;
using Adeeb.Modules.Mmt.Domain;
using Adeeb.Modules.Mmt.Infrastructure.Persistence;
using Adeeb.Application.Abstractions.Authorization;
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

    [Fact]
    public async Task Simulator_endpoints_enforce_owner_and_admin_boundaries()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var (clusterId, programId) = await SeedSimulatorProgramAsync();
        var owner = AuthenticatedClient(Token(userId));
        var profileResponse = await owner.PutAsJsonAsync("/api/v2/mmt/profile", new UpsertStudentMmtProfileDto(clusterId, DateTimeOffset.UtcNow.Year, programId));
        var profile = await ReadAsync<StudentMmtProfileDto>(profileResponse);
        var choicesResponse = await owner.PutAsJsonAsync("/api/v2/mmt/profile/choices", new UpsertAdmissionChoicesDto([new(programId, 1)]));
        choicesResponse.EnsureSuccessStatusCode();
        var evaluation = await ReadAsync<MmtEvaluationDto>(await owner.PostAsJsonAsync("/api/v2/mmt/evaluations/simulate", new SimulateMmtEvaluationDto(275m)));

        var other = AuthenticatedClient(Token(otherUserId));
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync("/api/v2/mmt/profile")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.GetAsync($"/api/v2/mmt/evaluations/{evaluation.Id}")).StatusCode);

        var admin = AuthenticatedClient(Token(Guid.NewGuid(), Permissions.Mmt.Manage));
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync($"/api/v2/admin/mmt/student-profiles/{profile.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync($"/api/v2/admin/mmt/evaluations/{evaluation.Id}")).StatusCode);
    }

    [Fact]
    public async Task PostgreSql_enforces_active_profile_and_choice_uniqueness()
    {
        var userId = Guid.NewGuid();
        var (clusterId, programId) = await SeedSimulatorProgramAsync();
        var profileId = Guid.NewGuid();
        await using var db = CreateDb();
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO mmt.student_profiles
                (id, user_id, cluster_id, admission_year, is_active, created_at_utc, updated_at_utc)
            VALUES ({profileId}, {userId}, {clusterId}, {DateTimeOffset.UtcNow.Year}, {true}, {DateTimeOffset.UtcNow}, {DateTimeOffset.UtcNow})
            """);
        var profileDuplicate = await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO mmt.student_profiles
                (id, user_id, cluster_id, admission_year, is_active, created_at_utc, updated_at_utc)
            VALUES ({Guid.NewGuid()}, {userId}, {clusterId}, {DateTimeOffset.UtcNow.Year}, {true}, {DateTimeOffset.UtcNow}, {DateTimeOffset.UtcNow})
            """));
        Assert.Equal("ux_mmt_student_profile_active_year", profileDuplicate.ConstraintName);

        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO mmt.student_admission_choices
                (id, student_mmt_profile_id, admission_program_id, priority_order, created_at_utc, updated_at_utc)
            VALUES ({Guid.NewGuid()}, {profileId}, {programId}, {1}, {DateTimeOffset.UtcNow}, {DateTimeOffset.UtcNow})
            """);
        var choiceDuplicate = await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO mmt.student_admission_choices
                (id, student_mmt_profile_id, admission_program_id, priority_order, created_at_utc, updated_at_utc)
            VALUES ({Guid.NewGuid()}, {profileId}, {programId}, {2}, {DateTimeOffset.UtcNow}, {DateTimeOffset.UtcNow})
            """));
        Assert.Equal("ux_mmt_choice_profile_program", choiceDuplicate.ConstraintName);
    }

    private MmtDbContext CreateDb() => new(new DbContextOptionsBuilder<MmtDbContext>().UseNpgsql(factory.ConnectionString).Options);

    private async Task<(Guid ClusterId, Guid ProgramId)> SeedSimulatorProgramAsync()
    {
        await using var db = CreateDb();
        var now = DateTimeOffset.UtcNow;
        var cluster = new MmtCluster(Guid.NewGuid(), "Simulator Cluster", $"SIM{Guid.NewGuid():N}", null, now);
        var university = new University(Guid.NewGuid(), $"Simulator University {Guid.NewGuid():N}", null, "Dushanbe", UniversityType.Public, null, now);
        var specialty = new Specialty(Guid.NewGuid(), $"SIM{Guid.NewGuid():N}", "Simulator Specialty", null, now);
        var program = new AdmissionProgram(Guid.NewGuid(), university.Id, specialty.Id, cluster.Id, AdmissionType.Budget, StudyForm.FullTime, StudyLanguage.Tajik, now.Year, 10, true, now);
        db.AddRange(cluster, university, specialty, program, new PassingScoreHistory(Guid.NewGuid(), program.Id, now.Year - 1, 270m, null, "test", null, now));
        await db.SaveChangesAsync();
        return (cluster.Id, program.Id);
    }

    private HttpClient AuthenticatedClient(string token)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>() ?? throw new InvalidOperationException("Response body was empty.");
    }

    private static string Token(Guid? userId = null, params string[] permissions)
    {
        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes("integration-tests-signing-key-32-bytes")), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            "https://tests.adeeb.tj",
            "adeeb-tests",
            [new Claim(JwtRegisteredClaimNames.Sub, (userId ?? Guid.NewGuid()).ToString()), new Claim(ClaimTypes.Role, "User"), new Claim("lang", "en-US"), .. permissions.Select(x => new Claim(AdeebClaimNames.Permission, x))],
            DateTime.UtcNow.AddMinutes(-1),
            DateTime.UtcNow.AddMinutes(10),
            credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
