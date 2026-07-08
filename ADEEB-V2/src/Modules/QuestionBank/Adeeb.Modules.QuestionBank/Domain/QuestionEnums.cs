namespace Adeeb.Modules.QuestionBank.Domain;

public enum QuestionType
{
    SingleChoice = 1,
    Matching = 2,
    ClosedAnswer = 3
}

public enum DifficultyLevel
{
    Easy = 1,
    Medium = 2,
    Hard = 3
}

public enum QuestionStatus
{
    Draft = 0,
    Active = 1,
    Archived = 2
}
