namespace Adeeb.Modules.QuestionBank.Domain;

public enum TestMode
{
    SubjectTest = 1,
    MmtPractice = 2,
    MonthlyExam = 3,
    RedListPractice = 4
}

public enum TestAttemptStatus
{
    Created = 0,
    InProgress = 1,
    Submitted = 2,
    AutoSubmitted = 3,
    Expired = 4,
    Cancelled = 5
}

public enum RedListStatus
{
    Active = 1,
    Mastered = 2,
    Archived = 3
}
