using Adeeb.Modules.QuestionBank.Infrastructure.DocumentExtraction;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Adeeb.QuestionBank.Tests;

public sealed class DocumentExtractorTests
{
    [Fact]
    public async Task Docx_extractor_preserves_paragraph_order_and_unicode()
    {
        await using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, DocumentFormat.OpenXml.WordprocessingDocumentType.Document, true))
        {
            var main = document.AddMainDocumentPart();
            main.Document = new Document(new Body(
                new Paragraph(new Run(new Text("Саволи тоҷикӣ?"))),
                new Paragraph(),
                new Paragraph(new Run(new Text("-- А) Ҷавоб")))));
            main.Document.Save();
        }

        stream.Position = 0;
        var extractor = new DocxQuestionTextExtractor();
        var text = await extractor.ExtractTextAsync(stream, CancellationToken.None);

        Assert.Contains("Саволи тоҷикӣ?", text);
        Assert.Contains("-- А) Ҷавоб", text);
        Assert.True(text.IndexOf("Саволи тоҷикӣ?", StringComparison.Ordinal) < text.IndexOf("-- А) Ҷавоб", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Pdf_extractor_reads_text_based_pdf()
    {
        await using var stream = new MemoryStream(CreateSimplePdf("Hello PDF"));
        var extractor = new PdfQuestionTextExtractor();

        var text = await extractor.ExtractTextAsync(stream, CancellationToken.None);

        Assert.Contains("Hello PDF", text);
    }

    [Fact]
    public async Task Pdf_extractor_returns_empty_text_for_image_only_like_pdf()
    {
        await using var stream = new MemoryStream(CreateSimplePdf(null));
        var extractor = new PdfQuestionTextExtractor();

        var text = await extractor.ExtractTextAsync(stream, CancellationToken.None);

        Assert.True(string.IsNullOrWhiteSpace(text));
    }

    private static byte[] CreateSimplePdf(string? text)
    {
        var content = text is null
            ? string.Empty
            : $"BT /F1 12 Tf 72 720 Td ({text}) Tj ET";
        var objects = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /Resources << /Font << /F1 4 0 R >> >> /MediaBox [0 0 612 792] /Contents 5 0 R >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
            $"<< /Length {content.Length} >>\nstream\n{content}\nendstream"
        };

        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream, System.Text.Encoding.ASCII, leaveOpen: true) { NewLine = "\n" };
        writer.WriteLine("%PDF-1.4");
        var offsets = new List<long> { 0 };
        for (var i = 0; i < objects.Length; i++)
        {
            writer.Flush();
            offsets.Add(stream.Position);
            writer.WriteLine($"{i + 1} 0 obj");
            writer.WriteLine(objects[i]);
            writer.WriteLine("endobj");
        }

        writer.Flush();
        var xref = stream.Position;
        writer.WriteLine("xref");
        writer.WriteLine($"0 {objects.Length + 1}");
        writer.WriteLine("0000000000 65535 f ");
        foreach (var offset in offsets.Skip(1))
        {
            writer.WriteLine($"{offset:0000000000} 00000 n ");
        }

        writer.WriteLine("trailer");
        writer.WriteLine($"<< /Size {objects.Length + 1} /Root 1 0 R >>");
        writer.WriteLine("startxref");
        writer.WriteLine(xref);
        writer.WriteLine("%%EOF");
        writer.Flush();
        return stream.ToArray();
    }
}
