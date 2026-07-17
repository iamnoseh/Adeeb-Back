import { z } from 'zod'

const optionalFileListSchema = z.custom<FileList>(
  (value) => typeof FileList !== 'undefined' && value instanceof FileList,
).optional()

const answerSchema = z.object({
  textTg: z.string().trim(),
  textRu: z.string().trim(),
  isCorrect: z.boolean(),
})

const matchingPairSchema = z.object({
  textTg: z.string().trim(),
  textRu: z.string().trim(),
  matchPairTg: z.string().trim(),
  matchPairRu: z.string().trim(),
})

export function createQuestionFormSchema(t: (key: string) => string) {
  return z
    .object({
      subjectId: z.string().uuid(t('validationChooseSubject')),
    topicId: z.string().trim(),
    contentTg: z.string().trim().min(1, t('validationQuestionTextTg')),
    contentRu: z.string().trim().min(1, t('validationQuestionTextRu')),
    explanationTg: z.string().trim(),
    explanationRu: z.string().trim(),
    type: z.coerce.number().int().min(1).max(3),
    difficulty: z.coerce.number().int().min(1).max(3),
    status: z.coerce.number().int().min(0).max(2),
    answers: z.array(answerSchema).length(4),
    matchingPairs: z.array(matchingPairSchema).length(4),
    correctAnswerTg: z.string().trim(),
    correctAnswerRu: z.string().trim(),
    image: optionalFileListSchema,
  })
  .superRefine((value, context) => {
    if (value.type === 1) {
      const correctCount = value.answers.filter((answer) => answer.isCorrect).length
      if (value.answers.some((answer) => !answer.textTg || !answer.textRu)) {
        context.addIssue({ code: 'custom', path: ['answers'], message: t('validationAllAnswers') })
      }
      if (correctCount !== 1) {
        context.addIssue({ code: 'custom', path: ['answers'], message: t('validationOneCorrect') })
      }
    }

    if (value.type === 2) {
      const rightValuesTg = value.matchingPairs.map((pair) => pair.matchPairTg.toLowerCase())
      const rightValuesRu = value.matchingPairs.map((pair) => pair.matchPairRu.toLowerCase())
      if (value.matchingPairs.some((pair) => !pair.textTg || !pair.textRu || !pair.matchPairTg || !pair.matchPairRu)) {
        context.addIssue({ code: 'custom', path: ['matchingPairs'], message: t('validationAllPairs') })
      }
      if (new Set(rightValuesTg).size !== rightValuesTg.length || new Set(rightValuesRu).size !== rightValuesRu.length) {
        context.addIssue({ code: 'custom', path: ['matchingPairs'], message: t('validationDuplicateRight') })
      }
    }

    if (value.type === 3 && (!value.correctAnswerTg || !value.correctAnswerRu)) {
      context.addIssue({ code: 'custom', path: ['correctAnswerTg'], message: t('validationCorrectAnswerBothLanguages') })
    }
  })
}

export const questionFormSchema = createQuestionFormSchema((key) => key)
