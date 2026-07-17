export type StudentActivityDayDto = {
  date: string
}

export type StudentActivityCalendarDto = {
  year: number
  month: number
  timeZoneId: string
  todayLocalDate: string
  currentStreak: number
  longestStreak: number
  activeDaysInMonth: number
  totalActiveDays: number
  days: StudentActivityDayDto[]
}
