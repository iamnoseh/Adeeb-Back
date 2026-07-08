using Adeeb.Application.Abstractions.Localization;
using Adeeb.Application.Abstractions.Time;
using Adeeb.Modules.Identity.Application;
using Adeeb.Modules.Identity.Domain.Users;
using Adeeb.Modules.Identity.Infrastructure.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Adeeb.Modules.Identity.Infrastructure.Persistence;

public static class IdentitySeeder
{
    public static async Task SeedSuperAdminAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<SeedSuperAdminOptions>>().Value;
        if (!options.Enabled)
        {
            return;
        }

        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var normalizedEmail = options.Email.Trim().ToUpperInvariant();
        if (await db.Users.AnyAsync(x => x.NormalizedEmail == normalizedEmail, cancellationToken))
        {
            return;
        }

        SupportedLanguageExtensions.TryParseCulture(options.Language, out var language);
        var now = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>().UtcNow;
        var phone = Validation.NormalizePhoneNumber(options.PhoneNumber);
        var user = new User(
            Guid.NewGuid(),
            options.Email.Trim(),
            normalizedEmail,
            options.PhoneNumber?.Trim(),
            phone,
            string.Empty,
            options.FirstName.Trim(),
            options.LastName.Trim(),
            language,
            now,
            UserRole.SuperAdmin);

        var hasher = scope.ServiceProvider.GetRequiredService<PasswordHasher<User>>();
        user.ChangePassword(hasher.HashPassword(user, options.Password), now);
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Adeeb.IdentitySeeder");
        logger.LogInformation("identity.superadmin.seeded user_id={UserId} email={Email}", user.Id, user.Email);
    }
}
