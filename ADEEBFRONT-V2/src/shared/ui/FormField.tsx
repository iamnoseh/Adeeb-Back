import type { ReactNode } from 'react'

type FormFieldProps = {
  label: string
  error?: string | undefined
  children: ReactNode
}

export function FormField({ label, error, children }: FormFieldProps) {
  return (
    <label className="grid gap-1.5 text-sm font-medium text-[var(--text)]">
      <span>{label}</span>
      {children}
      {error ? <span className="text-xs font-semibold text-[var(--danger)]">{error}</span> : null}
    </label>
  )
}
