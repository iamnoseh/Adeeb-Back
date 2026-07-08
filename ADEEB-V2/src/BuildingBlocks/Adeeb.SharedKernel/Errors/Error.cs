namespace Adeeb.SharedKernel.Errors;

public sealed record Error(
    string Code,
    string MessageKey,
    ErrorType Type,
    string? TypeUri = null)
{
    public static Error Validation(string code, string messageKey) => new(code, messageKey, ErrorType.Validation);
    public static Error Unauthorized(string code, string messageKey) => new(code, messageKey, ErrorType.Unauthorized);
    public static Error Conflict(string code, string messageKey) => new(code, messageKey, ErrorType.Conflict);
    public static Error Forbidden(string code, string messageKey) => new(code, messageKey, ErrorType.Forbidden);
    public static Error NotFound(string code, string messageKey) => new(code, messageKey, ErrorType.NotFound);
    public static Error Failure(string code, string messageKey) => new(code, messageKey, ErrorType.Failure);
}
