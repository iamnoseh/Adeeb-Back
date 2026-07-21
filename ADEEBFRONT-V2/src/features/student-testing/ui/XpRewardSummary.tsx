import { Award, CheckCircle2, Sparkles } from 'lucide-react'
import type { ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import type { TestResultDto } from '@/features/student-testing/model/student-testing.types'
import { TestingBadge, TestingCard } from '@/features/student-testing/ui/TestingUi'

export function XpRewardSummary({ data, language }: { data: TestResultDto; language: string }) {
  const { t } = useTranslation()
  const format = (value: number) => new Intl.NumberFormat(language, { maximumFractionDigits: 1 }).format(value)
  const difficulty = [
    { key: '1', value: data.easyCorrect },
    { key: '2', value: data.mediumCorrect },
    { key: '3', value: data.hardCorrect },
  ]

  return (
    <TestingCard data-testid="xp-reward-summary" data-awarded={data.xpAwarded} className={data.xpAwarded ? 'border-violet-200' : ''}>
      <div className="flex flex-col gap-5 lg:flex-row lg:items-center">
        <div className="flex min-w-0 flex-1 items-start gap-4">
          <span className={`grid h-12 w-12 shrink-0 place-items-center rounded-lg ${data.xpAwarded ? 'bg-violet-100 text-violet-700' : 'bg-[var(--student-surface-soft)] text-[var(--student-muted)]'}`}>
            <Award aria-hidden="true" className="h-6 w-6" />
          </span>
          <div className="min-w-0">
            <div className="flex flex-wrap items-center gap-2">
              <h2 className="text-lg font-black">{t('student.testing.result.xpTitle')}</h2>
              <TestingBadge tone={data.xpAwarded ? 'success' : 'neutral'}>
                {data.xpAwarded ? t('student.testing.result.xpAwarded') : t('student.testing.result.xpNotAwarded')}
              </TestingBadge>
            </div>
            <p className={`mt-2 text-4xl font-black ${data.xpAwarded ? 'text-violet-700' : 'text-[var(--student-muted)]'}`}>
              {data.xpAwarded ? '+' : ''}{format(data.totalXp)} <span className="text-xl">XP</span>
            </p>
            <p className="mt-1 text-sm text-[var(--student-muted)]">
              {data.xpAwarded ? t('student.testing.result.xpEarnedDescription') : t('student.testing.result.xpZeroDescription')}
            </p>
          </div>
        </div>
        <div className="grid min-w-0 gap-3 sm:grid-cols-2 lg:w-[430px]">
          <XpValue icon={<CheckCircle2 />} label={t('student.testing.result.answerXp')} value={`${format(data.answerXp)} XP`} />
          <XpValue icon={<Sparkles />} label={t('student.testing.result.completionBonusXp')} value={`${format(data.completionBonusXp)} XP`} />
        </div>
      </div>
      <div className="mt-5 border-t border-[var(--student-border)] pt-4">
        <p className="text-xs font-black text-[var(--student-muted)]">{t('student.testing.result.difficultyCorrect')}</p>
        <div className="mt-3 grid grid-cols-3 divide-x divide-[var(--student-border)]">
          {difficulty.map((item) => (
            <div key={item.key} className="px-3 first:pl-0 last:pr-0">
              <span className="block text-xs font-bold text-[var(--student-muted)]">{t(`student.testing.difficulty.${item.key}`)}</span>
              <strong className="mt-1 block text-xl">{item.value}</strong>
            </div>
          ))}
        </div>
      </div>
    </TestingCard>
  )
}

function XpValue({ icon, label, value }: { icon: ReactNode; label: string; value: string }) {
  return (
    <div className="flex min-w-0 items-center gap-3 rounded-lg bg-[var(--student-surface-soft)] px-4 py-3">
      <span aria-hidden="true" className="text-amber-600 [&>svg]:h-5 [&>svg]:w-5">{icon}</span>
      <span className="min-w-0 flex-1">
        <span className="block text-xs font-bold text-[var(--student-muted)]">{label}</span>
        <strong className="mt-0.5 block text-base">{value}</strong>
      </span>
    </div>
  )
}
