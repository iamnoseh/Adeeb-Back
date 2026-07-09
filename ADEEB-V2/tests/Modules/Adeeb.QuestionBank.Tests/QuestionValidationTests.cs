using Adeeb.Modules.QuestionBank.Application;
using Adeeb.Modules.QuestionBank.Contracts;

namespace Adeeb.QuestionBank.Tests;

public sealed class QuestionValidationTests
{
    [Fact]
    public void Single_choice_requires_exactly_four_options()
    {
        var request = ValidSingleChoice() with
        {
            AnswerOptions = ValidSingleChoice().AnswerOptions.Take(1).ToList()
        };

        var result = Validation.ValidateQuestion(request);

        Assert.True(result.IsFailure);
        Assert.Contains("answerOptions", result.ValidationErrors!.Keys);
    }

    [Fact]
    public void Matching_requires_exactly_four_pairs()
    {
        var request = ValidMatching() with
        {
            AnswerOptions = ValidMatching().AnswerOptions.Take(3).ToList()
        };

        var result = Validation.ValidateQuestion(request);

        Assert.True(result.IsFailure);
        Assert.Contains("answerOptions", result.ValidationErrors!.Keys);
    }

    [Fact]
    public void Single_choice_requires_exactly_one_correct_option()
    {
        var request = ValidSingleChoice() with
        {
            AnswerOptions = ValidSingleChoice().AnswerOptions.Select(x => x with { IsCorrect = true }).ToList()
        };

        var result = Validation.ValidateQuestion(request);

        Assert.True(result.IsFailure);
        Assert.Contains("answerOptions", result.ValidationErrors!.Keys);
    }

    [Fact]
    public void Matching_rejects_duplicate_right_side_values()
    {
        var request = ValidMatching() with
        {
            AnswerOptions =
            [
                Pair(1, "A", "Same"),
                Pair(2, "B", "Same"),
                Pair(3, "C", "Three"),
                Pair(4, "D", "Four")
            ]
        };

        var result = Validation.ValidateQuestion(request);

        Assert.True(result.IsFailure);
        Assert.Contains("answerOptions.0.matchPairText", result.ValidationErrors!.Keys);
    }

    [Fact]
    public void Closed_answer_requires_one_canonical_correct_answer()
    {
        var request = Base(3) with
        {
            AnswerOptions = []
        };

        var result = Validation.ValidateQuestion(request);

        Assert.True(result.IsFailure);
        Assert.Contains("answerOptions", result.ValidationErrors!.Keys);
    }

    private static QuestionUpsertRequest ValidSingleChoice() =>
        Base(1) with
        {
            AnswerOptions =
            [
                Option(1, true, "A"),
                Option(2, false, "B"),
                Option(3, false, "C"),
                Option(4, false, "D")
            ]
        };

    private static QuestionUpsertRequest ValidMatching() =>
        Base(2) with
        {
            AnswerOptions =
            [
                Pair(1, "A", "One"),
                Pair(2, "B", "Two"),
                Pair(3, "C", "Three"),
                Pair(4, "D", "Four")
            ]
        };

    private static QuestionUpsertRequest Base(int type) =>
        new(
            Guid.NewGuid(),
            null,
            null,
            type,
            1,
            0,
            null,
            [new QuestionTranslationRequest(0, "Question?", null)],
            []);

    private static AnswerOptionRequest Option(int order, bool correct, string text) =>
        new(order, correct, [new AnswerOptionTranslationRequest(0, text, null)]);

    private static AnswerOptionRequest Pair(int order, string left, string right) =>
        new(order, false, [new AnswerOptionTranslationRequest(0, left, right)]);
}
