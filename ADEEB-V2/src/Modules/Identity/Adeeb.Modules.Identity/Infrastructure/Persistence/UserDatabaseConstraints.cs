namespace Adeeb.Modules.Identity.Infrastructure.Persistence;

internal static class UserDatabaseConstraints
{
    public const string NormalizedEmailUnique = "IX_users_normalized_email";
    public const string NormalizedPhoneNumberUnique = "IX_users_normalized_phone_number";
}
