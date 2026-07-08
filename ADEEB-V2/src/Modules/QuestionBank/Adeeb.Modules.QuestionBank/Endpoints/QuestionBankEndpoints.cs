using System.Text.Json;
using Adeeb.Application.Abstractions.Localization;
using Adeeb.Modules.QuestionBank.Application;
using Adeeb.Modules.QuestionBank.Application.Import;
using Adeeb.Modules.QuestionBank.Contracts;
using Adeeb.Modules.QuestionBank.Infrastructure.Files;
using Adeeb.SharedKernel.Errors;
using Adeeb.SharedKernel.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Adeeb.Modules.QuestionBank.Endpoints;

public static class QuestionBankEndpoints
{
    public static IEndpointRouteBuilder MapQuestionBankEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v2/admin/questions").WithTags("Question Bank Admin").RequireAuthorization("ContentAdmin");
        group.MapGet("/", async ([AsParameters] QuestionListQuery query, QuestionBankService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
            (await service.GetQuestionsAsync(query, CurrentLanguage(), ct)).ToHttpResult(context, localizer));
        group.MapGet("/{id:guid}", async (Guid id, QuestionBankService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
            (await service.GetQuestionAsync(id, CurrentLanguage(), ct)).ToHttpResult(context, localizer));
        group.MapPost("/import/parse", async ([FromForm] QuestionImportParseFormRequest form, IQuestionImportService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
            (await service.ParseAsync(form, ct)).ToHttpResult(context, localizer))
            .Accepts<QuestionImportParseFormRequest>("multipart/form-data")
            .DisableAntiforgery();
        group.MapPost("/import/confirm", async (QuestionImportConfirmRequest request, IQuestionImportService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
            (await service.ConfirmAsync(request, ct)).ToHttpResult(context, localizer));
        group.MapPost("/", async ([FromForm] QuestionFormRequest form, QuestionImageStorage images, QuestionBankService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
        {
            var parsed = ToUpsertRequest(form, imageUrl: null);
            if (parsed.IsFailure)
            {
                return parsed.ToHttpResult(context, localizer);
            }

            var saved = await images.SaveAsync(form.Image, ct);
            if (saved.IsFailure)
            {
                return saved.ToHttpResult(context, localizer);
            }

            var request = parsed.Value! with { ImageUrl = saved.Value };
            return (await service.CreateQuestionAsync(request, CurrentLanguage(), ct)).ToHttpResult(context, localizer);
        })
        .Accepts<QuestionFormRequest>("multipart/form-data")
        .DisableAntiforgery();

        group.MapPut("/{id:guid}", async (Guid id, [FromForm] QuestionFormRequest form, QuestionImageStorage images, QuestionBankService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
        {
            var parsed = ToUpsertRequest(form, imageUrl: null);
            if (parsed.IsFailure)
            {
                return parsed.ToHttpResult(context, localizer);
            }

            var saved = await images.SaveAsync(form.Image, ct);
            if (saved.IsFailure)
            {
                return saved.ToHttpResult(context, localizer);
            }

            var request = parsed.Value! with { ImageUrl = saved.Value };
            return (await service.UpdateQuestionAsync(id, request, CurrentLanguage(), ct)).ToHttpResult(context, localizer);
        })
        .Accepts<QuestionFormRequest>("multipart/form-data")
        .DisableAntiforgery();
        group.MapPost("/{id:guid}/archive", async (Guid id, QuestionBankService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
            (await service.ArchiveQuestionAsync(id, ct)).ToHttpResult(context, localizer));
        group.MapDelete("/{id:guid}", async (Guid id, QuestionBankService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
            (await service.DeleteQuestionAsync(id, ct)).ToHttpResult(context, localizer));
        return app;
    }

    private static SupportedLanguage CurrentLanguage() =>
        SupportedLanguageExtensions.TryParseCulture(System.Globalization.CultureInfo.CurrentUICulture.Name, out var language)
            ? language
            : SupportedLanguage.Tajik;

    private static Result<QuestionUpsertRequest> ToUpsertRequest(QuestionFormRequest form, string? imageUrl)
    {
        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var answerOptions = ToAnswerOptions(form, options);
            var translations = new List<QuestionTranslationRequest>
            {
                new((int)SupportedLanguage.Tajik, form.Content, form.Explanation),
                new((int)SupportedLanguage.Russian, form.Content, form.Explanation),
                new((int)SupportedLanguage.English, form.Content, form.Explanation)
            };
            return Result<QuestionUpsertRequest>.Success(new(
                form.SubjectId,
                form.TopicId,
                null,
                form.Type,
                form.Difficulty,
                form.Status,
                imageUrl,
                translations,
                answerOptions));
        }
        catch (JsonException)
        {
            return Result<QuestionUpsertRequest>.ValidationFailure(new Dictionary<string, IReadOnlyList<Error>>
            {
                ["form"] = [Error.Validation("question.form.invalid_json", "QuestionBank.InvalidFormJson")]
            });
        }
    }

    private static IReadOnlyList<AnswerOptionRequest> ToAnswerOptions(QuestionFormRequest form, JsonSerializerOptions options)
    {
        if (form.Type == 3)
        {
            return
            [
                new AnswerOptionRequest(
                    1,
                    true,
                    [
                        new AnswerOptionTranslationRequest((int)SupportedLanguage.Tajik, form.CorrectAnswer ?? string.Empty, null),
                        new AnswerOptionTranslationRequest((int)SupportedLanguage.Russian, form.CorrectAnswer ?? string.Empty, null),
                        new AnswerOptionTranslationRequest((int)SupportedLanguage.English, form.CorrectAnswer ?? string.Empty, null)
                    ])
            ];
        }

        var legacyAnswers = string.IsNullOrWhiteSpace(form.AnswersJson)
            ? []
            : JsonSerializer.Deserialize<IReadOnlyList<LegacyAnswerFormRequest>>(form.AnswersJson, options) ?? [];

        return legacyAnswers.Select((answer, index) =>
        {
            var text = answer.Text ?? answer.Answer ?? string.Empty;
            return new AnswerOptionRequest(
                index + 1,
                answer.IsCorrect,
                [
                    new AnswerOptionTranslationRequest((int)SupportedLanguage.Tajik, text, answer.MatchPair),
                    new AnswerOptionTranslationRequest((int)SupportedLanguage.Russian, text, answer.MatchPair),
                    new AnswerOptionTranslationRequest((int)SupportedLanguage.English, text, answer.MatchPair)
                ]);
        }).ToList();
    }
}
