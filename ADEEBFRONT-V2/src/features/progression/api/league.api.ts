import { httpClient } from '@/shared/api/http-client'
import type { LeagueDto, LeagueForm, LeaderboardDto, PagedDto, PreviousSeasonDto, ProgressionOverviewDto, SeasonDto } from '@/features/progression/model/league.types'

export const leagueKeys = {
  all: ['progression'] as const,
  admin: () => [...leagueKeys.all, 'admin'] as const,
  leagues: () => [...leagueKeys.admin(), 'leagues'] as const,
  season: () => [...leagueKeys.admin(), 'season'] as const,
  adminHistory: (page: number) => [...leagueKeys.admin(), 'history', page] as const,
  adminLeaderboard: (leagueId: string, page: number) => [...leagueKeys.admin(), 'leaderboard', leagueId, page] as const,
  overview: () => [...leagueKeys.all, 'overview'] as const,
  leaguesList: () => [...leagueKeys.all, 'leagues-list'] as const,
  leaderboard: (page: number, leagueId?: string) => [...leagueKeys.all, 'leaderboard', page, leagueId ?? ''] as const,
  history: (page: number) => [...leagueKeys.all, 'history', page] as const,
}

function formData(value: LeagueForm) {
  const data = new FormData()
  data.set('NameTg', value.nameTg); data.set('NameRu', value.nameRu)
  data.set('MinXp', String(value.minXp)); data.set('DisplayOrder', String(value.displayOrder))
  data.set('IsActive', String(value.isActive)); data.set('RemoveAvatar', String(Boolean(value.removeAvatar)))
  if (value.maxXp != null) data.set('MaxXp', String(value.maxXp))
  if (value.avatar) data.set('Avatar', value.avatar)
  return data
}

export const progressionAdminApi = {
  leagues: async () => (await httpClient.get<LeagueDto[]>('/api/v2/admin/progression/leagues')).data,
  season: async () => (await httpClient.get<SeasonDto | null>('/api/v2/admin/progression/seasons/current')).data,
  createLeague: async (value: LeagueForm) => (await httpClient.post<LeagueDto>('/api/v2/admin/progression/leagues', formData(value))).data,
  updateLeague: async ({ id, value }: { id: string; value: LeagueForm }) => (await httpClient.put<LeagueDto>(`/api/v2/admin/progression/leagues/${id}`, formData(value))).data,
  archiveLeague: async (id: string) => { await httpClient.post(`/api/v2/admin/progression/leagues/${id}/archive`) },
  startSeason: async () => (await httpClient.post<SeasonDto>('/api/v2/admin/progression/seasons/start')).data,
  setAutoRenewal: async (enabled: boolean) => (await httpClient.put<SeasonDto>('/api/v2/admin/progression/seasons/auto-renewal', { enabled })).data,
  history: async (page = 1) => (await httpClient.get<PagedDto<SeasonDto>>('/api/v2/admin/progression/seasons/history', { params: { page, pageSize: 10 } })).data,
  leaderboard: async (leagueId: string, page = 1) => (await httpClient.get<LeaderboardDto>(`/api/v2/admin/progression/leagues/${leagueId}/leaderboard`, { params: { page, pageSize: 50 } })).data,
}

export const progressionStudentApi = {
  overview: async () => (await httpClient.get<ProgressionOverviewDto>('/api/v2/student/progression/overview')).data,
  leagues: async () => (await httpClient.get<LeagueDto[]>('/api/v2/student/progression/leagues')).data,
  leaderboard: async (page = 1, leagueId?: string) => (await httpClient.get<LeaderboardDto>('/api/v2/student/progression/league/leaderboard', { params: { page, pageSize: 50, leagueId } })).data,
  history: async (page = 1) => (await httpClient.get<PagedDto<PreviousSeasonDto>>('/api/v2/student/progression/seasons/history', { params: { page, pageSize: 10 } })).data,
}
