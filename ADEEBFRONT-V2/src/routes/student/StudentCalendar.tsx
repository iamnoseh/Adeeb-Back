import { ChevronLeft, ChevronRight } from 'lucide-react'
import { useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'

export function StudentCalendar() {
  const { i18n, t } = useTranslation()
  const today = useMemo(() => new Date(), [])
  const [visibleMonth, setVisibleMonth] = useState(() => new Date(today.getFullYear(), today.getMonth(), 1))
  const locale = i18n.language === 'ru-RU' ? 'ru-RU' : 'tg-TJ'
  const monthLabel = new Intl.DateTimeFormat(locale, { month: 'long', year: 'numeric' }).format(visibleMonth)
  const weekdays = Array.from({ length: 7 }, (_, index) => {
    const monday = new Date(2024, 0, 1 + index)
    return new Intl.DateTimeFormat(locale, { weekday: 'short' }).format(monday).replace('.', '')
  })
  const firstWeekday = (visibleMonth.getDay() + 6) % 7
  const daysInMonth = new Date(visibleMonth.getFullYear(), visibleMonth.getMonth() + 1, 0).getDate()
  const cells = Array.from({ length: firstWeekday + daysInMonth }, (_, index) => index < firstWeekday ? null : index - firstWeekday + 1)

  function changeMonth(offset: number) {
    setVisibleMonth((current) => new Date(current.getFullYear(), current.getMonth() + offset, 1))
  }

  return (
    <section className="rounded-lg border border-[#e1e4ef] bg-white p-5 shadow-[0_10px_28px_rgb(20_31_70/0.04)]">
      <h2 className="text-base font-black tracking-normal text-[#111b3d]">{t('student.calendar')}</h2>
      <div className="mt-5 flex items-center justify-between gap-3">
        <button type="button" className="grid h-9 w-9 place-items-center rounded-lg text-[#68718c] hover:bg-[#f0efff] hover:text-[#5146f0]" onClick={() => changeMonth(-1)} aria-label={t('student.previousMonth')}><ChevronLeft className="h-4 w-4" /></button>
        <strong className="text-sm capitalize text-[#111b3d]">{monthLabel}</strong>
        <button type="button" className="grid h-9 w-9 place-items-center rounded-lg text-[#68718c] hover:bg-[#f0efff] hover:text-[#5146f0]" onClick={() => changeMonth(1)} aria-label={t('student.nextMonth')}><ChevronRight className="h-4 w-4" /></button>
      </div>
      <div className="mt-4 grid grid-cols-7 gap-y-2 text-center">
        {weekdays.map((day) => <span key={day} className="text-[0.68rem] font-bold text-[#9299ad]">{day}</span>)}
        {cells.map((day, index) => {
          const isToday = day === today.getDate() && visibleMonth.getMonth() === today.getMonth() && visibleMonth.getFullYear() === today.getFullYear()
          return <span key={`${day ?? 'blank'}-${index}`} className="grid h-8 place-items-center text-xs"><span className={isToday ? 'grid h-8 w-8 place-items-center rounded-full bg-[#5146f0] font-black text-white shadow-[0_7px_16px_rgb(81_70_240/0.24)]' : 'text-[#303957]'}>{day}</span></span>
        })}
      </div>
    </section>
  )
}
