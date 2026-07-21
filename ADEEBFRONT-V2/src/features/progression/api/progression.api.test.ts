import { beforeEach, describe, expect, it, vi } from 'vitest'

const get = vi.fn()
vi.mock('@/shared/api/http-client', () => ({ httpClient: { get } }))

describe('progression API', () => {
  beforeEach(() => get.mockReset())

  it('loads the authenticated student XP balance', async () => {
    get.mockResolvedValue({ data: { totalXp: 22, updatedAtUtc: '2026-07-21T10:00:00Z' } })
    const { progressionApi } = await import('@/features/progression/api/progression.api')
    const result = await progressionApi.getXpSummary()

    expect(get).toHaveBeenCalledWith('/api/v2/student/xp')
    expect(result.totalXp).toBe(22)
  })
})
