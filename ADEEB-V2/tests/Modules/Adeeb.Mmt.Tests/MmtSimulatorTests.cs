using System.Security.Claims;
using Adeeb.Application.Abstractions.Time;
using Adeeb.Modules.Mmt.Application;
using Adeeb.Modules.Mmt.Contracts;
using Adeeb.Modules.Mmt.Domain;
using Adeeb.Modules.Mmt.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Adeeb.Mmt.Tests;

public sealed class MmtSimulatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 14, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Profile_rejects_inactive_cluster()
    {
        await using var db = Db();
        var cluster = new MmtCluster(Guid.NewGuid(), "Cluster", "C1", null, Now);
        cluster.SetActive(false, Now);
        db.Add(cluster); await db.SaveChangesAsync();
        var result = await Service(db).UpsertProfileAsync(User(Guid.NewGuid()), new(cluster.Id, 2026, null), default);
        Assert.Equal(MmtErrors.InactiveReference.Code, result.Error?.Code);
    }

    [Fact]
    public async Task Choices_reject_unpublished_or_inactive_programs()
    {
        await using var db = Db();
        var data = await SeedProgramsAsync(db, firstPublished: false);
        var service = Service(db); var principal = User(data.UserId);
        await service.UpsertProfileAsync(principal, new(data.Cluster.Id, 2026, null), default);
        var result = await service.ReplaceChoicesAsync(principal, new([new(data.First.Id, 1)]), default);
        Assert.Equal(MmtErrors.ChoiceProgramInvalid.Code, result.Error?.Code);

        var inactive = await db.AdmissionPrograms.SingleAsync(x => x.Id == data.First.Id);
        inactive.SetPublished(true, Now);
        inactive.SetActive(false, Now);
        await db.SaveChangesAsync();
        result = await service.ReplaceChoicesAsync(principal, new([new(data.First.Id, 1)]), default);
        Assert.Equal(MmtErrors.ChoiceProgramInvalid.Code, result.Error?.Code);
    }

    [Fact]
    public async Task Choices_reject_more_than_twelve_and_duplicate_programs()
    {
        await using var db = Db(); var service = Service(db); var principal = User(Guid.NewGuid());
        var tooMany = await service.ReplaceChoicesAsync(principal,
            new(Enumerable.Range(1, 13).Select(i => new AdmissionChoiceInputDto(Guid.NewGuid(), i)).ToList()), default);
        Assert.Equal(MmtErrors.TooManyChoices.Code, tooMany.Error?.Code);

        var programId = Guid.NewGuid();
        var duplicate = await service.ReplaceChoicesAsync(principal,
            new([new(programId, 1), new(programId, 2)]), default);
        Assert.Equal(MmtErrors.DuplicateChoiceProgram.Code, duplicate.Error?.Code);
    }

    [Fact]
    public async Task Replacing_choices_reorders_atomically_with_unique_priorities()
    {
        await using var db = Db(); var data = await SeedProgramsAsync(db); var service = Service(db); var principal = User(data.UserId);
        await service.UpsertProfileAsync(principal, new(data.Cluster.Id, 2026, null), default);
        Assert.True((await service.ReplaceChoicesAsync(principal, new([new(data.First.Id, 1), new(data.Second.Id, 2)]), default)).IsSuccess);
        var reordered = await service.ReplaceChoicesAsync(principal, new([new(data.Second.Id, 1), new(data.First.Id, 2)]), default);
        Assert.True(reordered.IsSuccess);
        var choices = reordered.Value!;
        Assert.Equal([data.Second.Id, data.First.Id], choices.Select(x => x.AdmissionProgram.Id));
        Assert.Equal([1, 2], choices.Select(x => x.PriorityOrder));
    }

    [Fact]
    public async Task Evaluation_uses_first_qualifying_priority_and_goal_readiness()
    {
        await using var db = Db(); var data = await SeedProgramsAsync(db); var service = Service(db); var principal = User(data.UserId);
        await service.UpsertProfileAsync(principal, new(data.Cluster.Id, 2026, data.First.Id), default);
        await service.ReplaceChoicesAsync(principal, new([new(data.First.Id, 1), new(data.Second.Id, 2)]), default);

        var result = await service.SimulateAsync(principal, new(260m), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.AcceptedChoicePriority);
        Assert.Equal(data.Second.Id, result.Value.AcceptedAdmissionProgramId);
        Assert.Equal(40m, result.Value.MissingScoreForGoal);
        Assert.Equal(86.67m, result.Value.ReadinessPercentage);
        Assert.Equal("MMT.Accepted", result.Value.MotivationalMessageKey);
        Assert.False(result.Value.Choices.Single(x => x.PriorityOrder == 1).IsAccepted);
        Assert.True(result.Value.Choices.Single(x => x.PriorityOrder == 2).IsAccepted);
        Assert.Equal(300m, result.Value.Choices.Single(x => x.PriorityOrder == 1).ConservativeThresholdUsed);
    }

    [Fact]
    public async Task Evaluation_snapshots_remain_unchanged_after_score_update()
    {
        await using var db = Db(); var data = await SeedProgramsAsync(db); var service = Service(db); var principal = User(data.UserId);
        await service.UpsertProfileAsync(principal, new(data.Cluster.Id, 2026, data.First.Id), default);
        await service.ReplaceChoicesAsync(principal, new([new(data.First.Id, 1)]), default);
        var original = (await service.SimulateAsync(principal, new(250m), default)).Value!;

        var latest = await db.PassingScores.SingleAsync(x => x.AdmissionProgramId == data.First.Id && x.Year == 2025);
        latest.Update(2025, 200m, null, "changed", null, Now.AddDays(1));
        await db.SaveChangesAsync();
        var historical = (await service.GetCurrentEvaluationAsync(principal, original.Id, default)).Value!;

        Assert.Equal(300m, historical.Choices.Single().PassingScoreUsed);
        Assert.Equal(300m, historical.Choices.Single().ConservativeThresholdUsed);
        Assert.Equal(50m, historical.Choices.Single().MissingScore);
    }

    [Fact]
    public async Task Student_cannot_read_another_users_profile_or_evaluation_but_admin_can()
    {
        await using var db = Db(); var data = await SeedProgramsAsync(db); var service = Service(db); var owner = User(data.UserId);
        var profile = (await service.UpsertProfileAsync(owner, new(data.Cluster.Id, 2026, null), default)).Value!;
        await service.ReplaceChoicesAsync(owner, new([new(data.First.Id, 1)]), default);
        var evaluation = (await service.SimulateAsync(owner, new(300m), default)).Value!;

        Assert.Equal(MmtErrors.StudentProfileNotFound.Code, (await service.GetCurrentProfileAsync(User(Guid.NewGuid()), default)).Error?.Code);
        Assert.Equal(MmtErrors.EvaluationNotFound.Code, (await service.GetCurrentEvaluationAsync(User(Guid.NewGuid()), evaluation.Id, default)).Error?.Code);
        Assert.Equal(profile.Id, (await service.GetAdminProfileAsync(profile.Id, default)).Value!.Id);
        Assert.Equal(evaluation.Id, (await service.GetAdminEvaluationAsync(evaluation.Id, default)).Value!.Id);
    }

    [Fact]
    public async Task Configured_admission_year_is_enforced()
    {
        await using var db = Db();
        var cluster = new MmtCluster(Guid.NewGuid(), "Cluster", "C1", null, Now); db.Add(cluster); await db.SaveChangesAsync();
        var service = Service(db, 2027);
        var result = await service.UpsertProfileAsync(User(Guid.NewGuid()), new(cluster.Id, 2026, null), default);
        Assert.Equal(MmtErrors.AdmissionYearUnavailable.Code, result.Error?.Code);
    }

    [Fact]
    public async Task Missing_threshold_data_returns_null_readiness_and_motivational_key()
    {
        await using var db = Db();
        var userId = Guid.NewGuid();
        var cluster = new MmtCluster(Guid.NewGuid(), "Cluster", "C1", null, Now);
        var university = new University(Guid.NewGuid(), "University", null, "Dushanbe", UniversityType.Public, null, Now);
        var specialty = new Specialty(Guid.NewGuid(), "SPEC", "Specialty", null, Now);
        var program = new AdmissionProgram(Guid.NewGuid(), university.Id, specialty.Id, cluster.Id,
            AdmissionType.Budget, StudyForm.FullTime, StudyLanguage.Tajik, 2026, null, true, Now);
        db.AddRange(cluster, university, specialty, program); await db.SaveChangesAsync();
        var service = Service(db); var principal = User(userId);
        await service.UpsertProfileAsync(principal, new(cluster.Id, 2026, program.Id), default);
        await service.ReplaceChoicesAsync(principal, new([new(program.Id, 1)]), default);

        var result = (await service.SimulateAsync(principal, new(200m), default)).Value!;

        Assert.Null(result.ReadinessPercentage);
        Assert.Null(result.MissingScoreForGoal);
        Assert.Equal("MMT.NoThresholdData", result.MotivationalMessageKey);
        Assert.Null(result.Choices.Single().ConservativeThresholdUsed);
    }

    private static async Task<SeedData> SeedProgramsAsync(MmtDbContext db, bool firstPublished = true)
    {
        var cluster = new MmtCluster(Guid.NewGuid(), "Cluster 2", "C2", null, Now);
        var university = new University(Guid.NewGuid(), "Tajik National University", "TNU", "Dushanbe", UniversityType.Public, null, Now);
        var specialty = new Specialty(Guid.NewGuid(), "LAW", "Law", null, Now);
        var first = new AdmissionProgram(Guid.NewGuid(), university.Id, specialty.Id, cluster.Id,
            AdmissionType.Budget, StudyForm.FullTime, StudyLanguage.Tajik, 2026, 50, firstPublished, Now);
        var second = new AdmissionProgram(Guid.NewGuid(), university.Id, specialty.Id, cluster.Id,
            AdmissionType.Contract, StudyForm.FullTime, StudyLanguage.Tajik, 2026, 50, true, Now);
        db.AddRange(cluster, university, specialty, first, second,
            new PassingScoreHistory(Guid.NewGuid(), first.Id, 2025, 300m, null, null, null, Now),
            new PassingScoreHistory(Guid.NewGuid(), first.Id, 2024, 280m, null, null, null, Now),
            new PassingScoreHistory(Guid.NewGuid(), first.Id, 2023, 290m, null, null, null, Now),
            new PassingScoreHistory(Guid.NewGuid(), second.Id, 2025, 250m, null, null, null, Now),
            new PassingScoreHistory(Guid.NewGuid(), second.Id, 2024, 230m, null, null, null, Now),
            new PassingScoreHistory(Guid.NewGuid(), second.Id, 2023, 240m, null, null, null, Now));
        await db.SaveChangesAsync();
        return new(Guid.NewGuid(), cluster, first, second);
    }

    private static MmtDbContext Db() => new(new DbContextOptionsBuilder<MmtDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static MmtSimulatorService Service(MmtDbContext db, int year = 2026) =>
        new(db, new Clock(), Options.Create(new MmtOptions { CurrentAdmissionYear = year }));
    private static ClaimsPrincipal User(Guid id) => new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, id.ToString())], "test"));
    private sealed record SeedData(Guid UserId, MmtCluster Cluster, AdmissionProgram First, AdmissionProgram Second);
    private sealed class Clock : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => Now;
        public DateTimeOffset DushanbeNow => Now.ToOffset(TimeSpan.FromHours(5));
        public DateTimeOffset ToDushanbeTime(DateTimeOffset value) => value.ToOffset(TimeSpan.FromHours(5));
    }
}
