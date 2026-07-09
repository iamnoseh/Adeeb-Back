using Adeeb.Modules.QuestionBank.Application.Import;
using Adeeb.Modules.QuestionBank.Domain;

namespace Adeeb.QuestionBank.Tests;

public sealed class QuestionImportParserTests
{
    private readonly QuestionDocumentParser parser = new(new QuestionImportTextNormalizer());

    [Fact]
    public void Basic_valid_question_has_four_options_and_one_correct_answer()
    {
        var result = parser.Parse("""
            <<<Question?>>>
            -- А) Correct
            Б) Wrong
            С) Wrong
            Д) Wrong
            """);

        var question = Assert.Single(result.Questions);
        Assert.True(question.IsValid);
        Assert.Equal(4, question.Options.Count);
        Assert.Single(question.Options, option => option.IsCorrect);
        Assert.Equal("A", question.Options[0].Label);
    }

    [Fact]
    public void Direct_answer_basic_is_detected_as_closed_answer()
    {
        var result = parser.Parse("""
            <<<2 + 5 = ?>>>
            -- A) 7
            """);

        var question = Assert.Single(result.Questions);
        Assert.True(question.IsValid);
        Assert.Equal(QuestionType.ClosedAnswer, question.QuestionType);
        Assert.Equal("7", question.ExpectedAnswer);
        Assert.Single(question.Options);
    }

    [Fact]
    public void Direct_answer_cyrillic_label_is_supported()
    {
        var result = parser.Parse("<<<2 * 9 = ?>>>\n-- \u0410) 18");

        var question = Assert.Single(result.Questions);
        Assert.True(question.IsValid);
        Assert.Equal(QuestionType.ClosedAnswer, question.QuestionType);
        Assert.Equal("18", question.ExpectedAnswer);
    }

    [Theory]
    [InlineData("--A) 7")]
    [InlineData("-- A) 7")]
    [InlineData("--  A) 7")]
    [InlineData("-- A. 7")]
    [InlineData("-- A: 7")]
    [InlineData("-- a) 7")]
    public void Direct_answer_marker_and_label_variants_are_supported(string answerLine)
    {
        var result = parser.Parse($"""
            <<<2 + 5 = ?>>>
            {answerLine}
            """);

        var question = Assert.Single(result.Questions);
        Assert.True(question.IsValid);
        Assert.Equal(QuestionType.ClosedAnswer, question.QuestionType);
        Assert.Equal("7", question.ExpectedAnswer);
    }

    [Fact]
    public void Direct_answer_multiline_text_is_preserved()
    {
        var result = parser.Parse("""
            <<<Explain shortly>>>
            -- A) First line
            second line
            third line
            """);

        var question = Assert.Single(result.Questions);
        Assert.True(question.IsValid);
        Assert.Equal(QuestionType.ClosedAnswer, question.QuestionType);
        Assert.Equal("First line\nsecond line\nthird line", question.ExpectedAnswer);
    }

    [Theory]
    [InlineData("-- A) -7", "-7")]
    [InlineData("-- A) 2.5", "2.5")]
    [InlineData("-- A) 3,14", "3,14")]
    [InlineData("-- A) 1/2", "1/2")]
    [InlineData("-- A) 2025-2026", "2025-2026")]
    [InlineData("-- A) A-B", "A-B")]
    public void Direct_answer_preserves_numeric_and_punctuation_text(string answerLine, string expected)
    {
        var result = parser.Parse($"""
            <<<Answer?>>>
            {answerLine}
            """);

        var question = Assert.Single(result.Questions);
        Assert.True(question.IsValid);
        Assert.Equal(QuestionType.ClosedAnswer, question.QuestionType);
        Assert.Equal(expected, question.ExpectedAnswer);
    }

    [Fact]
    public void One_unmarked_answer_is_invalid()
    {
        var result = parser.Parse("""
            <<<Question?>>>
            A) Answer
            """);

        var question = Assert.Single(result.Questions);
        Assert.False(question.IsValid);
        Assert.Equal(QuestionType.SingleChoice, question.QuestionType);
        Assert.Contains(question.Errors, issue => issue.Code == "question_import.correct_option_required");
    }

    [Fact]
    public void Empty_direct_answer_is_invalid()
    {
        var result = parser.Parse("""
            <<<Question?>>>
            -- A)
            """);

        var question = Assert.Single(result.Questions);
        Assert.False(question.IsValid);
        Assert.Equal(QuestionType.ClosedAnswer, question.QuestionType);
        Assert.Contains(question.Errors, issue => issue.Code == "question_import.expected_answer_required");
    }

    [Theory]
    [InlineData("--А) Correct")]
    [InlineData("-- А) Correct")]
    [InlineData("--  А) Correct")]
    public void Correct_marker_spacing_is_tolerated(string correctLine)
    {
        var result = parser.Parse($"""
            <<<Question?>>>
            {correctLine}
            Б) Wrong
            С) Wrong
            Д) Wrong
            """);

        var question = Assert.Single(result.Questions);
        Assert.True(question.Options[0].IsCorrect);
        Assert.True(question.IsValid);
    }

