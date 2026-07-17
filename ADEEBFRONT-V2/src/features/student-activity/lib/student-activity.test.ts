import { describe, expect, it, vi } from 'vitest'
import { browserTimeZone, calendarCells, defaultStudentTimeZone, isoDate, millisecondsUntilNextLocalMidnight, periodAfter } from './student-activity'

describe('student activity helpers', () => {
  it('builds a Monday-first leap-month grid', () => {
    const cells = calendarCells(2024, 2)
    expect(cells.slice(0, 3)).toEqual([null, null, null])
    expect(cells.at(-1)).toBe(29)
  })

  it('formats stable ISO dates and compares periods', () => {
    expect(isoDate(2026, 7, 2)).toBe('2026-07-02')
    expect(periodAfter(2026, 8, 2026, 7)).toBe(true)
    expect(periodAfter(2026, 7, 2026, 7)).toBe(false)
  })

  it('schedules the next visit just after local midnight', () => {
    const now = new Date(2026, 6, 17, 23, 59, 30)
    expect(millisecondsUntilNextLocalMidnight(now)).toBe(31_000)
  })

  it('falls back to Dushanbe when browser timezone detection fails', () => {
    const formatter = vi.spyOn(Intl, 'DateTimeFormat').mockImplementation(() => { throw new Error('unavailable') })
    expect(browserTimeZone()).toBe(defaultStudentTimeZone)
    formatter.mockRestore()
  })
})
