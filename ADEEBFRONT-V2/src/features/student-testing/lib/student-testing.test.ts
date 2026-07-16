import { describe, expect, it } from 'vitest'
import { buildSubmitPayload, canStartRedList, formatTimer, secondsUntil } from '@/features/student-testing/lib/student-testing'
import type { TestQuestionDto } from '@/features/student-testing/model/student-testing.types'

const question: TestQuestionDto = {
  id: 'question-1', order: 1, subjectId: 'subject-1', topicId: null, type: 2, difficulty: 1,
  content: 'Match', imageUrl: null, options: [{ id: 'left-1', text: 'A' }], matchingOptions: ['B'],
}

describe('student testing helpers', () => {
  it('builds a clean matching submit payload without leaking a mapping', () => {
    expect(buildSubmitPayload([question], { 'question-1': { matchingPairs: { 'left-1': 'B' } } })).toEqual({
      answers: [{ questionId: 'question-1', matchingPairs: { 'left-1': 'B' } }],
    })
    expect(question).toHaveProperty('matchingOptions')
    expect(question).not.toHaveProperty('matchingRightOptions')
  })

  it('enables Red List practice only at the configured minimum', () => {
    expect(canStartRedList(4, 5)).toBe(false)
    expect(canStartRedList(5, 5)).toBe(true)
  })

  it('calculates and formats the remaining time safely', () => {
    expect(secondsUntil('2026-07-16T10:01:01.000Z', Date.parse('2026-07-16T10:00:00.000Z'))).toBe(61)
    expect(formatTimer(61)).toBe('01:01')
    expect(secondsUntil('2026-07-16T09:00:00.000Z', Date.parse('2026-07-16T10:00:00.000Z'))).toBe(0)
  })
})
