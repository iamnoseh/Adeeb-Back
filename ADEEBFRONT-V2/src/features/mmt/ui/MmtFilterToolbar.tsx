import type { ReactNode } from 'react'
import {
  AdminListToolbar,
} from '@/shared/ui/AdminListToolbar'
import { type AdminListColumn, type useColumnVisibility } from '@/shared/ui/useColumnVisibility'

export function MmtFilterToolbar({
  searchValue,
  onSearchChange,
  searchPlaceholder,
  filterCount,
  onClearFilters,
  children,
  columns,
  columnVisibility,
}: {
  searchValue: string
  onSearchChange: (value: string) => void
  searchPlaceholder: string
  filterCount: number
  onClearFilters: () => void
  children: ReactNode
  columns?: AdminListColumn[]
  columnVisibility?: ReturnType<typeof useColumnVisibility>
}) {
  return (
    <div className="mb-4">
      <AdminListToolbar
        searchValue={searchValue}
        onSearchChange={onSearchChange}
        searchPlaceholder={searchPlaceholder}
        filterCount={filterCount}
        onClearFilters={onClearFilters}
        filters={children}
        {...(columns ? { columns } : {})}
        {...(columnVisibility ? { columnVisibility } : {})}
      />
    </div>
  )
}
