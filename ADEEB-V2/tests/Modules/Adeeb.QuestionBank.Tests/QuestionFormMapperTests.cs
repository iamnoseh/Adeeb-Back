using System.Text.Json;
using Adeeb.Application.Abstractions.Localization;
using Adeeb.Modules.QuestionBank.Contracts;
using Adeeb.Modules.QuestionBank.Endpoints;
using Adeeb.Modules.QuestionBank.Application.Import;
using Adeeb.Modules.QuestionBank.Domain;

namespace Adeeb.QuestionBank.Tests;

public sealed class QuestionFormMapperTests
{
    [Fact]
    public void Maps_distinct_tajik_and_russian_question_and_answer_translations()
    {
        var form = new QuestionFormRequest
        {
            SubjectId = Guid.NewGuid(),
            ContentTg = "Савол ба тоҷикӣ",
            ContentRu = "Вопрос на русском",
            ExplanationTg = "Шарҳ",
            ExplanationRu = "Объяснение",
            Type = 1,
            Difficulty = 1,
            Status = 1,
            AnswersJson = JsonSerializer.Serialize(new[]
            {
                new QuestionAnswerFormRequest("Ҷавоб", "Ответ", true, null, null),
                new QuestionAnswerFormRequest("Ду", "Два", false, null, null),
                new QuestionAnswerFormRequest("Се", "Три", false, null, null),
                new QuestionAnswerFormRequest("Чор", "Четыре", false, null, null)
            })
        };

        var result = QuestionFormMapper.ToUpsertRequest(form, null);

        Assert.True(result.IsSuccess);
        var request = result.Value!;
        Assert.Equal("Савол ба тоҷикӣ", request.Translations.Single(x => x.Language == (int)SupportedLanguage.Tajik).Content);
        Assert.Equal("Вопрос на русском", request.Translations.Single(x => x.Language == (int)SupportedLanguage.Russian).Content);
        Assert.Equal("Ҷавоб", request.AnswerOptions[0].Translations.Single(x => x.Language == (int)SupportedLanguage.Tajik).Text);
        Assert.Equal("Ответ", request.AnswerOptions[0].Translations.Single(x => x.Language == (int)SupportedLanguage.Russian).Text);
        Assert.DoesNotContain(request.Translations, x => x.Language == (int)SupportedLanguage.English);
    }

    [Fact]
    public void Keeps_legacy_single_language_form_compatible_without_losing_required_languages()
    {
        var form = new QuestionFormRequest
        {
            SubjectId = Guid.NewGuid(),
            Content = "Legacy question",
            Type = 3,
            Difficulty = 1,
            Status = 1,
            CorrectAnswer = "Legacy answer"
        };

        var result = QuestionFormMapper.ToUpsertRequest(form, null);

        Assert.True(result.IsSuccess);
        Assert.Equal(["Legacy question", "Legacy question"], result.Value!.Translations.Select(x => x.Content));
        Assert.Equal(["Legacy answer", "Legacy answer"], result.Value.AnswerOptions[0].Translations.Select(x => x.Text));
    }

    [Fact]
    public void Maps_matching_pairs_independently_for_each_language()
    {
        var form = new QuestionFormRequest
        {
            SubjectId = Guid.NewGuid(),
            ContentTg = "Мувофиқат кунед",
            ContentRu = "Установите соответствие",
            Type = 2,
            Difficulty = 1,
            Status = 1,
            AnswersJson = JsonSerializer.Serialize(new[]
            {
                new QuestionAnswerFormRequest("Чап", "Лево", false, "Рост", "Право"),
                new QuestionAnswerFormRequest("Ду", "Два", false, "Ҷуфт 2", "Пара 2"),
                new QuestionAnswerFormRequest("Се", "Три", false, "Ҷуфт 3", "Пара 3"),
                new QuestionAnswerFormRequest("Чор", "Четыре", false, "Ҷуфт 4", "Пара 4")
            })
        };

        var result = QuestionFormMapper.ToUpsertRequest(form, null);

        Assert.True(result.IsSuccess);
        var translations = result.Value!.AnswerOptions[0].Translations;
        Assert.Equal("Рост", translations.Single(x => x.Language == (int)SupportedLanguage.Tajik).MatchPairText);
        Assert.Equal("Право", translations.Single(x => x.Language == (int)SupportedLanguage.Russian).MatchPairText);
    }

    [Fact]
    public void Question_content_selection_uses_the_requested_language()
    {
        var id = Guid.NewGuid();
        var question = new Question(id, Guid.NewGuid(), null, null, QuestionType.ClosedAnswer,
            DifficultyLevel.Easy, null, DateTimeOffset.UtcNow);
        question.ReplaceContent(
        [
            new QuestionTranslation(id, SupportedLanguage.Tajik, "Савол", null),
            new QuestionTranslation(id, SupportedLanguage.Russian, "Вопрос", null)
        ], []);

        Assert.Equal("Савол", question.ContentFor(SupportedLanguage.Tajik));
        Assert.Equal("Вопрос", question.ContentFor(SupportedLanguage.Russian));
    }

    [Fact]
    public void Import_keeps_only_the_declared_source_language_and_creates_a_draft()
    {
        var parsed = new ParsedQuestion
        {
            QuestionType = QuestionType.SingleChoice,
            QuestionText = "Русский вопрос",
            Options =
            [
                new ParsedOption("A", "Один", true),
                new ParsedOption("B", "Два", false),
                new ParsedOption("C", "Три", false),
                new ParsedOption("D", "Четыре", false)
            ]
        };

        var request = QuestionImportService.ToUpsertRequest(
            Guid.NewGuid(), null, (int)DifficultyLevel.Easy, SupportedLanguage.Russian, parsed);

        Assert.Equal((int)QuestionStatus.Draft, request.Status);
        var translation = Assert.Single(request.Translations);
        Assert.Equal((int)SupportedLanguage.Russian, translation.Language);
        Assert.All(request.AnswerOptions, option =>
            Assert.Equal((int)SupportedLanguage.Russian, Assert.Single(option.Translations).Language));
    }
}
