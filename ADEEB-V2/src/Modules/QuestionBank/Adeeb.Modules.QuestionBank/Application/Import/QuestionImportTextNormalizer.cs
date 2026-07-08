using System.Text;
using System.Text.RegularExpressions;

namespace Adeeb.Modules.QuestionBank.Application.Import;

public sealed partial class QuestionImportTextNormalizer : IQuestionImportTextNormalizer
{
    public string Normalize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var normalized = text
            .Trim('\uFEFF')
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Replace('\t', ' ');

        var lines = normalized
            .Split('\n')
            .Select(line => RepeatedHorizontalSpaceRegex().Replace(line.Trim(), " "))
            .ToList();

        var result = new StringBuilder();
        var previousBlank = true;
        foreach (var line in lines)
        {
            var blank = string.IsNullOrWhiteSpace(line);
            if (blank && previousBlank)
            {
                continue;
            }

            result.AppendLine(line);
            previousBlank = blank;
        }

        return result.ToString().Trim();
    }

    public string NormalizeForDuplicateComparison(string text) =>
        RepeatedHorizontalSpaceRegex().Replace((text ?? string.Empty).Trim(), " ").ToUpperInvariant();

    [GeneratedRegex("[ \\u00A0]{2,}")]
    private static partial Regex RepeatedHorizontalSpaceRegex();
}
