import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { ArrowLeft, CheckCircle2, RotateCcw, XCircle } from 'lucide-react'
import { useMemo, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Link, useParams } from 'react-router-dom'
import { studentVocabularyApi, vocabularyKeys } from '@/features/vocabulary/api/vocabulary.api'
import { VocabularyQuestionType, VocabularySessionStatus, type StudentVocabularyQuestionDto, type VocabularyAnswerFeedbackDto, type VocabularySessionDto, type VocabularySessionResultDto } from '@/features/vocabulary/model/vocabulary.types'
import { canShowImmediateFeedback, sessionProgress, shuffledOptions, tokensFromPrompt, vocabularyModeLabel, vocabularyQuestionTypeLabel } from '@/features/vocabulary/lib/vocabulary'
import { TestingBadge, TestingButton, TestingCard, TestingEmpty, TestingError, TestingLoading } from '@/features/student-testing/ui/TestingUi'
import { cn } from '@/shared/lib/cn'

export function StudentVocabularySessionPage() {
  const { sessionId = '' } = useParams()
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const [localSession, setLocalSession] = useState<VocabularySessionDto | null>(null)
  const [feedback, setFeedback] = useState<VocabularyAnswerFeedbackDto | null>(null)
  const [result, setResult] = useState<VocabularySessionResultDto | null>(null)
  const sessionQuery = useQuery({ queryKey: vocabularyKeys.student.session(sessionId), queryFn: () => studentVocabularyApi.session(sessionId), enabled: Boolean(sessionId) })
  const session = localSession ?? sessionQuery.data
  const currentQuestion = useMemo(() => session?.questions.find((question) => !question.isAnswered) ?? session?.questions[0], [session])
  const answer = useMutation({
    mutationFn: (input: Parameters<typeof studentVocabularyApi.answer>[1]) => studentVocabularyApi.answer(sessionId, input),
    onSuccess: (response) => {
      setLocalSession(response.session)
      setFeedback(response.feedback ?? null)
    },
  })
  const complete = useMutation({
    mutationFn: () => studentVocabularyApi.complete(sessionId),
    onSuccess: async (response) => {
      setResult(response)
      await queryClient.invalidateQueries({ queryKey: vocabularyKeys.student.all })
    },
  })

  if (sessionQuery.isLoading) return <TestingLoading />
  if (sessionQuery.isError || !session) return <TestingError error={sessionQuery.error} onRetry={() => void sessionQuery.refetch()} />
  if (result) return <ResultCard result={result} />
  if (!session.questions.length) return <TestingEmpty title={t('vocabulary.sessionEmpty')} />

  const progress = sessionProgress(session.answeredCount, session.questionCount)
  const isComplete = session.status === VocabularySessionStatus.Completed || session.answeredCount >= session.questionCount

  return (
    <div className="grid gap-5">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <Link to="/student/vocabulary" className="inline-flex min-h-10 items-center gap-2 rounded-lg border border-[var(--student-border)] px-3 text-sm font-black text-[var(--student-text)] no-underline hover:bg-[var(--student-surface-soft)]"><ArrowLeft className="h-4 w-4" />{t('vocabulary.backToVocabulary')}</Link>
        <TestingBadge>{vocabularyModeLabel(session.mode, t)}</TestingBadge>
      </div>
      <TestingCard>
        <div className="flex items-center justify-between gap-4">
          <div className="min-w-0">
            <p className="text-sm font-black text-[var(--student-muted)]">{t('vocabulary.question')} {Math.min(session.answeredCount + 1, session.questionCount)} / {session.questionCount}</p>
            <h1 className="mt-1 text-xl font-black">{currentQuestion ? vocabularyQuestionTypeLabel(currentQuestion.type, t) : t('vocabulary.complete')}</h1>
          </div>
          <strong className="text-2xl font-black text-[#5146f0]">{progress}%</strong>
        </div>
        <div className="mt-4 h-2 overflow-hidden rounded-full bg-[var(--student-surface-soft)]"><div className="h-full rounded-full bg-[#5146f0] transition-all" style={{ width: `${progress}%` }} /></div>
      </TestingCard>

      {currentQuestion && !isComplete ? (
        <QuestionCard
          key={currentQuestion.id}
          question={currentQuestion}
          session={session}
          feedback={feedback?.questionId === currentQuestion.id ? feedback : null}
          pending={answer.isPending}
          onSubmit={(payload) => answer.mutate({ questionId: currentQuestion.id, ...payload })}
        />
      ) : (
        <TestingCard>
          <h2 className="text-xl font-black">{t('vocabulary.complete')}</h2>
          <p className="mt-2 text-sm text-[var(--student-muted)]">{t('vocabulary.score', { correct: session.correctCount, total: session.questionCount })}</p>
          <TestingButton className="mt-5" disabled={complete.isPending} onClick={() => complete.mutate()}>{complete.isPending ? t('vocabulary.completing') : t('vocabulary.complete')}</TestingButton>
        </TestingCard>
      )}
    </div>
  )
}

