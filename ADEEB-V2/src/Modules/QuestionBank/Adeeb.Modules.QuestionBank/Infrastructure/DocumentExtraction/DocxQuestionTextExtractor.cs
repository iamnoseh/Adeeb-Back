using System.Text;
using Adeeb.Modules.QuestionBank.Application.Import;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Adeeb.Modules.QuestionBank.Infrastructure.DocumentExtraction;

public sealed class DocxQuestionTextExtractor : IDocumentTextExtractor
{
    public bool CanHandle(string extension, string? contentType) =>
        extension.Equals(".docx", StringComparison.OrdinalIgnoreCase)
        && (string.IsNullOrWhiteSpace(contentType)
            || contentType.Equals("application/vnd.openxmlformats-officedocument.wordprocessingml.document", StringComparison.OrdinalIgnoreCase)
            || contentType.Equals("application/octet-stream", StringComparison.OrdinalIgnoreCase));

    public Task<string> ExtractTextAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var document = WordprocessingDocument.Open(stream, false);
        var body = document.MainDocumentPart?.Document.Body;
        if (body is null)
        {
            return Task.FromResult(string.Empty);
        }

        var text = new StringBuilder();
        foreach (var paragraph in body.Elements<Paragraph>())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var paragraphText = ExtractParagraphText(paragraph);
            text.AppendLine(paragraphText);
        }

        return Task.FromResult(text.ToString());
    }

    private static string ExtractParagraphText(Paragraph paragraph)
    {
        var text = new StringBuilder();
        foreach (var child in paragraph.Descendants())
        {
            switch (child)
            {
                case Text runText:
                    text.Append(runText.Text);
                    break;
                case TabChar:
                    text.Append(' ');
                    break;
                case Break:
                    text.AppendLine();
                    break;
            }
        }

        return text.ToString();
    }
}
