import { useMutation, useQuery } from '@tanstack/react-query'
import { ArrowLeft, CalendarClock, ListChecks, Route, ShieldCheck } from 'lucide-react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useNavigate } from 'react-router-dom'
import { subjectsApi, subjectKeys } from '@/features/academic/api/subjects.api'
import { studentTestingApi, studentTestingKeys } from '@/features/student-testing/api/student-testing.api'
import { canStartRedList, testingErrorKey } from '@/features/student-testing/lib/student-testing'
import type { TestAttemptDto } from '@/features/student-testing/model/student-testing.types'
import { TestingButton, TestingCard, TestingError, TestingLoading, TestingToggle } from '@/features/student-testing/ui/TestingUi'
import { StudentPageHeader } from '@/routes/student/StudentUi'
import { SelectField } from '@/shared/ui/SelectField'

function BackLink() { const { t } = useTranslation(); return <Link to="/student/tests" className="inline-flex items-center gap-2 text-sm font-black text-[#5146f0]"><ArrowLeft className="h-4 w-4" />{t('student.testing.backToTests')}</Link> }

function useStart(onStart: (navigate: ReturnType<typeof useNavigate>) => Promise<TestAttemptDto>) {
  const navigate = useNavigate()
  return useMutation({ mutationFn: () => onStart(navigate), onSuccess: (attempt) => navigate(`/student/tests/attempts/${attempt.id}`) })
}

export function SubjectTestStartPage() {
  const { t } = useTranslation(); const [subjectId, setSubjectId] = useState(''); const [questionCount, setQuestionCount] = useState(''); const [includeRedList, setIncludeRedList] = useState(true)
  const config = useQuery({ queryKey: studentTestingKeys.config(), queryFn: studentTestingApi.getTestingConfig })
  const subjects = useQuery({ queryKey: subjectKeys.publicList({ page: 1, pageSize: 50, status: 1 }), queryFn: () => subjectsApi.publicList({ page: 1, pageSize: 50, status: 1 }) })
  const start = useStart(() => studentTestingApi.startSubjectTest({ subjectId, questionCount: Number(questionCount), includeRedList }))
  if (config.isLoading || subjects.isLoading) return <TestingLoading />
  if (config.isError) return <TestingError error={config.error} onRetry={() => void config.refetch()} />
  if (subjects.isError) return <TestingError error={subjects.error} onRetry={() => void subjects.refetch()} />
  if (!config.data || !subjects.data) return <TestingLoading />
  const counts = config.data.subjectQuestionCounts
  return <StartShell title={t('student.testing.subject.title')} description={t('student.testing.subject.startDescription')}><form onSubmit={(event) => { event.preventDefault(); if (subjectId && questionCount) start.mutate() }} className="grid gap-5"><label className="grid gap-2 text-sm font-black">{t('student.testing.subject.choose')}<SelectField searchable value={subjectId} onValueChange={setSubjectId} placeholder={t('student.testing.subject.placeholder')} searchPlaceholder={t('student.testing.search')} options={subjects.data.items.map((subject) => ({ value: subject.id, label: subject.name }))} className="[&_button]:rounded-lg [&_button]:border [&_button]:border-[var(--student-border)]" /></label><fieldset><legend className="mb-2 text-sm font-black">{t('student.testing.questionCount')}</legend><div className="grid grid-cols-3 gap-2">{counts.map((count) => <button key={count} type="button" onClick={() => setQuestionCount(String(count))} className={`min-h-12 rounded-lg border text-sm font-black ${questionCount === String(count) ? 'border-[#5146f0] bg-[#f0efff] text-[#5146f0]' : 'border-[var(--student-border)]'}`}>{count}</button>)}</div></fieldset><TestingToggle checked={includeRedList} onChange={setIncludeRedList} label={t('student.testing.subject.includeRedList')} description={t('student.testing.subject.timing')} />{start.isError ? <TestingError error={start.error} /> : null}<TestingButton type="submit" disabled={!subjectId || !questionCount || start.isPending}>{start.isPending ? t('student.testing.starting') : t('student.testing.start')}</TestingButton></form></StartShell>
}

