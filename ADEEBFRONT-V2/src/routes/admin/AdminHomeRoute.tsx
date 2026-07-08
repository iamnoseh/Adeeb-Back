import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { PageHeader } from '@/shared/ui/PageHeader'

export function AdminHomeRoute() {
  const { t } = useTranslation()
  return (
    <>
      <PageHeader title={t('appName')} />
      <div className="grid gap-4 md:grid-cols-3">
        {[
          { label: t('navSubjects'), to: '/admin/subjects' },
          { label: t('navTopics'), to: '/admin/topics' },
          { label: t('navQuestions'), to: '/admin/questions' },
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
