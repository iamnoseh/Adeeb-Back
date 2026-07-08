import type { ReactNode } from 'react'

type TableShellProps = {
  children: ReactNode
}

export function TableShell({ children }: TableShellProps) {
  return <div className="app-surface custom-scrollbar overflow-x-auto rounded-[1.5rem]">{children}</div>
}

export function Table({ children }: TableShellProps) {
  return <table className="min-w-full border-separate border-spacing-0 text-left text-sm">{children}</table>
}
