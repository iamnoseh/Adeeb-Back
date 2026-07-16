import { useMutation, useQuery } from '@tanstack/react-query'
import { AlertTriangle, ChevronLeft, ChevronRight, Clock3, Send } from 'lucide-react'
import { useCallback, useEffect, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate, useParams } from 'react-router-dom'
import { studentTestingApi, studentTestingKeys } from '@/features/student-testing/api/student-testing.api'
import { buildSubmitPayload, clearDraft, formatTimer, isAnswered, readDraft, secondsUntil, testingErrorKey, writeDraft } from '@/features/student-testing/lib/student-testing'
import { TestAttemptStatus, TestQuestionType, type AttemptAnswers, type DraftAnswer, type TestQuestionDto } from '@/features/student-testing/model/student-testing.types'
import { TestingButton, TestingCard, TestingError, TestingLoading } from '@/features/student-testing/ui/TestingUi'
import { AttemptExitGuard } from '@/features/student-testing/ui/AttemptExitGuard'
import { cn } from '@/shared/lib/cn'
import { SelectField } from '@/shared/ui/SelectField'

export function TestAttemptPage() {
  const { attemptId = '' } = useParams(); const { t } = useTranslation(); const navigate = useNavigate()
  const attempt = useQuery({ queryKey: studentTestingKeys.attempt(attemptId), queryFn: () => studentTestingApi.getAttempt(attemptId), enabled: Boolean(attemptId), refetchOnWindowFocus: false })
  const [currentIndex, setCurrentIndex] = useState(0); const [answers, setAnswers] = useState<AttemptAnswers>(() => readDraft(attemptId)); const answersRef = useRef(answers); const [confirmOpen, setConfirmOpen] = useState(false); const autoSubmitted = useRef(false); const allowExit = useRef(false)
  useEffect(() => { answersRef.current = answers; if (attemptId) writeDraft(attemptId, answers) }, [answers, attemptId])
  const submit = useMutation({
    mutationFn: () => studentTestingApi.submitAttempt(attemptId, buildSubmitPayload(attempt.data?.questions ?? [], answersRef.current)),
    onSuccess: () => { allowExit.current = true; clearDraft(attemptId); navigate(`/student/tests/attempts/${attemptId}/result`, { replace: true }) },
    onError: (error) => {
      if (testingErrorKey(error) === 'alreadySubmitted') {
        allowExit.current = true
        clearDraft(attemptId)
        navigate(`/student/tests/attempts/${attemptId}/result`, { replace: true })
      }
    },
  })
  const submitNow = useCallback(() => { if (!submit.isPending) submit.mutate() }, [submit])
  const remaining = useAttemptTimer(attempt.data?.expiresAtUtc, () => { if (!autoSubmitted.current) { autoSubmitted.current = true; submitNow() } })
  useEffect(() => {
    if (attempt.data && [TestAttemptStatus.submitted, TestAttemptStatus.autoSubmitted].includes(attempt.data.status as 2 | 3)) { allowExit.current = true; navigate(`/student/tests/attempts/${attemptId}/result`, { replace: true }) }
  }, [attempt.data, attemptId, navigate])
  if (attempt.isLoading) return <TestingLoading />
  if (attempt.isError) return <TestingError error={attempt.error} onRetry={() => void attempt.refetch()} />
  if (!attempt.data || attempt.data.questions.length === 0) return <TestingError error={new Error('No questions')} />
  const question = attempt.data.questions[currentIndex] ?? attempt.data.questions[0]!
  const answeredCount = attempt.data.questions.filter((item) => isAnswered(answers[item.id])).length
  function updateQuestion(value: DraftAnswer) { setAnswers((current) => ({ ...current, [question.id]: value })) }
  return <div className="grid gap-4 xl:grid-cols-[minmax(0,1fr)_280px]">
    <main className="min-w-0 grid gap-4">
      <div className="sticky top-20 z-20 flex items-center justify-between gap-3 rounded-lg border border-[var(--student-border)] bg-[var(--student-surface)]/95 px-4 py-3 shadow-sm backdrop-blur">
        <div><p className="text-xs font-bold text-[var(--student-muted)]">{t('student.testing.questionProgress', { current: currentIndex + 1, total: attempt.data.questionCount })}</p><div className="mt-2 h-1.5 w-36 max-w-[35vw] overflow-hidden rounded-full bg-[var(--student-surface-soft)]"><span className="block h-full bg-[#5146f0]" style={{ width: `${((currentIndex + 1) / attempt.data.questionCount) * 100}%` }} /></div></div>
        <div className={cn('inline-flex items-center gap-2 rounded-lg px-3 py-2 font-black tabular-nums', remaining <= 60 ? 'bg-red-50 text-red-700' : 'bg-[#f0efff] text-[#5146f0]')} aria-live="polite"><Clock3 className="h-4 w-4" />{formatTimer(remaining)}</div>
      </div>
      <TestingCard className="min-h-[24rem]">
        <div className="flex items-start justify-between gap-3"><span className="text-xs font-black uppercase text-[#5146f0]">{t('student.testing.questionNumber', { number: question.order })}</span><span className="text-xs font-bold text-[var(--student-muted)]">{t(`student.testing.difficulty.${question.difficulty}`, { defaultValue: t('student.testing.difficulty.unknown') })}</span></div>
        <h1 className="mt-5 whitespace-pre-wrap text-lg font-black leading-8 sm:text-xl">{question.content}</h1>
        {question.imageUrl ? <img src={question.imageUrl} alt="" className="mt-5 max-h-80 w-auto max-w-full rounded-lg object-contain" /> : null}
        <div className="mt-7"><QuestionAnswer question={question} answer={answers[question.id]} onChange={updateQuestion} /></div>
      </TestingCard>
      {submit.isError ? <TestingError error={submit.error} /> : null}
      <div className="flex flex-col-reverse gap-3 sm:flex-row sm:justify-between"><TestingButton variant="secondary" disabled={currentIndex === 0} onClick={() => setCurrentIndex((value) => Math.max(0, value - 1))}><ChevronLeft className="h-4 w-4" />{t('student.testing.previous')}</TestingButton>{currentIndex < attempt.data.questions.length - 1 ? <TestingButton onClick={() => setCurrentIndex((value) => Math.min(attempt.data.questions.length - 1, value + 1))}>{t('student.testing.next')}<ChevronRight className="h-4 w-4" /></TestingButton> : <TestingButton onClick={() => setConfirmOpen(true)}><Send className="h-4 w-4" />{t('student.testing.finish')}</TestingButton>}</div>
    </main>
    <aside className="order-first min-w-0 xl:order-none"><TestingCard className="xl:sticky xl:top-20"><div className="flex items-center justify-between gap-3"><h2 className="font-black">{t('student.testing.navigation')}</h2><span className="text-xs font-bold text-[var(--student-muted)]">{answeredCount}/{attempt.data.questionCount}</span></div><div className="mt-4 flex gap-2 overflow-x-auto pb-2 xl:grid xl:grid-cols-5 xl:overflow-visible">{attempt.data.questions.map((item, index) => <button key={item.id} type="button" onClick={() => setCurrentIndex(index)} aria-label={t('student.testing.goToQuestion', { number: index + 1 })} className={cn('h-10 w-10 shrink-0 rounded-lg border text-sm font-black', index === currentIndex ? 'border-[#5146f0] bg-[#5146f0] text-white' : isAnswered(answers[item.id]) ? 'border-emerald-200 bg-emerald-50 text-emerald-700' : 'border-[var(--student-border)] text-[var(--student-muted)]')}>{index + 1}</button>)}</div><div className="mt-4 grid grid-cols-2 gap-2 text-xs font-bold text-[var(--student-muted)]"><span>{t('student.testing.answered')}: {answeredCount}</span><span>{t('student.testing.unanswered')}: {attempt.data.questionCount - answeredCount}</span></div><TestingButton className="mt-5 w-full" onClick={() => setConfirmOpen(true)}>{t('student.testing.finish')}</TestingButton></TestingCard></aside>
    {confirmOpen ? <ConfirmSubmit unanswered={attempt.data.questionCount - answeredCount} pending={submit.isPending} onCancel={() => setConfirmOpen(false)} onConfirm={submitNow} /> : null}
    <AttemptExitGuard active={attempt.data.status === TestAttemptStatus.inProgress} allowExit={allowExit} finishing={submit.isPending} unanswered={attempt.data.questionCount - answeredCount} onFinish={submitNow} />
  </div>
}

