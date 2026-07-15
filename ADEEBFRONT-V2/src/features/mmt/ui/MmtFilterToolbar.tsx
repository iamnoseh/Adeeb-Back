import type { ReactNode } from 'react'
import {
  AdminListToolbar,
  type AdminListColumn,
  useColumnVisibility,
} from '@/shared/ui/AdminListToolbar'

export { useColumnVisibility }
export type { AdminListColumn }

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
    <AdminListToolbar
      searchValue={searchValue}
      onSearchChange={onSearchChange}
      searchPlaceholder={searchPlaceholder}
      filterCount={filterCount}
      onClearFilters={onClearFilters}
      filters={children}
      columns={columns}
      columnVisibility={columnVisibility}
    />
  )
}
