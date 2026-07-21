import { httpClient } from '@/shared/api/http-client'
import type { StudentXpSummaryDto } from '@/features/progression/model/progression.types'

const xpBase = '/api/v2/student/xp'

export const progressionKeys = {
  all: ['student-progression'] as const,
  xpSummary: () => [...progressionKeys.all, 'xp-summary'] as const,
}

export const progressionApi = {
  async getXpSummary() {
    return (await httpClient.get<StudentXpSummaryDto>(xpBase)).data
  },
}
