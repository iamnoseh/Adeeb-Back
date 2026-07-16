using System.Security.Claims;
using Adeeb.Application.Abstractions.Localization;
using Adeeb.Modules.QuestionBank.Application;
using Adeeb.Modules.QuestionBank.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Adeeb.Modules.QuestionBank.Endpoints;

public static class StudentTestingEndpoints
{
    public static IEndpointRouteBuilder MapStudentTestingEndpoints(this IEndpointRouteBuilder app)
    {
        var tests = app.MapGroup("/api/v2/student/tests").WithTags("Student Testing")
            .RequireAuthorization(policy => policy.RequireRole("User"));
        tests.MapGet("/config", async (StudentTestingService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
            (await WithUser(context, id => service.ConfigAsync(id, ct))).ToHttpResult(context, localizer));
        tests.MapPost("/subject/start", async (StartSubjectTestRequest request, StudentTestingService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
            (await WithUser(context, id => service.StartSubjectAsync(id, request, CurrentLanguage(), ct))).ToHttpResult(context, localizer));
        tests.MapPost("/mmt-practice/start", async (StartMmtPracticeRequest request, StudentTestingService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
            (await WithUser(context, id => service.StartMmtPracticeAsync(id, request, CurrentLanguage(), ct))).ToHttpResult(context, localizer));
        tests.MapPost("/monthly-exam/start", async (StudentTestingService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
            (await WithUser(context, id => service.StartMonthlyExamAsync(id, CurrentLanguage(), ct))).ToHttpResult(context, localizer));
        tests.MapPost("/red-list/start", async (StartRedListPracticeRequest request, StudentTestingService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
            (await WithUser(context, id => service.StartRedListAsync(id, request, CurrentLanguage(), ct))).ToHttpResult(context, localizer));
        tests.MapGet("/attempts/{attemptId:guid}", async (Guid attemptId, StudentTestingService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
            (await WithUser(context, id => service.GetAttemptAsync(id, attemptId, ct))).ToHttpResult(context, localizer));
        tests.MapPost("/attempts/{attemptId:guid}/submit", async (Guid attemptId, SubmitAttemptRequest request, StudentTestingService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
            (await WithUser(context, id => service.SubmitAsync(id, attemptId, request, ct))).ToHttpResult(context, localizer));
        tests.MapGet("/attempts/{attemptId:guid}/result", async (Guid attemptId, StudentTestingService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
            (await WithUser(context, id => service.GetResultAsync(id, attemptId, ct))).ToHttpResult(context, localizer));
        tests.MapGet("/history", async ([AsParameters] TestHistoryQuery query, StudentTestingService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
            (await WithUser(context, id => service.HistoryAsync(id, query, ct))).ToHttpResult(context, localizer));

        var redList = app.MapGroup("/api/v2/student/red-list").WithTags("Student Red List")
            .RequireAuthorization(policy => policy.RequireRole("User"));
        redList.MapGet("/", async ([AsParameters] RedListQuery query, RedListService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
            (await WithUser(context, id => service.GetAsync(id, query, CurrentLanguage(), ct))).ToHttpResult(context, localizer));
        redList.MapGet("/summary", async (RedListService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
            (await WithUser(context, id => service.SummaryAsync(id, ct))).ToHttpResult(context, localizer));
        redList.MapPost("/{questionId:guid}/archive", async (Guid questionId, RedListService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
            (await WithUser(context, id => service.ArchiveAsync(id, questionId, ct))).ToHttpResult(context, localizer));
        redList.MapPost("/{questionId:guid}/restore", async (Guid questionId, RedListService service, HttpContext context, IMessageLocalizer localizer, CancellationToken ct) =>
            (await WithUser(context, id => service.RestoreAsync(id, questionId, ct))).ToHttpResult(context, localizer));
        return app;
    }

    private static async Task<Adeeb.SharedKernel.Results.Result<T>> WithUser<T>(HttpContext context,
        Func<Guid, Task<Adeeb.SharedKernel.Results.Result<T>>> action)
    {
        var id = UserId(context.User);
        return id.HasValue ? await action(id.Value) : Adeeb.SharedKernel.Results.Result<T>.Failure(StudentTestingErrors.UserRequired);
    }
    private static async Task<Adeeb.SharedKernel.Results.Result> WithUser(HttpContext context,
        Func<Guid, Task<Adeeb.SharedKernel.Results.Result>> action)
    {
        var id = UserId(context.User);
        return id.HasValue ? await action(id.Value) : Adeeb.SharedKernel.Results.Result.Failure(StudentTestingErrors.UserRequired);
    }
    private static Guid? UserId(ClaimsPrincipal principal) => Guid.TryParse(
        principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub"), out var id) ? id : null;
    private static SupportedLanguage CurrentLanguage() => SupportedLanguageExtensions.TryParseCulture(
        System.Globalization.CultureInfo.CurrentUICulture.Name, out var language) ? language : SupportedLanguage.Tajik;
}
