import { httpClient } from '@/shared/api/http-client'
import type { StudentActivityCalendarDto } from '@/features/student-activity/model/student-activity.types'

const activityBase = '/api/v2/students/me/activity'

export const studentActivityKeys = {
  all: ['student-activity'] as const,
  current: () => [...studentActivityKeys.all, 'calendar', 'current'] as const,
  month: (year: number, month: number) => [...studentActivityKeys.all, 'calendar', year, month] as const,
}

export const studentActivityApi = {
  async recordVisit(timeZoneId: string) {
    return (await httpClient.post<StudentActivityCalendarDto>(`${activityBase}/visit`, { timeZoneId })).data
  },
  async calendar(year?: number, month?: number) {
    const query = year && month ? `?year=${year}&month=${month}` : ''
    return (await httpClient.get<StudentActivityCalendarDto>(`${activityBase}/calendar${query}`)).data
  },
}
