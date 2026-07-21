import { ListRestart } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import type { RedListQuestionProgressDto } from '@/features/student-testing/model/student-testing.types'

export function RedListQuestionProgress({ progress }: { progress: RedListQuestionProgressDto }) {
  const { t } = useTranslation()
  const required = Math.max(1, progress.requiredCorrectStreak)
  const current = Math.min(required, Math.max(0, progress.correctStreak))

  return <aside
    data-testid="red-list-question-progress"
    className="mt-4 flex flex-col gap-3 rounded-lg border border-amber-200 bg-amber-50/80 px-4 py-3 sm:flex-row sm:items-center"
    aria-label={t('student.testing.redListQuestion.label')}
    title={t('student.testing.redListQuestion.remaining', { count: progress.correctAnswersRemaining })}
  >
    <div className="flex min-w-0 items-center gap-3">
      <span className="grid h-9 w-9 shrink-0 place-items-center rounded-lg bg-white text-amber-700 shadow-sm"><ListRestart className="h-4 w-4" /></span>
      <div className="min-w-0">
        <p className="truncate text-sm font-black text-amber-950">{t('student.testing.redListQuestion.label')}</p>
        <p className="mt-0.5 text-xs font-bold text-amber-800">{t('student.testing.redListQuestion.progress')}</p>
      </div>
    </div>
    <div className="flex items-center gap-3 sm:ml-auto">
      <div className="flex flex-1 gap-1.5 sm:w-28 sm:flex-none" aria-hidden="true">
        {Array.from({ length: required }, (_, index) => <span key={index} className={`h-2 flex-1 rounded-full ${index < current ? 'bg-amber-500' : 'bg-amber-200'}`} />)}
      </div>
      <strong className="min-w-10 text-right text-sm font-black tabular-nums text-amber-950">{current}/{required}</strong>
    </div>
  </aside>
}
