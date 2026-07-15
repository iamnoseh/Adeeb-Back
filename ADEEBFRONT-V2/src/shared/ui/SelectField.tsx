import { ChevronDown, Search } from 'lucide-react'
import { useEffect, useId, useRef, useState } from 'react'
import { cn } from '@/shared/lib/cn'

export type SelectFieldOption = {
  value: string
  label: string
  disabled?: boolean
}

type SelectFieldProps = {
  value: string
  options: SelectFieldOption[]
  onValueChange: (value: string) => void
  placeholder?: string
  name?: string
  disabled?: boolean
  className?: string
  searchable?: boolean
  searchPlaceholder?: string
}

export function SelectField({
  value,
  options,
  onValueChange,
  placeholder,
  name,
  disabled = false,
  className,
  searchable = false,
  searchPlaceholder,
}: SelectFieldProps) {
  const id = useId()
  const rootRef = useRef<HTMLDivElement>(null)
  const [open, setOpen] = useState(false)
  const [search, setSearch] = useState('')
  const selectedOption = options.find((option) => option.value === value)
  const visibleOptions = searchable
    ? options.filter((option) => option.label.toLocaleLowerCase().includes(search.trim().toLocaleLowerCase()))
    : options

  useEffect(() => {
    function closeOnOutsideClick(event: MouseEvent) {
      if (!rootRef.current?.contains(event.target as Node)) {
        setOpen(false)
      }
    }

    document.addEventListener('mousedown', closeOnOutsideClick)
    return () => document.removeEventListener('mousedown', closeOnOutsideClick)
  }, [])

  function choose(option: SelectFieldOption) {
    if (option.disabled) return
    onValueChange(option.value)
    setOpen(false)
    setSearch('')
  }

  return (
    <div ref={rootRef} className={cn('relative', open ? 'z-30' : '', className)}>
      {name ? <input type="hidden" name={name} value={value} /> : null}
      <button
        type="button"
        aria-expanded={open}
        aria-haspopup="listbox"
        aria-controls={id}
        disabled={disabled}
        className={cn(
          'flex min-h-14 w-full items-center justify-between gap-3 rounded-[1.55rem] border-2 border-[var(--border)] bg-white px-5 py-3 text-left text-base font-semibold text-[var(--text)] shadow-[0_8px_22px_rgb(24_49_45/0.06)] transition',
          'hover:border-[color-mix(in_srgb,var(--primary)_45%,var(--border))] hover:bg-[var(--surface-soft)]',
          'focus:border-[var(--primary)] focus:bg-white focus:shadow-[0_0_0_5px_rgb(47_125_115/0.16)] focus:outline-none',
          open ? 'border-[var(--primary)] bg-white shadow-[0_0_0_5px_rgb(47_125_115/0.16)]' : '',
          disabled ? 'cursor-not-allowed opacity-60' : '',
        )}
        onClick={() => setOpen((current) => !current)}
      >
        <span className={cn('truncate', selectedOption ? '' : 'text-[var(--muted)]')}>
          {selectedOption?.label ?? placeholder}
        </span>
        <ChevronDown className={cn('h-4 w-4 shrink-0 text-[var(--muted)] transition', open ? 'rotate-180' : '')} aria-hidden />
      </button>

      {open ? (
        <div
          id={id}
          role="listbox"
          className="absolute left-0 right-0 top-[calc(100%+0.45rem)] max-h-72 overflow-auto rounded-[1.25rem] border border-[var(--border)] bg-white p-1.5 shadow-[0_22px_55px_rgb(24_49_45/0.18)]"
        >
          {searchable ? (
            <div className="relative mb-1.5">
              <Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-[var(--muted)]" aria-hidden />
              <input
                autoFocus
                value={search}
                onChange={(event) => setSearch(event.target.value)}
                placeholder={searchPlaceholder}
                className="min-h-10 w-full rounded-xl border border-[var(--border)] bg-[var(--surface-muted)] pl-9 pr-3 text-sm outline-none focus:border-[var(--primary)]"
              />
            </div>
          ) : null}
          {visibleOptions.map((option) => {
            const selected = option.value === value
            return (
              <button
                key={option.value}
                type="button"
                role="option"
                aria-selected={selected}
                disabled={option.disabled}
                className={cn(
                  'flex min-h-11 w-full items-center rounded-2xl px-4 py-2.5 text-left text-sm font-semibold transition',
                  selected ? 'bg-[var(--primary)] text-white' : 'text-[var(--text)] hover:bg-[var(--surface-muted)]',
                  option.disabled ? 'cursor-not-allowed opacity-50' : '',
                )}
                onClick={() => choose(option)}
              >
                {option.label}
              </button>
            )
          })}
        </div>
      ) : null}
    </div>
  )
}
