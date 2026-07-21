using Adeeb.Application.Abstractions.Localization;
using Adeeb.Application.Abstractions.Progression;
using Adeeb.Application.Abstractions.Testing;
using Adeeb.Application.Abstractions.Time;
using Adeeb.Modules.QuestionBank.Application;
using Adeeb.Modules.QuestionBank.Application.Assessment;
using Adeeb.Modules.QuestionBank.Contracts;
using Adeeb.Modules.QuestionBank.Domain;
using Adeeb.Modules.QuestionBank.Infrastructure.Persistence;
using Adeeb.SharedKernel.Results;
using Adeeb.SharedKernel.Progression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Adeeb.IntegrationTests;

public sealed class TestXpConcurrencyIntegrationScenarios(AdeebApiFactory factory)
    : IClassFixture<AdeebApiFactory>, IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 7, 20, 8, 0, 0, TimeSpan.Zero);

    public async Task InitializeAsync()
    {
        using var client = factory.CreateClient();
        await factory.ResetDatabaseAsync();
        await using var db = CreateDb();
        await db.Database.MigrateAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Parallel_submit_awards_exactly_one_reward_and_one_balance_credit()
    {
        var userId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        await using var setupDb = CreateDb();
        var questions = Enumerable.Range(0, 15).Select(_ => CreateQuestion(subjectId)).ToList();
        setupDb.Questions.AddRange(questions);
        await setupDb.SaveChangesAsync();
        var startService = Service(setupDb, new FixedPicker(questions));
        var attempt = await startService.StartSubjectAsync(userId, new(subjectId, 15, false),
            SupportedLanguage.Tajik, default);
        var firstQuestion = questions[0];
        var request = new SubmitAttemptRequest([
            new(firstQuestion.Id, SelectedOptionId: firstQuestion.AnswerOptions.Single(x => x.IsCorrect).Id)
        ]);

        await using var firstDb = CreateDb();
        await using var secondDb = CreateDb();
        var submissions = await Task.WhenAll(
            Service(firstDb).SubmitAsync(userId, attempt.Value!.Id, request, default),
            Service(secondDb).SubmitAsync(userId, attempt.Value.Id, request, default));

        Assert.Single(submissions, x => x.IsSuccess);
        Assert.Single(submissions, x => x.Error?.Code == "test.attempt_already_submitted");
        await using var verify = CreateDb();
        var reward = await verify.XpLedgerEntries.AsNoTracking().SingleAsync();
        var balance = await verify.StudentXpBalances.AsNoTracking().SingleAsync();
        Assert.Equal(13, reward.AmountUnits);
        Assert.Equal(13, balance.TotalXpUnits);
    }

    [Fact]
    public async Task Parallel_global_grants_for_two_sources_are_added_without_lost_update()
    {
        var userId = Guid.NewGuid();
        await using var firstDb = CreateDb();
        await using var secondDb = CreateDb();

        var grants = await Task.WhenAll(
            XpService(firstDb).GrantAsync(new(userId, XpSourceType.TestAttempt, "source-a", 13,
                "test-xp:source-a", XpEntryType.Credit), default),
            XpService(secondDb).GrantAsync(new(userId, XpSourceType.TestAttempt, "source-b", 7,
                "test-xp:source-b", XpEntryType.Credit), default));

        Assert.All(grants, x => Assert.True(x.IsSuccess, x.Error?.Code));
        await using var verify = CreateDb();
        Assert.Equal(20, (await verify.StudentXpBalances.AsNoTracking().SingleAsync()).TotalXpUnits);
        Assert.Equal(2, await verify.XpLedgerEntries.AsNoTracking().CountAsync());
    }

    [Fact]
    public async Task Parallel_duplicate_global_grant_creates_one_entry_and_one_first_balance()
    {
        var userId = Guid.NewGuid();
        var request = new XpGrantRequest(userId, XpSourceType.TestAttempt, "same-source", 13,
            "test-xp:same-source", XpEntryType.Credit);
        await using var firstDb = CreateDb();
        await using var secondDb = CreateDb();

        var grants = await Task.WhenAll(
            XpService(firstDb).GrantAsync(request, default),
            XpService(secondDb).GrantAsync(request, default));

        Assert.All(grants, x => Assert.True(x.IsSuccess, x.Error?.Code));
        Assert.Single(grants, x => !x.Value!.WasAlreadyProcessed);
        Assert.Single(grants, x => x.Value!.WasAlreadyProcessed);
        await using var verify = CreateDb();
        Assert.Equal(13, (await verify.StudentXpBalances.AsNoTracking().SingleAsync()).TotalXpUnits);
        Assert.Single(await verify.XpLedgerEntries.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task Ambient_transaction_rollback_leaves_no_ledger_or_balance()
    {
        var userId = Guid.NewGuid();
        await using var db = CreateDb();
        await using (var transaction = await db.Database.BeginTransactionAsync())
        {
            var grant = await XpService(db).GrantAsync(new(userId, XpSourceType.TestAttempt, "rollback-source", 13,
                "test-xp:rollback-source", XpEntryType.Credit), default);
            Assert.True(grant.IsSuccess, grant.Error?.Code);
            await transaction.RollbackAsync();
        }

        await using var verify = CreateDb();
        Assert.False(await verify.XpLedgerEntries.AsNoTracking().AnyAsync(x => x.UserId == userId));
        Assert.False(await verify.StudentXpBalances.AsNoTracking().AnyAsync(x => x.UserId == userId));
    }

    [Fact]
    public async Task Cancellation_rolls_back_owned_transaction()
    {
        var userId = Guid.NewGuid();
        await using var db = CreateDb();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => XpService(db).GrantAsync(new(
            userId, XpSourceType.TestAttempt, "cancelled-source", 13,
            "test-xp:cancelled-source", XpEntryType.Credit), cancellation.Token));

        await using var verify = CreateDb();
        Assert.False(await verify.XpLedgerEntries.AsNoTracking().AnyAsync(x => x.UserId == userId));
        Assert.False(await verify.StudentXpBalances.AsNoTracking().AnyAsync(x => x.UserId == userId));
    }

    [Fact]
    public async Task PostgreSql_unique_conflict_returns_typed_source_conflict_without_second_credit()
    {
        var userId = Guid.NewGuid();
        await using var db = CreateDb();
        var service = XpService(db);
        var first = await service.GrantAsync(new(userId, XpSourceType.TestAttempt, "source-one", 13,
            "test-xp:shared-key", XpEntryType.Credit), default);
        var conflict = await service.GrantAsync(new(userId, XpSourceType.TestAttempt, "source-two", 7,
            "test-xp:shared-key", XpEntryType.Credit), default);

        Assert.True(first.IsSuccess);
        Assert.Equal("xp.source_conflict", conflict.Error?.Code);
        await using var verify = CreateDb();
        Assert.Equal(13, (await verify.StudentXpBalances.AsNoTracking().SingleAsync()).TotalXpUnits);
        Assert.Single(await verify.XpLedgerEntries.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task Manual_and_expired_auto_submit_race_creates_one_settlement()
    {
        var userId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        await using var setupDb = CreateDb();
        var questions = Enumerable.Range(0, 15).Select(_ => CreateQuestion(subjectId)).ToList();
        setupDb.Questions.AddRange(questions);
        await setupDb.SaveChangesAsync();
        var attempt = await Service(setupDb, new FixedPicker(questions)).StartSubjectAsync(
            userId, new(subjectId, 15, false), SupportedLanguage.Tajik, default);
        var request = new SubmitAttemptRequest([]);

        await using var manualDb = CreateDb();
        await using var automaticDb = CreateDb();
        var submissions = await Task.WhenAll(
            Service(manualDb, clock: new FixedClock(Now)).SubmitAsync(userId, attempt.Value!.Id, request, default),
            Service(automaticDb, clock: new FixedClock(Now.AddDays(1))).SubmitAsync(userId, attempt.Value.Id, request, default));

        Assert.Single(submissions, x => x.IsSuccess);
        Assert.Single(submissions, x => x.Error?.Code == "test.attempt_already_submitted");
        await using var verify = CreateDb();
        Assert.Single(await verify.TestXpSettlements.AsNoTracking().ToListAsync());
        Assert.Single(await verify.XpLedgerEntries.AsNoTracking().ToListAsync());
    }

    private QuestionBankDbContext CreateDb() => new(new DbContextOptionsBuilder<QuestionBankDbContext>()
        .UseNpgsql(factory.ConnectionString).Options);

    private static StudentXpService XpService(QuestionBankDbContext db) =>
        new(db, new FixedClock(), NullLogger<StudentXpService>.Instance);

    private static StudentTestingService Service(QuestionBankDbContext db, IQuestionPickerService? picker = null,
        IDateTimeProvider? clock = null)
    {
        clock ??= new FixedClock();
        var options = Options.Create(new StudentTestingOptions());
        return new(db, picker ?? new FixedPicker([]),
            new SingleChoiceEvaluator(),
            new RedListService(db, clock), new EmptyMmtContext(),
            new ClosedMonthlyWindow(), new FixedTimingPolicy(), clock, options,
            new StableRandomizer(), new TestXpPolicy(Options.Create(new TestXpRewardOptions())),
            Options.Create(new TestXpRewardOptions()),
            new StudentXpService(db, clock, NullLogger<StudentXpService>.Instance),
            NullLogger<StudentTestingService>.Instance);
    }

    private static Question CreateQuestion(Guid subjectId)
    {
        var question = new Question(Guid.NewGuid(), subjectId, Guid.NewGuid(), null,
            QuestionType.SingleChoice, DifficultyLevel.Easy, null, Now);
        var correct = new AnswerOption(Guid.NewGuid(), question.Id, 1, true);
        correct.ReplaceTranslations([new(correct.Id, SupportedLanguage.Tajik, "Дуруст", null)]);
        var wrong = new AnswerOption(Guid.NewGuid(), question.Id, 2, false);
        wrong.ReplaceTranslations([new(wrong.Id, SupportedLanguage.Tajik, "Хато", null)]);
        question.ReplaceContent([new(question.Id, SupportedLanguage.Tajik, "Савол", "Шарҳ")], [correct, wrong]);
        question.Update(subjectId, question.TopicId, null, QuestionType.SingleChoice,
            DifficultyLevel.Easy, QuestionStatus.Active, null, Now);
        return question;
    }

    private sealed class FixedPicker(IReadOnlyList<Question> questions) : IQuestionPickerService
    {
        public Task<Result<IReadOnlyList<Question>>> PickAsync(QuestionPickerRequest request, CancellationToken ct) =>
            Task.FromResult(Result<IReadOnlyList<Question>>.Success(questions));
    }

    private sealed class StableRandomizer : ITestingRandomizer
    {
        public void Shuffle<T>(IList<T> values) { }
    }

    private sealed class SingleChoiceEvaluator : IAnswerEvaluationService
    {
        public AnswerEvaluationResult Evaluate(Question question, AnswerEvaluationInput input, SupportedLanguage language)
        {
            var correct = question.AnswerOptions.Single(x => x.IsCorrect);
            return new(input.SelectedOptionId.HasValue, input.SelectedOptionId == correct.Id, correct.Id);
        }
    }

    private sealed class ClosedMonthlyWindow : IMonthlyExamAvailabilityService
    {
        public MonthlyExamAvailability Current() => new(false, null, null, null);
    }

    private sealed class FixedTimingPolicy : ISubjectTestTimingPolicy
    {
        public Task<int> DurationMinutesAsync(Guid subjectId, int questionCount, SupportedLanguage language,
            CancellationToken ct) => Task.FromResult(questionCount);
    }

    private sealed class EmptyMmtContext : IStudentMmtTestingContext
    {
        public Task<StudentMmtTestingContext?> GetAsync(Guid userId, CancellationToken ct) => Task.FromResult<StudentMmtTestingContext?>(null);
    }

    private sealed class FixedClock(DateTimeOffset? now = null) : IDateTimeProvider
    {
        private readonly DateTimeOffset _now = now ?? Now;
        public DateTimeOffset UtcNow => _now;
        public DateTimeOffset DushanbeNow => _now.ToOffset(TimeSpan.FromHours(5));
        public DateTimeOffset ToDushanbeTime(DateTimeOffset value) => value.ToOffset(TimeSpan.FromHours(5));
    }
}
