import { beforeEach, describe, expect, it, vi } from 'vitest'

const post = vi.fn()
const get = vi.fn()
vi.mock('@/shared/api/http-client', () => ({ httpClient: { get, post } }))

describe('student testing API', () => {
  beforeEach(() => { post.mockReset(); get.mockReset() })

  it('starts a monthly exam without a request body', async () => {
    post.mockResolvedValue({ data: { id: 'attempt-1' } })
    const { studentTestingApi } = await import('@/features/student-testing/api/student-testing.api')
    await studentTestingApi.startMonthlyExam()
    expect(post).toHaveBeenCalledWith('/api/v2/student/tests/monthly-exam/start')
  })

  it('returns the persisted XP breakdown from the result endpoint unchanged', async () => {
    const xp = { easyCorrect: 4, mediumCorrect: 3, hardCorrect: 2, answerXp: 17, completionBonusXp: 5, totalXp: 22, xpAwarded: true }
    get.mockResolvedValue({ data: { attemptId: 'attempt-1', ...xp } })
    const { studentTestingApi } = await import('@/features/student-testing/api/student-testing.api')

    const result = await studentTestingApi.getAttemptResult('attempt-1')

    expect(get).toHaveBeenCalledWith('/api/v2/student/tests/attempts/attempt-1/result')
    expect(result).toMatchObject(xp)
  })
})
