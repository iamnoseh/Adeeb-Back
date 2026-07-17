import { describe, expect, it } from 'vitest'
import { createQuestionFormSchema } from '@/features/questions/model/question.schema'

const translate = (key: string) => key

function validSingleChoice() {
  return {
    subjectId: '11111111-1111-4111-8111-111111111111',
    topicId: '',
    contentTg: 'Савол',
    contentRu: 'Вопрос',
    explanationTg: '',
    explanationRu: '',
    type: 1,
    difficulty: 1,
    status: 1,
    answers: [
      { textTg: 'Як', textRu: 'Один', isCorrect: true },
      { textTg: 'Ду', textRu: 'Два', isCorrect: false },
      { textTg: 'Се', textRu: 'Три', isCorrect: false },
      { textTg: 'Чор', textRu: 'Четыре', isCorrect: false },
    ],
    matchingPairs: Array.from({ length: 4 }, (_, index) => ({ textTg: `Чап ${index}`, textRu: `Лево ${index}`, matchPairTg: `Рост ${index}`, matchPairRu: `Право ${index}` })),
    correctAnswerTg: '',
    correctAnswerRu: '',
  }
}

describe('question form localization validation', () => {
  it('accepts distinct Tajik and Russian question and option text', () => {
    expect(createQuestionFormSchema(translate).safeParse(validSingleChoice()).success).toBe(true)
  })

  it('rejects a missing Russian question translation', () => {
    const result = createQuestionFormSchema(translate).safeParse({ ...validSingleChoice(), contentRu: '' })
    expect(result.success).toBe(false)
    if (!result.success) expect(result.error.issues.some((issue) => issue.path[0] === 'contentRu')).toBe(true)
  })

  it('rejects an option missing either language', () => {
    const value = validSingleChoice()
    value.answers[0]!.textRu = ''
    const result = createQuestionFormSchema(translate).safeParse(value)
    expect(result.success).toBe(false)
    if (!result.success) expect(result.error.issues.some((issue) => issue.path[0] === 'answers')).toBe(true)
  })
})
