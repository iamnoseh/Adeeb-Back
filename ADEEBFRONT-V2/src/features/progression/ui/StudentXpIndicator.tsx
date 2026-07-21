import { useQuery } from '@tanstack/react-query'
import { Award, RefreshCw, X } from 'lucide-react'
import { useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { progressionApi, progressionKeys } from '@/features/progression/api/progression.api'

export function StudentXpIndicator() {
  const { t, i18n } = useTranslation()
  const [open, setOpen] = useState(false)
  const containerRef = useRef<HTMLDivElement>(null)
  const summary = useQuery({ queryKey: progressionKeys.xpSummary(), queryFn: progressionApi.getXpSummary, staleTime: 30_000 })
  const formattedXp = new Intl.NumberFormat(i18n.language, { maximumFractionDigits: 1 }).format(summary.data?.totalXp ?? 0)

  useEffect(() => {
    if (!open) return
    const closeOnOutsideClick = (event: MouseEvent) => {
      if (!containerRef.current?.contains(event.target as Node)) setOpen(false)
    }
    const closeOnEscape = (event: KeyboardEvent) => {
      if (event.key === 'Escape') setOpen(false)
    }
    document.addEventListener('mousedown', closeOnOutsideClick)
    document.addEventListener('keydown', closeOnEscape)
    return () => {
      document.removeEventListener('mousedown', closeOnOutsideClick)
      document.removeEventListener('keydown', closeOnEscape)
    }
  }, [open])

  return <div ref={containerRef} className="relative">
    <button
      type="button"
      className="inline-flex min-h-10 items-center gap-2 rounded-lg border border-[var(--student-border)] bg-[var(--student-surface)] px-3 text-sm font-black text-[var(--student-text)] shadow-sm transition hover:border-[#aaa4ff] hover:bg-[var(--student-surface-soft)]"
      onClick={() => setOpen((value) => !value)}
      aria-label={t('student.xp.title')}
      aria-expanded={open}
      aria-haspopup="dialog"
    >
      <span className="grid h-7 w-7 place-items-center rounded-md bg-amber-50 text-amber-600"><Award className="h-4 w-4" /></span>
      <span className="tabular-nums">{summary.isLoading ? '...' : summary.isError ? '--' : formattedXp} XP</span>
    </button>
    {open ? <div role="dialog" aria-label={t('student.xp.title')} className="absolute right-0 top-[calc(100%+0.65rem)] z-50 w-[min(21rem,calc(100vw-2rem))] rounded-lg border border-[var(--student-border)] bg-[var(--student-surface)] p-4 shadow-[0_18px_50px_rgb(20_31_70/0.16)]">
      <div className="flex items-start gap-3">
        <span className="grid h-10 w-10 shrink-0 place-items-center rounded-lg bg-amber-50 text-amber-600"><Award className="h-5 w-5" /></span>
        <div className="min-w-0 flex-1"><p className="text-sm font-black">{t('student.xp.title')}</p><p className="mt-1 text-2xl font-black tabular-nums text-[#5146f0]">{formattedXp} XP</p></div>
        <button type="button" className="grid h-8 w-8 place-items-center rounded-md text-[var(--student-muted)] hover:bg-[var(--student-surface-soft)]" onClick={() => setOpen(false)} aria-label={t('student.closeNotice')}><X className="h-4 w-4" /></button>
      </div>
      {summary.isError ? <div className="mt-4 rounded-lg bg-red-50 p-3 text-sm font-bold text-red-700"><p>{t('student.xp.loadFailed')}</p><button type="button" className="mt-2 inline-flex items-center gap-1 text-xs font-black" onClick={() => void summary.refetch()}><RefreshCw className="h-3.5 w-3.5" />{t('student.retry')}</button></div> : <><p className="mt-4 text-sm leading-6 text-[var(--student-muted)]">{t('student.xp.whatIsXp')}</p><div className="mt-4 border-t border-[var(--student-border)] pt-3"><p className="text-xs font-black uppercase text-[var(--student-muted)]">{t('student.xp.howToEarn')}</p><p className="mt-2 text-sm font-bold leading-6">{t('student.xp.earnTests')}</p></div></>}
    </div> : null}
  </div>
}
