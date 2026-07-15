import type { ReactNode } from 'react'
import { Link } from 'react-router-dom'
import { cn } from '@/shared/lib/cn'

type TableActionButtonProps = {
  label: string
  icon: ReactNode
  to?: string
  onClick?: () => void
  disabled?: boolean
  tone?: 'default' | 'danger'
}

export function TableActionButton({ label, icon, to, onClick, disabled = false, tone = 'default' }: TableActionButtonProps) {
  const className = cn(
    'inline-grid h-11 min-h-11 w-11 shrink-0 place-items-center rounded-xl border border-[var(--border)] bg-white p-0 shadow-sm transition',
    'text-[var(--text)] hover:-translate-y-0.5 hover:border-[var(--primary)] hover:text-[var(--primary-strong)] hover:shadow-md',
    'focus-visible:outline-none focus-visible:ring-4 focus-visible:ring-[rgb(47_125_115/0.16)]',
    tone === 'danger' ? 'text-[var(--danger)] hover:border-[var(--danger)] hover:text-[var(--danger)]' : '',
    disabled ? 'pointer-events-none cursor-not-allowed opacity-40' : '',
  )

  if (to) {
    return <Link to={to} className={className} aria-label={label} title={label}>{icon}</Link>
  }

  return <button type="button" className={className} onClick={onClick} disabled={disabled} aria-label={label} title={label}>{icon}</button>
}
