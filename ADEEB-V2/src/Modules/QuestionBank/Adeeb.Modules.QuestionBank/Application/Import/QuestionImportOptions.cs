namespace Adeeb.Modules.QuestionBank.Application.Import;

public sealed class QuestionImportOptions
{
    public const string SectionName = "QuestionImport";

    public long MaxFileSizeBytes { get; init; } = 5 * 1024 * 1024;
    public int MaxQuestionsPerImport { get; init; } = 200;
    public int MaxQuestionTextLength { get; init; } = 4000;
    public int MaxOptionTextLength { get; init; } = 1000;
    public int MaxOptionsPerQuestion { get; init; } = 8;
    public string[] AllowedExtensions { get; init; } = [".docx", ".pdf"];
}
