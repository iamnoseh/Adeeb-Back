namespace Adeeb.Modules.QuestionBank.Application;

public sealed class StudentTestingOptions
{
    public const string SectionName = "StudentTesting";
    public int RedListMinimumQuestions { get; set; } = 20;
    public int RedListDefaultQuestions { get; set; } = 20;
    public int MmtPracticeDefaultQuestions { get; set; } = 100;
    public int MonthlyExamQuestionCount { get; set; } = 100;
    public int MinutesPerSubjectQuestion { get; set; } = 1;
    public int MmtDurationMinutes { get; set; } = 150;
    public int MonthlyExamWindowHours { get; set; } = 24;
}
