import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { ArrowRight, BookOpenCheck, CheckCircle2, Languages, RotateCcw, Sparkles, Target, Trophy } from 'lucide-react'
import type { ReactNode } from 'react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'
import { studentVocabularyApi, vocabularyKeys } from '@/features/vocabulary/api/vocabulary.api'
import { VocabularySessionMode, type LearningLanguageDto } from '@/features/vocabulary/model/vocabulary.types'
import { vocabularyLevelLabel, vocabularyLevels, vocabularyModeLabel } from '@/features/vocabulary/lib/vocabulary'
import { TestingButton, TestingCard, TestingEmpty, TestingError, TestingLoading } from '@/features/student-testing/ui/TestingUi'
import { SelectField } from '@/shared/ui/SelectField'
import { StudentPageHeader } from '@/routes/student/StudentUi'

export function StudentVocabularyPage() {
  const { t } = useTranslation()
  const navigate = useNavigate()
  const languages = useQuery({ queryKey: vocabularyKeys.student.languages(), queryFn: studentVocabularyApi.languages })
  const dashboard = useQuery({ queryKey: vocabularyKeys.student.dashboard(), queryFn: studentVocabularyApi.dashboard, retry: false })
  const mistakes = useQuery({ queryKey: vocabularyKeys.student.mistakes({ page: 1, pageSize: 5 }), queryFn: () => studentVocabularyApi.mistakes({ page: 1, pageSize: 5 }), retry: false })
  const history = useQuery({ queryKey: vocabularyKeys.student.history({ page: 1, pageSize: 5 }), queryFn: () => studentVocabularyApi.history({ page: 1, pageSize: 5 }), retry: false })
  const startSession = useMutation({
    mutationFn: studentVocabularyApi.startSession,
    onSuccess: (session) => navigate(`/student/vocabulary/sessions/${session.id}`),
  })

  if (languages.isLoading || (dashboard.isLoading && !dashboard.isError)) return <TestingLoading />
  if (languages.isError) return <TestingError error={languages.error} onRetry={() => void languages.refetch()} />

  const hasCourse = Boolean(dashboard.data)

  return (
    <div className="grid gap-5">
      <StudentPageHeader title={t('vocabulary.title')} description={t('vocabulary.description')} />
      {!hasCourse ? (
        <CourseSetup languages={languages.data ?? []} />
      ) : (
        <>
          <section className="grid gap-4 lg:grid-cols-[minmax(0,1.15fr)_0.85fr]">
            <TestingCard className="overflow-hidden p-0">
              <div className="grid gap-4 p-5 sm:grid-cols-[minmax(0,1fr)_auto] sm:items-start">
                <div className="min-w-0">
                  <span className="inline-flex items-center gap-2 rounded-lg bg-[#f0efff] px-3 py-1.5 text-xs font-black text-[#5146f0]"><Languages className="h-4 w-4" />{dashboard.data!.course.languageName} · {vocabularyLevelLabel(dashboard.data!.course.level)}</span>
                  <h2 className="mt-4 text-2xl font-black tracking-normal text-[var(--student-text)]">{dashboard.data!.today.word.targetText}</h2>
                  <p className="mt-2 text-base font-bold text-[#5146f0]">{dashboard.data!.today.word.translation}</p>
                  {dashboard.data!.today.word.explanation ? <p className="mt-3 max-w-2xl text-sm leading-6 text-[var(--student-muted)]">{dashboard.data!.today.word.explanation}</p> : null}
                </div>
                <span className="grid h-14 w-14 place-items-center rounded-lg bg-[#fff3e7] text-[#e98a16]"><Sparkles className="h-7 w-7" /></span>
              </div>
              <div className="border-t border-[var(--student-border)] bg-[var(--student-surface-soft)] px-5 py-4">
                <p className="text-sm font-bold text-[var(--student-muted)]">{dashboard.data!.today.word.example}</p>
              </div>
            </TestingCard>

            <TestingCard>
              <div className="flex items-center justify-between gap-3">
                <h2 className="text-lg font-black">{t('vocabulary.todayPractice')}</h2>
                <BookOpenCheck className="h-5 w-5 text-[#5146f0]" />
              </div>
              <div className="mt-5 grid gap-3">
                <PracticeButton icon={<Sparkles />} title={t('vocabulary.startDaily')} subtitle={vocabularyModeLabel(VocabularySessionMode.DailyPractice, t)} onClick={() => startSession.mutate({ mode: VocabularySessionMode.DailyPractice })} pending={startSession.isPending} />
                <PracticeButton icon={<RotateCcw />} title={t('vocabulary.startMistakes')} subtitle={t('vocabulary.mistakeReview')} onClick={() => startSession.mutate({ mode: VocabularySessionMode.MistakeReview })} pending={startSession.isPending} />
                <PracticeButton icon={<Target />} title={t('vocabulary.startTest')} subtitle="20" onClick={() => startSession.mutate({ mode: VocabularySessionMode.Test, questionCount: 20 })} pending={startSession.isPending} />
              </div>
            </TestingCard>
          </section>

          <section className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
            <Metric icon={<Trophy />} label={t('vocabulary.masteredWords')} value={dashboard.data!.masteredWords} tone="green" />
            <Metric icon={<RotateCcw />} label={t('vocabulary.dueReviews')} value={dashboard.data!.dueReviews} tone="orange" />
            <Metric icon={<CheckCircle2 />} label={t('vocabulary.completedSessions')} value={dashboard.data!.completedSessions} tone="purple" />
            <Metric icon={<BookOpenCheck />} label={t('vocabulary.totalPracticedWords')} value={dashboard.data!.totalPracticedWords} tone="blue" />
          </section>

          <section className="grid gap-4 lg:grid-cols-2">
            <TestingCard>
              <h2 className="text-lg font-black">{t('vocabulary.mistakes')}</h2>
              <div className="mt-4 grid gap-2">
                {mistakes.data?.items.length ? mistakes.data.items.map((item) => (
                  <div key={item.wordId} className="flex min-h-14 items-center gap-3 rounded-lg bg-[var(--student-surface-soft)] p-3">
                    <span className="grid h-9 w-9 place-items-center rounded-lg bg-[#fff3e7] text-sm font-black text-[#e98a16]">{item.wrongCount}</span>
                    <span className="min-w-0 flex-1"><strong className="block truncate text-sm">{item.targetText}</strong><small className="block truncate text-[var(--student-muted)]">{item.translation}</small></span>
                  </div>
                )) : <TestingEmpty title={t('vocabulary.noMistakes')} />}
              </div>
            </TestingCard>

            <TestingCard>
              <h2 className="text-lg font-black">{t('vocabulary.history')}</h2>
              <div className="mt-4 grid gap-2">
                {history.data?.items.length ? history.data.items.map((item) => (
                  <div key={item.sessionId} className="flex min-h-14 items-center gap-3 rounded-lg bg-[var(--student-surface-soft)] p-3">
                    <span className="grid h-9 w-9 place-items-center rounded-lg bg-[#f0efff] text-xs font-black text-[#5146f0]">{Math.round(item.percentage)}%</span>
                    <span className="min-w-0 flex-1"><strong className="block truncate text-sm">{vocabularyModeLabel(item.mode, t)}</strong><small className="block truncate text-[var(--student-muted)]">{t('vocabulary.score', { correct: item.correctCount, total: item.questionCount })}</small></span>
                  </div>
                )) : <TestingEmpty title={t('vocabulary.noHistory')} />}
              </div>
            </TestingCard>
          </section>
        </>
      )}
    </div>
  )
}

