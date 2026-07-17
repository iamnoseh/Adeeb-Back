export const defaultStudentTimeZone = 'Asia/Dushanbe'

export function browserTimeZone() {
  try {
    return Intl.DateTimeFormat().resolvedOptions().timeZone || defaultStudentTimeZone
  } catch {
    return defaultStudentTimeZone
  }
}

export function millisecondsUntilNextLocalMidnight(now = new Date()) {
  const next = new Date(now)
  next.setHours(24, 0, 1, 0)
  return Math.max(1_000, next.getTime() - now.getTime())
}

export function periodAfter(year: number, month: number, otherYear: number, otherMonth: number) {
  return year > otherYear || (year === otherYear && month > otherMonth)
}

export function calendarCells(year: number, month: number) {
  const firstWeekday = (new Date(Date.UTC(year, month - 1, 1)).getUTCDay() + 6) % 7
  const daysInMonth = new Date(Date.UTC(year, month, 0)).getUTCDate()
  return Array.from(
    { length: firstWeekday + daysInMonth },
    (_, index) => index < firstWeekday ? null : index - firstWeekday + 1,
  )
}

export function isoDate(year: number, month: number, day: number) {
  return `${String(year).padStart(4, '0')}-${String(month).padStart(2, '0')}-${String(day).padStart(2, '0')}`
}
