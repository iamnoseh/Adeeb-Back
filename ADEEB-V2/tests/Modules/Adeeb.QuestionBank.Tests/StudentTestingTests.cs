using Adeeb.Application.Abstractions.AcademicCatalog;
using Adeeb.Application.Abstractions.Localization;
using Adeeb.Application.Abstractions.Time;
using Adeeb.Modules.QuestionBank.Application;
using Adeeb.Modules.QuestionBank.Contracts;
using Adeeb.Modules.QuestionBank.Domain;
using Adeeb.Modules.QuestionBank.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Adeeb.QuestionBank.Tests;

public sealed class StudentTestingTests
{
    [Theory]
    [InlineData(2026, 6, 30, 18, 59, false, null)]
    [InlineData(2026, 6, 30, 19, 0, true, "2026-07-01")]
    [InlineData(2026, 7, 1, 18, 59, true, "2026-07-01")]
    [InlineData(2026, 7, 1, 19, 0, false, null)]
    [InlineData(2026, 7, 15, 19, 0, true, "2026-07-16")]
    [InlineData(2026, 7, 16, 19, 0, false, null)]
    public void Monthly_exam_opens_only_inside_first_and_sixteenth_windows(
        int year, int month, int day, int hour, int minute, bool expectedOpen, string? expectedKey)
    {
        var clock = new FakeClock(new DateTimeOffset(year, month, day, hour, minute, 0, TimeSpan.Zero));
        var service = new MonthlyExamAvailabilityService(clock, Options.Create(new StudentTestingOptions
        {
            MonthlyExamWindowHours = 24
        }));

        var availability = service.Current();

        Assert.Equal(expectedOpen, availability.IsOpen);
        Assert.Equal(expectedKey, availability.WindowKey);
        if (expectedOpen)
        {
            Assert.Equal(TimeSpan.Zero, availability.OpensAtUtc!.Value.Offset);
            Assert.Equal(19, availability.OpensAtUtc.Value.Hour);
        }
    }

    [Theory]
    [InlineData("MATH", 40)]
    [InlineData("physics", 40)]
    [InlineData("HISTORY", 20)]
    public async Task Subject_timing_uses_configured_subject_code_policy(string code, int expectedMinutes)
    {
        var subjectId = Guid.NewGuid();
        var policy = new SubjectTestTimingPolicy(new FakeSubjectLookup(subjectId, code),
            Options.Create(new StudentTestingOptions()));

        var duration = await policy.DurationMinutesAsync(subjectId, 20, SupportedLanguage.Tajik, default);

        Assert.Equal(expectedMinutes, duration);
    }

    [Fact]
    public void Red_list_item_is_mastered_after_three_correct_answers_and_wrong_answer_reactivates_it()
    {
        var now = new DateTimeOffset(2026, 7, 16, 10, 0, 0, TimeSpan.Zero);
        var item = new StudentRedListItem(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), QuestionType.SingleChoice, now);

        item.RecordCorrect(now.AddMinutes(1));
        item.RecordCorrect(now.AddMinutes(2));
        item.RecordCorrect(now.AddMinutes(3));

        Assert.Equal(RedListStatus.Mastered, item.Status);
        Assert.Equal(3, item.CorrectStreak);
        Assert.NotNull(item.MasteredAtUtc);

        item.RecordWrong(now.AddMinutes(4));

