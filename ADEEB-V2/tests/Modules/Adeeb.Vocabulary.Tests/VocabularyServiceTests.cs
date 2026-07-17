using System.Security.Claims;
using Adeeb.Application.Abstractions.Time;
using Adeeb.Modules.Students.Contracts;
using Adeeb.Modules.Vocabulary.Application;
using Adeeb.Modules.Vocabulary.Contracts;
using Adeeb.Modules.Vocabulary.Domain;
using Adeeb.Modules.Vocabulary.Infrastructure.Persistence;
using Adeeb.Infrastructure.Localization;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Adeeb.Vocabulary.Tests;

public sealed class VocabularyServiceTests
{
    [Fact]
    public async Task Daily_session_has_ten_questions_and_resumes_without_leaking_answers()
    {
        await using var db = Database(); var fixture = Seed(db, 10); await db.SaveChangesAsync();
        var service = new VocabularyStudentService(db, new StudentLookup(fixture.StudentId, fixture.IdentityId), new Clock()); var principal = Principal(fixture.IdentityId);
        var first = await service.StartSessionAsync(principal, new((int)VocabularySessionMode.DailyPractice), default);
        var second = await service.StartSessionAsync(principal, new((int)VocabularySessionMode.DailyPractice), default);
        Assert.True(first.IsSuccess); Assert.Equal(10, first.Value!.Questions.Count); Assert.Equal(first.Value.Id, second.Value!.Id);
        Assert.All(first.Value.Questions, q => Assert.Equal(4, q.Options.Count));
    }

    [Fact]
    public async Task Practice_reveals_feedback_but_test_conceals_it_until_completion()
    {
        await using var db = Database(); var fixture = Seed(db, 20); await db.SaveChangesAsync();
        var service = new VocabularyStudentService(db, new StudentLookup(fixture.StudentId, fixture.IdentityId), new Clock()); var principal = Principal(fixture.IdentityId);
        var practice = (await service.StartSessionAsync(principal, new((int)VocabularySessionMode.FreePractice, QuestionCount: 10), default)).Value!;
        var practiceAnswer = await service.SubmitAnswerAsync(principal, practice.Id, new(practice.Questions[0].Id, practice.Questions[0].Options[0].Id), default);
        Assert.NotNull(practiceAnswer.Value!.Feedback);
        var test = (await service.StartSessionAsync(principal, new((int)VocabularySessionMode.Test, QuestionCount: 10), default)).Value!;
        var testAnswer = await service.SubmitAnswerAsync(principal, test.Id, new(test.Questions[0].Id, test.Questions[0].Options[0].Id), default);
        Assert.Null(testAnswer.Value!.Feedback); Assert.Equal(0, testAnswer.Value.Session.CorrectCount);
    }

    [Fact]
    public async Task Suspended_student_cannot_start_vocabulary_session()
    {
        await using var db = Database(); var fixture = Seed(db, 10); await db.SaveChangesAsync();
        var service = new VocabularyStudentService(db, new StudentLookup(fixture.StudentId, fixture.IdentityId, "Suspended"), new Clock());
        var result = await service.StartSessionAsync(Principal(fixture.IdentityId), new((int)VocabularySessionMode.DailyPractice), default);
        Assert.True(result.IsFailure); Assert.Equal("vocabulary.student_unavailable", result.Error!.Code);
    }

    [Fact]
    public async Task Generator_builds_all_six_reviewable_question_types()
    {
        await using var db = Database(); var fixture = Seed(db, 6);
        var firstQuestion = db.Questions.Local.First(x => x.WordId == fixture.WordIds[0]); db.Questions.Remove(firstQuestion);
        db.Relations.Add(new VocabularyRelation(Guid.NewGuid(), fixture.WordIds[0], fixture.WordIds[1], VocabularyRelationType.Synonym, DateTimeOffset.UtcNow));
        db.Relations.Add(new VocabularyRelation(Guid.NewGuid(), fixture.WordIds[0], fixture.WordIds[2], VocabularyRelationType.Antonym, DateTimeOffset.UtcNow));
        await db.SaveChangesAsync();
        var result = await new VocabularyAdminService(db, new Clock()).GenerateDraftsAsync(fixture.WordIds[0], default);
        Assert.True(result.IsSuccess); Assert.Equal(6, result.Value!.Created.Count); Assert.Empty(result.Value.Warnings);
        Assert.Equal(Enum.GetValues<VocabularyQuestionType>().Select(x => (int)x).Order(), result.Value.Created.Select(x => x.Type).Order());
    }