function CourseSetup({ languages }: { languages: LearningLanguageDto[] }) {
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const [languageId, setLanguageId] = useState(languages[0]?.id ?? '')
  const [level, setLevel] = useState('0')
  const setCourse = useMutation({
    mutationFn: studentVocabularyApi.setCourse,
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: vocabularyKeys.student.all })
    },
  })
  const languageOptions = languages.map((language) => ({ value: language.id, label: language.name }))
  const levelOptions = vocabularyLevels.map((item) => ({ value: String(item), label: vocabularyLevelLabel(item) }))

  return (
    <TestingCard className="max-w-3xl">
      <div className="flex items-start gap-4">
        <span className="grid h-12 w-12 place-items-center rounded-lg bg-[#f0efff] text-[#5146f0]"><Languages className="h-6 w-6" /></span>
        <div>
          <h2 className="text-xl font-black">{t('vocabulary.chooseCourse')}</h2>
          <p className="mt-1 text-sm leading-6 text-[var(--student-muted)]">{t('vocabulary.chooseCourseDescription')}</p>
        </div>
      </div>
      <div className="mt-6 grid gap-4 sm:grid-cols-2">
        <label className="grid gap-2 text-sm font-black">{t('vocabulary.language')}<SelectField searchable value={languageId} options={languageOptions} onValueChange={setLanguageId} /></label>
        <label className="grid gap-2 text-sm font-black">{t('vocabulary.level')}<SelectField value={level} options={levelOptions} onValueChange={setLevel} /></label>
      </div>
      <TestingButton className="mt-6 w-full sm:w-auto" disabled={!languageId || setCourse.isPending} onClick={() => setCourse.mutate({ languageId, level: Number(level) })}>{t('vocabulary.saveCourse')}<ArrowRight className="h-4 w-4" /></TestingButton>
      {setCourse.isError ? <p className="mt-3 text-sm font-bold text-red-600">{t('vocabulary.loadFailed')}</p> : null}
    </TestingCard>
  )
}

function PracticeButton({ icon, title, subtitle, onClick, pending }: { icon: ReactNode; title: string; subtitle: string; onClick: () => void; pending: boolean }) {
  return <button type="button" disabled={pending} onClick={onClick} className="flex min-h-16 items-center gap-3 rounded-lg border border-[var(--student-border)] p-3 text-left transition hover:border-[#cbc7ff] hover:bg-[var(--student-surface-soft)] disabled:opacity-60"><span className="grid h-10 w-10 place-items-center rounded-lg bg-[#f0efff] text-[#5146f0] [&>svg]:h-5 [&>svg]:w-5">{icon}</span><span className="min-w-0 flex-1"><strong className="block truncate text-sm">{title}</strong><small className="block truncate text-[var(--student-muted)]">{subtitle}</small></span><ArrowRight className="h-4 w-4 text-[var(--student-muted)]" /></button>
}

function Metric({ icon, label, value, tone }: { icon: ReactNode; label: string; value: number; tone: 'green' | 'orange' | 'purple' | 'blue' }) {
  const tones = {
    green: 'bg-[#eaf8ee] text-[#2fa451]',
    orange: 'bg-[#fff3e7] text-[#e98a16]',
    purple: 'bg-[#f0efff] text-[#5146f0]',
    blue: 'bg-[#edf5ff] text-[#2b78db]',
  }
  return <article className="flex min-h-24 items-center gap-3 rounded-lg border border-[var(--student-border)] bg-[var(--student-surface)] p-4 shadow-[0_8px_24px_rgb(20_31_70/0.035)]"><span className={`grid h-11 w-11 place-items-center rounded-lg [&>svg]:h-5 [&>svg]:w-5 ${tones[tone]}`}>{icon}</span><span><small className="block font-bold text-[var(--student-muted)]">{label}</small><strong className="mt-1 block text-2xl font-black">{value}</strong></span></article>
}
