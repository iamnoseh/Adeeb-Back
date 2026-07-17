using System.Security.Claims;
using Adeeb.Application.Abstractions.Time;
using Adeeb.Modules.Students.Contracts;
using Adeeb.Modules.Vocabulary.Application;
using Adeeb.Modules.Vocabulary.Contracts;
using Adeeb.Modules.Vocabulary.Domain;
using Adeeb.Modules.Vocabulary.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Adeeb.IntegrationTests;

public sealed class VocabularyConcurrencyIntegrationScenarios(AdeebApiFactory factory) : IClassFixture<AdeebApiFactory>, IAsyncLifetime
{
    private readonly Guid _studentId = Guid.NewGuid();
    private readonly Guid _identityId = Guid.NewGuid();

    public async Task InitializeAsync()
    {
        using var client = factory.CreateClient(); await factory.ResetDatabaseAsync();
        await using var db = CreateDb(); await db.Database.MigrateAsync(); Seed(db); await db.SaveChangesAsync();
    }
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Concurrent_daily_start_and_answer_create_one_session_daily_word_and_answer()
    {
        await using var firstDb = CreateDb(); await using var secondDb = CreateDb();
        var first = Service(firstDb); var second = Service(secondDb); var principal = Principal(); var request = new StartVocabularySessionRequest((int)VocabularySessionMode.DailyPractice);
        var starts = await Task.WhenAll(first.StartSessionAsync(principal, request, default), second.StartSessionAsync(principal, request, default));
        Assert.All(starts, x => Assert.True(x.IsSuccess, x.Error?.Code)); var session = starts[0].Value!; Assert.Equal(session.Id, starts[1].Value!.Id);
        var question = session.Questions[0];
        await using var thirdDb = CreateDb(); await using var fourthDb = CreateDb();
        var answers = await Task.WhenAll(
            Service(thirdDb).SubmitAnswerAsync(principal, session.Id, new(question.Id, question.Options[0].Id), default),
            Service(fourthDb).SubmitAnswerAsync(principal, session.Id, new(question.Id, question.Options[1].Id), default));
        Assert.All(answers, x => Assert.True(x.IsSuccess, x.Error?.Code));
        await using var verify = CreateDb(); Assert.Equal(1, await verify.Sessions.CountAsync()); Assert.Equal(10, await verify.SessionQuestions.CountAsync()); Assert.Equal(1, await verify.DailyWords.CountAsync()); Assert.Equal(1, await verify.SessionAnswers.CountAsync());
    }

    private VocabularyStudentService Service(VocabularyDbContext db) => new(db, new Lookup(_studentId, _identityId), new FixedClock());
    private VocabularyDbContext CreateDb() => new(new DbContextOptionsBuilder<VocabularyDbContext>().UseNpgsql(factory.ConnectionString).Options);
    private ClaimsPrincipal Principal() => new(new ClaimsIdentity([new Claim("sub", _identityId.ToString())], "Test"));
    private void Seed(VocabularyDbContext db)
    {
        var now = FixedClock.Now; var language = new LearningLanguage(Guid.NewGuid(), "en", "Англисӣ", "Английский", 1, now); db.Languages.Add(language);
        var topic = new VocabularyTopic(Guid.NewGuid(), language.Id, VocabularyLevel.A1, "Асосӣ", "Основы", null, null, now); topic.Update(language.Id, VocabularyLevel.A1, "Асосӣ", "Основы", null, null, VocabularyContentStatus.Published, now); db.Topics.Add(topic);
        for (var index = 0; index < 10; index++)
        {
            var word = new VocabularyWord(Guid.NewGuid(), language.Id, topic.Id, VocabularyLevel.A1, $"word{index}", $"калима{index}", $"слово{index}", null, null, $"word{index} example", $"мисол {index}", $"пример {index}", now); word.Update(language.Id, topic.Id, VocabularyLevel.A1, word.TargetText, word.TranslationTg, word.TranslationRu, null, null, word.ExampleTarget, word.ExampleTg, word.ExampleRu, VocabularyContentStatus.Published, now); db.Words.Add(word);
            var question = new VocabularyQuestion(Guid.NewGuid(), word.Id, VocabularyQuestionType.Translation, word.TargetText, word.TargetText, word.TargetText, null, now); var options = Enumerable.Range(0, 4).Select(option => new VocabularyQuestionOption(Guid.NewGuid(), question.Id, null, $"target{option}", $"tg{option}", $"ru{option}", option, option == 0, null)); question.Replace(word.TargetText, word.TargetText, word.TargetText, null, options, now); question.Publish(Guid.NewGuid(), now); db.Questions.Add(question);
        }
        db.Courses.Add(new StudentVocabularyCourse(_studentId, language.Id, VocabularyLevel.A1, now));
    }
    private sealed class Lookup(Guid studentId, Guid identityId) : IStudentLookup
    {
        private readonly StudentReference _student = new(studentId, identityId, "Active", "Asia/Dushanbe");
        public Task<StudentReference?> FindByIdentityUserIdAsync(Guid id, CancellationToken ct) => Task.FromResult<StudentReference?>(id == identityId ? _student : null);
        public Task<StudentReference?> FindByStudentIdAsync(Guid id, CancellationToken ct) => Task.FromResult<StudentReference?>(id == studentId ? _student : null);
    }
    private sealed class FixedClock : IDateTimeProvider
    {
        public static readonly DateTimeOffset Now = new(2026, 7, 17, 6, 0, 0, TimeSpan.Zero);
        public DateTimeOffset UtcNow => Now; public DateTimeOffset DushanbeNow => Now.ToOffset(TimeSpan.FromHours(5)); public DateTimeOffset ToDushanbeTime(DateTimeOffset value) => value.ToOffset(TimeSpan.FromHours(5));
    }
}
