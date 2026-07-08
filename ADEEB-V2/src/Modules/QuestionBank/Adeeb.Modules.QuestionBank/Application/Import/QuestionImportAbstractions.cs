using Microsoft.AspNetCore.Http;

namespace Adeeb.Modules.QuestionBank.Application.Import;

public interface IDocumentTextExtractor
{
    bool CanHandle(string extension, string? contentType);
    Task<string> ExtractTextAsync(Stream stream, CancellationToken cancellationToken);
}

public interface IQuestionImportTextNormalizer
{
    string Normalize(string text);
    string NormalizeForDuplicateComparison(string text);
}

public interface IQuestionDocumentParser
{
    QuestionParseResult Parse(string normalizedText);
}

public interface IQuestionImportService
{
    Task<Adeeb.SharedKernel.Results.Result<Adeeb.Modules.QuestionBank.Contracts.QuestionImportPreviewResponse>> ParseAsync(
        Adeeb.Modules.QuestionBank.Contracts.QuestionImportParseFormRequest request,
        CancellationToken cancellationToken);

    Task<Adeeb.SharedKernel.Results.Result<Adeeb.Modules.QuestionBank.Contracts.QuestionImportConfirmResponse>> ConfirmAsync(
        Adeeb.Modules.QuestionBank.Contracts.QuestionImportConfirmRequest request,
        CancellationToken cancellationToken);
}
