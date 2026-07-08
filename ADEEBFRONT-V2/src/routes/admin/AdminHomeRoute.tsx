import { Link } from 'react-router-dom'
import { PageHeader } from '@/shared/ui/PageHeader'

export function AdminHomeRoute() {
  return (
    <>
      <PageHeader title="Панели идоракунӣ" description="Аввалин scope: фанҳо, мавзуъҳо ва бонки саволҳо." />
      <div className="grid gap-4 md:grid-cols-3">
        {[
          { label: 'Фанҳо', to: '/admin/subjects' },
          { label: 'Мавзуъҳо', to: '/admin/topics' },
          { label: 'Саволҳо', to: '/admin/questions' },
        ].map(({ label, to }) => (
          <Link key={to} to={to} className="app-surface rounded-lg p-5 text-[var(--text)] no-underline transition hover:-translate-y-0.5 hover:shadow-sm">
            <strong>{label}</strong>
            <p className="mt-2 text-sm text-[var(--muted)]">Идоракунӣ</p>
          </Link>
        ))}
      </div>
    </>
  )
}
