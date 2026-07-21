import { CheckCircle2, ListRestart, Sparkles, X, XCircle } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { RedListAnswerAction, type CheckedTestAnswerDto } from '@/features/student-testing/model/student-testing.types'

export function QuestionCheckFeedback({ feedback }: { feedback: CheckedTestAnswerDto }) {
  const { t } = useTranslation()
  return <section className={`mt-5 rounded-lg border p-4 ${feedback.isCorrect ? 'border-emerald-200 bg-emerald-50/80' : 'border-red-200 bg-red-50/80'}`} aria-live="polite">
    <div className="flex items-start gap-3">
      <span className={`grid h-9 w-9 shrink-0 place-items-center rounded-lg bg-white shadow-sm ${feedback.isCorrect ? 'text-emerald-700' : 'text-red-700'}`}>{feedback.isCorrect ? <CheckCircle2 className="h-5 w-5" /> : <XCircle className="h-5 w-5" />}</span>
      <div className="min-w-0 flex-1">
        <p className={`font-black ${feedback.isCorrect ? 'text-emerald-950' : 'text-red-950'}`}>{feedback.isCorrect ? t('student.testing.check.correct') : t('student.testing.check.wrong')}</p>
        {!feedback.isCorrect && feedback.correctAnswer ? <p className="mt-2 text-sm leading-6 text-red-900"><strong>{t('student.testing.correctAnswer')}:</strong> {feedback.correctAnswer}</p> : null}
        {feedback.explanation ? <p className="mt-2 text-sm leading-6 text-[var(--student-muted)]">{feedback.explanation}</p> : null}
      </div>
    </div>
    {feedback.redList ? <div className="mt-3 flex flex-wrap items-center gap-2 border-t border-current/10 pt-3 text-xs font-black text-[var(--student-muted)]"><ListRestart className="h-4 w-4" /><span>{t(`student.testing.check.redListActions.${feedback.redList.action}`)}</span>{feedback.redList.action !== RedListAnswerAction.mastered ? <span className="rounded-md bg-white/80 px-2 py-1 tabular-nums">{feedback.redList.correctStreak}/{feedback.redList.requiredCorrectStreak}</span> : null}</div> : null}
  </section>
}

export function RedListMasteryToast({ xp, onClose }: { xp: number; onClose: () => void }) {
  const { t } = useTranslation()
  return <div role="status" className="fixed right-4 top-[5.5rem] z-[70] w-[min(22rem,calc(100vw-2rem))] overflow-hidden rounded-lg border border-amber-200 bg-[var(--student-surface)] p-4 shadow-[0_18px_55px_rgb(20_31_70/0.2)]">
    <div className="flex items-center gap-3"><span className="grid h-11 w-11 shrink-0 place-items-center rounded-lg bg-amber-50 text-amber-600"><Sparkles className="h-5 w-5" /></span><div className="min-w-0 flex-1"><p className="font-black">{t('student.testing.check.masteredTitle')}</p><p className="mt-1 text-sm text-[var(--student-muted)]">{t('student.testing.check.masteredDescription')}</p></div><strong className="shrink-0 text-lg font-black text-amber-600">+{xp} XP</strong><button type="button" className="grid h-8 w-8 shrink-0 place-items-center rounded-md text-[var(--student-muted)] hover:bg-[var(--student-surface-soft)]" aria-label={t('student.closeNotice')} onClick={onClose}><X className="h-4 w-4" /></button></div>
  </div>
}
