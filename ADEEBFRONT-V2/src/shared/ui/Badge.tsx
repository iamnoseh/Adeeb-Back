import type { ReactNode } from 'react'
import { cn } from '@/shared/lib/cn'

type BadgeProps = {
  children: ReactNode
  tone?: 'neutral' | 'success' | 'warning' | 'danger'
}

const tones = {
  neutral: 'border-[var(--border)] bg-[var(--surface-muted)] text-[var(--muted)]',
  success: 'border-emerald-100 bg-emerald-50 text-[var(--success)]',
  warning: 'border-amber-100 bg-amber-50 text-[var(--warning)]',
  danger: 'border-red-100 bg-red-50 text-[var(--danger)]',
}

export function Badge({ children, tone = 'neutral' }: BadgeProps) {
  return <span className={cn('inline-flex rounded-full border px-2.5 py-1 text-xs font-bold', tones[tone])}>{children}</span>
}
