import { AlertCircle } from 'lucide-react'

type StateBlockProps = {
  title: string
  description?: string
}

export function EmptyState({ title, description }: StateBlockProps) {
  return (
    <div className="app-surface rounded-lg p-10 text-center">
      <h2 className="text-lg font-bold">{title}</h2>
      {description ? <p className="mt-2 text-sm text-[var(--muted)]">{description}</p> : null}
    </div>
  )
}

export function ErrorState({ title, description }: StateBlockProps) {
  return (
    <div className="rounded-lg border border-red-200 bg-red-50 p-5 text-[var(--danger)]">
      <div className="flex items-start gap-3">
        <AlertCircle className="mt-0.5 h-5 w-5" aria-hidden />
        <div>
          <h2 className="font-bold">{title}</h2>
          {description ? <p className="mt-1 text-sm">{description}</p> : null}
        </div>
      </div>
    </div>
  )
}
