using Adeeb.Application.Abstractions.Authorization;
using Adeeb.Application.Abstractions.Localization;
using Adeeb.Modules.Progression.Application;
using Adeeb.Modules.Progression.Contracts;
using Adeeb.Modules.Progression.Infrastructure.Files;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Adeeb.Modules.Progression.Endpoints;

public static class ProgressionEndpoints
{
    public static IEndpointRouteBuilder MapProgressionEndpoints(this IEndpointRouteBuilder app)
    {
        var admin = app.MapGroup("/api/v2/admin/progression").WithTags("Progression Admin");
        var view = admin.MapGroup("").RequireAuthorization(Permissions.Progression.View);
        view.MapGet("/leagues", async (ProgressionService s, CancellationToken ct) => Results.Ok(await s.GetLeaguesAsync(ct)));
        view.MapGet("/seasons/current", async (ProgressionService s, CancellationToken ct) => Results.Ok(await s.GetCurrentSeasonAsync(ct)));
        view.MapGet("/seasons/history", async (int? page, int? pageSize, ProgressionService s, CancellationToken ct) => Results.Ok(await s.GetSeasonHistoryAsync(page ?? 1, pageSize ?? 10, ct)));
        view.MapGet("/leagues/{id:guid}/leaderboard", async (Guid id, int? page, int? pageSize, ProgressionService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) =>
            (await s.GetAdminLeaderboardAsync(id, page ?? 1, pageSize ?? 50, ct)).ToHttpResult(c, l));
        var manage = admin.MapGroup("").RequireAuthorization(Permissions.Progression.Manage);
        manage.MapPost("/leagues", async ([FromForm] LeagueFormRequest r, LeagueAvatarStorage files, ProgressionService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) =>
        { var file = await files.SaveAsync(r.Avatar, ct); return file.IsFailure ? ResultFailure(file, c, l) : (await s.CreateLeagueAsync(r, file.Value, ct)).ToHttpResult(c, l); }).DisableAntiforgery();
        manage.MapPut("/leagues/{id:guid}", async (Guid id, [FromForm] LeagueFormRequest r, LeagueAvatarStorage files, ProgressionService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) =>
        { var file = await files.SaveAsync(r.Avatar, ct); return file.IsFailure ? ResultFailure(file, c, l) : (await s.UpdateLeagueAsync(id, r, file.Value, ct)).ToHttpResult(c, l); }).DisableAntiforgery();
        manage.MapPost("/leagues/{id:guid}/archive", async (Guid id, ProgressionService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.ArchiveLeagueAsync(id, ct)).ToHttpResult(c, l));
        manage.MapPost("/seasons/start", async (ProgressionService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.StartSeasonAsync(ct)).ToHttpResult(c, l));
        manage.MapPut("/seasons/auto-renewal", async (AutoRenewalRequest r, ProgressionService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.SetAutoRenewalAsync(r.Enabled, ct)).ToHttpResult(c, l));

        var student = app.MapGroup("/api/v2/student/progression").WithTags("Student Progression").RequireAuthorization();
        student.MapGet("/overview", async (ProgressionService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.GetOverviewAsync(c.User, ct)).ToHttpResult(c, l));
        student.MapGet("/leagues", async (ProgressionService s, CancellationToken ct) => Results.Ok(await s.GetLeaguesAsync(ct)));
        student.MapGet("/league/leaderboard", async (Guid? leagueId, int? page, int? pageSize, ProgressionService s, HttpContext c, IMessageLocalizer l, CancellationToken ct) => (await s.GetStudentLeaderboardAsync(c.User, leagueId, page ?? 1, pageSize ?? 50, ct)).ToHttpResult(c, l));
        student.MapGet("/seasons/history", async (int? page, int? pageSize, ProgressionService s, HttpContext c, CancellationToken ct) => Results.Ok(await s.GetStudentHistoryAsync(c.User, page ?? 1, pageSize ?? 10, ct)));
        return app;
    }
    private static IResult ResultFailure<T>(Adeeb.SharedKernel.Results.Result<T> result, HttpContext c, IMessageLocalizer l) => result.ToHttpResult(c, l);
}
