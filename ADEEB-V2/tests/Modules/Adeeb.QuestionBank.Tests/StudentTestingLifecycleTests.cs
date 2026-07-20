using Adeeb.Application.Abstractions.Localization;
using Adeeb.Application.Abstractions.Testing;
using Adeeb.Application.Abstractions.Time;
using Adeeb.Modules.QuestionBank.Application;
using Adeeb.Modules.QuestionBank.Application.Assessment;
using Adeeb.Modules.QuestionBank.Contracts;
using Adeeb.Modules.QuestionBank.Domain;
using Adeeb.Modules.QuestionBank.Infrastructure.Persistence;
using Adeeb.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Adeeb.QuestionBank.Tests;

public sealed class StudentTestingLifecycleTests
{
    [Fact]
    public async Task Wrong_simple_and_closed_answers_enter_red_list_but_matching_does_not()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        for (var index = 0; index < 15; index++)
        {
            var type = index < 5 ? QuestionType.SingleChoice
                : index < 10 ? QuestionType.ClosedAnswer : QuestionType.Matching;
            db.Questions.Add(CreateActiveQuestion(type, subjectId));
        }
        await db.SaveChangesAsync();
        var service = CreateService(db, new MutableClock(DateTimeOffset.UtcNow));
        var attempt = await service.StartSubjectAsync(userId, new(subjectId, 15, false),
            SupportedLanguage.Tajik, default);

        var result = await service.SubmitAsync(userId, attempt.Value!.Id, new([]), default);

