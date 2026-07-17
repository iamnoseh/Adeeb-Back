import { beforeEach, describe, expect, it, vi } from 'vitest'
import { httpClient } from '@/shared/api/http-client'
import { studentActivityApi } from './student-activity.api'

vi.mock('@/shared/api/http-client', () => ({ httpClient: { get: vi.fn(), post: vi.fn() } }))

describe('studentActivityApi', () => {
  beforeEach(() => vi.clearAllMocks())

  it('records a visit with the browser timezone', async () => {
    vi.mocked(httpClient.post).mockResolvedValue({ data: { currentStreak: 1 } })
    await studentActivityApi.recordVisit('Asia/Dushanbe')
    expect(httpClient.post).toHaveBeenCalledWith('/api/v2/students/me/activity/visit', { timeZoneId: 'Asia/Dushanbe' })
  })

  it('loads current and selected calendar months', async () => {
    vi.mocked(httpClient.get).mockResolvedValue({ data: {} })
    await studentActivityApi.calendar()
    await studentActivityApi.calendar(2026, 7)
    expect(httpClient.get).toHaveBeenNthCalledWith(1, '/api/v2/students/me/activity/calendar')
    expect(httpClient.get).toHaveBeenNthCalledWith(2, '/api/v2/students/me/activity/calendar?year=2026&month=7')
  })
})
