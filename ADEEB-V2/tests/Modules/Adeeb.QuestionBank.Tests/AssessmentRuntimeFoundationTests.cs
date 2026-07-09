using Adeeb.Application.Abstractions.Localization;
using Adeeb.Modules.QuestionBank.Application.Assessment;
using Adeeb.Modules.QuestionBank.Domain;

namespace Adeeb.QuestionBank.Tests;

public sealed class AssessmentRuntimeFoundationTests
{
    [Fact]
    public void SingleChoice_uses_stable_option_id_for_correctness()
    {
        var question = SingleChoiceQuestion(out var correctOptionId);
        var service = CreateEvaluationService();

        var result = service.Evaluate(question, new AnswerEvaluationInput(SelectedOptionId: correctOptionId), SupportedLanguage.Tajik);

        Assert.True(result.IsAnswered);
        Assert.True(result.IsCorrect);
    }

    [Fact]
    public void SingleChoice_rejects_wrong_or_foreign_option_id()
    {
        var question = SingleChoiceQuestion(out var correctOptionId);
        var otherQuestion = SingleChoiceQuestion(out var foreignOptionId);
        Assert.NotEqual(correctOptionId, foreignOptionId);
        var wrongOptionId = question.AnswerOptions.First(x => !x.IsCorrect).Id;
        var service = CreateEvaluationService();

        var wrong = service.Evaluate(question, new AnswerEvaluationInput(SelectedOptionId: wrongOptionId), SupportedLanguage.Tajik);
        var foreign = service.Evaluate(question, new AnswerEvaluationInput(SelectedOptionId: foreignOptionId), SupportedLanguage.Tajik);

        Assert.False(wrong.IsCorrect);
        Assert.False(foreign.IsCorrect);
        Assert.False(foreign.IsAnswered);
        Assert.NotEqual("A", wrong.NormalizedSubmittedAnswer);
        Assert.NotEqual("A", foreign.NormalizedSubmittedAnswer);
        _ = otherQuestion;
    }

    [Theory]
    [InlineData(" Душанбе ", true)]
    [InlineData("душанбе", true)]
    [InlineData("Душанбе", true)]
    [InlineData("Dushanbe", false)]
    [InlineData("", false)]
    public void ClosedAnswer_uses_trimmed_case_insensitive_text_policy(string submitted, bool expectedCorrect)
    {
        var question = ClosedAnswerQuestion("душанбе");
        var service = CreateEvaluationService();

        var result = service.Evaluate(question, new AnswerEvaluationInput(TextResponse: submitted), SupportedLanguage.Tajik);

        Assert.Equal(expectedCorrect, result.IsCorrect);
        Assert.Equal(submitted.Trim(), result.NormalizedSubmittedAnswer);
    }

    [Fact]
    public void ClosedAnswer_preserves_numeric_answers_as_text()
    {
        var question = ClosedAnswerQuestion("7.0");
        var service = CreateEvaluationService();

        var exact = service.Evaluate(question, new AnswerEvaluationInput(TextResponse: " 7.0 "), SupportedLanguage.Tajik);
        var differentText = service.Evaluate(question, new AnswerEvaluationInput(TextResponse: "7"), SupportedLanguage.Tajik);

        Assert.True(exact.IsCorrect);
        Assert.False(differentText.IsCorrect);
    }

    [Fact]
    public void Matching_evaluates_supported_pairs_by_option_identity()
    {
        var question = MatchingQuestion();
        var service = CreateEvaluationService();
        var pairs = question.AnswerOptions.ToDictionary(
            x => x.Id,
            x => x.Translations.Single(t => t.Language == SupportedLanguage.Tajik).MatchPairText!);

        var result = service.Evaluate(question, new AnswerEvaluationInput(MatchingPairs: pairs), SupportedLanguage.Tajik);

        Assert.True(result.IsAnswered);
        Assert.True(result.IsCorrect);
        Assert.Equal(4, result.CorrectPairsCount);
        Assert.Equal(4, result.TotalPairsCount);
    }

