// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import type { ReactNode } from 'react'
import { studentActivityApi } from '@/features/student-activity/api/student-activity.api'
import type { StudentActivityCalendarDto } from '@/features/student-activity/model/student-activity.types'
import { StudentCalendar } from './StudentCalendar'

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    i18n: { language: 'ru-RU' },
    t: (key: string, values?: { count?: number }) => values?.count === undefined ? key : `${key}:${values.count}`,
  }),
}))

afterEach(() => {
  cleanup()
  vi.restoreAllMocks()
})

describe('StudentCalendar', () => {
  it('renders server-defined today and active days and loads previous month', async () => {
    const july = calendar(2026, 7, ['2026-07-16', '2026-07-17'])
    const june = calendar(2026, 6, ['2026-06-04'])
    const api = vi.spyOn(studentActivityApi, 'calendar').mockImplementation(async (year, month) =>
      year === 2026 && month === 6 ? june : july)
    const { container } = renderWithQuery(<StudentCalendar />)

    await screen.findByLabelText('2026-07-17')
    expect(screen.getByText('student.calendar')).toBeInTheDocument()
    expect(container.querySelector('[aria-label="2026-07-17"]')).toHaveAttribute('data-active', 'true')
    expect(container.querySelector('[aria-label="2026-07-17"]')).toHaveAttribute('data-today', 'true')
    expect(screen.getByRole('button', { name: 'student.nextMonth' })).toBeDisabled()

    fireEvent.click(screen.getByRole('button', { name: 'student.previousMonth' }))
    await waitFor(() => expect(api).toHaveBeenCalledWith(2026, 6))
    expect(await screen.findByLabelText('2026-06-04')).toHaveAttribute('data-active', 'true')
  })
})

function renderWithQuery(children: ReactNode) {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  return render(<QueryClientProvider client={client}>{children}</QueryClientProvider>)
}

function calendar(year: number, month: number, days: string[]): StudentActivityCalendarDto {
  return {
    year,
    month,
    timeZoneId: 'Asia/Dushanbe',
    todayLocalDate: '2026-07-17',
    currentStreak: 2,
    longestStreak: 4,
    activeDaysInMonth: days.length,
    totalActiveDays: 9,
    days: days.map((date) => ({ date })),
  }
}