    [Fact]
    public async Task Automatic_daily_word_uses_student_timezone_date_and_is_persisted()
    {
        await using var db = Database(); var fixture = Seed(db, 1); await db.SaveChangesAsync();
        var clock = new Clock(new DateTimeOffset(2026, 7, 16, 20, 30, 0, TimeSpan.Zero));
        var service = new VocabularyStudentService(db, new StudentLookup(fixture.StudentId, fixture.IdentityId), clock);
        var result = await service.GetTodayAsync(Principal(fixture.IdentityId), default);
        Assert.True(result.IsSuccess); Assert.Equal(new DateOnly(2026, 7, 17), result.Value!.LocalDate); Assert.True(result.Value.IsAutomatic);
        Assert.Single(db.DailyWords);
    }

    [Theory]
    [InlineData("tg-TJ")]
    [InlineData("ru-RU")]
    public void Vocabulary_errors_are_localized(string culture)
    {
        var previous = CultureInfo.CurrentUICulture;
        try { CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture); Assert.NotEqual("Vocabulary.CourseRequired", new StaticMessageLocalizer()["Vocabulary.CourseRequired"]); }
        finally { CultureInfo.CurrentUICulture = previous; }
    }

    private static (Guid StudentId, Guid IdentityId, IReadOnlyList<Guid> WordIds) Seed(VocabularyDbContext db, int count)
    {
        var now = DateTimeOffset.UtcNow; var language = new LearningLanguage(Guid.NewGuid(), "en", "Англисӣ", "Английский", 1, now); db.Languages.Add(language);
        var topic = new VocabularyTopic(Guid.NewGuid(), language.Id, VocabularyLevel.A1, "Асосӣ", "Основы", null, null, now); topic.Update(language.Id, VocabularyLevel.A1, "Асосӣ", "Основы", null, null, VocabularyContentStatus.Published, now); db.Topics.Add(topic);
        var wordIds = new List<Guid>();
        for (var index = 0; index < count; index++)
        {
            var word = new VocabularyWord(Guid.NewGuid(), language.Id, topic.Id, VocabularyLevel.A1, $"word{index}", $"калима{index}", $"слово{index}", null, null, $"word{index} example", $"мисол {index}", $"пример {index}", now); word.Update(language.Id, topic.Id, VocabularyLevel.A1, word.TargetText, word.TranslationTg, word.TranslationRu, null, null, word.ExampleTarget, word.ExampleTg, word.ExampleRu, VocabularyContentStatus.Published, now); db.Words.Add(word);
            wordIds.Add(word.Id);
            var question = new VocabularyQuestion(Guid.NewGuid(), word.Id, VocabularyQuestionType.Translation, word.TargetText, word.TargetText, word.TargetText, null, now);
            var options = Enumerable.Range(0, 4).Select(option => new VocabularyQuestionOption(Guid.NewGuid(), question.Id, null, $"target{option}", $"tg{option}", $"ru{option}", option, option == 0, null)).ToList(); question.Replace(word.TargetText, word.TargetText, word.TargetText, null, options, now); question.Publish(Guid.NewGuid(), now); db.Questions.Add(question);
        }
        var studentId = Guid.NewGuid(); var identityId = Guid.NewGuid(); db.Courses.Add(new StudentVocabularyCourse(studentId, language.Id, VocabularyLevel.A1, now)); return (studentId, identityId, wordIds);
    }
    private static VocabularyDbContext Database() => new(new DbContextOptionsBuilder<VocabularyDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static ClaimsPrincipal Principal(Guid id) => new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, id.ToString())], "test"));
    private sealed class StudentLookup(Guid studentId, Guid identityId, string status = "Active") : IStudentLookup
    {
        private readonly StudentReference _student = new(studentId, identityId, status, "Asia/Dushanbe");
        public Task<StudentReference?> FindByIdentityUserIdAsync(Guid id, CancellationToken ct) => Task.FromResult<StudentReference?>(id == identityId ? _student : null);
        public Task<StudentReference?> FindByStudentIdAsync(Guid id, CancellationToken ct) => Task.FromResult<StudentReference?>(id == studentId ? _student : null);
    }
    private sealed class Clock(DateTimeOffset? now = null) : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => now ?? new DateTimeOffset(2026, 7, 17, 6, 0, 0, TimeSpan.Zero);
        public DateTimeOffset DushanbeNow => UtcNow.ToOffset(TimeSpan.FromHours(5));
        public DateTimeOffset ToDushanbeTime(DateTimeOffset value) => value.ToOffset(TimeSpan.FromHours(5));
    }
}
