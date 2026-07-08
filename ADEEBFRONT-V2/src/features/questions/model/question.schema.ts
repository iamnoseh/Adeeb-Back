import { z } from 'zod'

const answerSchema = z.object({
  text: z.string().trim(),
  isCorrect: z.boolean(),
})

const matchingPairSchema = z.object({
  text: z.string().trim(),
  matchPair: z.string().trim(),
})

export const questionFormSchema = z
  .object({
    subjectId: z.string().uuid('Фанро интихоб кунед'),
    topicId: z.string().trim(),
    content: z.string().trim().min(1, 'Матни савол лозим аст'),
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
        context.addIssue({ code: 'custom', path: ['answers'], message: 'Ҳамаи 4 ҷавобро пур кунед' })
      }
      if (correctCount !== 1) {
        context.addIssue({ code: 'custom', path: ['answers'], message: 'Танҳо 1 ҷавоби дуруст интихоб шавад' })
      }
    }

    if (value.type === 2) {
      const rightValues = value.matchingPairs.map((pair) => pair.matchPair.toLowerCase())
      if (value.matchingPairs.some((pair) => !pair.text || !pair.matchPair)) {
        context.addIssue({ code: 'custom', path: ['matchingPairs'], message: 'Ҳамаи 4 ҷуфтро пур кунед' })
      }
      if (new Set(rightValues).size !== rightValues.length) {
        context.addIssue({ code: 'custom', path: ['matchingPairs'], message: 'Қисми рост такрор нашавад' })
      }
    }

    if (value.type === 3 && value.correctAnswer.length === 0) {
      context.addIssue({ code: 'custom', path: ['correctAnswer'], message: 'Ҷавоби дуруст лозим аст' })
    }
  })