export function MmtPracticeStartPage() {
  const { t } = useTranslation(); const [strict, setStrict] = useState(false)
  const start = useStart(() => studentTestingApi.startMmtPractice({ strictSimulation: strict }))
  const profileRequired = start.isError && testingErrorKey(start.error) === 'profileRequired'
  return <StartShell title={t('student.testing.mmt.title')} description={t('student.testing.mmt.startDescription')} icon={<Route />}><div className="grid gap-5"><TestingToggle checked={strict} onChange={setStrict} label={t('student.testing.mmt.strict')} description={t(strict ? 'student.testing.mmt.strictDescription' : 'student.testing.mmt.normalDescription')} />{start.isError ? <TestingError error={start.error} /> : null}{profileRequired ? <Link to="/student/mmt/setup" className="inline-flex min-h-11 items-center justify-center rounded-lg border border-[#5146f0] px-4 text-sm font-black text-[#5146f0] no-underline">{t('student.testing.setupMmt')}</Link> : null}<TestingButton type="button" onClick={() => start.mutate()} disabled={start.isPending}>{t('student.testing.start')}</TestingButton></div></StartShell>
}

export function MonthlyExamStartPage() {
  const { t } = useTranslation(); const config = useQuery({ queryKey: studentTestingKeys.config(), queryFn: studentTestingApi.getTestingConfig }); const start = useStart(() => studentTestingApi.startMonthlyExam())
  if (config.isLoading) return <TestingLoading />
  if (config.isError) return <TestingError error={config.error} onRetry={() => void config.refetch()} />
  if (!config.data) return <TestingLoading />
  const setupNeeded = start.isError && ['profileRequired', 'choicesRequired'].includes(testingErrorKey(start.error))
  return <StartShell title={t('student.testing.monthly.title')} description={t('student.testing.monthly.startDescription')} icon={<CalendarClock />}><div className="grid gap-5"><div className="rounded-lg bg-[var(--student-surface-soft)] p-4 text-sm leading-6 text-[var(--student-muted)]"><p>{t('student.testing.monthly.rules')}</p>{config.data.monthlyExamClosesAtUtc ? <p className="mt-2 font-black text-[var(--student-text)]">{t('student.testing.monthly.closes', { date: new Date(config.data.monthlyExamClosesAtUtc).toLocaleString() })}</p> : null}</div>{start.isError ? <TestingError error={start.error} /> : null}{setupNeeded ? <Link to="/student/mmt/setup" className="inline-flex min-h-11 items-center justify-center rounded-lg border border-[#5146f0] px-4 text-sm font-black text-[#5146f0] no-underline">{t('student.testing.setupMmt')}</Link> : null}<TestingButton type="button" onClick={() => start.mutate()} disabled={!config.data.monthlyExamAvailable || start.isPending}>{config.data.monthlyExamAvailable ? t('student.testing.start') : t('student.testing.unavailable')}</TestingButton></div></StartShell>
}

export function RedListPracticeStartPage() {
  const { t } = useTranslation(); const config = useQuery({ queryKey: studentTestingKeys.config(), queryFn: studentTestingApi.getTestingConfig }); const summary = useQuery({ queryKey: studentTestingKeys.redListSummary(), queryFn: studentTestingApi.getRedListSummary }); const start = useStart(() => studentTestingApi.startRedListPractice({}))
  if (config.isLoading || summary.isLoading) return <TestingLoading />
  if (config.isError) return <TestingError error={config.error} onRetry={() => void config.refetch()} />
  if (summary.isError) return <TestingError error={summary.error} onRetry={() => void summary.refetch()} />
  if (!config.data || !summary.data) return <TestingLoading />
  const allowed = canStartRedList(summary.data.activeCount, config.data.redListMinimumQuestions)
  return <StartShell title={t('student.testing.redPractice.title')} description={t('student.testing.redPractice.startDescription')} icon={<ListChecks />}><div className="grid gap-5"><div className={`rounded-lg p-4 text-sm font-bold ${allowed ? 'bg-emerald-50 text-emerald-800' : 'bg-amber-50 text-amber-800'}`}>{allowed ? t('student.testing.redPractice.ready', { count: summary.data.activeCount }) : t('student.testing.redPractice.locked', { minimum: config.data.redListMinimumQuestions, count: summary.data.activeCount })}</div>{start.isError ? <TestingError error={start.error} /> : null}<TestingButton type="button" onClick={() => start.mutate()} disabled={!allowed || start.isPending}><ShieldCheck className="h-4 w-4" />{t('student.testing.start')}</TestingButton></div></StartShell>
}

function StartShell({ title, description, children, icon }: { title: string; description: string; children: React.ReactNode; icon?: React.ReactNode }) {
  return <div className="grid gap-5"><BackLink /><StudentPageHeader title={title} description={description} /><TestingCard className="mx-auto w-full max-w-2xl">{icon ? <span className="mb-5 grid h-12 w-12 place-items-center rounded-lg bg-[#f0efff] text-[#5146f0] [&>svg]:h-6 [&>svg]:w-6">{icon}</span> : null}{children}</TestingCard></div>
}
