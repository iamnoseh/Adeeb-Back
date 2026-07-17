// @vitest-environment jsdom
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { cleanup, render, waitFor } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { studentActivityApi } from '@/features/student-activity/api/student-activity.api'
import { useStudentActivityVisit } from './useStudentActivity'

afterEach(() => {
  cleanup()
  vi.restoreAllMocks()
})

describe('useStudentActivityVisit', () => {
  it('records on mount and when the visible tab is revisited', async () => {
    const record = vi.spyOn(studentActivityApi, 'recordVisit').mockResolvedValue({
      year: 2026, month: 7, timeZoneId: 'Asia/Dushanbe', todayLocalDate: '2026-07-17',
      currentStreak: 1, longestStreak: 1, activeDaysInMonth: 1, totalActiveDays: 1,
      days: [{ date: '2026-07-17' }],
    })
    Object.defineProperty(document, 'visibilityState', { configurable: true, value: 'visible' })
    const client = new QueryClient({ defaultOptions: { mutations: { retry: false } } })

    render(<QueryClientProvider client={client}><Harness /></QueryClientProvider>)
    await waitFor(() => expect(record).toHaveBeenCalledTimes(1))
    document.dispatchEvent(new Event('visibilitychange'))
    await waitFor(() => expect(record).toHaveBeenCalledTimes(2))
  })
})

function Harness() {
  useStudentActivityVisit()
  return null
}
