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
        MapClusters(admin); MapUniversities(admin); MapSpecialties(admin); MapPrograms(admin); MapImport(admin);
        var student = app.MapGroup("/api/v2/mmt/admission-programs").WithTags("MMT Admissions").RequireAuthorization();
        student.MapGet("/", async ([AsParameters] AdmissionProgramFilter filter, AdmissionProgramService service, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await service.GetProgramsAsync(filter, false, ct)).ToHttpResult(c, l));
        student.MapGet("/{id:guid}", async (Guid id, AdmissionProgramService service, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await service.GetProgramAsync(id, false, ct)).ToHttpResult(c, l));
        return app;
    }

    private static void MapClusters(RouteGroupBuilder root)
    {
        var g = root.MapGroup("/clusters");
        g.MapGet("/", async ([AsParameters] MmtPageQuery q, MmtCatalogService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.GetClustersAsync(q, ct)).ToHttpResult(c, l));
        g.MapGet("/{id:guid}", async (Guid id, MmtCatalogService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.GetClusterAsync(id, ct)).ToHttpResult(c, l));
        g.MapPost("/", async (CreateMmtClusterDto r, MmtCatalogService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.CreateClusterAsync(r, ct)).ToHttpResult(c, l));
        g.MapPut("/{id:guid}", async (Guid id, UpdateMmtClusterDto r, MmtCatalogService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.UpdateClusterAsync(id, r, ct)).ToHttpResult(c, l));
        g.MapPatch("/{id:guid}/status", async (Guid id, StatusRequest r, MmtCatalogService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.SetClusterStatusAsync(id, r.IsActive, ct)).ToHttpResult(c, l));
    }
    private static void MapUniversities(RouteGroupBuilder root)
    {
        var g = root.MapGroup("/universities");
        g.MapGet("/", async ([AsParameters] MmtPageQuery q, MmtCatalogService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.GetUniversitiesAsync(q, ct)).ToHttpResult(c, l));
        g.MapGet("/{id:guid}", async (Guid id, MmtCatalogService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.GetUniversityAsync(id, ct)).ToHttpResult(c, l));
        g.MapPost("/", async (CreateUniversityDto r, MmtCatalogService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.CreateUniversityAsync(r, ct)).ToHttpResult(c, l));
        g.MapPut("/{id:guid}", async (Guid id, UpdateUniversityDto r, MmtCatalogService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.UpdateUniversityAsync(id, r, ct)).ToHttpResult(c, l));
        g.MapPatch("/{id:guid}/status", async (Guid id, StatusRequest r, MmtCatalogService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.SetUniversityStatusAsync(id, r.IsActive, ct)).ToHttpResult(c, l));
    }
    private static void MapSpecialties(RouteGroupBuilder root)
    {
        var g = root.MapGroup("/specialties");
        g.MapGet("/", async ([AsParameters] MmtPageQuery q, MmtCatalogService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.GetSpecialtiesAsync(q, ct)).ToHttpResult(c, l));
        g.MapGet("/{id:guid}", async (Guid id, MmtCatalogService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.GetSpecialtyAsync(id, ct)).ToHttpResult(c, l));
        g.MapPost("/", async (CreateSpecialtyDto r, MmtCatalogService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.CreateSpecialtyAsync(r, ct)).ToHttpResult(c, l));
        g.MapPut("/{id:guid}", async (Guid id, UpdateSpecialtyDto r, MmtCatalogService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.UpdateSpecialtyAsync(id, r, ct)).ToHttpResult(c, l));
        g.MapPatch("/{id:guid}/status", async (Guid id, StatusRequest r, MmtCatalogService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.SetSpecialtyStatusAsync(id, r.IsActive, ct)).ToHttpResult(c, l));
    }
    private static void MapPrograms(RouteGroupBuilder root)
    {
        var g = root.MapGroup("/admission-programs");
        g.MapGet("/", async ([AsParameters] AdmissionProgramFilter q, AdmissionProgramService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.GetProgramsAsync(q, true, ct)).ToHttpResult(c, l));
        g.MapGet("/{id:guid}", async (Guid id, AdmissionProgramService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.GetProgramAsync(id, true, ct)).ToHttpResult(c, l));
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
    }
}
