using Microsoft.AspNetCore.Http;

namespace Adeeb.Modules.Progression.Contracts;

public sealed class LeagueFormRequest
{
    public string? NameTg { get; init; }
    public string? NameRu { get; init; }
    public IFormFile? Avatar { get; init; }
    public decimal? MinXp { get; init; }
    public decimal? MaxXp { get; init; }
    public int? DisplayOrder { get; init; }
    public bool IsActive { get; init; } = true;
    public bool RemoveAvatar { get; init; }
}
public sealed record LeagueDto(Guid Id, string Name, string NameTg, string NameRu, string? AvatarUrl,
    decimal MinXp, decimal? MaxXp, int DisplayOrder, int Status, int ConfigurationVersion, bool StructuralLocked);
public sealed record SeasonDto(Guid Id, int Number, int Status, DateTimeOffset StartsAtUtc, DateTimeOffset EndsAtUtc,
    DateTimeOffset ServerNowUtc, bool AutoStartNext, int ConfigurationVersion);
public sealed record AutoRenewalRequest(bool Enabled);
public sealed record LeaderboardItemDto(int Rank, Guid UserId, string DisplayName, string? AvatarUrl,
    decimal SeasonXp, bool IsCurrentUser, string Zone);
public sealed record LeaderboardDto(LeagueDto League, SeasonDto Season, IReadOnlyList<LeaderboardItemDto> Items,
    int Page, int PageSize, int TotalCount, int MovementCount, LeaderboardItemDto? CurrentUser);
public sealed record ProgressionOverviewDto(decimal LifetimeXp, long? GlobalRank, long RankedStudentsCount,
    LeagueDto? League, decimal SeasonXp, int? SeasonRank, int ParticipantCount, string Zone, int MovementCount,
    DateTimeOffset ServerNowUtc, DateTimeOffset? StartsAtUtc, DateTimeOffset? EndsAtUtc,
    PreviousSeasonDto? PreviousSeason);
public sealed record PreviousSeasonDto(int SeasonNumber, int FinalRank, string Outcome, Guid LeagueId);
public sealed record PagedProgressionDto<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
