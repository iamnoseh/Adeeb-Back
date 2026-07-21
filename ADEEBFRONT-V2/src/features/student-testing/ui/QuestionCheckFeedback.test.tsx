// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest'
import { cleanup, render, screen } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import type { CheckedTestAnswerDto } from '@/features/student-testing/model/student-testing.types'
import { QuestionCheckFeedback, RedListMasteryToast } from './QuestionCheckFeedback'

vi.mock('react-i18next', () => ({ useTranslation: () => ({ t: (key: string) => key }) }))
afterEach(cleanup)

describe('QuestionCheckFeedback', () => {
  it('shows immediate correctness and the Red List transition', () => {
    render(<QuestionCheckFeedback feedback={feedback()} />)
    expect(screen.getByText('student.testing.check.correct')).toBeInTheDocument()
    expect(screen.getByText('student.testing.check.redListActions.4')).toBeInTheDocument()
  })

  it('shows the compact mastery reward', () => {
    render(<RedListMasteryToast xp={1} onClose={vi.fn()} />)
    expect(screen.getByRole('status')).toHaveTextContent('+1 XP')
    expect(screen.getByText('student.testing.check.masteredTitle')).toBeInTheDocument()
  })
})

function feedback(): CheckedTestAnswerDto {
  return {
    questionId: 'question-1', isCorrect: true, userAnswer: 'A', correctAnswer: 'A', correctOptionId: 'option-1',
    explanation: 'Explanation', selectedOptionId: 'option-1', textResponse: null, matchingPairs: null,
    correctPairsCount: null, totalPairsCount: null,
    redList: { action: 4, correctStreak: 3, requiredCorrectStreak: 3, correctAnswersRemaining: 0, masteryBonusXp: 1, masteryBonusAwarded: true, totalXp: 5 },
  }
}
