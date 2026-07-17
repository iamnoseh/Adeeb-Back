using Adeeb.Application.Abstractions.Authorization;
using Adeeb.Application.Abstractions.Localization;
using Adeeb.Modules.Mmt.Application;
using Adeeb.Modules.Mmt.Application.Import;
using Adeeb.Modules.Mmt.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Adeeb.Modules.Mmt.Endpoints;

public static class MmtEndpoints
{
    public static IEndpointRouteBuilder MapMmtEndpoints(this IEndpointRouteBuilder app)
    {
        var admin = app.MapGroup("/api/v2/admin/mmt").WithTags("MMT Data Management").RequireAuthorization(Permissions.Mmt.Manage);
        MapClusters(admin); MapUniversities(admin); MapSpecialties(admin); MapPrograms(admin); MapImport(admin); MapSimulatorAdmin(admin);
        admin.MapGet("/dashboard", async (MmtDashboardService service, CancellationToken ct) =>
            Results.Ok(await service.GetAsync(ct)));
        var student = app.MapGroup("/api/v2/mmt/admission-programs").WithTags("MMT Admissions").RequireAuthorization();
        student.MapGet("/", async ([AsParameters] AdmissionProgramFilter filter, AdmissionProgramService service, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await service.GetProgramsAsync(filter, false, CurrentLanguage(c), ct)).ToHttpResult(c, l));
        student.MapGet("/{id:guid}", async (Guid id, AdmissionProgramService service, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await service.GetProgramAsync(id, false, CurrentLanguage(c), ct)).ToHttpResult(c, l));
        student.MapGet("/{id:guid}/passing-scores", async (Guid id, AdmissionProgramService service, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await service.GetStudentScoresAsync(id, ct)).ToHttpResult(c, l));
        var studentLookups = app.MapGroup("/api/v2/mmt").WithTags("MMT Student Lookups").RequireAuthorization();
        studentLookups.MapGet("/clusters", async ([AsParameters] MmtPageQuery query, AdmissionProgramService service, HttpContext c, IMessageLocalizer l, CancellationToken ct) =>
            (await service.GetStudentClustersAsync(query, CurrentLanguage(c), ct)).ToHttpResult(c, l));
        studentLookups.MapGet("/specialties", async ([AsParameters] StudentSpecialtyLookupQuery query, AdmissionProgramService service, HttpContext c, IMessageLocalizer l, CancellationToken ct) =>
            (await service.GetStudentSpecialtiesAsync(query, CurrentLanguage(c), ct)).ToHttpResult(c, l));
        studentLookups.MapGet("/universities", async ([AsParameters] StudentUniversityLookupQuery query, AdmissionProgramService service, HttpContext c, IMessageLocalizer l, CancellationToken ct) =>
            (await service.GetStudentUniversitiesAsync(query, CurrentLanguage(c), ct)).ToHttpResult(c, l));
        MapSimulatorStudent(app);
        return app;
    }

