using System.Security.Cryptography;
using Adeeb.Application.Abstractions.Localization;
using Adeeb.Modules.QuestionBank.Domain;

namespace Adeeb.Modules.QuestionBank.Application.Assessment;

public interface IAssessmentPresentationRandomizer
{
    AssessmentPresentationSnapshot CreateSnapshot(IReadOnlyList<Question> questions);
    IReadOnlyList<PresentedQuestion> Project(
        IReadOnlyList<Question> questions,
        AssessmentPresentationSnapshot snapshot,
        SupportedLanguage language);
}

internal sealed class AssessmentPresentationRandomizer : IAssessmentPresentationRandomizer
{
    private readonly Func<int, int> _next;

    public AssessmentPresentationRandomizer()
        : this(maxExclusive => RandomNumberGenerator.GetInt32(maxExclusive))
    {
    }

    internal AssessmentPresentationRandomizer(Func<int, int> next)
    {
        _next = next;
    }

    public AssessmentPresentationSnapshot CreateSnapshot(IReadOnlyList<Question> questions)
    {
        var questionOrder = Shuffle(questions.Select(x => x.Id).ToList());
        var snapshots = questions.Select(question =>
        {
            var optionIds = question.Type is QuestionType.SingleChoice or QuestionType.Matching
                ? Shuffle(question.AnswerOptions.Select(x => x.Id).ToList())
                : [];

            var matchRightIds = question.Type == QuestionType.Matching
                ? Shuffle(question.AnswerOptions
                    .Where(x => x.Translations.Any(t => !string.IsNullOrWhiteSpace(t.MatchPairText)))
                    .Select(x => x.Id)
                    .ToList())
                : [];

            return new QuestionPresentationSnapshot(question.Id, optionIds, matchRightIds);
        }).ToList();

        return new AssessmentPresentationSnapshot(
            AssessmentPresentationSnapshot.CurrentVersion,
            questionOrder,
            snapshots);
    }

    public IReadOnlyList<PresentedQuestion> Project(
        IReadOnlyList<Question> questions,
        AssessmentPresentationSnapshot snapshot,
        SupportedLanguage language)
    {
        var byQuestionId = questions.ToDictionary(x => x.Id);
        var snapshotByQuestionId = snapshot.Questions.ToDictionary(x => x.QuestionId);
        var projected = new List<PresentedQuestion>();

        foreach (var questionId in snapshot.QuestionOrder)
        {
            if (!byQuestionId.TryGetValue(questionId, out var question))
            {
                continue;
            }

            snapshotByQuestionId.TryGetValue(questionId, out var questionSnapshot);
            var orderedOptions = OrderOptions(question, questionSnapshot?.OptionOrder);
            var orderedRightOptions = OrderOptions(question, questionSnapshot?.MatchRightOptionOrder);

            projected.Add(new PresentedQuestion(
                question.Id,
                (int)question.Type,
                (int)question.Difficulty,
                question.ContentFor(language),
                question.ImageUrl,
                orderedOptions.Select(x => new PresentedAnswerOption(
                    x.Id,
                    AssessmentText.TextFor(x.Translations, language, t => t.Text))).ToList(),
                question.Type == QuestionType.Matching
                    ? orderedRightOptions.Select(x => new PresentedMatchingRightOption(
                        x.Id,
                        AssessmentText.TextFor(x.Translations, language, t => t.MatchPairText))).ToList()
                    : []));
        }

        return projected;
    }

    private List<T> Shuffle<T>(List<T> values)
    {
        for (var i = values.Count - 1; i > 0; i--)
        {
            var j = _next(i + 1);
            (values[i], values[j]) = (values[j], values[i]);
        }

        return values;
    }

    private static IReadOnlyList<AnswerOption> OrderOptions(Question question, IReadOnlyList<Guid>? order)
    {
        var optionsById = question.AnswerOptions.ToDictionary(x => x.Id);
        if (order is null || order.Count == 0)
        {
            return question.AnswerOptions.OrderBy(x => x.DisplayOrder).ToList();
        }

        var ordered = order.Where(optionsById.ContainsKey).Select(id => optionsById[id]).ToList();
        ordered.AddRange(question.AnswerOptions
            .Where(x => !order.Contains(x.Id))
            .OrderBy(x => x.DisplayOrder));
        return ordered;
    }
}
