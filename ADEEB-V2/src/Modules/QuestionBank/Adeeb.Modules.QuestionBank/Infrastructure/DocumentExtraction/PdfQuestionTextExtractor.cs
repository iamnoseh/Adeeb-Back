using Adeeb.Modules.QuestionBank.Application.Import;
using UglyToad.PdfPig;

namespace Adeeb.Modules.QuestionBank.Infrastructure.DocumentExtraction;

public sealed class PdfQuestionTextExtractor : IDocumentTextExtractor
{
    public bool CanHandle(string extension, string? contentType) =>
        extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase)
        && (string.IsNullOrWhiteSpace(contentType)
            || contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase)
            || contentType.Equals("application/octet-stream", StringComparison.OrdinalIgnoreCase));

    public Task<string> ExtractTextAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var document = PdfDocument.Open(stream);
        var pages = new List<string>();
        foreach (var page in document.GetPages())
        {
            cancellationToken.ThrowIfCancellationRequested();
            pages.Add(page.Text);
        }

        return Task.FromResult(string.Join(Environment.NewLine + Environment.NewLine, pages));
    }
}
