using Adeeb.SharedKernel.Errors;

namespace Adeeb.Modules.Identity.Application;

public static class IdentityErrors
{
    public static readonly Error InvalidCredentials = new("auth.invalid_credentials", "Auth.InvalidCredentials", ErrorType.Unauthorized, "https://api.adeeb.tj/errors/auth/invalid-credentials");
    public static readonly Error InvalidRefreshToken = new("auth.invalid_refresh_token", "Auth.InvalidRefreshToken", ErrorType.Unauthorized, "https://api.adeeb.tj/errors/auth/invalid-refresh-token");
    public static readonly Error AccountBlocked = new("auth.account_blocked", "Auth.AccountBlocked", ErrorType.Unauthorized, "https://api.adeeb.tj/errors/auth/account-blocked");
    public static readonly Error EmailAlreadyExists = Error.Conflict("auth.email_already_exists", "Auth.EmailAlreadyExists");
    public static readonly Error Forbidden = Error.Forbidden("common.forbidden", "Common.Forbidden");
    public static readonly Error NotFound = Error.NotFound("common.not_found", "Common.NotFound");
}
