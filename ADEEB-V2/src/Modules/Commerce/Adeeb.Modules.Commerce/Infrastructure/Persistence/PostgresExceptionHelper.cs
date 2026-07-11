using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Adeeb.Modules.Commerce.Infrastructure.Persistence;

internal static class PostgresExceptionHelper
{
    public static bool IsUniqueViolation(DbUpdateException exception, string constraintName) =>
        exception.InnerException is PostgresException postgres &&
        postgres.SqlState == PostgresErrorCodes.UniqueViolation &&
        string.Equals(postgres.ConstraintName, constraintName, StringComparison.Ordinal);
}
