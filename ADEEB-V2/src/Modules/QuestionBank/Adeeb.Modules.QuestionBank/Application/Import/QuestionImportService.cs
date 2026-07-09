using Adeeb.Application.Abstractions.Localization;
using Adeeb.Modules.AcademicCatalog.Contracts;
using Adeeb.Modules.QuestionBank.Contracts;
using Adeeb.SharedKernel.Errors;
using Adeeb.SharedKernel.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Adeeb.Modules.QuestionBank.Application.Import;

public sealed class QuestionImportService(
    IEnumerable<IDocumentTextExtractor> extractors,
    IQuestionImportTextNormalizer normalizer,
    IQuestionDocumentParser parser,
    IAcademicCatalogLookup catalogLookup,
    QuestionBankService questionService,
    IOptions<QuestionImportOptions> options,
    ILogger<QuestionImportService> logger) : IQuestionImportService
{
    private readonly QuestionImportOptions options = options.Value;

    public async Task<Result<QuestionImportPreviewResponse>> ParseAsync(QuestionImportParseFormRequest request, CancellationToken cancellationToken)
    {
        var validation = await ValidateImportContextAsync(request.SubjectId, request.TopicId, request.Difficulty, cancellationToken);
        if (validation.IsFailure)
        {
            return validation.ValidationErrors is not null
                ? Result<QuestionImportPreviewResponse>.ValidationFailure(validation.ValidationErrors)
                : Result<QuestionImportPreviewResponse>.Failure(validation.Error!);
        }

        var fileValidation = ValidateFile(request.File);
        if (fileValidation.IsFailure)
        {
            return Result<QuestionImportPreviewResponse>.ValidationFailure(fileValidation.ValidationErrors!);
        }

        var file = request.File!;
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var extractor = extractors.FirstOrDefault(x => x.CanHandle(extension, file.ContentType));
        if (extractor is null)
        {
            return Result<QuestionImportPreviewResponse>.Failure(QuestionBankErrors.ImportExtractorNotFound);
        }

        var safeFileName = Path.GetFileName(file.FileName);
        logger.LogInformation("Question import parse started. FileName={FileName} Extension={Extension} Size={Size}", safeFileName, extension, file.Length);

        await using var memory = new MemoryStream();
        await file.CopyToAsync(memory, cancellationToken);
        memory.Position = 0;

        var extracted = await extractor.ExtractTextAsync(memory, cancellationToken);
        var normalized = normalizer.Normalize(extracted);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return Result<QuestionImportPreviewResponse>.Failure(QuestionBankErrors.ImportNoExtractableText);
        }

        var parseResult = parser.Parse(normalized);
        if (parseResult.DocumentErrors.Count > 0 && parseResult.Questions.Count == 0)
        {
            return Result<QuestionImportPreviewResponse>.ValidationFailure(new Dictionary<string, IReadOnlyList<Error>>
            {
                ["document"] = parseResult.DocumentErrors.Select(ToValidationError).ToList()
            });
        }

        var questions = parseResult.Questions.Take(options.MaxQuestionsPerImport).Select(ApplyConfiguredValidation).ToList();
        if (parseResult.Questions.Count > options.MaxQuestionsPerImport)
        {
            questions.Add(new ParsedQuestion
            {
                ClientKey = $"q-{options.MaxQuestionsPerImport + 1}",
                Errors = [new("question_import.max_questions_exceeded", $"Maximum questions per import is {options.MaxQuestionsPerImport}.")]
            });
        }

        await AddDatabaseDuplicateWarningsAsync(request.SubjectId, request.TopicId, questions, cancellationToken);
        var response = ToPreviewResponse(safeFileName, questions);

        logger.LogInformation(
            "Question import parse completed. FileName={FileName} Total={Total} Valid={Valid} Invalid={Invalid} Warnings={Warnings}",
            safeFileName,
            response.Summary.TotalDetected,
            response.Summary.Valid,
            response.Summary.Invalid,
            response.Summary.Warnings);

        return Result<QuestionImportPreviewResponse>.Success(response);
    }

    public async Task<Result<QuestionImportConfirmResponse>> ConfirmAsync(QuestionImportConfirmRequest request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Question import confirm started. SubjectId={SubjectId} TopicId={TopicId} Count={Count}", request.SubjectId, request.TopicId, request.Questions.Count);

        var validation = await ValidateImportContextAsync(request.SubjectId, request.TopicId, request.Difficulty, cancellationToken);
        if (validation.IsFailure)
        {
            return validation.ValidationErrors is not null
                ? Result<QuestionImportConfirmResponse>.ValidationFailure(validation.ValidationErrors)
                : Result<QuestionImportConfirmResponse>.Failure(validation.Error!);
        }

        var requests = new List<QuestionUpsertRequest>();
        var errors = new Dictionary<string, IReadOnlyList<Error>>(StringComparer.OrdinalIgnoreCase);
        if (request.Questions.Count == 0)
        {
            errors["questions"] = [Error.Validation("question_import.questions_required", "Validation.Required")];
        }

        if (request.Questions.Count > options.MaxQuestionsPerImport)
        {
            errors["questions"] = [Error.Validation("question_import.max_questions_exceeded", "QuestionImport.MaxQuestionsExceeded")];
        }

        for (var i = 0; i < request.Questions.Count && errors.Count == 0; i++)
        {
            var confirmQuestion = request.Questions[i];
            if (confirmQuestion.QuestionType is { } requestedType && !Enum.IsDefined(typeof(Domain.QuestionType), requestedType))
            {
                errors[$"questions[{i}].questionType"] = [Error.Validation("question.type.invalid", "QuestionBank.InvalidType")];
                continue;
            }

            var questionType = DetectConfirmQuestionType(confirmQuestion);
            var parsed = new ParsedQuestion
            {
                ClientKey = $"q-{i + 1}",
                QuestionType = questionType,
                QuestionText = confirmQuestion.QuestionText,
                ExpectedAnswer = questionType == Domain.QuestionType.ClosedAnswer ? confirmQuestion.ExpectedAnswer : null,
                Options = ToParsedOptions(confirmQuestion, questionType)
            };

            parsed = ApplyConfiguredValidation(parsed);
            if (!parsed.IsValid)
            {
                errors[$"questions[{i}]"] = parsed.Errors.Select(ToValidationError).ToList();
                continue;
            }

            requests.Add(ToUpsertRequest(request.SubjectId, request.TopicId, request.Difficulty, parsed));
        }

        if (errors.Count > 0)
        {
            return Result<QuestionImportConfirmResponse>.ValidationFailure(errors);
        }

        var created = await questionService.CreateQuestionsAsync(requests, SupportedLanguage.Tajik, cancellationToken);
        if (created.IsFailure)
        {
            return created.ValidationErrors is not null
                ? Result<QuestionImportConfirmResponse>.ValidationFailure(created.ValidationErrors)
                : Result<QuestionImportConfirmResponse>.Failure(created.Error!);
        }

        var ids = created.Value!.Select(x => x.Id).ToList();
        logger.LogInformation("Question import confirm completed. ImportedCount={ImportedCount}", ids.Count);
        return Result<QuestionImportConfirmResponse>.Success(new QuestionImportConfirmResponse(ids.Count, ids));
    }

    private async Task<Result> ValidateImportContextAsync(Guid subjectId, Guid? topicId, int difficulty, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, IReadOnlyList<Error>>(StringComparer.OrdinalIgnoreCase);
        if (subjectId == Guid.Empty)
        {
            errors["subjectId"] = [Error.Validation("question.subject_id.required", "Validation.Required")];
        }

        if (!Enum.IsDefined(typeof(Domain.DifficultyLevel), difficulty))
        {
            errors["difficulty"] = [Error.Validation("question.difficulty.invalid", "QuestionBank.InvalidDifficulty")];
        }

        if (errors.Count > 0)
        {
            return Result.ValidationFailure(errors);
        }

        if (!await catalogLookup.SubjectExistsAsync(subjectId, cancellationToken))
        {
            return Result.Failure(QuestionBankErrors.SubjectNotFound);
        }

        if (topicId.HasValue && !await catalogLookup.TopicBelongsToSubjectAsync(topicId.Value, subjectId, cancellationToken))
        {
            return Result.Failure(QuestionBankErrors.TopicNotFound);
        }

        return Result.Success();
    }

    private Result ValidateFile(Microsoft.AspNetCore.Http.IFormFile? file)
    {
        var errors = new Dictionary<string, IReadOnlyList<Error>>(StringComparer.OrdinalIgnoreCase);
        if (file is null || file.Length == 0)
        {
            errors["file"] = [Error.Validation("question_import.file_required", "Validation.Required")];
            return Result.ValidationFailure(errors);
        }

        if (file.Length > options.MaxFileSizeBytes)
        {
            errors["file"] = [Error.Validation("question_import.file_too_large", "QuestionImport.FileTooLarge")];
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!options.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            errors["file"] = [Error.Validation("question_import.unsupported_extension", "QuestionImport.UnsupportedExtension")];
        }

        if (!extractors.Any(x => x.CanHandle(extension, file.ContentType)))
        {
            errors["file"] = [Error.Validation("question_import.unsupported_content_type", "QuestionImport.UnsupportedContentType")];
        }

        return errors.Count == 0 ? Result.Success() : Result.ValidationFailure(errors);
    }

    private ParsedQuestion ApplyConfiguredValidation(ParsedQuestion question)
    {
        var errors = question.Errors.ToList();
        var warnings = question.Warnings.ToList();

        if (question.QuestionText.Length > options.MaxQuestionTextLength)
        {
            errors.Add(new("question_import.question_text_too_long", $"Question text cannot exceed {options.MaxQuestionTextLength} characters."));
        }

        if (question.Options.Count > options.MaxOptionsPerQuestion)
        {
            errors.Add(new("question_import.too_many_options", $"Question cannot have more than {options.MaxOptionsPerQuestion} options."));
        }

        if (question.QuestionType == Domain.QuestionType.ClosedAnswer && string.IsNullOrWhiteSpace(question.ExpectedAnswer))
        {
            errors.Add(new("question_import.expected_answer_required", "Expected answer text is empty."));
        }

        foreach (var option in question.Options)
        {
            if (option.Text.Length > options.MaxOptionTextLength)
            {
                errors.Add(new("question_import.option_text_too_long", $"Option {option.Label} cannot exceed {options.MaxOptionTextLength} characters."));
            }
        }

        return new ParsedQuestion
        {
            ClientKey = question.ClientKey,
            QuestionType = question.QuestionType,
            QuestionText = question.QuestionText,
            ExpectedAnswer = question.ExpectedAnswer,
            Options = question.Options,
            Errors = errors,
            Warnings = warnings
        };
    }

    private async Task AddDatabaseDuplicateWarningsAsync(Guid subjectId, Guid? topicId, IList<ParsedQuestion> questions, CancellationToken cancellationToken)
    {
        var existing = (await questionService.GetQuestionTextsAsync(subjectId, topicId, cancellationToken))
            .Select(normalizer.NormalizeForDuplicateComparison)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < questions.Count; i++)
        {
            var key = normalizer.NormalizeForDuplicateComparison(questions[i].QuestionText);
            if (!existing.Contains(key))
            {
                continue;
            }

            questions[i] = questions[i].WithWarnings(new("question_import.possible_duplicate_in_database", "Possible duplicate question already exists in database."));
        }
    }

    private static QuestionImportPreviewResponse ToPreviewResponse(string fileName, IReadOnlyList<ParsedQuestion> questions)
    {
        var responseQuestions = questions.Select(question => new QuestionImportPreviewQuestionResponse(
            question.ClientKey,
            (int)question.QuestionType,
            question.QuestionType.ToString(),
            question.QuestionText,
            question.ExpectedAnswer,
            question.Options.Select(option => new QuestionImportPreviewOptionResponse(option.Label, option.Text, option.IsCorrect)).ToList(),
            question.IsValid,
            question.Errors.Select(issue => new QuestionImportIssueResponse(issue.Code, issue.Message)).ToList(),
            question.Warnings.Select(issue => new QuestionImportIssueResponse(issue.Code, issue.Message)).ToList())).ToList();

        return new QuestionImportPreviewResponse(
            fileName,
            new QuestionImportSummaryResponse(
                responseQuestions.Count,
                responseQuestions.Count(x => x.IsValid),
                responseQuestions.Count(x => !x.IsValid),
                responseQuestions.Sum(x => x.Warnings.Count)),
            responseQuestions);
    }

    private static QuestionUpsertRequest ToUpsertRequest(Guid subjectId, Guid? topicId, int difficulty, ParsedQuestion question) =>
        new(
            subjectId,
            topicId,
            null,
            (int)question.QuestionType,
            difficulty,
            (int)Domain.QuestionStatus.Active,
            null,
            [
                new((int)SupportedLanguage.Tajik, question.QuestionText, null),
                new((int)SupportedLanguage.Russian, question.QuestionText, null),
                new((int)SupportedLanguage.English, question.QuestionText, null)
            ],
            ToAnswerOptions(question));

    private static IReadOnlyList<AnswerOptionRequest> ToAnswerOptions(ParsedQuestion question)
    {
        if (question.QuestionType == Domain.QuestionType.ClosedAnswer)
        {
            var answer = question.ExpectedAnswer ?? question.Options.FirstOrDefault()?.Text ?? string.Empty;
            return
            [
                new AnswerOptionRequest(
                    1,
                    true,
                    [
                        new((int)SupportedLanguage.Tajik, answer, null),
                        new((int)SupportedLanguage.Russian, answer, null),
                        new((int)SupportedLanguage.English, answer, null)
                    ])
            ];
        }

        return question.Options.Select((option, index) => new AnswerOptionRequest(
                index + 1,
                option.IsCorrect,
                [
                    new((int)SupportedLanguage.Tajik, option.Text, null),
                    new((int)SupportedLanguage.Russian, option.Text, null),
                    new((int)SupportedLanguage.English, option.Text, null)
                ])).ToList();
    }

    private static Domain.QuestionType DetectConfirmQuestionType(QuestionImportConfirmQuestionRequest question)
    {
        if (question.QuestionType.HasValue && Enum.IsDefined(typeof(Domain.QuestionType), question.QuestionType.Value))
        {
            return (Domain.QuestionType)question.QuestionType.Value;
        }

        if (!string.IsNullOrWhiteSpace(question.ExpectedAnswer) && question.Options.Count == 0)
        {
            return Domain.QuestionType.ClosedAnswer;
        }

        return Domain.QuestionType.SingleChoice;
    }

    private static IReadOnlyList<ParsedOption> ToParsedOptions(QuestionImportConfirmQuestionRequest question, Domain.QuestionType questionType)
    {
        if (questionType == Domain.QuestionType.ClosedAnswer)
        {
            return
            [
                new ParsedOption("A", question.ExpectedAnswer ?? string.Empty, true)
            ];
        }

        return question.Options.Select((option, index) => new ParsedOption(((char)('A' + index)).ToString(), option.Text, option.IsCorrect)).ToList();
    }

    private static Error ToValidationError(QuestionParseIssue issue) => Error.Validation(issue.Code, issue.Message);
}

file static class ParsedQuestionExtensions
{
    public static ParsedQuestion WithWarnings(this ParsedQuestion question, QuestionParseIssue warning)
    {
        var warnings = question.Warnings.ToList();
        warnings.Add(warning);
        return new ParsedQuestion
        {
            ClientKey = question.ClientKey,
            QuestionType = question.QuestionType,
            QuestionText = question.QuestionText,
            ExpectedAnswer = question.ExpectedAnswer,
            Options = question.Options,
            Errors = question.Errors,
            Warnings = warnings
        };
    }
}
