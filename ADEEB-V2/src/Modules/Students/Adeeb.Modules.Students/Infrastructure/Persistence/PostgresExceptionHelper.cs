using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Adeeb.Modules.Students.Infrastructure.Persistence;

internal static class PostgresExceptionHelper
{
    public static bool IsUniqueViolation(DbUpdateException exception, string constraintName)
    {
        if (exception.InnerException is PostgresException pgEx)
        {
            return pgEx.SqlState == "23505" && string.Equals(pgEx.ConstraintName, constraintName, StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }
}
