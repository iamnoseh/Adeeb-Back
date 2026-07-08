import type { ReactNode } from 'react'

type PageHeaderProps = {
  title: string
  description?: string
  actions?: ReactNode
}

export function PageHeader({ title, description, actions }: PageHeaderProps) {
  return (
    <header className="mx-auto mb-6 w-full overflow-hidden rounded-[2rem] border border-white/70 bg-[linear-gradient(135deg,#ffffff_0%,#f5faf8_50%,#dfecea_100%)] p-5 shadow-[var(--shadow)] md:p-7">
      <div className="flex flex-col gap-4 md:flex-row md:items-end md:justify-between">
        <div>
          <h1 className="text-2xl font-black tracking-normal text-[var(--text)] md:text-3xl">{title}</h1>
          {description ? <p className="mt-2 max-w-3xl text-sm leading-6 text-[var(--muted)]">{description}</p> : null}
        </div>
        {actions ? <div className="flex flex-wrap gap-2">{actions}</div> : null}
      </div>
    </header>
  )
}