function QuestionCard({ question, session, feedback, pending, onSubmit }: { question: StudentVocabularyQuestionDto; session: VocabularySessionDto; feedback: VocabularyAnswerFeedbackDto | null; pending: boolean; onSubmit: (payload: { selectedOptionId?: string; selectedTokenIndex?: number; orderedOptionIds?: string[] }) => void }) {
  const { t } = useTranslation()
  const [selectedOptionId, setSelectedOptionId] = useState('')
  const [selectedTokenIndex, setSelectedTokenIndex] = useState<number | null>(null)
  const [orderedOptionIds, setOrderedOptionIds] = useState<string[]>([])
  const tokens = tokensFromPrompt(question.prompt)
  const immediate = canShowImmediateFeedback(session.mode)
  const locked = pending || question.isAnswered || Boolean(feedback)

  function submit() {
    if (question.type === VocabularyQuestionType.WordOrder) onSubmit({ orderedOptionIds })
    else if (question.type === VocabularyQuestionType.OddWordReplacement && selectedTokenIndex !== null) onSubmit({ selectedTokenIndex, selectedOptionId })
    else onSubmit({ selectedOptionId })
  }

  const canSubmit = question.type === VocabularyQuestionType.WordOrder
    ? orderedOptionIds.length === question.options.length
    : question.type === VocabularyQuestionType.OddWordReplacement
      ? selectedTokenIndex !== null && Boolean(selectedOptionId)
      : Boolean(selectedOptionId)

  return (
    <TestingCard>
      <div className="rounded-lg bg-[var(--student-surface-soft)] p-5">
        <p className="text-lg font-black leading-8">{question.prompt}</p>
      </div>
      <div className="mt-5">
        {question.type === VocabularyQuestionType.OddWordReplacement ? (
          <div className="grid gap-5">
            <div>
              <p className="mb-3 text-sm font-black text-[var(--student-muted)]">{t('vocabulary.selectWrongToken')}</p>
              <div className="flex flex-wrap gap-2">{tokens.map((token, index) => <button key={`${token}-${index}`} type="button" disabled={locked} onClick={() => setSelectedTokenIndex(index)} className={cn('rounded-lg border px-3 py-2 text-sm font-black', selectedTokenIndex === index ? 'border-[#5146f0] bg-[#f0efff] text-[#5146f0]' : 'border-[var(--student-border)] bg-[var(--student-surface)]')}>{token}</button>)}</div>
            </div>
            <OptionGrid options={question.options} value={selectedOptionId} locked={locked} onChange={setSelectedOptionId} />
          </div>
        ) : question.type === VocabularyQuestionType.WordOrder ? (
          <div className="grid gap-4">
            <p className="text-sm font-black text-[var(--student-muted)]">{t('vocabulary.wordOrderHint')}</p>
            <div className="min-h-14 rounded-lg border border-dashed border-[var(--student-border)] bg-[var(--student-surface-soft)] p-3 text-sm font-black">{orderedOptionIds.length ? orderedOptionIds.map((id) => question.options.find((option) => option.id === id)?.value).join(' ') : t('vocabulary.selectedOrder')}</div>
            <div className="flex flex-wrap gap-2">{shuffledOptions(question.options).map((option) => <button key={option.id} type="button" disabled={locked || orderedOptionIds.includes(option.id)} onClick={() => setOrderedOptionIds((current) => [...current, option.id])} className="rounded-lg border border-[var(--student-border)] bg-[var(--student-surface)] px-3 py-2 text-sm font-black disabled:opacity-35">{option.value}</button>)}</div>
            <button type="button" className="inline-flex w-fit items-center gap-2 text-sm font-black text-[#5146f0]" onClick={() => setOrderedOptionIds([])} disabled={locked}><RotateCcw className="h-4 w-4" />{t('vocabulary.resetOrder')}</button>
          </div>
        ) : (
          <OptionGrid options={question.options} value={selectedOptionId} locked={locked} onChange={setSelectedOptionId} />
        )}
      </div>
      {immediate && feedback ? <Feedback feedback={feedback} /> : null}
      <TestingButton className="mt-6 w-full sm:w-auto" disabled={!canSubmit || locked} onClick={submit}>{t('vocabulary.answer')}</TestingButton>
    </TestingCard>
  )
}