    private static void MapClusters(RouteGroupBuilder root)
    {
        var g = root.MapGroup("/clusters");
        g.MapGet("/", async ([AsParameters] MmtPageQuery q, MmtCatalogService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.GetClustersAsync(q, CurrentLanguage(c), ct)).ToHttpResult(c, l));
        g.MapGet("/{id:guid}", async (Guid id, MmtCatalogService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.GetClusterAsync(id, CurrentLanguage(c), ct)).ToHttpResult(c, l));
        g.MapPost("/", async (CreateMmtClusterDto r, MmtCatalogService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.CreateClusterAsync(r, ct)).ToHttpResult(c, l));
        g.MapPut("/{id:guid}", async (Guid id, UpdateMmtClusterDto r, MmtCatalogService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.UpdateClusterAsync(id, r, ct)).ToHttpResult(c, l));
        g.MapPatch("/{id:guid}/status", async (Guid id, StatusRequest r, MmtCatalogService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.SetClusterStatusAsync(id, r.IsActive, ct)).ToHttpResult(c, l));
    }
    private static void MapUniversities(RouteGroupBuilder root)
    {
        var g = root.MapGroup("/universities");
        g.MapGet("/", async ([AsParameters] MmtPageQuery q, MmtCatalogService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.GetUniversitiesAsync(q, CurrentLanguage(c), ct)).ToHttpResult(c, l));
        g.MapGet("/{id:guid}", async (Guid id, MmtCatalogService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.GetUniversityAsync(id, CurrentLanguage(c), ct)).ToHttpResult(c, l));
        g.MapPost("/", async (CreateUniversityDto r, MmtCatalogService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.CreateUniversityAsync(r, ct)).ToHttpResult(c, l));
        g.MapPut("/{id:guid}", async (Guid id, UpdateUniversityDto r, MmtCatalogService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.UpdateUniversityAsync(id, r, ct)).ToHttpResult(c, l));
        g.MapPatch("/{id:guid}/status", async (Guid id, StatusRequest r, MmtCatalogService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.SetUniversityStatusAsync(id, r.IsActive, ct)).ToHttpResult(c, l));
    }
    private static void MapSpecialties(RouteGroupBuilder root)
    {
        var g = root.MapGroup("/specialties");
        g.MapGet("/", async ([AsParameters] MmtPageQuery q, MmtCatalogService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.GetSpecialtiesAsync(q, CurrentLanguage(c), ct)).ToHttpResult(c, l));
        g.MapGet("/{id:guid}", async (Guid id, MmtCatalogService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.GetSpecialtyAsync(id, CurrentLanguage(c), ct)).ToHttpResult(c, l));
        g.MapPost("/", async (CreateSpecialtyDto r, MmtCatalogService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.CreateSpecialtyAsync(r, ct)).ToHttpResult(c, l));
        g.MapPut("/{id:guid}", async (Guid id, UpdateSpecialtyDto r, MmtCatalogService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.UpdateSpecialtyAsync(id, r, ct)).ToHttpResult(c, l));
        g.MapPatch("/{id:guid}/status", async (Guid id, StatusRequest r, MmtCatalogService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.SetSpecialtyStatusAsync(id, r.IsActive, ct)).ToHttpResult(c, l));
    }
    private static void MapPrograms(RouteGroupBuilder root)
    {
        var g = root.MapGroup("/admission-programs");
        g.MapGet("/", async ([AsParameters] AdmissionProgramFilter q, AdmissionProgramService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.GetProgramsAsync(q, true, CurrentLanguage(c), ct)).ToHttpResult(c, l));
        g.MapGet("/{id:guid}", async (Guid id, AdmissionProgramService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.GetProgramAsync(id, true, CurrentLanguage(c), ct)).ToHttpResult(c, l));
        g.MapPost("/", async (CreateAdmissionProgramDto r, AdmissionProgramService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.CreateProgramAsync(r, ct)).ToHttpResult(c, l));
        g.MapPut("/{id:guid}", async (Guid id, UpdateAdmissionProgramDto r, AdmissionProgramService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.UpdateProgramAsync(id, r, ct)).ToHttpResult(c, l));
        g.MapPatch("/{id:guid}/status", async (Guid id, StatusRequest r, AdmissionProgramService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.SetStatusAsync(id, r.IsActive, ct)).ToHttpResult(c, l));
        g.MapPatch("/{id:guid}/publish", async (Guid id, PublishRequest r, AdmissionProgramService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.SetPublishedAsync(id, r.IsPublished, ct)).ToHttpResult(c, l));
        g.MapGet("/{id:guid}/passing-scores", async (Guid id, AdmissionProgramService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.GetScoresAsync(id, ct)).ToHttpResult(c, l));
        g.MapGet("/{id:guid}/passing-scores/analytics", async (Guid id, AdmissionProgramService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.GetScoreAnalyticsAsync(id, ct)).ToHttpResult(c, l));
        g.MapPost("/{id:guid}/passing-scores", async (Guid id, CreatePassingScoreHistoryDto r, AdmissionProgramService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.AddScoreAsync(id, r, ct)).ToHttpResult(c, l));
        root.MapPut("/passing-scores/{id:guid}", async (Guid id, UpdatePassingScoreHistoryDto r, AdmissionProgramService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.UpdateScoreAsync(id, r, ct)).ToHttpResult(c, l));
        root.MapDelete("/passing-scores/{id:guid}", async (Guid id, AdmissionProgramService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.DeleteScoreAsync(id, ct)).ToHttpResult(c, l));
    }
    private static void MapImport(RouteGroupBuilder root)
    {
        var g = root.MapGroup("/import").RequireAuthorization(Permissions.Mmt.Import);
        g.MapGet("/template", (MmtImportService s) => Results.File(s.CreateTemplate(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "mmt-import-template.xlsx"));
        g.MapPost("/preview", async ([FromForm] MmtImportPreviewRequestDto r, MmtImportService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.PreviewAsync(r, ct)).ToHttpResult(c, l)).Accepts<MmtImportPreviewRequestDto>("multipart/form-data").DisableAntiforgery();
        g.MapPost("/confirm", async ([FromForm] MmtImportConfirmRequestDto r, MmtImportService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.ConfirmAsync(r, ct)).ToHttpResult(c, l)).Accepts<MmtImportConfirmRequestDto>("multipart/form-data").DisableAntiforgery();
        g.MapGet("/catalog/template", (MmtCatalogImportService s) => Results.File(s.CreateTemplate(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "mmt-catalog-template.xlsx"));
        g.MapPost("/catalog/preview", async ([FromForm] MmtCatalogImportRequestDto r, MmtCatalogImportService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.PreviewAsync(r, ct)).ToHttpResult(c, l)).Accepts<MmtCatalogImportRequestDto>("multipart/form-data").DisableAntiforgery();
        g.MapPost("/catalog/confirm", async ([FromForm] MmtCatalogImportRequestDto r, MmtCatalogImportService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.ConfirmAsync(r, ct)).ToHttpResult(c, l)).Accepts<MmtCatalogImportRequestDto>("multipart/form-data").DisableAntiforgery();
    }

    private static void MapSimulatorStudent(IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/v2/mmt").WithTags("MMT Simulator").RequireAuthorization();
        g.MapGet("/profile", async (MmtSimulatorService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) =>
            (await s.GetCurrentProfileAsync(c.User, ct)).ToHttpResult(c, l));
        g.MapPut("/profile", async (UpsertStudentMmtProfileDto r, MmtSimulatorService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) =>
            (await s.UpsertProfileAsync(c.User, r, ct)).ToHttpResult(c, l));
        g.MapGet("/profile/choices", async (MmtSimulatorService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) =>
            (await s.GetCurrentChoicesAsync(c.User, ct)).ToHttpResult(c, l));
        g.MapPut("/profile/choices", async (UpsertAdmissionChoicesDto r, MmtSimulatorService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) =>
            (await s.ReplaceChoicesAsync(c.User, r, ct)).ToHttpResult(c, l));
        g.MapPost("/evaluations/simulate", async (SimulateMmtEvaluationDto r, MmtSimulatorService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) =>
            (await s.SimulateAsync(c.User, r, ct)).ToHttpResult(c, l));
        g.MapGet("/evaluations", async ([AsParameters] MmtEvaluationFilter q, MmtSimulatorService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) =>
            (await s.GetCurrentEvaluationsAsync(c.User, q, ct)).ToHttpResult(c, l));
        g.MapGet("/evaluations/{id:guid}", async (Guid id, MmtSimulatorService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) =>
            (await s.GetCurrentEvaluationAsync(c.User, id, ct)).ToHttpResult(c, l));
    }

    private static void MapSimulatorAdmin(RouteGroupBuilder root)
    {
        root.MapGet("/student-profiles", async ([AsParameters] StudentMmtProfileFilter q, MmtSimulatorService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) =>
            (await s.GetAdminProfilesAsync(q, ct)).ToHttpResult(c, l));
        root.MapGet("/student-profiles/{id:guid}", async (Guid id, MmtSimulatorService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) =>
            (await s.GetAdminProfileAsync(id, ct)).ToHttpResult(c, l));
        root.MapGet("/student-profiles/{id:guid}/choices", async (Guid id, MmtSimulatorService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) =>
            (await s.GetAdminChoicesAsync(id, ct)).ToHttpResult(c, l));
        root.MapGet("/evaluations", async ([AsParameters] MmtEvaluationFilter q, MmtSimulatorService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) =>
            (await s.GetAdminEvaluationsAsync(q, ct)).ToHttpResult(c, l));
        root.MapGet("/evaluations/{id:guid}", async (Guid id, MmtSimulatorService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) =>
            (await s.GetAdminEvaluationAsync(id, ct)).ToHttpResult(c, l));
    }

    private static SupportedLanguage CurrentLanguage(HttpContext context)
    {
        var header = context.Request.Headers["X-Adeeb-Language"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(header)
            && SupportedLanguageExtensions.TryParseCulture(header, out var language)) return language;
        var claim = context.User.FindFirst("lang")?.Value;
        if (!string.IsNullOrWhiteSpace(claim)
            && SupportedLanguageExtensions.TryParseCulture(claim, out language)) return language;
        return SupportedLanguageExtensions.TryParseCulture(System.Globalization.CultureInfo.CurrentUICulture.Name, out language)
            ? language
            : SupportedLanguage.Tajik;
    }
}