function QuestionAnswer({ question, answer, onChange }: { question: TestQuestionDto; answer: DraftAnswer | undefined; onChange: (answer: DraftAnswer) => void }) {
  const { t } = useTranslation()
  if (question.type === TestQuestionType.singleChoice) return <fieldset className="grid gap-3"><legend className="sr-only">{t('student.testing.chooseAnswer')}</legend>{question.options.map((option, index) => <label key={option.id} className={cn('flex min-h-14 cursor-pointer items-center gap-3 rounded-lg border p-3 transition', answer?.selectedOptionId === option.id ? 'border-[#5146f0] bg-[#f0efff]' : 'border-[var(--student-border)] hover:border-[#aaa4ff]')}><input type="radio" name={`question-${question.id}`} checked={answer?.selectedOptionId === option.id} onChange={() => onChange({ selectedOptionId: option.id })} className="h-5 w-5 accent-[#5146f0]" /><span className="grid h-8 w-8 shrink-0 place-items-center rounded-md bg-[var(--student-surface-soft)] text-xs font-black">{String.fromCharCode(65 + index)}</span><span className="text-sm font-bold leading-6">{option.text}</span></label>)}</fieldset>
  if (question.type === TestQuestionType.closedAnswer) return <label className="grid gap-2 text-sm font-black">{t('student.testing.yourAnswer')}<input value={answer?.textResponse ?? ''} onChange={(event) => onChange({ textResponse: event.target.value })} autoComplete="off" className="min-h-12 rounded-lg border border-[var(--student-border)] bg-[var(--student-surface-soft)] px-4 text-base font-medium outline-none focus:border-[#5146f0]" placeholder={t('student.testing.closedPlaceholder')} /></label>
  if (question.type === TestQuestionType.matching) {
    const pairs = answer?.matchingPairs ?? {}
    return <div className="grid gap-3"><p className="text-sm text-[var(--student-muted)]">{t('student.testing.matchingHint')}</p>{question.options.map((left) => { const selectedElsewhere = new Set(Object.entries(pairs).filter(([id]) => id !== left.id).map(([, value]) => value)); return <div key={left.id} className="grid items-center gap-2 rounded-lg border border-[var(--student-border)] p-3 md:grid-cols-[minmax(0,1fr)_minmax(0,1fr)]"><span className="text-sm font-black leading-6">{left.text}</span><SelectField value={pairs[left.id] ?? ''} onValueChange={(value) => onChange({ matchingPairs: { ...pairs, [left.id]: value } })} placeholder={t('student.testing.chooseMatch')} options={question.matchingOptions.map((text) => ({ value: text, label: text, disabled: selectedElsewhere.has(text) }))} className="[&_button]:min-h-11 [&_button]:rounded-lg [&_button]:border [&_button]:border-[var(--student-border)] [&_button]:px-3 [&_button]:py-2 [&_button]:text-sm" /></div> })}</div>
  }
  return <p className="text-sm text-red-700">{t('student.testing.unsupportedQuestion')}</p>
}

