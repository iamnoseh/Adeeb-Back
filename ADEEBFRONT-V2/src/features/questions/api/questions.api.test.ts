import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { QuestionFormValues } from '@/features/questions/model/question.types'

const post = vi.fn()
vi.mock('@/shared/api/http-client', () => ({ httpClient: { get: vi.fn(), post, put: vi.fn(), delete: vi.fn() } }))

const values: QuestionFormValues = {
  subjectId: '11111111-1111-4111-8111-111111111111', topicId: '',
  contentTg: 'Савол', contentRu: 'Вопрос', explanationTg: 'Шарҳ', explanationRu: 'Объяснение',
  type: 1, difficulty: 1, status: 1,
  answers: [
    { textTg: 'Як', textRu: 'Один', isCorrect: true },
    { textTg: 'Ду', textRu: 'Два', isCorrect: false },
    { textTg: 'Се', textRu: 'Три', isCorrect: false },
    { textTg: 'Чор', textRu: 'Четыре', isCorrect: false },
  ],
  matchingPairs: [], correctAnswerTg: '', correctAnswerRu: '',
}

describe('questions API localization contract', () => {
  beforeEach(() => post.mockReset())

  it('sends bilingual question and answer fields', async () => {
    post.mockResolvedValue({ data: { id: 'question-1' } })
    const { questionsApi } = await import('@/features/questions/api/questions.api')
    await questionsApi.create(values)

    const body = post.mock.calls[0]?.[1] as FormData
    expect(body.get('ContentTg')).toBe('Савол')
    expect(body.get('ContentRu')).toBe('Вопрос')
    const answers = JSON.parse(String(body.get('AnswersJson'))) as QuestionFormValues['answers']
    expect(answers[0]).toMatchObject({ textTg: 'Як', textRu: 'Один' })
    expect(body.get('Content')).toBeNull()
  })
})
