import type { PagedResponse } from '@/features/academic/model/academic.types'

export type QuestionListQuery = {
  subjectId?: string
  topicId?: string
  type?: number
  difficulty?: number
  status?: number
  search?: string
  page?: number
  pageSize?: number
  sort?: string
}

export type QuestionTranslationResponse = {
  language: number
  content: string
  explanation?: string | null
}

export type AnswerOptionTranslationResponse = {
  language: number
  text: string
  matchPairText?: string | null
}

export type AnswerOptionResponse = {
  id: string
  displayOrder: number
  isCorrect: boolean
  translations: AnswerOptionTranslationResponse[]
}

export type QuestionResponse = {
  id: string
  subjectId: string
  topicId?: string | null
  topic?: string | null
  type: number
  difficulty: number
  status: number
  content: string
  imageUrl?: string | null
  translations: QuestionTranslationResponse[]
  answerOptions: AnswerOptionResponse[]
}

export type QuestionListResponse = PagedResponse<QuestionResponse>

export type SingleChoiceOptionForm = {
  text: string
  isCorrect: boolean
}

export type MatchingPairForm = {
  text: string
  matchPair: string
}

export type QuestionFormValues = {
  subjectId: string
  topicId: string
  content: string
  explanation: string
  type: number
  difficulty: number
  status: number
  answers: SingleChoiceOptionForm[]
  matchingPairs: MatchingPairForm[]
  correctAnswer: string
  image?: FileList
}
