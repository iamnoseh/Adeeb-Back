.using Adeeb.Modules.QuestionBank.Application.Import;

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
        Assert.Contains(question.Errors, issue => issue.Code == "question_import.multiple_correct_options");
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