function ConfirmSubmit({ unanswered, pending, onCancel, onConfirm }: { unanswered: number; pending: boolean; onCancel: () => void; onConfirm: () => void }) {
  const { t } = useTranslation()
  return <div className="fixed inset-0 z-50 grid place-items-center bg-black/45 p-4" role="presentation" onMouseDown={(event) => { if (event.currentTarget === event.target) onCancel() }}><div role="dialog" aria-modal="true" aria-labelledby="submit-title" className="w-full max-w-md rounded-lg bg-[var(--student-surface)] p-5 shadow-2xl"><span className="grid h-11 w-11 place-items-center rounded-lg bg-amber-50 text-amber-700"><AlertTriangle className="h-5 w-5" /></span><h2 id="submit-title" className="mt-4 text-xl font-black">{t('student.testing.confirmTitle')}</h2><p className="mt-2 text-sm leading-6 text-[var(--student-muted)]">{unanswered > 0 ? t('student.testing.confirmUnanswered', { count: unanswered }) : t('student.testing.confirmDescription')}</p><div className="mt-6 flex justify-end gap-3"><TestingButton variant="secondary" onClick={onCancel} disabled={pending}>{t('cancel')}</TestingButton><TestingButton onClick={onConfirm} disabled={pending}>{pending ? t('student.testing.submitting') : t('student.testing.finish')}</TestingButton></div></div></div>
}

function useAttemptTimer(expiresAtUtc: string | undefined, onExpire: () => void) {
  const [remaining, setRemaining] = useState(() => expiresAtUtc ? secondsUntil(expiresAtUtc) : 0); const onExpireRef = useRef(onExpire); onExpireRef.current = onExpire
  useEffect(() => {
    if (!expiresAtUtc) return
    function tick() { const next = secondsUntil(expiresAtUtc!); setRemaining(next); if (next === 0) onExpireRef.current() }
    tick(); const interval = window.setInterval(tick, 1000); return () => window.clearInterval(interval)
  }, [expiresAtUtc])
  return remaining
}
