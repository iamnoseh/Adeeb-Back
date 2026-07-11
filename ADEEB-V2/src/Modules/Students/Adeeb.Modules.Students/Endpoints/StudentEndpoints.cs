using Adeeb.Application.Abstractions.Localization;
using Adeeb.Modules.Students.Application;
using Adeeb.Modules.Students.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

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

        group.MapPost("/me/provision", async (StudentsService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
        {
            var userId = context.User.FindFirst("sub")?.Value;
            return Guid.TryParse(userId, out var identityUserId)
                ? (await service.ProvisionForIdentityUserAsync(identityUserId, ct)).ToHttpResult(context, localizer)
                : Results.Unauthorized();
        }).RequireAuthorization();

        var admin = app.MapGroup("/api/v2/admin/students").WithTags("Students Admin").RequireAuthorization("ContentAdmin");
        admin.MapGet("/{studentId:guid}", async (Guid studentId, StudentsService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
            (await service.GetByIdAsync(studentId, ct)).ToHttpResult(context, localizer));

        admin.MapPatch("/{studentId:guid}/status", async (Guid studentId, ChangeStudentStatusRequest request, StudentsService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
            (await service.ChangeStatusAsync(studentId, context.User, request, ct)).ToHttpResult(context, localizer));

        return app;
    }
}
