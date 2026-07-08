import { z } from 'zod'

const answerSchema = z.object({
  text: z.string().trim(),
  isCorrect: z.boolean(),
})

const matchingPairSchema = z.object({
  text: z.string().trim(),
  matchPair: z.string().trim(),
})

export function createQuestionFormSchema(t: (key: string) => string) {
  return z
    .object({
      subjectId: z.string().uuid(t('validationChooseSubject')),
    topicId: z.string().trim(),
    content: z.string().trim().min(1, t('validationQuestionText')),
    explanation: z.string().trim(),
    type: z.coerce.number().int().min(1).max(3),
    difficulty: z.coerce.number().int().min(1).max(3),
    status: z.coerce.number().int().min(0).max(2),
    answers: z.array(answerSchema).length(4),
    matchingPairs: z.array(matchingPairSchema).length(4),
    correctAnswer: z.string().trim(),
    image: z.instanceof(FileList).optional(),
  })
  .superRefine((value, context) => {
    if (value.type === 1) {
      const correctCount = value.answers.filter((answer) => answer.isCorrect).length
      if (value.answers.some((answer) => answer.text.length === 0)) {
        context.addIssue({ code: 'custom', path: ['answers'], message: t('validationAllAnswers') })
      }
      if (correctCount !== 1) {
        context.addIssue({ code: 'custom', path: ['answers'], message: t('validationOneCorrect') })
      }
    }

    if (value.type === 2) {
      const rightValues = value.matchingPairs.map((pair) => pair.matchPair.toLowerCase())
      if (value.matchingPairs.some((pair) => !pair.text || !pair.matchPair)) {
        context.addIssue({ code: 'custom', path: ['matchingPairs'], message: t('validationAllPairs') })
      }
      if (new Set(rightValues).size !== rightValues.length) {
        context.addIssue({ code: 'custom', path: ['matchingPairs'], message: t('validationDuplicateRight') })
      }
    }

    if (value.type === 3 && value.correctAnswer.length === 0) {
      context.addIssue({ code: 'custom', path: ['correctAnswer'], message: t('validationCorrectAnswer') })
    }
  })
}

export const questionFormSchema = createQuestionFormSchema((key) => key)
