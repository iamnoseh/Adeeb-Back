import { useQuery } from '@tanstack/react-query'
import { BookOpenCheck, CalendarClock, History, ListChecks, Route, ShieldCheck } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import { studentTestingApi, studentTestingKeys } from '@/features/student-testing/api/student-testing.api'
import { canStartRedList } from '@/features/student-testing/lib/student-testing'
import { TestingBadge, TestingCard, TestingError, TestingLoading } from '@/features/student-testing/ui/TestingUi'
import { StudentPageHeader } from '@/routes/student/StudentUi'

export function StudentTestsHubPage() {
  const { t } = useTranslation()
  const config = useQuery({ queryKey: studentTestingKeys.config(), queryFn: studentTestingApi.getTestingConfig })
  const summary = useQuery({ queryKey: studentTestingKeys.redListSummary(), queryFn: studentTestingApi.getRedListSummary })
  if (config.isLoading || summary.isLoading) return <TestingLoading />
  if (config.isError) return <TestingError error={config.error} onRetry={() => void config.refetch()} />
  if (summary.isError) return <TestingError error={summary.error} onRetry={() => void summary.refetch()} />
  if (!config.data || !summary.data) return <TestingLoading />
  const locked = !canStartRedList(summary.data.activeCount, config.data.redListMinimumQuestions)
  const cards = [
    { to: '/student/tests/subject', icon: BookOpenCheck, title: t('student.testing.subject.title'), description: t('student.testing.subject.description') },
    { to: '/student/tests/mmt-practice', icon: Route, title: t('student.testing.mmt.title'), description: t('student.testing.mmt.description') },
    { to: '/student/tests/monthly-exam', icon: CalendarClock, title: t('student.testing.monthly.title'), description: t('student.testing.monthly.description'), disabled: !config.data.monthlyExamAvailable, badge: config.data.monthlyExamAvailable ? t('student.testing.available') : t('student.testing.unavailable') },
    { to: '/student/tests/red-list', icon: ListChecks, title: t('student.testing.redPractice.title'), description: t('student.testing.redPractice.description'), disabled: locked, badge: t('student.testing.activeCount', { count: summary.data.activeCount }) },
  ]
  return <div className="grid gap-6"><StudentPageHeader title={t('student.testing.title')} description={t('student.testing.hubDescription')} /><div className="grid gap-4 md:grid-cols-2">{cards.map((card) => <TestingCard key={card.to} className="flex min-h-56 flex-col"><div className="flex items-start justify-between gap-3"><span className="grid h-12 w-12 place-items-center rounded-lg bg-[#f0efff] text-[#5146f0]"><card.icon className="h-6 w-6" /></span>{card.badge ? <TestingBadge tone={card.disabled ? 'warning' : 'success'}>{card.badge}</TestingBadge> : null}</div><h2 className="mt-6 text-xl font-black">{card.title}</h2><p className="mt-2 flex-1 text-sm leading-6 text-[var(--student-muted)]">{card.description}</p>{card.disabled ? <span className="mt-5 inline-flex min-h-11 items-center justify-center rounded-lg bg-[var(--student-surface-soft)] px-4 text-sm font-black text-[var(--student-muted)]">{t('student.testing.locked')}</span> : <Link to={card.to} className="mt-5 inline-flex min-h-11 items-center justify-center rounded-lg bg-[#5146f0] px-4 text-sm font-black text-white no-underline hover:bg-[#4036d6]">{t('student.testing.start')}</Link>}</TestingCard>)}</div><div className="flex flex-wrap gap-3"><Link className="inline-flex items-center gap-2 text-sm font-black text-[#5146f0]" to="/student/tests/history"><History className="h-4 w-4" />{t('student.testing.history.title')}</Link><Link className="inline-flex items-center gap-2 text-sm font-black text-[#5146f0]" to="/student/red-list"><ShieldCheck className="h-4 w-4" />{t('student.testing.redList.title')}</Link></div></div>
}
