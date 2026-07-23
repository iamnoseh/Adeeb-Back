// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest'
import { cleanup, render, screen, within } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import type { TestResultDto } from '@/features/student-testing/model/student-testing.types'
import { XpRewardSummary } from './XpRewardSummary'

vi.mock('react-i18next', () => ({
  useTranslation: () => ({ t: (key: string) => key }),
}))

afterEach(cleanup)

describe('XpRewardSummary', () => {
  it('renders the persisted reward and difficulty breakdown', () => {
    render(<XpRewardSummary data={result({ easyCorrect: 4, mediumCorrect: 3, hardCorrect: 2, answerXp: 17, completionBonusXp: 5, totalXp: 22, xpAwarded: true })} language="ru-RU" />)

    const summary = screen.getByTestId('xp-reward-summary')
    expect(summary).toHaveAttribute('data-awarded', 'true')
    expect(within(summary).getByText('+22', { exact: false })).toBeInTheDocument()
    expect(within(summary).getByText('17 XP')).toBeInTheDocument()
    expect(within(summary).getByText('5 XP')).toBeInTheDocument()
    expect(within(summary).getByText('student.testing.result.xpAwarded')).toBeInTheDocument()
  })

  it('renders zero XP as a completed neutral settlement', () => {
    render(<XpRewardSummary data={result({})} language="tg-TJ" />)

    const summary = screen.getByTestId('xp-reward-summary')
    expect(summary).toHaveAttribute('data-awarded', 'false')
    expect(within(summary).getByText('student.testing.result.xpNotAwarded')).toBeInTheDocument()
    expect(within(summary).getByText('student.testing.result.xpZeroDescription')).toBeInTheDocument()
    expect(within(summary).queryByText('+0', { exact: false })).not.toBeInTheDocument()
  })

  it('renders completion-only XP rewards for monthly exam submissions', () => {
    render(<XpRewardSummary data={result({ answerXp: 0, completionBonusXp: 25, totalXp: 25, xpAwarded: true })} language="ru-RU" />)

    const summary = screen.getByTestId('xp-reward-summary')
    expect(summary).toHaveAttribute('data-awarded', 'true')
    expect(within(summary).getByText('+25', { exact: false })).toBeInTheDocument()
    expect(within(summary).getByText('0 XP')).toBeInTheDocument()
    expect(within(summary).getByText('25 XP')).toBeInTheDocument()
  })
})

function result(values: Partial<Pick<TestResultDto, 'easyCorrect' | 'mediumCorrect' | 'hardCorrect' | 'answerXp' | 'completionBonusXp' | 'totalXp' | 'xpAwarded'>>): TestResultDto {
  return {
    attemptId: 'attempt-1', mode: 1, status: 2, questionCount: 1, correctCount: 0, wrongCount: 1,
    score: 0, percentage: 0, submittedAtUtc: '2026-07-21T08:00:00Z', topicBreakdown: [],
    subjectBreakdown: [], weakTopics: [], answers: [], easyCorrect: 0, mediumCorrect: 0,
    hardCorrect: 0, answerXp: 0, completionBonusXp: 0, totalXp: 0, xpAwarded: false, ...values,
  }
}
