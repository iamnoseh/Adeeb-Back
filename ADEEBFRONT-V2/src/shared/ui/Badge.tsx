import type { ReactNode } from 'react'
import { cn } from '@/shared/lib/cn'

type BadgeProps = {
  children: ReactNode
  tone?: 'neutral' | 'success' | 'warning' | 'danger'
}

const tones = {
  neutral: 'border-[var(--border)] text-[var(--muted)]',
  success: 'border-green-200 bg-green-50 text-[var(--success)]',
  warning: 'border-amber-200 bg-amber-50 text-[var(--warning)]',
  danger: 'border-red-200 bg-red-50 text-[var(--danger)]',
}

export function Badge({ children, tone = 'neutral' }: BadgeProps) {
  return <span className={cn('inline-flex rounded-md border px-2 py-1 text-xs font-bold', tones[tone])}>{children}</span>
}