    [Fact]
    public void Presentation_snapshot_preserves_ids_and_hides_correctness()
    {
        var question = SingleChoiceQuestion(out var correctOptionId);
        var randomizer = new AssessmentPresentationRandomizer(_ => 0);

        var snapshot = randomizer.CreateSnapshot([question]);
        var projected = randomizer.Project([question], snapshot, SupportedLanguage.Tajik).Single();

        Assert.Contains(correctOptionId, snapshot.Questions.Single().OptionOrder);
        Assert.Equal(question.AnswerOptions.Select(x => x.Id).OrderBy(x => x), projected.AnswerOptions.Select(x => x.Id).OrderBy(x => x));
        Assert.DoesNotContain(typeof(PresentedAnswerOption).GetProperties(), x => x.Name == "IsCorrect");
        Assert.DoesNotContain(typeof(PresentedQuestion).GetProperties(), x => x.Name.Contains("Correct", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Same_snapshot_projects_same_order_but_new_snapshot_can_differ()
    {
        var first = SingleChoiceQuestion(out _);
        var second = SingleChoiceQuestion(out _);
        var reverseRandomizer = new AssessmentPresentationRandomizer(_ => 0);
        var identityRandomizer = new AssessmentPresentationRandomizer(maxExclusive => maxExclusive - 1);

        var frozenSnapshot = reverseRandomizer.CreateSnapshot([first, second]);
        var firstProjection = reverseRandomizer.Project([first, second], frozenSnapshot, SupportedLanguage.Tajik);
        var secondProjection = reverseRandomizer.Project([first, second], frozenSnapshot, SupportedLanguage.Tajik);
        var newSessionSnapshot = identityRandomizer.CreateSnapshot([first, second]);

        Assert.Equal(firstProjection.Select(x => x.Id), secondProjection.Select(x => x.Id));
        Assert.Equal(firstProjection.Single(x => x.Id == first.Id).AnswerOptions.Select(x => x.Id),
            secondProjection.Single(x => x.Id == first.Id).AnswerOptions.Select(x => x.Id));
        Assert.NotEqual(frozenSnapshot.QuestionOrder, newSessionSnapshot.QuestionOrder);
        Assert.NotEqual(
            frozenSnapshot.Questions.Single(x => x.QuestionId == first.Id).OptionOrder,
            newSessionSnapshot.Questions.Single(x => x.QuestionId == first.Id).OptionOrder);
    }

    private static IAnswerEvaluationService CreateEvaluationService() =>
        new AnswerEvaluationService([
            new SingleChoiceAnswerEvaluator(),
            new ClosedAnswerEvaluator(),
            new MatchingAnswerEvaluator()
        ]);

    private static Question SingleChoiceQuestion(out Guid correctOptionId)
    {
        var question = NewQuestion(QuestionType.SingleChoice);
        var options = new[]
        {
            Option(question.Id, 1, false, "12"),
            Option(question.Id, 2, true, "22"),
            Option(question.Id, 3, false, "32"),
            Option(question.Id, 4, false, "42")
        };
        correctOptionId = options.Single(x => x.IsCorrect).Id;
        question.ReplaceContent(Translations(question.Id, "2 + 20 = ?"), options);
        return question;
    }

    private static Question ClosedAnswerQuestion(string answer)
    {
        var question = NewQuestion(QuestionType.ClosedAnswer);
        question.ReplaceContent(
            Translations(question.Id, "Пойтахти Тоҷикистон?"),
            [Option(question.Id, 1, true, answer)]);
        return question;
    }

    private static Question MatchingQuestion()
    {
        var question = NewQuestion(QuestionType.Matching);
        question.ReplaceContent(
            Translations(question.Id, "Мувофиқат кунед"),
            [
                Pair(question.Id, 1, "Тоҷикистон", "Душанбе"),
                Pair(question.Id, 2, "Русия", "Москва"),
                Pair(question.Id, 3, "Узбекистон", "Тошканд"),
                Pair(question.Id, 4, "Қазоқистон", "Остона")
            ]);
        return question;
    }

    private static Question NewQuestion(QuestionType type) =>
        new(Guid.NewGuid(), Guid.NewGuid(), null, null, type, DifficultyLevel.Easy, null, DateTimeOffset.UtcNow);

    private static IReadOnlyList<QuestionTranslation> Translations(Guid questionId, string content) =>
        [
            new(questionId, SupportedLanguage.Tajik, content, null),
            new(questionId, SupportedLanguage.Russian, content, null)
        ];

    private static AnswerOption Option(Guid questionId, int displayOrder, bool isCorrect, string text)
    {
        var option = new AnswerOption(Guid.NewGuid(), questionId, displayOrder, isCorrect);
        option.ReplaceTranslations([
            new AnswerOptionTranslation(option.Id, SupportedLanguage.Tajik, text, null),
            new AnswerOptionTranslation(option.Id, SupportedLanguage.Russian, text, null)
        ]);
        return option;
    }

    private static AnswerOption Pair(Guid questionId, int displayOrder, string left, string right)
    {
        var option = new AnswerOption(Guid.NewGuid(), questionId, displayOrder, false);
        option.ReplaceTranslations([
            new AnswerOptionTranslation(option.Id, SupportedLanguage.Tajik, left, right),
            new AnswerOptionTranslation(option.Id, SupportedLanguage.Russian, left, right)
        ]);
        return option;
    }
}
