namespace Adeeb.Modules.QuestionBank.Application.Import;

public sealed record QuestionParseIssue(string Code, string Message);

public sealed record ParsedOption(string Label, string Text, bool IsCorrect);

public sealed class ParsedQuestion
{
    public string ClientKey { get; init; } = string.Empty;
    public string QuestionText { get; init; } = string.Empty;
    public IReadOnlyList<ParsedOption> Options { get; init; } = [];
    public IReadOnlyList<QuestionParseIssue> Errors { get; init; } = [];
    public IReadOnlyList<QuestionParseIssue> Warnings { get; init; } = [];
    public bool IsValid => Errors.Count == 0;
}

public sealed record QuestionParseResult(IReadOnlyList<ParsedQuestion> Questions, IReadOnlyList<QuestionParseIssue> DocumentErrors);
