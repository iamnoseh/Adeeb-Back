import type { ReactNode } from 'react'

type TableShellProps = {
  children: ReactNode
}

export function TableShell({ children }: TableShellProps) {
  return <div className="app-surface overflow-x-auto rounded-xl shadow-sm">{children}</div>
}

export function Table({ children }: TableShellProps) {
  return <table className="min-w-full border-collapse text-left text-sm">{children}</table>
}
