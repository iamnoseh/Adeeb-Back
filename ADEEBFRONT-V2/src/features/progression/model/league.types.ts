export type LeagueDto = { id: string; name: string; nameTg: string; nameRu: string; avatarUrl: string | null; minXp: number; maxXp: number | null; displayOrder: number; status: number; configurationVersion: number; structuralLocked: boolean }
export type SeasonDto = { id: string; number: number; status: number; startsAtUtc: string; endsAtUtc: string; serverNowUtc: string; autoStartNext: boolean; configurationVersion: number }
export type LeaderboardItemDto = { rank: number; userId: string; displayName: string; avatarUrl: string | null; seasonXp: number; isCurrentUser: boolean; zone: string }
export type LeaderboardDto = { league: LeagueDto; season: SeasonDto; items: LeaderboardItemDto[]; page: number; pageSize: number; totalCount: number; movementCount: number; currentUser: LeaderboardItemDto | null }
export type PreviousSeasonDto = { seasonNumber: number; finalRank: number; outcome: string; leagueId: string }
export type ProgressionOverviewDto = { lifetimeXp: number; globalRank: number | null; rankedStudentsCount: number; league: LeagueDto | null; seasonXp: number; seasonRank: number | null; participantCount: number; zone: string; movementCount: number; serverNowUtc: string; startsAtUtc: string | null; endsAtUtc: string | null; previousSeason: PreviousSeasonDto | null }
export type PagedDto<T> = { items: T[]; page: number; pageSize: number; totalCount: number }
export type LeagueForm = { nameTg: string; nameRu: string; avatar?: File | null; minXp: number; maxXp?: number | null; displayOrder: number; isActive: boolean; removeAvatar?: boolean }
