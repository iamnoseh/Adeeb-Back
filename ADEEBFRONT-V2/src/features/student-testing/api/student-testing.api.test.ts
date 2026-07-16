import { beforeEach, describe, expect, it, vi } from 'vitest'

const post = vi.fn()
vi.mock('@/shared/api/http-client', () => ({ httpClient: { get: vi.fn(), post } }))

describe('student testing API', () => {
  beforeEach(() => post.mockReset())

  it('starts a monthly exam without a request body', async () => {
    post.mockResolvedValue({ data: { id: 'attempt-1' } })
    const { studentTestingApi } = await import('@/features/student-testing/api/student-testing.api')
    await studentTestingApi.startMonthlyExam()
    expect(post).toHaveBeenCalledWith('/api/v2/student/tests/monthly-exam/start')
  })
})
