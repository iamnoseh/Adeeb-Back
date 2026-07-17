import { useQuery } from '@tanstack/react-query'
import { CalendarCheck2, ChevronLeft, ChevronRight, Flame, RotateCcw } from 'lucide-react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { studentActivityApi, studentActivityKeys } from '@/features/student-activity/api/student-activity.api'
import { calendarCells, isoDate, periodAfter } from '@/features/student-activity/lib/student-activity'
import { useCurrentStudentActivity } from '@/features/student-activity/model/useStudentActivity'
import type { StudentActivityCalendarDto } from '@/features/student-activity/model/student-activity.types'

type Period = { year: number; month: number }

export function StudentCalendar() {
  const { i18n, t } = useTranslation()
  const [period, setPeriod] = useState<Period | null>(null)
  const current = useCurrentStudentActivity()
  const selected = useQuery({
    queryKey: studentActivityKeys.month(period?.year ?? 0, period?.month ?? 0),
    queryFn: () => studentActivityApi.calendar(period!.year, period!.month),
    enabled: period !== null,
    staleTime: 60_000,
  })
  const query = period ? selected : current
  const data = query.data
  const locale = i18n.language === 'ru-RU' ? 'ru-RU' : 'tg-TJ'

  function changeMonth(offset: number) {
    if (!data) return
    const date = new Date(Date.UTC(data.year, data.month - 1 + offset, 1))
    setPeriod({ year: date.getUTCFullYear(), month: date.getUTCMonth() + 1 })
  }

  const todayParts = data?.todayLocalDate.split('-').map(Number) ?? []
  const todayYear = todayParts[0] ?? 0
  const todayMonth = todayParts[1] ?? 0
  const nextDisabled = !data || !periodAfter(todayYear, todayMonth, data.year, data.month)

  return (
    <section className="rounded-lg border border-[var(--student-border)] bg-[var(--student-surface)] p-5 shadow-[0_10px_28px_rgb(20_31_70/0.04)]" aria-busy={query.isLoading}>
      <div className="flex items-center justify-between gap-3">
        <div className="flex min-w-0 items-center gap-3">
          <span className="grid h-10 w-10 shrink-0 place-items-center rounded-lg bg-[#f0efff] text-[#5146f0] [&>svg]:h-5 [&>svg]:w-5">
            <CalendarCheck2 />
          </span>
          <h2 className="truncate text-base font-black tracking-normal text-[var(--student-text)]">{t('student.calendar')}</h2>
        </div>
          {data ? (
            <span className="inline-flex min-h-8 shrink-0 items-center gap-1.5 rounded-lg bg-[#fff3e7] px-2.5 text-xs font-black text-[#d97706]">
              <Flame className="h-4 w-4" />
              {data.currentStreak}
            </span>
          ) : null}
      </div>

      {query.isLoading && !data ? <CalendarSkeleton /> : null}
      {query.isError && !data ? (
        <div className="mt-4 rounded-lg bg-[var(--student-surface-soft)] p-4 text-center">
          <p className="text-sm text-[var(--student-muted)]">{t('student.activityLoadFailed')}</p>
          <button type="button" onClick={() => void query.refetch()} className="mt-3 inline-flex min-h-9 items-center gap-2 rounded-lg border border-[var(--student-border)] px-3 text-xs font-black"><RotateCcw className="h-4 w-4" />{t('student.retry')}</button>
        </div>
      ) : null}
      {data ? (
        <>
          <div className="mt-5 flex items-center justify-between gap-3">
            <button type="button" className="grid h-9 w-9 place-items-center rounded-lg text-[var(--student-muted)] transition hover:bg-[var(--student-surface)] hover:text-[#5146f0] hover:shadow-sm" onClick={() => changeMonth(-1)} aria-label={t('student.previousMonth')}><ChevronLeft className="h-4 w-4" /></button>
            <strong className="text-sm capitalize text-[var(--student-text)]">{monthLabel(data.year, data.month, locale)}</strong>
            <button type="button" disabled={nextDisabled} className="grid h-9 w-9 place-items-center rounded-lg text-[var(--student-muted)] transition hover:bg-[var(--student-surface)] hover:text-[#5146f0] hover:shadow-sm disabled:cursor-not-allowed disabled:opacity-35" onClick={() => changeMonth(1)} aria-label={t('student.nextMonth')}><ChevronRight className="h-4 w-4" /></button>
          </div>
          <CalendarGrid data={data} locale={locale} />
          {data.activeDaysInMonth === 0 ? <p className="mt-3 text-center text-xs text-[var(--student-muted)]">{t('student.noActiveDays')}</p> : null}
        </>
      ) : null}
    </section>
  )
}

function CalendarGrid({ data, locale }: { data: StudentActivityCalendarDto; locale: string }) {
  const weekdays = Array.from({ length: 7 }, (_, index) => {
    const monday = new Date(Date.UTC(2024, 0, 1 + index))
    return new Intl.DateTimeFormat(locale, { weekday: 'short', timeZone: 'UTC' }).format(monday).replace('.', '')
  })
  const activeDates = new Set(data.days.map((day) => day.date))
  return (
    <div className="mt-4 grid grid-cols-7 gap-x-1 gap-y-2 text-center">
      {weekdays.map((day) => <span key={day} className="py-1 text-[0.68rem] font-black uppercase text-[var(--student-muted)]">{day}</span>)}
      {calendarCells(data.year, data.month).map((day, index) => {
        if (day === null) return <span key={`blank-${index}`} className="h-8" aria-hidden="true" />
        const date = isoDate(data.year, data.month, day)
        const active = activeDates.has(date)
        const today = date === data.todayLocalDate
        return (
          <span key={date} className="grid h-9 place-items-center text-xs" aria-label={date} data-active={active || undefined} data-today={today || undefined}>
            <span className={`relative grid h-8 w-8 place-items-center rounded-full font-black transition ${active ? 'bg-[#e9f8ef] text-[#209348] shadow-[0_7px_14px_rgb(32_147_72/0.13)]' : 'text-[var(--student-text)] hover:bg-[var(--student-surface-soft)]'} ${today ? 'ring-2 ring-[#5146f0] ring-offset-2 ring-offset-[var(--student-surface)]' : ''}`}>
              {day}
              {active ? <span className="absolute -bottom-0.5 h-1.5 w-1.5 rounded-full bg-[#31b25f]" /> : null}
            </span>
          </span>
        )
      })}
    </div>
  )
}

function CalendarSkeleton() {
  return <div className="mt-5 animate-pulse"><div className="h-9 rounded-lg bg-[var(--student-surface-soft)]" /><div className="mt-4 h-56 rounded-lg bg-[var(--student-surface-soft)]" /></div>
}

function monthLabel(year: number, month: number, locale: string) {
  return new Intl.DateTimeFormat(locale, { month: 'long', year: 'numeric', timeZone: 'UTC' }).format(new Date(Date.UTC(year, month - 1, 1)))
}
