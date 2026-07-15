import { Check, Eye, EyeOff, Filter, RotateCcw, Search, Settings2, X } from 'lucide-react'
import { useEffect, useRef, useState, type ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { Button } from '@/shared/ui/Button'
import { Input } from '@/shared/ui/Input'

export type AdminListColumn = {
  id: string
  label: string
  locked?: boolean
  defaultVisible?: boolean
}

export function useColumnVisibility(storageKey: string, columns: AdminListColumn[]) {
  const [visibility, setVisibility] = useState<Record<string, boolean>>(() => readVisibility(storageKey, columns))

  useEffect(() => {
    setVisibility(readVisibility(storageKey, columns))
  }, [storageKey])

  useEffect(() => {
    window.localStorage.setItem(storageKey, JSON.stringify(visibility))
  }, [storageKey, visibility])

  function isVisible(id: string) {
    return columns.find((column) => column.id === id)?.locked || visibility[id] !== false
  }

  function toggle(id: string) {
    const column = columns.find((item) => item.id === id)
    if (!column || column.locked) return
    setVisibility((current) => ({ ...current, [id]: current[id] === false }))
  }

  function reset() {
    setVisibility(defaultVisibility(columns))
  }

  function showAll() {
    setVisibility(Object.fromEntries(columns.map((column) => [column.id, true])))
  }

  function hideOptional() {
    setVisibility(Object.fromEntries(columns.map((column) => [column.id, Boolean(column.locked)])))
  }

  return { isVisible, toggle, reset, showAll, hideOptional }
}

type ColumnVisibility = ReturnType<typeof useColumnVisibility>

export function AdminListToolbar({
  searchValue,
  onSearchChange,
  searchPlaceholder,
  filterCount = 0,
  onClearFilters,
  filters,
  columns,
  columnVisibility,
}: {
  searchValue: string
  onSearchChange: (value: string) => void
  searchPlaceholder: string
  filterCount?: number
  onClearFilters?: () => void
  filters?: ReactNode
  columns?: AdminListColumn[]
  columnVisibility?: ColumnVisibility
}) {
  const { t } = useTranslation()
  const [filtersOpen, setFiltersOpen] = useState(false)
  const [columnsOpen, setColumnsOpen] = useState(false)
  const columnsRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    function closeColumns(event: MouseEvent) {
      if (!columnsRef.current?.contains(event.target as Node)) setColumnsOpen(false)
    }
    document.addEventListener('mousedown', closeColumns)
    return () => document.removeEventListener('mousedown', closeColumns)
  }, [])

  const hasFilters = Boolean(filters)
  const hasColumns = Boolean(columns?.length && columnVisibility)

  return (
    <div className="app-surface relative rounded-lg">
      <div className="flex flex-col gap-3 p-4 sm:flex-row sm:items-center sm:justify-between">
        <div className="relative w-full sm:max-w-md">
          <Search className="pointer-events-none absolute left-4 top-1/2 h-4 w-4 -translate-y-1/2 text-[var(--muted)]" aria-hidden />
          <Input
            value={searchValue}
            onChange={(event) => onSearchChange(event.target.value)}
            className="min-h-11 pl-11"
            placeholder={searchPlaceholder}
            aria-label={searchPlaceholder}
          />
        </div>

        <div className="flex flex-wrap justify-end gap-2">
          {hasFilters ? (
            <Button
              type="button"
              variant="secondary"
              className={filtersOpen ? 'border-[var(--primary)] text-[var(--primary-strong)]' : ''}
              onClick={() => setFiltersOpen((current) => !current)}
              aria-expanded={filtersOpen}
            >
              <Filter className="h-4 w-4" aria-hidden />
              {t('filters')}{filterCount > 0 ? ` (${filterCount})` : ''}
            </Button>
          ) : null}

          {hasColumns ? (
            <div ref={columnsRef} className="relative">
              <Button
                type="button"
                variant="secondary"
                className={columnsOpen ? 'border-[var(--primary)] text-[var(--primary-strong)]' : ''}
                onClick={() => setColumnsOpen((current) => !current)}
                aria-expanded={columnsOpen}
              >
                <Settings2 className="h-4 w-4" aria-hidden />
                {t('columnSettings')}
              </Button>

              {columnsOpen ? (
                <div className="absolute right-0 top-[calc(100%+0.5rem)] z-50 w-[min(22rem,calc(100vw-2rem))] rounded-lg border border-[var(--border)] bg-white p-3 shadow-[0_20px_50px_rgb(24_49_45/0.18)]">
                  <p className="border-b border-[var(--border)] px-2 pb-3 text-xs font-bold uppercase text-[var(--muted)]">{t('columnVisibility')}</p>
                  <div className="grid gap-1 py-2 sm:grid-cols-2">
                    {columns!.map((column) => {
                      const visible = columnVisibility!.isVisible(column.id)
                      return (
                        <button
                          key={column.id}
                          type="button"
                          disabled={column.locked}
                          className="flex min-h-10 items-center gap-3 rounded-md px-2 text-left text-sm font-semibold hover:bg-[var(--surface-muted)] disabled:cursor-default disabled:opacity-70"
                          onClick={() => columnVisibility!.toggle(column.id)}
                        >
                          <span className={`grid h-5 w-5 shrink-0 place-items-center rounded-md border ${visible ? 'border-[var(--primary)] bg-[var(--primary)] text-white' : 'border-[var(--border)]'}`}>
                            {visible ? <Check className="h-3.5 w-3.5" aria-hidden /> : null}
                          </span>
                          <span className="truncate">{column.label}</span>
                        </button>
                      )
                    })}
                  </div>
                  <div className="flex flex-wrap gap-1 border-t border-[var(--border)] pt-2">
                    <Button type="button" variant="ghost" className="px-2" onClick={columnVisibility!.reset}><RotateCcw className="h-4 w-4" /> {t('resetColumns')}</Button>
                    <Button type="button" variant="ghost" className="px-2" onClick={columnVisibility!.showAll}><Eye className="h-4 w-4" /> {t('showAll')}</Button>
                    <Button type="button" variant="ghost" className="px-2 text-[var(--danger)]" onClick={columnVisibility!.hideOptional}><EyeOff className="h-4 w-4" /> {t('hideOptional')}</Button>
                  </div>
                </div>
              ) : null}
            </div>
          ) : null}
        </div>
      </div>

      {hasFilters && filtersOpen ? (
        <div className="border-t border-[var(--border)] p-4">
          <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-3">{filters}</div>
          {filterCount > 0 && onClearFilters ? (
            <div className="mt-4 flex justify-end">
              <Button type="button" variant="ghost" className="text-[var(--danger)]" onClick={onClearFilters}><X className="h-4 w-4" /> {t('clearFilters')}</Button>
            </div>
          ) : null}
        </div>
      ) : null}
    </div>
  )
}

function readVisibility(storageKey: string, columns: AdminListColumn[]) {
  try {
    const stored = JSON.parse(window.localStorage.getItem(storageKey) ?? '{}') as Record<string, boolean>
    return { ...defaultVisibility(columns), ...stored }
  } catch {
    return defaultVisibility(columns)
  }
}

function defaultVisibility(columns: AdminListColumn[]) {
  return Object.fromEntries(columns.map((column) => [column.id, column.defaultVisible !== false]))
}