function OptionGrid({ options, value, locked, onChange }: { options: StudentVocabularyQuestionDto['options']; value: string; locked: boolean; onChange: (value: string) => void }) {
  return <div className="grid gap-3 sm:grid-cols-2">{shuffledOptions(options).map((option) => <button key={option.id} type="button" disabled={locked} onClick={() => onChange(option.id)} className={cn('min-h-14 rounded-lg border px-4 py-3 text-left text-sm font-black transition', value === option.id ? 'border-[#5146f0] bg-[#f0efff] text-[#5146f0]' : 'border-[var(--student-border)] bg-[var(--student-surface)] hover:bg-[var(--student-surface-soft)]')}>{option.value}</button>)}</div>
}

function Feedback({ feedback }: { feedback: VocabularyAnswerFeedbackDto }) {
  const { t } = useTranslation()
  return <div className={cn('mt-5 flex items-center gap-2 rounded-lg p-3 text-sm font-black', feedback.isCorrect ? 'bg-emerald-50 text-emerald-700' : 'bg-red-50 text-red-700')}>{feedback.isCorrect ? <CheckCircle2 className="h-5 w-5" /> : <XCircle className="h-5 w-5" />}{feedback.isCorrect ? t('vocabulary.correct') : t('vocabulary.wrong')}</div>
}

function ResultCard({ result }: { result: VocabularySessionResultDto }) {
  const { t } = useTranslation()
  return <TestingCard className="mx-auto max-w-xl text-center"><span className="mx-auto grid h-16 w-16 place-items-center rounded-lg bg-[#f0efff] text-[#5146f0]"><TrophyIcon /></span><h1 className="mt-5 text-2xl font-black">{t('vocabulary.result')}</h1><p className="mt-2 text-sm text-[var(--student-muted)]">{t('vocabulary.score', { correct: result.correctCount, total: result.questionCount })}</p><strong className="mt-5 block text-5xl font-black text-[#5146f0]">{Math.round(result.percentage)}%</strong><Link to="/student/vocabulary" className="mt-6 inline-flex min-h-11 items-center justify-center rounded-lg bg-[#5146f0] px-5 text-sm font-black text-white no-underline">{t('vocabulary.backToVocabulary')}</Link></TestingCard>
}

function TrophyIcon() {
  return <CheckCircle2 className="h-8 w-8" />
}