        Assert.True(result.IsSuccess);
        var redItems = await db.StudentRedListItems.AsNoTracking().ToListAsync();
        Assert.Equal(10, redItems.Count);
        Assert.DoesNotContain(redItems, x => x.QuestionType == QuestionType.Matching);
        Assert.Single(result.Value!.SubjectBreakdown);
        Assert.NotEmpty(result.Value.WeakTopics);
        Assert.Equal(0m, result.Value.TotalXp);
        Assert.False(result.Value.XpAwarded);
        Assert.Single(await db.TestXpRewards.AsNoTracking().ToListAsync());
        Assert.Empty(await db.StudentTestXpBalances.AsNoTracking().ToListAsync());
    }

    [Fact]
    public async Task Completion_awards_snapshot_based_xp_once_and_result_replays_persisted_breakdown()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var difficulties = Enumerable.Repeat(DifficultyLevel.Easy, 4)
            .Concat(Enumerable.Repeat(DifficultyLevel.Medium, 3))
            .Concat(Enumerable.Repeat(DifficultyLevel.Hard, 2))
            .Concat(Enumerable.Repeat(DifficultyLevel.Easy, 6))
            .ToList();
        var questions = difficulties.Select(x => CreateActiveQuestion(QuestionType.SingleChoice, subjectId, x)).ToList();
        db.Questions.AddRange(questions);
        await db.SaveChangesAsync();
        var service = CreateService(db, new MutableClock(DateTimeOffset.UtcNow));
        var attempt = await service.StartSubjectAsync(userId, new(subjectId, 15, false), SupportedLanguage.Tajik, default);
        var answers = questions.Take(9).Select(question =>
            new SubmitAnswerDto(question.Id, SelectedOptionId: question.AnswerOptions.Single(x => x.IsCorrect).Id)).ToList();

        var submitted = await service.SubmitAsync(userId, attempt.Value!.Id, new(answers), default);
        var replayed = await service.GetResultAsync(userId, attempt.Value.Id, default);
        var duplicate = await service.SubmitAsync(userId, attempt.Value.Id, new(answers), default);

        Assert.True(submitted.IsSuccess);
        Assert.Equal(4, submitted.Value!.EasyCorrect);
        Assert.Equal(3, submitted.Value.MediumCorrect);
        Assert.Equal(2, submitted.Value.HardCorrect);
        Assert.Equal(17m, submitted.Value.AnswerXp);
        Assert.Equal(5m, submitted.Value.CompletionBonusXp);
        Assert.Equal(22m, submitted.Value.TotalXp);
        Assert.True(submitted.Value.XpAwarded);
        Assert.Equal(submitted.Value.TotalXp, replayed.Value!.TotalXp);
        Assert.Equal("test.attempt_already_submitted", duplicate.Error?.Code);
        Assert.Equal(44, (await db.TestXpRewards.AsNoTracking().SingleAsync()).TotalXpUnits);
        Assert.Equal(44, (await db.StudentTestXpBalances.AsNoTracking().SingleAsync()).TotalXpUnits);
    }

    [Fact]
    public async Task Attempt_scores_from_snapshot_after_source_question_is_edited()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var questions = Enumerable.Range(0, 15).Select(_ => CreateActiveQuestion(QuestionType.SingleChoice, subjectId)).ToList();
        db.Questions.AddRange(questions);
        await db.SaveChangesAsync();
        var service = CreateService(db, new MutableClock(DateTimeOffset.UtcNow));
        var attempt = await service.StartSubjectAsync(userId, new(subjectId, 15, false), SupportedLanguage.Tajik, default);
        var targetId = attempt.Value!.Questions[0].Id;
        var target = questions.Single(x => x.Id == targetId);
        var originalCorrect = target.AnswerOptions.Single(x => x.IsCorrect);
        var originalWrong = target.AnswerOptions.First(x => !x.IsCorrect);
        originalCorrect.Update(false);
        originalWrong.Update(true);
        target.Update(subjectId, target.TopicId, null, target.Type, DifficultyLevel.Hard,
            QuestionStatus.Active, null, DateTimeOffset.UtcNow);
        await db.SaveChangesAsync();

        var result = await service.SubmitAsync(userId, attempt.Value.Id,
            new([new(targetId, SelectedOptionId: originalCorrect.Id)]), default);

        Assert.True(result.Value!.Answers.Single(x => x.QuestionId == targetId).IsCorrect);
        Assert.Equal(1, result.Value.EasyCorrect);
        Assert.Equal(0, result.Value.HardCorrect);
        Assert.Equal(6.5m, result.Value.TotalXp);
    }

    [Fact]
    public async Task Attempt_enforces_ownership_and_single_submission()
    {
        await using var db = CreateDb();
        var ownerId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var questions = Enumerable.Range(0, 15).Select(_ => CreateActiveQuestion(QuestionType.SingleChoice, subjectId)).ToList();
        db.Questions.AddRange(questions);
        await db.SaveChangesAsync();
        var service = CreateService(db, new MutableClock(DateTimeOffset.UtcNow));
        var attempt = await service.StartSubjectAsync(ownerId, new(subjectId, 15, false), SupportedLanguage.Tajik, default);

        var foreign = await service.SubmitAsync(Guid.NewGuid(), attempt.Value!.Id, new([]), default);
        var first = await service.SubmitAsync(ownerId, attempt.Value.Id, new([]), default);
        var duplicate = await service.SubmitAsync(ownerId, attempt.Value.Id, new([]), default);

        Assert.Equal("test.attempt_not_found", foreign.Error?.Code);
        Assert.True(first.IsSuccess);
        Assert.Equal("test.attempt_already_submitted", duplicate.Error?.Code);
    }

    [Fact]
    public async Task Expired_attempt_is_auto_submitted()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var questions = Enumerable.Range(0, 15).Select(_ => CreateActiveQuestion(QuestionType.SingleChoice, subjectId)).ToList();
        db.Questions.AddRange(questions);
        await db.SaveChangesAsync();
        var clock = new MutableClock(new DateTimeOffset(2026, 7, 16, 8, 0, 0, TimeSpan.Zero));
        var service = CreateService(db, clock);
        var attempt = await service.StartSubjectAsync(userId, new(subjectId, 15, false), SupportedLanguage.Tajik, default);
        clock.UtcNow = attempt.Value!.ExpiresAtUtc.AddSeconds(1);

        var first = questions[0];
        var result = await service.SubmitAsync(userId, attempt.Value.Id,
            new([new(first.Id, SelectedOptionId: first.AnswerOptions.Single(x => x.IsCorrect).Id)]), default);

        Assert.True(result.IsSuccess);
        Assert.Equal((int)TestAttemptStatus.AutoSubmitted, result.Value!.Status);
        Assert.Equal(6.5m, result.Value.TotalXp);
        Assert.True(result.Value.XpAwarded);
    }

    [Fact]
    public async Task Background_finalizer_auto_submits_without_a_user_request()
    {
        var databaseName = Guid.NewGuid().ToString();
        var clock = new MutableClock(new DateTimeOffset(2026, 7, 16, 8, 0, 0, TimeSpan.Zero));
        var testingOptions = Options.Create(new StudentTestingOptions());
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IDateTimeProvider>(clock);
        services.AddSingleton<IOptions<StudentTestingOptions>>(testingOptions);
        services.AddSingleton<ITestXpPolicy>(new TestXpPolicy(Options.Create(new TestXpRewardOptions())));
        services.AddDbContext<QuestionBankDbContext>(builder => builder.UseInMemoryDatabase(databaseName));
        services.AddSingleton<ITestingRandomizer, StableRandomizer>();
        services.AddScoped<IQuestionPickerService, QuestionPickerService>();
        services.AddScoped<IAnswerEvaluationService>(_ => new AnswerEvaluationService(
            [new SingleChoiceAnswerEvaluator(), new ClosedAnswerEvaluator(), new MatchingAnswerEvaluator()]));
        services.AddScoped<RedListService>();
        services.AddScoped<IStudentMmtTestingContext>(_ => new FakeMmtContext(null));
        services.AddScoped<IMonthlyExamAvailabilityService, MonthlyExamAvailabilityService>();
        services.AddScoped<ISubjectTestTimingPolicy, FakeTimingPolicy>();
        services.AddScoped<StudentTestingService>();
        await using var provider = services.BuildServiceProvider();

        Guid attemptId;
        using (var setupScope = provider.CreateScope())
        {
            var db = setupScope.ServiceProvider.GetRequiredService<QuestionBankDbContext>();
            var subjectId = Guid.NewGuid();
            db.Questions.AddRange(Enumerable.Range(0, 15)
                .Select(_ => CreateActiveQuestion(QuestionType.SingleChoice, subjectId)));
            await db.SaveChangesAsync();
            var service = setupScope.ServiceProvider.GetRequiredService<StudentTestingService>();
            var attempt = await service.StartSubjectAsync(Guid.NewGuid(), new(subjectId, 15, false),
                SupportedLanguage.Tajik, default);
            attemptId = attempt.Value!.Id;
            clock.UtcNow = attempt.Value.ExpiresAtUtc.AddSeconds(1);
        }

        var finalizer = new ExpiredTestAttemptFinalizer(
            provider.GetRequiredService<IServiceScopeFactory>(),
            clock,
            testingOptions,
            provider.GetRequiredService<ILogger<ExpiredTestAttemptFinalizer>>());
        var finalized = await finalizer.FinalizeBatchAsync(default);

        using var assertionScope = provider.CreateScope();
        var assertionDb = assertionScope.ServiceProvider.GetRequiredService<QuestionBankDbContext>();
        var persisted = await assertionDb.TestAttempts.AsNoTracking().SingleAsync(x => x.Id == attemptId);
        Assert.Equal(1, finalized);
        Assert.Equal(TestAttemptStatus.AutoSubmitted, persisted.Status);
        Assert.NotNull(persisted.SubmittedAtUtc);
    }

    [Fact]
    public async Task Mmt_strict_simulation_and_monthly_exam_disable_red_list_injection()
    {
        await using var db = CreateDb();
        var clock = new MutableClock(new DateTimeOffset(2026, 7, 16, 8, 0, 0, TimeSpan.Zero));
        var picker = new CapturingPicker();
        var context = new StudentMmtTestingContext(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], 12, 2026);
        var service = CreateService(db, clock, picker, new FakeMmtContext(context));

        var strict = await service.StartMmtPracticeAsync(Guid.NewGuid(), new(true, 25), SupportedLanguage.Tajik, default);
        var strictRequest = picker.Requests.Single();
        picker.Requests.Clear();
        var monthly = await service.StartMonthlyExamAsync(Guid.NewGuid(), SupportedLanguage.Tajik, default);
        var monthlyRequest = picker.Requests.Single();

        Assert.True(strict.IsSuccess);
        Assert.False(strictRequest.InjectRedList);
        Assert.True(monthly.IsSuccess);
        Assert.False(monthlyRequest.InjectRedList);
        Assert.Equal(TestMode.MonthlyExam, monthlyRequest.Mode);
    }

    [Fact]
    public async Task Monthly_exam_allows_one_start_per_window_and_reports_started_semantics()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        var clock = new MutableClock(new DateTimeOffset(2026, 7, 15, 20, 0, 0, TimeSpan.Zero));
        var picker = new CapturingPicker();
        var context = new StudentMmtTestingContext(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], 12, 2026);
        var service = CreateService(db, clock, picker, new FakeMmtContext(context));

        var first = await service.StartMonthlyExamAsync(userId, SupportedLanguage.Tajik, default);
        var duplicate = await service.StartMonthlyExamAsync(userId, SupportedLanguage.Tajik, default);

        Assert.True(first.IsSuccess);
        Assert.Equal("monthly_exam.already_started", duplicate.Error?.Code);
    }

    [Fact]
    public async Task Matching_display_pool_is_shuffled_once_without_exposing_the_mapping()
    {
        await using var db = CreateDb();
        var subjectId = Guid.NewGuid();
        var matching = CreateActiveQuestion(QuestionType.Matching, subjectId);
        db.Questions.Add(matching);
        db.Questions.AddRange(Enumerable.Range(0, 14)
            .Select(_ => CreateActiveQuestion(QuestionType.SingleChoice, subjectId)));
        await db.SaveChangesAsync();
        var service = CreateService(db, new MutableClock(DateTimeOffset.UtcNow), randomizer: new ReverseRandomizer());

        var attempt = await service.StartSubjectAsync(Guid.NewGuid(), new(subjectId, 15, false),
            SupportedLanguage.Tajik, default);
        var presented = attempt.Value!.Questions.Single(x => x.Id == matching.Id);
        var canonical = matching.AnswerOptions.OrderBy(x => x.DisplayOrder)
            .Select(x => x.Translations.Single().MatchPairText).ToList();
        var secondRead = await service.GetAttemptAsync(
            (await db.TestAttempts.AsNoTracking().SingleAsync()).UserId, attempt.Value.Id, default);

        Assert.Equal(canonical.AsEnumerable().Reverse(), presented.MatchingOptions);
        Assert.Equal(presented.MatchingOptions, secondRead.Value!.Questions.Single(x => x.Id == matching.Id).MatchingOptions);
        Assert.All(presented.Options, option => Assert.DoesNotContain(option.Text, presented.MatchingOptions));
    }

    [Fact]
    public async Task Subject_red_list_injection_never_crosses_subject_boundary()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        var selectedSubject = Guid.NewGuid();
        var otherSubject = Guid.NewGuid();
        var selectedQuestions = Enumerable.Range(0, 15)
            .Select(_ => CreateActiveQuestion(QuestionType.SingleChoice, selectedSubject)).ToList();
        var foreignQuestions = Enumerable.Range(0, 4)
            .Select(_ => CreateActiveQuestion(QuestionType.SingleChoice, otherSubject)).ToList();
        db.Questions.AddRange(selectedQuestions.Concat(foreignQuestions));
        foreach (var question in selectedQuestions.Take(4).Concat(foreignQuestions))
            db.StudentRedListItems.Add(new(Guid.NewGuid(), userId, question.Id, question.SubjectId,
                question.TopicId, question.Type, DateTimeOffset.UtcNow));
        await db.SaveChangesAsync();
        var picker = new QuestionPickerService(db, new StableRandomizer());

        var result = await picker.PickAsync(new(userId, TestMode.SubjectTest, 15, selectedSubject, null, true), default);

        Assert.True(result.IsSuccess);
        Assert.All(result.Value!, x => Assert.Equal(selectedSubject, x.SubjectId));
        Assert.True(result.Value!.Count(x => selectedQuestions.Take(4).Select(q => q.Id).Contains(x.Id)) >= 3);
    }

    private static StudentTestingService CreateService(
        QuestionBankDbContext db,
        MutableClock clock,
        IQuestionPickerService? picker = null,
        IStudentMmtTestingContext? mmtContext = null,
        ITestingRandomizer? randomizer = null) => new(
            db,
            picker ?? new QuestionPickerService(db, randomizer ?? new StableRandomizer()),
            new AnswerEvaluationService([new SingleChoiceAnswerEvaluator(), new ClosedAnswerEvaluator(), new MatchingAnswerEvaluator()]),
            new RedListService(db, clock),
            mmtContext ?? new FakeMmtContext(null),
            new MonthlyExamAvailabilityService(clock, Options.Create(new StudentTestingOptions())),
            new FakeTimingPolicy(),
            clock,
            Options.Create(new StudentTestingOptions()),
            randomizer ?? new StableRandomizer(),
            new TestXpPolicy(Options.Create(new TestXpRewardOptions())),
            NullLogger<StudentTestingService>.Instance);

    private static QuestionBankDbContext CreateDb() => new(new DbContextOptionsBuilder<QuestionBankDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static Question CreateActiveQuestion(QuestionType type, Guid subjectId,
        DifficultyLevel difficulty = DifficultyLevel.Easy)
    {
        var now = DateTimeOffset.UtcNow;
        var question = new Question(Guid.NewGuid(), subjectId, Guid.NewGuid(), null, type, difficulty, null, now);
        var count = type == QuestionType.Matching ? 4 : 2;
        var options = Enumerable.Range(1, count).Select(index =>
        {
            var option = new AnswerOption(Guid.NewGuid(), question.Id, index, index == 1);
            option.ReplaceTranslations([new AnswerOptionTranslation(option.Id, SupportedLanguage.Tajik,
                $"Option {index}", type == QuestionType.Matching ? $"Pair {index}" : null)]);
            return option;
        }).ToList();
        question.ReplaceContent([new QuestionTranslation(question.Id, SupportedLanguage.Tajik, "Question", "Explanation")], options);
        question.Update(subjectId, question.TopicId, null, type, difficulty, QuestionStatus.Active, null, now);
        return question;
    }

    private sealed class StableRandomizer : ITestingRandomizer
    {
        public void Shuffle<T>(IList<T> values) { }
    }

    private sealed class ReverseRandomizer : ITestingRandomizer
    {
        public void Shuffle<T>(IList<T> values)
        {
            for (var left = 0; left < values.Count / 2; left++)
            {
                var right = values.Count - left - 1;
                (values[left], values[right]) = (values[right], values[left]);
            }
        }
    }

    private sealed class FakeTimingPolicy : ISubjectTestTimingPolicy
    {
        public Task<int> DurationMinutesAsync(Guid subjectId, int questionCount, SupportedLanguage language,
            CancellationToken ct) => Task.FromResult(questionCount);
    }

    private sealed class MutableClock(DateTimeOffset now) : IDateTimeProvider
    {
        public DateTimeOffset UtcNow { get; set; } = now;
        public DateTimeOffset DushanbeNow => UtcNow.ToOffset(TimeSpan.FromHours(5));
        public DateTimeOffset ToDushanbeTime(DateTimeOffset value) => value.ToOffset(TimeSpan.FromHours(5));
    }

    private sealed class FakeMmtContext(StudentMmtTestingContext? context) : IStudentMmtTestingContext
    {
        public Task<StudentMmtTestingContext?> GetAsync(Guid userId, CancellationToken ct) => Task.FromResult(context);
    }

    private sealed class CapturingPicker : IQuestionPickerService
    {
        public List<QuestionPickerRequest> Requests { get; } = [];
        public Task<Result<IReadOnlyList<Question>>> PickAsync(QuestionPickerRequest request, CancellationToken ct)
        {
            Requests.Add(request);
            return Task.FromResult(Result<IReadOnlyList<Question>>.Success([]));
        }
    }
}
