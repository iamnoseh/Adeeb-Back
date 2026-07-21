import { ApiError } from '@/shared/api/problem-details'
import type { AttemptAnswers, SubmitAttemptRequest, TestQuestionDto } from '@/features/student-testing/model/student-testing.types'

export function canStartRedList(activeCount: number, minimum: number) {
  return activeCount >= minimum
}

export function secondsUntil(expiresAtUtc: string, now = Date.now()) {
  return Math.max(0, Math.ceil((new Date(expiresAtUtc).getTime() - now) / 1000))
}

export function formatTimer(totalSeconds: number) {
  const safe = Math.max(0, totalSeconds)
  const hours = Math.floor(safe / 3600)
  const minutes = Math.floor((safe % 3600) / 60)
  const seconds = safe % 60
  return hours > 0
    ? `${String(hours).padStart(2, '0')}:${String(minutes).padStart(2, '0')}:${String(seconds).padStart(2, '0')}`
    : `${String(minutes).padStart(2, '0')}:${String(seconds).padStart(2, '0')}`
}

export function isAnswered(answer: AttemptAnswers[string] | undefined) {
  return Boolean(
    answer?.selectedOptionId ||
      answer?.textResponse?.trim() ||
      (answer?.matchingPairs && Object.values(answer.matchingPairs).some(Boolean)),
  )
}

export function buildSubmitPayload(questions: TestQuestionDto[], answers: AttemptAnswers): SubmitAttemptRequest {
  return {
    answers: questions.map((question) => {
      const answer = answers[question.id]
      const textResponse = answer?.textResponse?.trim()
      return {
        questionId: question.id,
        ...(answer?.selectedOptionId ? { selectedOptionId: answer.selectedOptionId } : {}),
        ...(textResponse ? { textResponse } : {}),
        ...(answer?.matchingPairs ? { matchingPairs: answer.matchingPairs } : {}),
      }
    }),
  }
}

export function testingErrorKey(error: unknown) {
  if (!(error instanceof ApiError)) return 'unknown'
  const code = error.problem?.code
  const known: Record<string, string> = {
    'redlist.not_enough_questions': 'redListNotEnough',
    'test.not_enough_questions': 'notEnoughQuestions',
    'test.attempt_not_found': 'attemptNotFound',
    'test.attempt_already_submitted': 'alreadySubmitted',
    'test.attempt_expired': 'attemptExpired',
    'test.invalid_mode': 'invalidMode',
    'test.immediate_check_not_allowed': 'immediateCheckNotAllowed',
    'test.question_not_in_attempt': 'questionNotInAttempt',
    'test.answer_required': 'answerRequired',
    'test.invalid_question_count': 'invalidQuestionCount',
    'mmt.profile_required': 'profileRequired',
    'mmt.choices_required': 'choicesRequired',
    'monthly_exam.closed': 'monthlyClosed',
    'monthly_exam.already_started': 'monthlyAlreadyStarted',
  }
  return code ? (known[code] ?? 'unknown') : 'unknown'
}

export function draftStorageKey(attemptId: string) {
  return `adeeb:test-attempt:${attemptId}`
}

export function readDraft(attemptId: string): AttemptAnswers {
  try {
    const value = localStorage.getItem(draftStorageKey(attemptId))
    return value ? (JSON.parse(value) as AttemptAnswers) : {}
  } catch {
    return {}
  }
}

export function writeDraft(attemptId: string, answers: AttemptAnswers) {
  localStorage.setItem(draftStorageKey(attemptId), JSON.stringify(answers))
}

export function clearDraft(attemptId: string) {
  localStorage.removeItem(draftStorageKey(attemptId))
}
