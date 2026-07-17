using Adeeb.Application.Abstractions.Localization;
using Adeeb.SharedKernel.Errors;
using Adeeb.SharedKernel.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Adeeb.Modules.Vocabulary.Endpoints;

internal static class ProblemDetailsMapper
{
    public static IResult ToHttpResult(this Result result, HttpContext context, IMessageLocalizer localizer) => result.IsSuccess ? Results.NoContent() : Problem(result, context, localizer);
    public static IResult ToHttpResult<T>(this Result<T> result, HttpContext context, IMessageLocalizer localizer) => result.IsSuccess ? Results.Ok(result.Value) : Problem(result, context, localizer);
    private static IResult Problem(Result result, HttpContext context, IMessageLocalizer localizer)
    {
        var error = result.Error ?? Error.Failure("common.unexpected_error", "Common.UnexpectedError"); var status = error.Type switch { ErrorType.Validation => 422, ErrorType.NotFound => 404, ErrorType.Conflict => 409, ErrorType.Unauthorized => 401, ErrorType.Forbidden => 403, _ => 500 };
        var details = new ProblemDetails { Status = status, Title = localizer[error.MessageKey], Type = error.TypeUri ?? $"https://api.adeeb.tj/errors/{error.Code.Replace('.', '/')}", Instance = context.Request.Path };
        details.Extensions["code"] = error.Code; details.Extensions["traceId"] = context.TraceIdentifier;
        if (result.ValidationErrors is not null) details.Extensions["errors"] = result.ValidationErrors.ToDictionary(x => x.Key, x => x.Value.Select(e => new { code = e.Code, message = localizer[e.MessageKey] }).ToArray());
        return Results.Problem(details);
    }
}
