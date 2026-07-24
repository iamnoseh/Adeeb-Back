using Adeeb.Application.Abstractions.Localization;
using Adeeb.Application.Abstractions.Authorization;
using Adeeb.Modules.Students.Application;
using Adeeb.Modules.Students.Contracts;
using Adeeb.Modules.Students.Infrastructure.Files;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System.Globalization;

namespace Adeeb.Modules.Students.Endpoints;

public static class StudentEndpoints
{
    public static IEndpointRouteBuilder MapStudentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v2/students").WithTags("Students");

        group.MapGet("/me", async (StudentsService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
            (await service.GetCurrentAsync(context.User, ct)).ToHttpResult(context, localizer))
            .RequireAuthorization();

        group.MapPatch("/me/profile", async (UpdateStudentProfileRequest request, StudentsService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
            (await service.UpdateCurrentProfileAsync(context.User, request, ct)).ToHttpResult(context, localizer))
            .RequireAuthorization();

        group.MapPost("/me/profile/avatar", async ([FromForm] UpdateStudentAvatarRequest request, StudentAvatarStorage storage, StudentsService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
        {
            var avatar = request.Avatar
                ?? context.Request.Form.Files.GetFile("Avatar")
                ?? context.Request.Form.Files.GetFile("avatar")
                ?? context.Request.Form.Files.FirstOrDefault(file => string.Equals(file.Name, "Avatar", StringComparison.OrdinalIgnoreCase));
            var stored = await storage.SaveAsync(avatar, ct);
            return stored.IsFailure
                ? stored.ToHttpResult(context, localizer)
                : (await service.UpdateCurrentAvatarAsync(context.User, stored.Value!, ct)).ToHttpResult(context, localizer);
        })
            .RequireAuthorization()
            .Accepts<UpdateStudentAvatarRequest>("multipart/form-data")
            .DisableAntiforgery();

        group.MapPost("/me/provision", async (StudentsService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
        {
            var userId = context.User.FindFirst("sub")?.Value;
            return Guid.TryParse(userId, out var identityUserId)
                ? (await service.ProvisionForIdentityUserAsync(identityUserId, ct)).ToHttpResult(context, localizer)
                : Results.Unauthorized();
        })
            .RequireAuthorization()
            .RequireRateLimiting("student-provision");

        group.MapPost("/me/activity/visit", async (StudentActivityVisitRequest request, StudentActivityService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
            (await service.RecordVisitAsync(context.User, request, ct)).ToHttpResult(context, localizer))
            .RequireAuthorization();

        group.MapGet("/me/activity/calendar", async (int? year, int? month, StudentActivityService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
            (await service.GetCalendarAsync(context.User, year, month, ct)).ToHttpResult(context, localizer))
            .RequireAuthorization();

        group.MapGet("/me/education-profile", async (StudentEducationService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
            (await service.GetCurrentAsync(context.User, IsRussian(context), ct)).ToHttpResult(context, localizer))
            .RequireAuthorization();

        group.MapPut("/me/education-profile", async (UpsertStudentEducationProfileRequest request, StudentEducationService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
            (await service.UpsertCurrentAsync(context.User, request, IsRussian(context), context.TraceIdentifier, ct)).ToHttpResult(context, localizer))
            .RequireAuthorization();

        group.MapGet("/regions", async (Guid? parentId, EducationCatalogService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
            (await service.GetRegionsAsync(parentId, IsRussian(context), ct)).ToHttpResult(context, localizer))
            .RequireAuthorization();

        group.MapGet("/schools/search", async ([AsParameters] SchoolSearchQuery request, EducationCatalogService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
            (await service.SearchStudentSchoolsAsync(request, IsRussian(context), ct)).ToHttpResult(context, localizer))
            .RequireAuthorization();

        group.MapPost("/me/school-suggestions", async (CreateSchoolSuggestionRequest request, StudentEducationService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
            (await service.CreateSuggestionAsync(context.User, request, IsRussian(context), context.TraceIdentifier, ct)).ToHttpResult(context, localizer))
            .RequireAuthorization();

        group.MapGet("/me/school-suggestion", async (StudentEducationService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
            (await service.GetCurrentSuggestionAsync(context.User, IsRussian(context), ct)).ToHttpResult(context, localizer))
            .RequireAuthorization();

        var admin = app.MapGroup("/api/v2/admin/students").WithTags("Students Admin").RequireAuthorization(Permissions.Students.Manage);
        admin.MapGet("/{studentId:guid}", async (Guid studentId, StudentsService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
            (await service.GetByIdAsync(studentId, ct)).ToHttpResult(context, localizer));

        admin.MapPatch("/{studentId:guid}/status", async (Guid studentId, ChangeStudentStatusRequest request, StudentsService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
            (await service.ChangeStatusAsync(studentId, context.User, request, ct)).ToHttpResult(context, localizer));

        admin.MapPut("/{studentId:guid}/education-profile", async (Guid studentId, AdminCorrectEducationProfileRequest request, StudentEducationService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
            (await service.CorrectByAdminAsync(studentId, request, context.User, IsRussian(context), context.TraceIdentifier, ct)).ToHttpResult(context, localizer));

        MapEducationCatalogAdmin(admin);

        return app;
    }

    private static void MapEducationCatalogAdmin(RouteGroupBuilder admin)
    {
        var regions = admin.MapGroup("/regions");
        regions.MapGet("/", async (Guid? parentId, EducationCatalogService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
            (await service.GetRegionsAsync(parentId, IsRussian(context), ct)).ToHttpResult(context, localizer));
        regions.MapPost("/", async (CreateRegionRequest request, EducationCatalogService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
            (await service.CreateRegionAsync(request, context.User, IsRussian(context), ct)).ToHttpResult(context, localizer));
        regions.MapPut("/{id:guid}", async (Guid id, UpdateRegionRequest request, EducationCatalogService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
            (await service.UpdateRegionAsync(id, request, context.User, IsRussian(context), ct)).ToHttpResult(context, localizer));
        regions.MapPut("/{id:guid}/parent", async (Guid id, MoveRegionRequest request, EducationCatalogService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
            (await service.MoveRegionAsync(id, request, context.User, IsRussian(context), ct)).ToHttpResult(context, localizer));
        regions.MapPatch("/{id:guid}/status", async (Guid id, SetRegionStatusRequest request, EducationCatalogService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
            (await service.SetRegionStatusAsync(id, request, context.User, ct)).ToHttpResult(context, localizer));

        var schools = admin.MapGroup("/schools");
        schools.MapGet("/", async ([AsParameters] AdminSchoolFilter request, EducationCatalogService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
            (await service.GetAdminSchoolsAsync(request, IsRussian(context), ct)).ToHttpResult(context, localizer));
        schools.MapPost("/", async (CreateSchoolRequest request, EducationCatalogService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
            (await service.CreateSchoolAsync(request, context.User, IsRussian(context), ct)).ToHttpResult(context, localizer));
        schools.MapPut("/{id:guid}", async (Guid id, UpdateSchoolRequest request, EducationCatalogService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
            (await service.UpdateSchoolAsync(id, request, context.User, IsRussian(context), ct)).ToHttpResult(context, localizer));
        schools.MapPost("/{id:guid}/verify", async (Guid id, SetSchoolStatusRequest request, EducationCatalogService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
            (await service.VerifySchoolAsync(id, request, context.User, IsRussian(context), ct)).ToHttpResult(context, localizer));
        schools.MapPost("/{id:guid}/archive", async (Guid id, SetSchoolStatusRequest request, EducationCatalogService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
            (await service.ArchiveSchoolAsync(id, request, context.User, ct)).ToHttpResult(context, localizer));
        schools.MapPost("/{id:guid}/deactivate", async (Guid id, SetSchoolStatusRequest request, EducationCatalogService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
            (await service.DeactivateSchoolAsync(id, request, context.User, ct)).ToHttpResult(context, localizer));
        schools.MapPost("/{id:guid}/merge", async (Guid id, MergeSchoolRequest request, EducationCatalogService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
            (await service.MergeSchoolsAsync(id, request, context.User, ct)).ToHttpResult(context, localizer));

        var suggestions = admin.MapGroup("/school-suggestions");
        suggestions.MapGet("/", async ([AsParameters] AdminSchoolSuggestionFilter request, StudentEducationService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
            (await service.GetSuggestionsAsync(request, IsRussian(context), ct)).ToHttpResult(context, localizer));
        suggestions.MapPost("/{id:guid}/review", async (Guid id, ReviewSchoolSuggestionRequest request, StudentEducationService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
            (await service.ReviewSuggestionAsync(id, request, context.User, IsRussian(context), context.TraceIdentifier, ct)).ToHttpResult(context, localizer));

        var imports = admin.MapGroup("/education-imports").RequireAuthorization(Permissions.Students.Import);
        imports.MapGet("/schools/template", (EducationSchoolImportService service) =>
            Results.File(service.CreateCsvTemplate(), "text/csv; charset=utf-8", "adeeb-school-catalog-template.csv"));
        imports.MapPost("/schools/preview", async ([FromForm] EducationSchoolImportRequest request, EducationSchoolImportService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
            (await service.PreviewAsync(request, ct)).ToHttpResult(context, localizer)).Accepts<EducationSchoolImportRequest>("multipart/form-data").DisableAntiforgery();
        imports.MapPost("/schools/confirm", async ([FromForm] EducationSchoolImportRequest request, EducationSchoolImportService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
            (await service.ConfirmAsync(request, context.User, ct)).ToHttpResult(context, localizer)).Accepts<EducationSchoolImportRequest>("multipart/form-data").DisableAntiforgery();

        var rollovers = admin.MapGroup("/academic-year-rollovers");
        rollovers.MapPost("/preview", async (CreateAcademicYearRolloverPreviewRequest request, AcademicYearRolloverService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
            (await service.CreatePreviewAsync(request, context.User, ct)).ToHttpResult(context, localizer));
        rollovers.MapGet("/{id:guid}", async (Guid id, AcademicYearRolloverService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
            (await service.GetAsync(id, ct)).ToHttpResult(context, localizer));
        rollovers.MapPost("/{id:guid}/approve", async (Guid id, ExecuteAcademicYearRolloverRequest request, AcademicYearRolloverService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
            (await service.ApproveAsync(id, request, context.User, ct)).ToHttpResult(context, localizer));
        rollovers.MapPost("/{id:guid}/execute", async (Guid id, ExecuteAcademicYearRolloverRequest request, AcademicYearRolloverService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
            (await service.ExecuteAsync(id, request, context.User, ct)).ToHttpResult(context, localizer));
    }

    private static bool IsRussian(HttpContext context) =>
        CultureInfo.CurrentUICulture.Name.StartsWith("ru", StringComparison.OrdinalIgnoreCase) ||
        context.User.FindFirst("lang")?.Value.Equals("ru-RU", StringComparison.OrdinalIgnoreCase) == true;
}
