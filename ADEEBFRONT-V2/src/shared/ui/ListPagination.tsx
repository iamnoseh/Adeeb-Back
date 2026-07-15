import { ChevronLeft, ChevronRight } from 'lucide-react'
import { Button } from '@/shared/ui/Button'

export function ListPagination({ page, pageSize, total, onPage }: { page: number; pageSize: number; total: number; onPage: (page: number) => void }) {
  const pages = Math.max(1, Math.ceil(total / pageSize))
  if (total <= pageSize) return null

  return (
    <div className="flex items-center justify-end gap-3 px-1 py-2">
      <span className="text-sm font-semibold text-[var(--muted)]">{page} / {pages}</span>
      <Button type="button" variant="secondary" className="h-10 min-h-10 w-10 px-0" disabled={page <= 1} onClick={() => onPage(page - 1)} aria-label="Previous page">
        <ChevronLeft className="h-4 w-4" aria-hidden />
      </Button>
      <Button type="button" variant="secondary" className="h-10 min-h-10 w-10 px-0" disabled={page >= pages} onClick={() => onPage(page + 1)} aria-label="Next page">
        <ChevronRight className="h-4 w-4" aria-hidden />
      </Button>
    </div>
  )
}
