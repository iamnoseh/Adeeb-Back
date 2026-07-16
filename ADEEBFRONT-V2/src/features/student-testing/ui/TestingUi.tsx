import { AlertCircle, LoaderCircle } from 'lucide-react'
import type { ButtonHTMLAttributes, ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { cn } from '@/shared/lib/cn'
import { testingErrorKey } from '@/features/student-testing/lib/student-testing'

export function TestingCard({ children, className }: { children: ReactNode; className?: string }) {
  return <section className={cn('rounded-lg border border-[var(--student-border)] bg-[var(--student-surface)] p-5 shadow-[0_10px_28px_rgb(20_31_70/0.04)] sm:p-6', className)}>{children}</section>
}

export function TestingButton({ className, children, variant = 'primary', ...props }: ButtonHTMLAttributes<HTMLButtonElement> & { variant?: 'primary' | 'secondary' | 'danger' }) {
  const variants = {
    primary: 'bg-[#5146f0] text-white hover:bg-[#4036d6] disabled:bg-[#aaa5ea]',
    secondary: 'border border-[var(--student-border)] bg-[var(--student-surface)] text-[var(--student-text)] hover:bg-[var(--student-surface-soft)]',
    danger: 'border border-red-200 bg-red-50 text-red-700 hover:bg-red-100',
  }
  return <button className={cn('inline-flex min-h-11 items-center justify-center gap-2 rounded-lg px-4 text-sm font-black transition disabled:cursor-not-allowed disabled:opacity-60', variants[variant], className)} {...props}>{children}</button>
}

export function TestingLoading() {
  const { t } = useTranslation()
  return <div className="grid min-h-56 place-items-center"><span className="inline-flex items-center gap-2 text-sm font-bold text-[var(--student-muted)]"><LoaderCircle className="h-5 w-5 animate-spin" />{t('student.testing.loading')}</span></div>
}

export function TestingError({ error, onRetry }: { error: unknown; onRetry?: () => void }) {
  const { t } = useTranslation()
  const key = testingErrorKey(error)
  return <div role="alert" className="rounded-lg border border-red-200 bg-red-50 p-5 text-red-800"><div className="flex items-start gap-3"><AlertCircle className="mt-0.5 h-5 w-5 shrink-0" /><div><p className="font-black">{t(`student.testing.errors.${key}`)}</p>{onRetry ? <button type="button" className="mt-3 text-sm font-black underline" onClick={onRetry}>{t('student.retry')}</button> : null}</div></div></div>
}

export function TestingEmpty({ title, description }: { title: string; description?: string }) {
  return <div className="rounded-lg bg-[var(--student-surface-soft)] px-5 py-10 text-center"><p className="font-black">{title}</p>{description ? <p className="mt-2 text-sm text-[var(--student-muted)]">{description}</p> : null}</div>
}

export function TestingBadge({ children, tone = 'neutral' }: { children: ReactNode; tone?: 'neutral' | 'success' | 'warning' | 'danger' }) {
  const styles = { neutral: 'bg-[#f0efff] text-[#5146f0]', success: 'bg-emerald-50 text-emerald-700', warning: 'bg-amber-50 text-amber-700', danger: 'bg-red-50 text-red-700' }
  return <span className={cn('inline-flex rounded-md px-2.5 py-1 text-xs font-black', styles[tone])}>{children}</span>
}

export function TestingToggle({ checked, onChange, label, description }: { checked: boolean; onChange: (value: boolean) => void; label: string; description?: string }) {
  return <label className="flex cursor-pointer items-start gap-3 rounded-lg border border-[var(--student-border)] p-4"><input type="checkbox" checked={checked} onChange={(event) => onChange(event.target.checked)} className="mt-1 h-5 w-5 accent-[#5146f0]" /><span><strong className="block text-sm">{label}</strong>{description ? <span className="mt-1 block text-sm leading-6 text-[var(--student-muted)]">{description}</span> : null}</span></label>
}
