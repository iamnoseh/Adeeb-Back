using Adeeb.Modules.QuestionBank.Domain;
using Microsoft.Extensions.Options;

namespace Adeeb.Modules.QuestionBank.Application;

public sealed class TestXpRewardOptions
{
    public const string SectionName = "TestXpRewards";
    public const int UnitsPerXp = 2;
    public const int MaximumConfiguredUnits = 1_000_000;

    public int EasyCorrectXpUnits { get; set; } = 3;
    public int MediumCorrectXpUnits { get; set; } = 4;
    public int HardCorrectXpUnits { get; set; } = 5;
    public int CompletionBonusXpUnits { get; set; } = 10;
    public int MmtPracticeCompletionBonusXpUnits { get; set; } = 20;
    public int MonthlyExamCompletionBonusXpUnits { get; set; } = 50;
    public int RedListPracticeRewardPercent { get; set; } = 50;
    public int RedListMasteryBonusXpUnits { get; set; } = UnitsPerXp;
}

public sealed record TestXpQuestionOutcome(DifficultyLevel Difficulty, bool IsCorrect);

public sealed record TestXpCalculation(
    int EasyCorrectCount,
    int MediumCorrectCount,
    int HardCorrectCount,
    int AnswerXpUnits,
    int CompletionBonusXpUnits,
    int TotalXpUnits)
{
    public static readonly TestXpCalculation None = new(0, 0, 0, 0, 0, 0);
}

public interface ITestXpPolicy
{
    TestXpCalculation Calculate(IReadOnlyCollection<TestXpQuestionOutcome> outcomes, bool isCompleted,
        TestMode mode = TestMode.SubjectTest);
}

public sealed class TestXpPolicy(IOptions<TestXpRewardOptions> options) : ITestXpPolicy
{
    public TestXpCalculation Calculate(IReadOnlyCollection<TestXpQuestionOutcome> outcomes, bool isCompleted,
        TestMode mode = TestMode.SubjectTest)
    {
        ArgumentNullException.ThrowIfNull(outcomes);
        if (!isCompleted) return TestXpCalculation.None;

        var easy = 0;
        var medium = 0;
        var hard = 0;
        foreach (var outcome in outcomes.Where(x => x.IsCorrect))
        {
            switch (outcome.Difficulty)
            {
                case DifficultyLevel.Easy: easy = checked(easy + 1); break;
                case DifficultyLevel.Medium: medium = checked(medium + 1); break;
                case DifficultyLevel.Hard: hard = checked(hard + 1); break;
                default: throw new TestXpCalculationException($"Unsupported test difficulty value: {(int)outcome.Difficulty}.");
            }
        }

        try
        {
            var value = options.Value;
            var answerUnits = checked(
                checked(easy * value.EasyCorrectXpUnits)
                + checked(medium * value.MediumCorrectXpUnits)
                + checked(hard * value.HardCorrectXpUnits));
            var correct = checked(easy + medium + hard);
            var completionUnits = CompletionBonusUnits(mode, value, correct);
            if (answerUnits == 0 && completionUnits == 0) return TestXpCalculation.None;
            if (mode == TestMode.RedListPractice)
            {
                answerUnits = Scale(answerUnits, value.RedListPracticeRewardPercent);
                completionUnits = Scale(completionUnits, value.RedListPracticeRewardPercent);
            }
            var totalUnits = checked(answerUnits + completionUnits);
            return new(easy, medium, hard, answerUnits, completionUnits, totalUnits);
        }
        catch (OverflowException exception)
        {
            throw new TestXpCalculationException("Test XP calculation overflowed.", exception);
        }
    }

    private static int Scale(int units, int percent) => checked(units * percent / 100);

    private static int CompletionBonusUnits(TestMode mode, TestXpRewardOptions options, int correctCount) => mode switch
    {
        TestMode.MonthlyExam => options.MonthlyExamCompletionBonusXpUnits,
        TestMode.MmtPractice => correctCount > 0 ? options.MmtPracticeCompletionBonusXpUnits : 0,
        _ => correctCount > 0 ? options.CompletionBonusXpUnits : 0
    };
}

public sealed class TestXpCalculationException : Exception
{
    public TestXpCalculationException(string message) : base(message) { }
    public TestXpCalculationException(string message, Exception innerException) : base(message, innerException) { }
}

public static class TestXpSourceIdentity
{
    public static string SourceId(Guid attemptId) => attemptId.ToString("N");
    public static string IdempotencyKey(Guid attemptId) => $"test-xp:{attemptId:N}";
}