    [Fact]
    public void Correct_option_can_be_non_first()
    {
        var result = parser.Parse("""
            <<<Question?>>>
            A) Wrong
            -- B) Correct
            C) Wrong
            D) Wrong
            """);

        var question = Assert.Single(result.Questions);
        Assert.True(question.Options[1].IsCorrect);
        Assert.True(question.IsValid);
    }

    [Fact]
    public void Multiple_questions_parse_independently()
    {
        var result = parser.Parse("""
            <<<First?>>>
            -- A) Correct
            B) Wrong

            <<<Second?>>>
            A) Wrong
            -- B) Correct
            """);

        Assert.Equal(2, result.Questions.Count);
        Assert.All(result.Questions, question => Assert.True(question.IsValid));
    }

    [Fact]
    public void Multiline_question_is_supported()
    {
        var result = parser.Parse("""
            <<<
            Long question
            continued?
            >>>
            -- A) Correct
            B) Wrong
            """);

        var question = Assert.Single(result.Questions);
        Assert.Equal("Long question\ncontinued?", question.QuestionText);
    }

    [Fact]
    public void Multiline_option_is_joined_as_continuation()
    {
        var result = parser.Parse("""
            <<<Question?>>>
            -- A) Correct first line
            continued line
            B) Wrong
            """);

        var question = Assert.Single(result.Questions);
        Assert.Equal("Correct first line\ncontinued line", question.Options[0].Text);
    }

    [Fact]
    public void Missing_correct_answer_is_invalid()
    {
        var result = parser.Parse("""
            <<<Question?>>>
            A) One
            B) Two
            """);

        var question = Assert.Single(result.Questions);
        Assert.False(question.IsValid);
        Assert.Contains(question.Errors, issue => issue.Code == "question_import.correct_option_required");
    }

    [Fact]
    public void Multiple_correct_answers_are_invalid()
    {
        var result = parser.Parse("""
            <<<Question?>>>
            -- A) One
            -- B) Two
            """);

        var question = Assert.Single(result.Questions);
        Assert.False(question.IsValid);
        Assert.Equal(QuestionType.SingleChoice, question.QuestionType);
        Assert.Contains(question.Errors, issue => issue.Code == "question_import.multiple_correct_options");
    }

    [Fact]
    public void Two_option_single_choice_remains_single_choice()
    {
        var result = parser.Parse("""
            <<<Question?>>>
            -- A) Correct
            B) Wrong
            """);

        var question = Assert.Single(result.Questions);
        Assert.True(question.IsValid);
        Assert.Equal(QuestionType.SingleChoice, question.QuestionType);
        Assert.Equal(2, question.Options.Count);
        Assert.Null(question.ExpectedAnswer);
    }

    [Fact]
    public void Mixed_document_supports_closed_answer_and_single_choice()
    {
        var result = parser.Parse("""
            <<<2 + 5 = ?>>>
            -- A) 7

            <<<Capital of Tajikistan?>>>
            -- A) Dushanbe

            <<<Choose correct item>>>
            A) Wrong
            -- B) Correct
            C) Wrong
            D) Wrong
            """);

        Assert.Equal(3, result.Questions.Count);
        Assert.All(result.Questions, question => Assert.True(question.IsValid));
        Assert.Equal(QuestionType.ClosedAnswer, result.Questions[0].QuestionType);
        Assert.Equal(QuestionType.ClosedAnswer, result.Questions[1].QuestionType);
        Assert.Equal(QuestionType.SingleChoice, result.Questions[2].QuestionType);
    }

    [Fact]
    public void Empty_question_is_invalid()
    {
        var result = parser.Parse("""
            <<<   >>>
            -- A) One
            B) Two
            """);

        var question = Assert.Single(result.Questions);
        Assert.False(question.IsValid);
        Assert.Contains(question.Errors, issue => issue.Code == "question_import.question_text_required");
    }

    [Fact]
    public void Missing_closing_marker_returns_document_error()
    {
        var result = parser.Parse("""
            <<<Question?
            -- A) Correct
            """);

        Assert.Empty(result.Questions);
        Assert.Contains(result.DocumentErrors, issue => issue.Code == "question_import.missing_closing_marker");
    }

    [Fact]
    public void Cyrillic_latin_and_lowercase_labels_are_normalized()
    {
        var result = parser.Parse("""
            <<<Question?>>>
            -- а) Correct
            b) Wrong
            с) Wrong
            d) Wrong
            """);

        var question = Assert.Single(result.Questions);
        Assert.Equal(["A", "B", "C", "D"], question.Options.Select(x => x.Label));
    }

    [Fact]
    public void Internal_duplicate_question_adds_warning()
    {
        var result = parser.Parse("""
            <<<Same?>>>
            -- A) Correct
            B) Wrong

            <<< Same? >>>
            -- A) Correct
            B) Wrong
            """);

        Assert.Contains(result.Questions[1].Warnings, issue => issue.Code == "question_import.duplicate_in_file");
    }
}
