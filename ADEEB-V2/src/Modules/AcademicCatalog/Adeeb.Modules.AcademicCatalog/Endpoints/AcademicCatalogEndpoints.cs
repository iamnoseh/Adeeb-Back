using Adeeb.Application.Abstractions.Localization;
using Adeeb.Modules.AcademicCatalog.Application;
using Adeeb.Modules.AcademicCatalog.Contracts;
using Adeeb.Modules.AcademicCatalog.Infrastructure.Files;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Adeeb.Modules.AcademicCatalog.Endpoints;

public static class AcademicCatalogEndpoints
{
    public static IEndpointRouteBuilder MapAcademicCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        var subjects = app.MapGroup("/api/v2/subjects").WithTags("Academic Catalog");
        subjects.MapGet("/", async ([AsParameters] AcademicListQuery query, AcademicCatalogService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
            (await service.GetSubjectsAsync(query, CurrentLanguage(), admin: false, ct)).ToHttpResult(context, localizer));

        subjects.MapGet("/{id:guid}", async (Guid id, AcademicCatalogService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
            (await service.GetSubjectAsync(id, CurrentLanguage(), admin: false, ct)).ToHttpResult(context, localizer));

        subjects.MapGet("/{id:guid}/topics", async (Guid id, [AsParameters] AcademicListQuery query, AcademicCatalogService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
            (await service.GetTopicsAsync(id, query, CurrentLanguage(), admin: false, ct)).ToHttpResult(context, localizer));

        var adminSubjects = app.MapGroup("/api/v2/admin/subjects").WithTags("Academic Catalog Admin").RequireAuthorization("ContentAdmin");
        adminSubjects.MapGet("/", async ([AsParameters] AcademicListQuery query, AcademicCatalogService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
            (await service.GetSubjectsAsync(query, CurrentLanguage(), admin: true, ct)).ToHttpResult(context, localizer));
        adminSubjects.MapGet("/{id:guid}", async (Guid id, AcademicCatalogService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
            (await service.GetSubjectAsync(id, CurrentLanguage(), admin: true, ct)).ToHttpResult(context, localizer));
        adminSubjects.MapPost("/", async ([FromForm] SubjectFormRequest form, AcademicFileStorage files, AcademicCatalogService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
        {
            var saved = await files.SaveSubjectIconAsync(form.Icon, ct);
            if (saved.IsFailure)
            {
                return saved.ToHttpResult(context, localizer);
            }

            return (await service.CreateSubjectAsync(ToSubjectUpsert(form, saved.Value), CurrentLanguage(), ct)).ToHttpResult(context, localizer);
        })
        .Accepts<SubjectFormRequest>("multipart/form-data")
        .DisableAntiforgery();

        adminSubjects.MapPut("/{id:guid}", async (Guid id, [FromForm] SubjectFormRequest form, AcademicFileStorage files, AcademicCatalogService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
        {
            var saved = await files.SaveSubjectIconAsync(form.Icon, ct);
            if (saved.IsFailure)
            {
                return saved.ToHttpResult(context, localizer);
            }

            return (await service.UpdateSubjectAsync(id, ToSubjectUpsert(form, saved.Value), CurrentLanguage(), ct)).ToHttpResult(context, localizer);
        })
        .Accepts<SubjectFormRequest>("multipart/form-data")
        .DisableAntiforgery();
        adminSubjects.MapPost("/{id:guid}/archive", async (Guid id, AcademicCatalogService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
            (await service.ArchiveSubjectAsync(id, ct)).ToHttpResult(context, localizer));
        adminSubjects.MapDelete("/{id:guid}", async (Guid id, AcademicCatalogService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
            (await service.DeleteSubjectAsync(id, ct)).ToHttpResult(context, localizer));

        var adminTopics = app.MapGroup("/api/v2/admin/topics").WithTags("Academic Catalog Admin").RequireAuthorization("ContentAdmin");
        adminTopics.MapGet("/", async (Guid? subjectId, [AsParameters] AcademicListQuery query, AcademicCatalogService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
            (await service.GetTopicsAsync(subjectId, query, CurrentLanguage(), admin: true, ct)).ToHttpResult(context, localizer));
        adminTopics.MapPost("/", async (TopicUpsertRequest request, AcademicCatalogService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
            (await service.CreateTopicAsync(request, CurrentLanguage(), ct)).ToHttpResult(context, localizer));
        adminTopics.MapPut("/{id:guid}", async (Guid id, TopicUpsertRequest request, AcademicCatalogService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
            (await service.UpdateTopicAsync(id, request, CurrentLanguage(), ct)).ToHttpResult(context, localizer));
        adminTopics.MapPost("/{id:guid}/archive", async (Guid id, AcademicCatalogService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
            (await service.ArchiveTopicAsync(id, ct)).ToHttpResult(context, localizer));
        adminTopics.MapDelete("/{id:guid}", async (Guid id, AcademicCatalogService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
            (await service.DeleteTopicAsync(id, ct)).ToHttpResult(context, localizer));

        return app;
    }

    private static SupportedLanguage CurrentLanguage() =>
        SupportedLanguageExtensions.TryParseCulture(System.Globalization.CultureInfo.CurrentUICulture.Name, out var language)
            ? language
            : SupportedLanguage.Tajik;

    private static SubjectUpsertRequest ToSubjectUpsert(SubjectFormRequest form, string? iconUrl)
    {
        var name = form.Name.Trim();
        return new SubjectUpsertRequest(
            ToCode(name),
            iconUrl,
            form.DisplayOrder,
            form.Status,
            [
                new TranslationRequest((int)SupportedLanguage.Tajik, name, null),
                new TranslationRequest((int)SupportedLanguage.Russian, name, null),
                new TranslationRequest((int)SupportedLanguage.English, name, null)
            ]);
    }

    private static string ToCode(string value)
    {
        var code = new string(value.Trim().Select(ch => char.IsLetterOrDigit(ch) ? char.ToUpperInvariant(ch) : '_').ToArray()).Trim('_');
        return string.IsNullOrWhiteSpace(code) ? "SUBJECT" : code;
    }
}
