using Adeeb.Application.Abstractions.Localization;
using Adeeb.SharedKernel.Results;
using Microsoft.AspNetCore.Http;

namespace Adeeb.Modules.Progression.Endpoints;

internal static class ProblemDetailsMapper
{
    public static IResult ToHttpResult(this Result result, HttpContext context, IMessageLocalizer localizer) => result.IsSuccess ? Results.NoContent() : Problem(result, context, localizer);
    public static IResult ToHttpResult<T>(this Result<T> result, HttpContext context, IMessageLocalizer localizer) => result.IsSuccess ? Results.Ok(result.Value) : Problem(result, context, localizer);
    private static IResult Problem(Result result, HttpContext context, IMessageLocalizer localizer)
    {
        var error = result.Error!; var status = error.Type.ToString() switch { "Validation" => 400, "NotFound" => 404, "Forbidden" => 403, "Conflict" => 409, _ => 400 };
        return Results.Problem(statusCode: status, title: localizer[error.MessageKey], type: $"https://api.adeeb.tj/errors/{error.Code.Replace('.', '/')}",
            extensions: new Dictionary<string, object?> { ["code"] = error.Code, ["traceId"] = context.TraceIdentifier });
    }
}
