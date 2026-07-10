using System.Text;
using System.Text.RegularExpressions;
using Adeeb.Application.Abstractions.Localization;

namespace Adeeb.Modules.QuestionBank.Application.Assessment;

public sealed record AnswerEvaluationInput(
    Guid? SelectedOptionId = null,
    string? TextResponse = null,
    IReadOnlyDictionary<Guid, string>? MatchingPairs = null);

public enum AssessmentFeedbackPolicy
{
    RevealCorrectness,
    ConcealCorrectness
}

public sealed record StudentAnswerFeedback(
    bool IsAnswered,
    bool? IsCorrect,
    int? CorrectPairsCount = null,
    int? TotalPairsCount = null);

public sealed record AnswerEvaluationResult(
    bool IsAnswered,
    bool IsCorrect,
    Guid? CorrectOptionId = null,
    string? SubmittedAnswerText = null,
    string? NormalizedSubmittedAnswer = null,
    int? CorrectPairsCount = null,
    int? TotalPairsCount = null)
{
    public StudentAnswerFeedback ToStudentFeedback(AssessmentFeedbackPolicy policy) =>
        new(
            IsAnswered,
            policy == AssessmentFeedbackPolicy.RevealCorrectness ? IsCorrect : null,
            policy == AssessmentFeedbackPolicy.RevealCorrectness ? CorrectPairsCount : null,
            TotalPairsCount);
}

public sealed record AssessmentPresentationSnapshot(
    int Version,
    IReadOnlyList<Guid> QuestionOrder,
    IReadOnlyList<QuestionPresentationSnapshot> Questions)
{
    public static int CurrentVersion => 1;
}

public sealed record QuestionPresentationSnapshot(
    Guid QuestionId,
    IReadOnlyList<Guid> OptionOrder,
    IReadOnlyList<Guid> MatchRightOptionOrder);

public sealed record PresentedQuestion(
    Guid Id,
    int Type,
    int Difficulty,
    string Content,
    string? ImageUrl,
    IReadOnlyList<PresentedAnswerOption> AnswerOptions,
    IReadOnlyList<PresentedMatchingRightOption> MatchingRightOptions);

public sealed record PresentedAnswerOption(Guid Id, string Text);

public sealed record PresentedMatchingRightOption(Guid SourceOptionId, string Text);

internal static class AssessmentText
{
    private static readonly Regex Whitespace = new(@"\s+", RegexOptions.Compiled);

    public static string NormalizeClosedAnswer(string? value) => NormalizeText(value);

    public static string NormalizeMatchingText(string? value) => NormalizeText(value);

    private static string NormalizeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Normalize(NormalizationForm.FormC).Trim();
        return Whitespace.Replace(normalized, " ");
    }

    public static string TextFor(
        IEnumerable<Domain.AnswerOptionTranslation> translations,
        SupportedLanguage language,
        Func<Domain.AnswerOptionTranslation, string?> selector)
    {
        var selected = translations.FirstOrDefault(x => x.Language == language)
            ?? translations.FirstOrDefault(x => x.Language == SupportedLanguage.Tajik)
            ?? translations.FirstOrDefault(x => x.Language == SupportedLanguage.Russian)
            ?? translations.FirstOrDefault();

        return selected is null ? string.Empty : selector(selected) ?? string.Empty;
    }
}
