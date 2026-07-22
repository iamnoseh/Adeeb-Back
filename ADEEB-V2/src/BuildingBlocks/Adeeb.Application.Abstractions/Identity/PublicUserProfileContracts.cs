namespace Adeeb.Application.Abstractions.Identity;

public sealed record PublicUserProfile(Guid UserId, string FirstName, string LastName)
{
    public string FullName => $"{FirstName} {LastName}".Trim();
}

public interface IPublicUserProfileDirectory
{
    Task<IReadOnlyDictionary<Guid, PublicUserProfile>> GetByUserIdsAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken);
}

