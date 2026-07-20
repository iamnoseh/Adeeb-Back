using Adeeb.Modules.QuestionBank.Application;
using Adeeb.Modules.QuestionBank.Domain;
using Microsoft.Extensions.Options;

namespace Adeeb.QuestionBank.Tests;

public sealed class TestXpPolicyTests
{
    private readonly TestXpPolicy _policy = new(Options.Create(new TestXpRewardOptions()));

    [Fact]
    public void Zero_correct_answers_awards_no_xp_or_completion_bonus()
    {
        var result = _policy.Calculate(
            [new(DifficultyLevel.Easy, false), new(DifficultyLevel.Hard, false)], isCompleted: true);

        Assert.Equal(TestXpCalculation.None, result);
    }

    [Theory]
    [InlineData(DifficultyLevel.Easy, 3, 13)]
    [InlineData(DifficultyLevel.Medium, 4, 14)]
    [InlineData(DifficultyLevel.Hard, 5, 15)]
    public void One_correct_answer_uses_difficulty_units_and_one_completion_bonus(
        DifficultyLevel difficulty, int answerUnits, int totalUnits)
    {
        var result = _policy.Calculate([new(difficulty, true)], isCompleted: true);

        Assert.Equal(answerUnits, result.AnswerXpUnits);
        Assert.Equal(10, result.CompletionBonusXpUnits);
        Assert.Equal(totalUnits, result.TotalXpUnits);
    }

    [Fact]
    public void Mixed_difficulties_use_integer_units_without_rounding()
    {
        var outcomes = Enumerable.Repeat(new TestXpQuestionOutcome(DifficultyLevel.Easy, true), 4)
            .Concat(Enumerable.Repeat(new TestXpQuestionOutcome(DifficultyLevel.Medium, true), 3))
            .Concat(Enumerable.Repeat(new TestXpQuestionOutcome(DifficultyLevel.Hard, true), 2))
            .Append(new(DifficultyLevel.Hard, false))
            .ToList();

        var result = _policy.Calculate(outcomes, isCompleted: true);

        Assert.Equal(4, result.EasyCorrectCount);
        Assert.Equal(3, result.MediumCorrectCount);
        Assert.Equal(2, result.HardCorrectCount);
        Assert.Equal(34, result.AnswerXpUnits);
        Assert.Equal(44, result.TotalXpUnits);
    }

    [Fact]
    public void Non_completed_attempt_awards_nothing()
    {
        var result = _policy.Calculate([new(DifficultyLevel.Hard, true)], isCompleted: false);

        Assert.Equal(TestXpCalculation.None, result);
    }

    [Fact]
    public void Unknown_difficulty_fails_closed()
    {
        Assert.Throws<TestXpCalculationException>(() =>
            _policy.Calculate([new((DifficultyLevel)999, true)], isCompleted: true));
    }

    [Fact]
    public void Overflow_fails_closed()
    {
        var policy = new TestXpPolicy(Options.Create(new TestXpRewardOptions
        {
            EasyCorrectXpUnits = int.MaxValue,
            CompletionBonusXpUnits = int.MaxValue
        }));

        Assert.Throws<TestXpCalculationException>(() =>
            policy.Calculate([new(DifficultyLevel.Easy, true)], isCompleted: true));
    }
}
