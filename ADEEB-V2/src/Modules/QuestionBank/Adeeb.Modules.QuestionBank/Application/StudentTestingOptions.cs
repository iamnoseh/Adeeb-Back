namespace Adeeb.Modules.QuestionBank.Application;

public sealed class StudentTestingOptions
{
    public const string SectionName = "StudentTesting";
    public int RedListMinimumQuestions { get; set; } = 20;
    public int RedListDefaultQuestions { get; set; } = 20;
    public int MmtPracticeDefaultQuestions { get; set; } = 100;
    public int MonthlyExamQuestionCount { get; set; } = 100;
    public int MinutesPerSubjectQuestion { get; set; } = 1;
    public int ExtendedMinutesPerSubjectQuestion { get; set; } = 2;
    public string[] ExtendedTimeSubjectCodes { get; set; } =
    [
        "MATH", "MATHEMATICS", "PHYSICS", "CHEMISTRY",
        "МАТЕМАТИКА", "ФИЗИКА", "ХИМИЯ"
    ];
    public int MmtDurationMinutes { get; set; } = 150;
    public int MonthlyExamWindowHours { get; set; } = 24;
    public int ExpiredAttemptSweepIntervalSeconds { get; set; } = 60;
    public int ExpiredAttemptSweepBatchSize { get; set; } = 100;
}
