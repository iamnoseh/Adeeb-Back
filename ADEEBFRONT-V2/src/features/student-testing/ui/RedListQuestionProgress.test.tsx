// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest'
import { cleanup, render, screen } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { RedListQuestionProgress } from './RedListQuestionProgress'

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
}))

afterEach(cleanup)

describe('RedListQuestionProgress', () => {
  it('shows the persisted mastery progress without exposing an answer', () => {
    render(<RedListQuestionProgress progress={{ correctStreak: 1, requiredCorrectStreak: 3, correctAnswersRemaining: 2 }} />)

    const progress = screen.getByTestId('red-list-question-progress')
    expect(progress).toHaveTextContent('student.testing.redListQuestion.label')
    expect(progress).toHaveTextContent('1/3')
    expect(progress).not.toHaveTextContent('correctAnswer')
  })
})
