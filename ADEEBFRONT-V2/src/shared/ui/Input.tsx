import { ChevronDown } from 'lucide-react'
import type { InputHTMLAttributes, SelectHTMLAttributes, TextareaHTMLAttributes } from 'react'
import { cn } from '@/shared/lib/cn'

const fieldClass =
  'min-h-11 w-full rounded-2xl border border-transparent bg-[var(--surface-muted)] px-4 py-2.5 text-sm text-[var(--text)] shadow-[inset_0_0_0_1px_rgb(17_24_23/0.04)] transition placeholder:text-[var(--muted)] hover:bg-white hover:border-[var(--border)] focus:bg-white focus:border-[var(--primary)] focus:shadow-[0_0_0_4px_rgb(47_125_115/0.12)] focus:outline-none disabled:cursor-not-allowed disabled:opacity-60'

export function Input({ className, ...props }: InputHTMLAttributes<HTMLInputElement>) {
  return <input className={cn(fieldClass, className)} {...props} />
}

export function Textarea({ className, ...props }: TextareaHTMLAttributes<HTMLTextAreaElement>) {
  return <textarea className={cn(fieldClass, 'min-h-28 resize-y', className)} {...props} />
}

export function Select({ className, ...props }: SelectHTMLAttributes<HTMLSelectElement>) {
  return (
    <span className="relative block">
      <select
        className={cn(
          fieldClass,
          'appearance-none pr-11 font-semibold text-[var(--text)]',
          className,
        )}
        {...props}
      />
      <ChevronDown
        className="pointer-events-none absolute right-3.5 top-1/2 h-4 w-4 -translate-y-1/2 text-[var(--muted)]"
        aria-hidden
      />
    </span>
  )
}
