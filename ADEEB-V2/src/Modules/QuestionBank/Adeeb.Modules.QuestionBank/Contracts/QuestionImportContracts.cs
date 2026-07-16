using Microsoft.AspNetCore.Http;

namespace Adeeb.Modules.QuestionBank.Contracts;

public sealed class QuestionImportParseFormRequest
{
    public Guid SubjectId { get; init; }
    public Guid? TopicId { get; init; }
    public int Difficulty { get; init; }
    public int Language { get; init; }
    public IFormFile? File { get; init; }
}

public sealed record QuestionImportConfirmRequest(Guid SubjectId, Guid? TopicId, int Difficulty, int Language, IReadOnlyList<QuestionImportConfirmQuestionRequest> Questions);
public sealed record QuestionImportConfirmQuestionRequest(string QuestionText, IReadOnlyList<QuestionImportConfirmOptionRequest> Options, int? QuestionType = null, string? ExpectedAnswer = null);
public sealed record QuestionImportConfirmOptionRequest(string Text, bool IsCorrect);

public sealed record QuestionImportPreviewResponse(
    string FileName,
    QuestionImportSummaryResponse Summary,
    IReadOnlyList<QuestionImportPreviewQuestionResponse> Questions);

public sealed record QuestionImportSummaryResponse(int TotalDetected, int Valid, int Invalid, int Warnings);

public sealed record QuestionImportPreviewQuestionResponse(
    string ClientKey,
    int QuestionType,
    string QuestionTypeName,
    string QuestionText,
    string? ExpectedAnswer,
    IReadOnlyList<QuestionImportPreviewOptionResponse> Options,
    bool IsValid,
    IReadOnlyList<QuestionImportIssueResponse> Errors,
    IReadOnlyList<QuestionImportIssueResponse> Warnings);

public sealed record QuestionImportPreviewOptionResponse(string Label, string Text, bool IsCorrect);
public sealed record QuestionImportIssueResponse(string Code, string Message);
public sealed record QuestionImportConfirmResponse(int ImportedCount, IReadOnlyList<Guid> QuestionIds);
