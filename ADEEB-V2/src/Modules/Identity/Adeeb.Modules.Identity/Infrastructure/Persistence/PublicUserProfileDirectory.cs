using Adeeb.Application.Abstractions.Identity;
using Microsoft.EntityFrameworkCore;

namespace Adeeb.Modules.Identity.Infrastructure.Persistence;

internal sealed class PublicUserProfileDirectory(IdentityDbContext db) : IPublicUserProfileDirectory
{
    public async Task<IReadOnlyDictionary<Guid, PublicUserProfile>> GetByUserIdsAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken)
    {
        if (userIds.Count == 0) return new Dictionary<Guid, PublicUserProfile>();

        return await db.Users.AsNoTracking()
            .Where(user => userIds.Contains(user.Id))
            .Select(user => new PublicUserProfile(user.Id, user.FirstName, user.LastName))
            .ToDictionaryAsync(user => user.UserId, cancellationToken);
    }
}

