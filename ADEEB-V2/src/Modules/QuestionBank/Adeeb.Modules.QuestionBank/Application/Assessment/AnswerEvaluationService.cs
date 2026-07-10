using Adeeb.Application.Abstractions.Localization;
using Adeeb.Modules.QuestionBank.Domain;

namespace Adeeb.Modules.QuestionBank.Application.Assessment;

public interface IAnswerEvaluationService
{
    AnswerEvaluationResult Evaluate(Question question, AnswerEvaluationInput input, SupportedLanguage language);
}

internal interface IQuestionAnswerEvaluator
{
    QuestionType QuestionType { get; }
    AnswerEvaluationResult Evaluate(Question question, AnswerEvaluationInput input, SupportedLanguage language);
}

internal sealed class AnswerEvaluationService : IAnswerEvaluationService
{
    private readonly IReadOnlyDictionary<QuestionType, IQuestionAnswerEvaluator> _evaluators;

    public AnswerEvaluationService(IEnumerable<IQuestionAnswerEvaluator> evaluators)
    {
        var dict = new Dictionary<QuestionType, IQuestionAnswerEvaluator>();
        foreach (var evaluator in evaluators)
        {
            if (!dict.TryAdd(evaluator.QuestionType, evaluator))
            {
                throw new InvalidOperationException($"Duplicate evaluator registered for question type: {evaluator.QuestionType}");
            }
        }
        _evaluators = dict;
    }

    public AnswerEvaluationResult Evaluate(Question question, AnswerEvaluationInput input, SupportedLanguage language) =>
        _evaluators.TryGetValue(question.Type, out var evaluator)
            ? evaluator.Evaluate(question, input, language)
            : throw new InvalidOperationException($"No evaluator found for question type: {question.Type}");
}

internal sealed class SingleChoiceAnswerEvaluator : IQuestionAnswerEvaluator
{
    public QuestionType QuestionType => QuestionType.SingleChoice;

    public AnswerEvaluationResult Evaluate(Question question, AnswerEvaluationInput input, SupportedLanguage language)
    {
        var correctOption = question.AnswerOptions.SingleOrDefault(x => x.IsCorrect);
        var selectedOption = input.SelectedOptionId.HasValue
            ? question.AnswerOptions.SingleOrDefault(x => x.Id == input.SelectedOptionId.Value)
            : null;

        return new AnswerEvaluationResult(
            IsAnswered: selectedOption is not null,
            IsCorrect: selectedOption?.IsCorrect == true,
            CorrectOptionId: correctOption?.Id,
            SubmittedAnswerText: selectedOption is null
                ? null
                : AssessmentText.TextFor(selectedOption.Translations, language, x => x.Text),
            NormalizedSubmittedAnswer: input.SelectedOptionId?.ToString());
    }
}

internal sealed class ClosedAnswerEvaluator : IQuestionAnswerEvaluator
{
    public QuestionType QuestionType => QuestionType.ClosedAnswer;

    public AnswerEvaluationResult Evaluate(Question question, AnswerEvaluationInput input, SupportedLanguage language)
    {
        var correctOption = question.AnswerOptions.SingleOrDefault(x => x.IsCorrect);
        var canonical = correctOption is null
            ? string.Empty
            : AssessmentText.TextFor(correctOption.Translations, language, x => x.Text);
        var submitted = input.TextResponse;
        var normalizedSubmitted = AssessmentText.NormalizeClosedAnswer(submitted);
        var normalizedCanonical = AssessmentText.NormalizeClosedAnswer(canonical);
        var isAnswered = !string.IsNullOrWhiteSpace(submitted);
        var isCorrect = isAnswered
            && normalizedCanonical.Length > 0
            && string.Equals(normalizedCanonical, normalizedSubmitted, StringComparison.OrdinalIgnoreCase);

        return new AnswerEvaluationResult(
            IsAnswered: isAnswered,
            IsCorrect: isCorrect,
            CorrectOptionId: correctOption?.Id,
            SubmittedAnswerText: submitted,
            NormalizedSubmittedAnswer: normalizedSubmitted);
    }
}

internal sealed class MatchingAnswerEvaluator : IQuestionAnswerEvaluator
{
    public QuestionType QuestionType => QuestionType.Matching;

    public AnswerEvaluationResult Evaluate(Question question, AnswerEvaluationInput input, SupportedLanguage language)
    {
        var submittedPairs = input.MatchingPairs ?? new Dictionary<Guid, string>();
        var pairs = question.AnswerOptions
            .Select(option => new
            {
                Option = option,
                RightText = AssessmentText.TextFor(option.Translations, language, x => x.MatchPairText)
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.RightText))
            .ToList();

        var correctCount = 0;
        foreach (var pair in pairs)
        {
            if (!submittedPairs.TryGetValue(pair.Option.Id, out var submittedRight))
            {
                continue;
            }

            var normalizedSubmitted = AssessmentText.NormalizeMatchingText(submittedRight);
            var normalizedCorrect = AssessmentText.NormalizeMatchingText(pair.RightText);
            if (normalizedSubmitted.Length > 0 &&
                string.Equals(normalizedSubmitted, normalizedCorrect, StringComparison.OrdinalIgnoreCase))
            {
                correctCount++;
            }
        }

        return new AnswerEvaluationResult(
            IsAnswered: submittedPairs.Count > 0,
            IsCorrect: pairs.Count > 0 && correctCount == pairs.Count,
            CorrectPairsCount: correctCount,
            TotalPairsCount: pairs.Count);
    }
}
