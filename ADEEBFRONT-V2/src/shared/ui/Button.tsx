import type { ButtonHTMLAttributes } from 'react'
import { cn } from '@/shared/lib/cn'

type ButtonVariant = 'primary' | 'secondary' | 'danger' | 'ghost'

type ButtonProps = ButtonHTMLAttributes<HTMLButtonElement> & {
  variant?: ButtonVariant
}

const variants: Record<ButtonVariant, string> = {
  primary: 'bg-[linear-gradient(180deg,var(--primary),var(--primary-strong))] text-white shadow-[0_12px_24px_rgb(47_125_115/0.24)] hover:brightness-105',
  secondary: 'border border-[var(--border)] bg-white text-[var(--text)] shadow-sm hover:bg-[var(--surface-soft)]',
  danger: 'bg-[var(--danger)] text-white shadow-[0_12px_24px_rgb(201_60_55/0.18)] hover:brightness-95',
  ghost: 'text-[var(--muted)] hover:bg-[var(--surface-muted)] hover:text-[var(--text)]',
}

export function Button({ className, variant = 'primary', ...props }: ButtonProps) {
  return (
    <button
      className={cn(
        'inline-flex min-h-11 items-center justify-center gap-2 rounded-2xl px-4 py-2.5 text-sm font-bold transition disabled:cursor-not-allowed disabled:opacity-60',
        variants[variant],
        className,
      )}
      {...props}
    />
  )
}
