using System.Text;
using System.Text.RegularExpressions;

namespace Adeeb.Modules.QuestionBank.Application.Import;

public sealed partial class QuestionDocumentParser(IQuestionImportTextNormalizer normalizer) : IQuestionDocumentParser
{
    public QuestionParseResult Parse(string normalizedText)
    {
        var documentErrors = new List<QuestionParseIssue>();
        var questions = new List<ParsedQuestion>();

        if (string.IsNullOrWhiteSpace(normalizedText))
        {
            documentErrors.Add(new("question_import.empty_document", "Document text is empty."));
            return new QuestionParseResult(questions, documentErrors);
        }

        if (normalizedText.Contains("<<<", StringComparison.Ordinal) && !QuestionBlockRegex().IsMatch(normalizedText))
        {
            documentErrors.Add(new("question_import.missing_closing_marker", "Opening question marker was found without a closing marker."));
        }

        var matches = QuestionBlockRegex().Matches(normalizedText);
        if (matches.Count == 0)
        {
            documentErrors.Add(new("question_import.no_questions_detected", "No question blocks were detected."));
            return new QuestionParseResult(questions, documentErrors);
        }

        var firstQuestionIndex = matches[0].Index;
        var leadingText = normalizedText[..firstQuestionIndex].Trim();
        if (!string.IsNullOrWhiteSpace(leadingText))
        {
            documentErrors.Add(new("question_import.orphan_answer_lines", "Text was found before the first question block and was ignored."));
        }

        for (var i = 0; i < matches.Count; i++)
        {
            var match = matches[i];
            var nextStart = i + 1 < matches.Count ? matches[i + 1].Index : normalizedText.Length;
            var answerBlockStart = match.Index + match.Length;
            var answerBlock = normalizedText[answerBlockStart..nextStart];
            questions.Add(ParseQuestion(i + 1, match.Groups["question"].Value, answerBlock));
        }

        AddDuplicateWarnings(questions);
        return new QuestionParseResult(questions, documentErrors);
    }

    private ParsedQuestion ParseQuestion(int number, string rawQuestionText, string answerBlock)
    {
        var errors = new List<QuestionParseIssue>();
        var warnings = new List<QuestionParseIssue>();
        var questionText = NormalizeBlockText(rawQuestionText);
        if (string.IsNullOrWhiteSpace(questionText))
        {
            errors.Add(new("question_import.question_text_required", "Question text is empty."));
        }

        var options = ParseOptions(answerBlock, errors, warnings);
        ValidateOptions(options, errors, warnings);

        return new ParsedQuestion
        {
            ClientKey = $"q-{number}",
            QuestionText = questionText,
            Options = options,
            Errors = errors,
            Warnings = warnings
        };
    }

    private IReadOnlyList<ParsedOption> ParseOptions(string answerBlock, List<QuestionParseIssue> errors, List<QuestionParseIssue> warnings)
    {
        var options = new List<ParsedOptionBuilder>();
        var ignoredLines = 0;

        foreach (var line in answerBlock.Split('\n').Select(x => x.Trim()).Where(x => !string.IsNullOrWhiteSpace(x)))
        {
            var match = OptionPrefixRegex().Match(line);
            if (match.Success)
            {
                var label = NormalizeLabel(match.Groups["label"].Value);
                if (options.Any(x => x.Label == label))
                {
                    warnings.Add(new("question_import.duplicate_option_label", $"Duplicate option label {label} was detected."));
                }

                options.Add(new ParsedOptionBuilder(label, match.Groups["correct"].Success, match.Groups["text"].Value.Trim()));
                continue;
            }

            if (options.Count == 0)
            {
                ignoredLines++;
                continue;
            }

            options[^1].AppendContinuation(line);
        }

        if (ignoredLines > 0)
        {
            warnings.Add(new("question_import.extra_text_ignored", "Extra text before the first option was ignored."));
        }

        return options.Select(x => new ParsedOption(x.Label, NormalizeBlockText(x.Text.ToString()), x.IsCorrect)).ToList();
    }

    private static void ValidateOptions(IReadOnlyList<ParsedOption> options, List<QuestionParseIssue> errors, List<QuestionParseIssue> warnings)
    {
        if (options.Count == 0)
        {
            errors.Add(new("question_import.options_required", "No answer options were detected."));
            return;
        }

        if (options.Count < 2)
        {
            errors.Add(new("question_import.too_few_options", "At least two answer options are required."));
        }

        if (options.Count != 4)
        {
            warnings.Add(new("question_import.option_count_not_four", $"{options.Count} options were detected. Four options are expected for this import format."));
        }

        if (options.Count(x => x.IsCorrect) == 0)
        {
            errors.Add(new("question_import.correct_option_required", "No correct answer was detected."));
        }

        if (options.Count(x => x.IsCorrect) > 1)
        {
            errors.Add(new("question_import.multiple_correct_options", "Multiple correct answers were detected."));
        }

        for (var i = 0; i < options.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(options[i].Text))
            {
                errors.Add(new("question_import.option_text_required", $"Option {options[i].Label} text is empty."));
            }
        }

        var normalizedTexts = options.Select(x => x.Text.Trim()).Where(x => x.Length > 0).ToList();
        if (normalizedTexts.Count != normalizedTexts.Distinct(StringComparer.OrdinalIgnoreCase).Count())
        {
            warnings.Add(new("question_import.duplicate_option_text", "Duplicate option text was detected inside the question."));
        }
    }

    private void AddDuplicateWarnings(IReadOnlyList<ParsedQuestion> questions)
    {
        var seen = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var question in questions)
        {
            var key = normalizer.NormalizeForDuplicateComparison(question.QuestionText);
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            if (seen.TryGetValue(key, out _))
            {
                ((List<QuestionParseIssue>)question.Warnings).Add(new("question_import.duplicate_in_file", "Possible duplicate question inside uploaded file."));
            }
            else
            {
                seen[key] = question.ClientKey;
            }
        }
    }

    private static string NormalizeBlockText(string text) =>
        string.Join('\n', text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n')
            .Split('\n')
            .Select(x => x.Trim())
            .Where(x => x.Length > 0));

    private static string NormalizeLabel(string value) =>
        value.ToUpperInvariant() switch
        {
            "А" or "A" => "A",
            "Б" or "B" => "B",
            "С" or "C" => "C",
            "Д" or "D" => "D",
            _ => value.ToUpperInvariant()
        };

    [GeneratedRegex("<<<(?<question>[\\s\\S]*?)>>>", RegexOptions.Compiled)]
    private static partial Regex QuestionBlockRegex();

    [GeneratedRegex("^\\s*(?<correct>--\\s*)?(?<label>[AaАаBbБбCcСсDdДд])\\s*[\\)\\.:]\\s*(?<text>.*)$", RegexOptions.Compiled)]
    private static partial Regex OptionPrefixRegex();

    private sealed record ParsedOptionBuilder(string Label, bool IsCorrect, StringBuilder Text)
    {
        public ParsedOptionBuilder(string label, bool isCorrect, string text)
            : this(label, isCorrect, new StringBuilder(text))
        {
        }

        public void AppendContinuation(string line)
        {
            if (Text.Length > 0)
            {
                Text.Append('\n');
            }

            Text.Append(line.Trim());
        }
    }
}