        Assert.Equal(RedListStatus.Active, item.Status);
        Assert.Equal(0, item.CorrectStreak);
        Assert.Equal(2, item.WrongCount);
        Assert.Null(item.MasteredAtUtc);
    }

    [Fact]
    public async Task Red_list_practice_requires_twenty_eligible_non_matching_questions()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        for (var index = 0; index < 20; index++)
        {
            var type = index == 19 ? QuestionType.Matching : QuestionType.SingleChoice;
            var question = CreateActiveQuestion(type, Guid.NewGuid(), Guid.NewGuid());
            db.Questions.Add(question);
            db.StudentRedListItems.Add(new StudentRedListItem(Guid.NewGuid(), userId, question.Id,
                question.SubjectId, question.TopicId, type, DateTimeOffset.UtcNow));
        }
        await db.SaveChangesAsync();
        var picker = new QuestionPickerService(db, new StableRandomizer());

        var result = await picker.PickAsync(new QuestionPickerRequest(userId, TestMode.RedListPractice, 20), default);

        Assert.True(result.IsFailure);
        Assert.Equal("redlist.not_enough_questions", result.Error?.Code);
    }

    [Fact]
    public async Task Red_list_practice_starts_with_twenty_eligible_questions()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        for (var index = 0; index < 20; index++)
        {
            var question = CreateActiveQuestion(QuestionType.SingleChoice, Guid.NewGuid(), Guid.NewGuid());
            db.Questions.Add(question);
            db.StudentRedListItems.Add(new StudentRedListItem(Guid.NewGuid(), userId, question.Id,
                question.SubjectId, question.TopicId, question.Type, DateTimeOffset.UtcNow));
        }
        await db.SaveChangesAsync();
        var picker = new QuestionPickerService(db, new StableRandomizer());

        var result = await picker.PickAsync(new QuestionPickerRequest(userId, TestMode.RedListPractice, 20), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(20, result.Value!.Count);
        Assert.All(result.Value, x => Assert.Equal(QuestionType.SingleChoice, x.Type));
    }

    [Fact]
    public async Task Monthly_picker_balances_questions_across_cluster_subjects()
    {
        await using var db = CreateDb();
        var firstSubject = Guid.NewGuid();
        var secondSubject = Guid.NewGuid();
        db.Questions.AddRange(Enumerable.Range(0, 10)
            .Select(_ => CreateActiveQuestion(QuestionType.SingleChoice, firstSubject, Guid.NewGuid())));
        db.Questions.AddRange(Enumerable.Range(0, 10)
            .Select(_ => CreateActiveQuestion(QuestionType.SingleChoice, secondSubject, Guid.NewGuid())));
        await db.SaveChangesAsync();
        var picker = new QuestionPickerService(db, new StableRandomizer());

        var result = await picker.PickAsync(new QuestionPickerRequest(Guid.NewGuid(), TestMode.MonthlyExam, 10,
            ClusterSubjectIds: [firstSubject, secondSubject]), default);

        Assert.True(result.IsSuccess);
        var counts = result.Value!.GroupBy(x => x.SubjectId).Select(x => x.Count()).OrderBy(x => x).ToArray();
        Assert.Equal([5, 5], counts);
    }

    [Fact]
    public async Task Red_list_updates_are_loaded_and_applied_as_a_batch()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        var clock = new FakeClock(DateTimeOffset.UtcNow);
        var service = new RedListService(db, clock);
        var updates = Enumerable.Range(0, 25).Select(_ => new RedListService.AnswerUpdate(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), QuestionType.ClosedAnswer, false)).ToList();

        await service.ApplyAnswersAsync(userId, updates, default);
        await db.SaveChangesAsync();

        Assert.Equal(25, await db.StudentRedListItems.CountAsync(x => x.UserId == userId));
        Assert.All(await db.StudentRedListItems.ToListAsync(), x => Assert.Equal(RedListStatus.Active, x.Status));
    }

    [Fact]
    public void Attempt_contract_does_not_expose_correct_answers_before_submission()
    {
        var questionProperties = typeof(TestQuestionDto).GetProperties().Select(x => x.Name).ToHashSet();
        var optionProperties = typeof(TestAnswerOptionDto).GetProperties().Select(x => x.Name).ToHashSet();

        Assert.DoesNotContain("IsCorrect", questionProperties);
        Assert.DoesNotContain("CorrectAnswer", questionProperties);
        Assert.DoesNotContain("MatchingRightOptions", questionProperties);
        Assert.Contains("MatchingOptions", questionProperties);
        Assert.DoesNotContain("IsCorrect", optionProperties);
        Assert.DoesNotContain("MatchPairText", optionProperties);
    }

    private static QuestionBankDbContext CreateDb() => new(new DbContextOptionsBuilder<QuestionBankDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static Question CreateActiveQuestion(QuestionType type, Guid subjectId, Guid topicId)
    {
        var now = DateTimeOffset.UtcNow;
        var question = new Question(Guid.NewGuid(), subjectId, topicId, null, type, DifficultyLevel.Easy, null, now);
        var options = new List<AnswerOption>();
        for (var index = 1; index <= (type == QuestionType.Matching ? 4 : 2); index++)
        {
            var option = new AnswerOption(Guid.NewGuid(), question.Id, index, index == 1);
            option.ReplaceTranslations([new AnswerOptionTranslation(option.Id, SupportedLanguage.Tajik,
                $"Option {index}", type == QuestionType.Matching ? $"Pair {index}" : null)]);
            options.Add(option);
        }
        question.ReplaceContent([new QuestionTranslation(question.Id, SupportedLanguage.Tajik, "Question", null)], options);
        question.Update(subjectId, topicId, null, type, DifficultyLevel.Easy, QuestionStatus.Active, null, now);
        return question;
    }

    private sealed class StableRandomizer : ITestingRandomizer
    {
        public void Shuffle<T>(IList<T> values) { }
    }

    private sealed class FakeClock(DateTimeOffset now) : IDateTimeProvider
    {
        public DateTimeOffset UtcNow { get; } = now;
        public DateTimeOffset DushanbeNow => UtcNow.ToOffset(TimeSpan.FromHours(5));
        public DateTimeOffset ToDushanbeTime(DateTimeOffset value) => value.ToOffset(TimeSpan.FromHours(5));
    }

    private sealed class FakeSubjectLookup(Guid subjectId, string code) : IAcademicSubjectLookup
    {
        public Task<IReadOnlyList<AcademicSubjectLookupItem>> GetActiveSubjectsAsync(
            IReadOnlyCollection<Guid> subjectIds,
            SupportedLanguage language,
            CancellationToken ct) => Task.FromResult<IReadOnlyList<AcademicSubjectLookupItem>>(
                subjectIds.Contains(subjectId) ? [new(subjectId, code, code)] : []);
    }
}
