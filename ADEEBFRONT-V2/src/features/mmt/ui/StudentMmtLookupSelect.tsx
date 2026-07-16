import { ChevronDown, ChevronLeft, ChevronRight, LoaderCircle, Search } from 'lucide-react'
import { useEffect, useId, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { cn } from '@/shared/lib/cn'

type LookupItem = { id: string }

type StudentMmtLookupSelectProps<T extends LookupItem> = {
  value: string
  selectedLabel?: string
  items: T[]
  getLabel: (item: T) => string
  onValueChange: (id: string, item: T) => void
  search: string
  onSearchChange: (value: string) => void
  page: number
  totalCount: number
  pageSize?: number
  onPageChange: (page: number) => void
  placeholder: string
  searchPlaceholder: string
  disabled?: boolean
  loading?: boolean
}

export function StudentMmtLookupSelect<T extends LookupItem>({
  value, selectedLabel, items, getLabel, onValueChange, search, onSearchChange,
  page, totalCount, pageSize = 10, onPageChange, placeholder, searchPlaceholder,
  disabled = false, loading = false,
}: StudentMmtLookupSelectProps<T>) {
  const { t } = useTranslation()
  const id = useId()
  const rootRef = useRef<HTMLDivElement>(null)
  const [open, setOpen] = useState(false)
  const currentLabel = items.find((item) => item.id === value)
  const pageCount = Math.max(1, Math.ceil(totalCount / pageSize))

  useEffect(() => {
    const close = (event: MouseEvent) => {
      if (!rootRef.current?.contains(event.target as Node)) setOpen(false)
    }
    document.addEventListener('mousedown', close)
    return () => document.removeEventListener('mousedown', close)
  }, [])

  return <div ref={rootRef} className={cn('relative', open && 'z-30')}>
    <button type="button" disabled={disabled} aria-expanded={open} aria-controls={id} aria-haspopup="listbox"
      onClick={() => setOpen((current) => !current)}
      className="flex min-h-12 w-full items-center justify-between gap-3 rounded-lg border border-[var(--student-border)] bg-[var(--student-surface)] px-4 text-left text-sm font-bold text-[var(--student-text)] shadow-sm transition hover:border-[#8b83f5] disabled:cursor-not-allowed disabled:opacity-55">
      <span className={cn('truncate', !value && 'text-[var(--student-muted)]')}>{currentLabel ? getLabel(currentLabel) : selectedLabel || placeholder}</span>
      <ChevronDown className={cn('h-4 w-4 shrink-0 text-[var(--student-muted)] transition', open && 'rotate-180')} />
    </button>
    {open ? <div id={id} role="listbox" className="absolute inset-x-0 top-[calc(100%+0.45rem)] overflow-hidden rounded-lg border border-[var(--student-border)] bg-[var(--student-surface)] shadow-[0_18px_50px_rgb(20_31_70/0.16)]">
      <div className="relative border-b border-[var(--student-border)] p-2">
        <Search className="pointer-events-none absolute left-5 top-1/2 h-4 w-4 -translate-y-1/2 text-[var(--student-muted)]" />
        <input autoFocus value={search} onChange={(event) => { onSearchChange(event.target.value); onPageChange(1) }} placeholder={searchPlaceholder}
          className="min-h-10 w-full rounded-md bg-[var(--student-surface-soft)] pl-9 pr-3 text-sm text-[var(--student-text)] outline-none focus:ring-2 focus:ring-[#8b83f5]" />
      </div>
      <div className="max-h-64 overflow-y-auto p-1.5">
        {loading ? <div className="grid min-h-24 place-items-center"><LoaderCircle className="h-5 w-5 animate-spin text-[#5146f0]" /></div> : null}
        {!loading && items.length === 0 ? <p className="px-3 py-6 text-center text-sm text-[var(--student-muted)]">{t('student.noLookupResults')}</p> : null}
        {!loading ? items.map((item) => <button key={item.id} type="button" role="option" aria-selected={item.id === value}
          onClick={() => { onValueChange(item.id, item); setOpen(false) }}
          className={cn('w-full rounded-md px-3 py-2.5 text-left text-sm font-bold transition', item.id === value ? 'bg-[#5146f0] text-white' : 'text-[var(--student-text)] hover:bg-[var(--student-surface-soft)]')}>{getLabel(item)}</button>) : null}
      </div>
      {pageCount > 1 ? <div className="flex items-center justify-end gap-2 border-t border-[var(--student-border)] px-3 py-2 text-xs font-bold text-[var(--student-muted)]">
        <span>{page} / {pageCount}</span>
        <button type="button" aria-label={t('student.previousPage')} disabled={page <= 1} onClick={() => onPageChange(page - 1)} className="grid h-8 w-8 place-items-center rounded-md border border-[var(--student-border)] disabled:opacity-40"><ChevronLeft className="h-4 w-4" /></button>
        <button type="button" aria-label={t('student.nextPage')} disabled={page >= pageCount} onClick={() => onPageChange(page + 1)} className="grid h-8 w-8 place-items-center rounded-md border border-[var(--student-border)] disabled:opacity-40"><ChevronRight className="h-4 w-4" /></button>
      </div> : null}
    </div> : null}
  </div>
}
