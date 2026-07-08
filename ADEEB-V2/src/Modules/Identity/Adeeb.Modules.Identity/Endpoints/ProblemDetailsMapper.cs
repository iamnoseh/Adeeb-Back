using Adeeb.Application.Abstractions.Localization;
using Adeeb.SharedKernel.Errors;
using Adeeb.SharedKernel.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Adeeb.Modules.Identity.Endpoints;

internal static class ProblemDetailsMapper
{
    public static IResult ToHttpResult(this Result result, HttpContext httpContext, IMessageLocalizer localizer) =>
        result.IsSuccess ? Results.NoContent() : ToProblem(result, httpContext, localizer);

    public static IResult ToHttpResult<T>(this Result<T> result, HttpContext httpContext, IMessageLocalizer localizer) =>
        result.IsSuccess ? Results.Ok(result.Value) : ToProblem(result, httpContext, localizer);

    private static IResult ToProblem(Result result, HttpContext httpContext, IMessageLocalizer localizer)
    {
        var error = result.Error ?? Error.Failure("common.unexpected_error", "Common.UnexpectedError");
        var status = GetStatus(error.Type);
        var details = new ProblemDetails
        {
            Status = status,
            Title = localizer[error.MessageKey],
            Type = error.TypeUri ?? GetTypeUri(error),
            Instance = httpContext.Request.Path
        };
        details.Extensions["code"] = error.Code;
        details.Extensions["traceId"] = httpContext.TraceIdentifier;

        if (result.ValidationErrors is not null)
        {
            details.Extensions["errors"] = result.ValidationErrors.ToDictionary(
                pair => pair.Key,
                pair => pair.Value.Select(e => new { code = e.Code, message = localizer[e.MessageKey] }).ToArray());
        }

        return Results.Problem(details);
    }

    private static int GetStatus(ErrorType type) =>
        type switch
        {
            ErrorType.Validation => StatusCodes.Status422UnprocessableEntity,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError
        };

    private static string GetTypeUri(Error error) =>
        error.Type == ErrorType.Validation
            ? "https://api.adeeb.tj/errors/validation"
            : $"https://api.adeeb.tj/errors/{error.Code.Replace('.', '/')}";
}
