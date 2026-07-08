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

export type QuestionImportIssue = {
  code: string
  message: string
}

export type QuestionImportPreviewOption = {
  label: string
  text: string
  isCorrect: boolean
}

export type QuestionImportPreviewQuestion = {
  clientKey: string
  questionText: string
  options: QuestionImportPreviewOption[]
  isValid: boolean
  errors: QuestionImportIssue[]
  warnings: QuestionImportIssue[]
}

export type QuestionImportPreviewResponse = {
  fileName: string
  summary: {
    totalDetected: number
    valid: number
    invalid: number
    warnings: number
  }
  questions: QuestionImportPreviewQuestion[]
}

export type QuestionImportParseRequest = {
  subjectId: string
  topicId?: string | null
  difficulty: number
  file: File
}

export type QuestionImportConfirmRequest = {
  subjectId: string
  topicId?: string | null
  difficulty: number
  questions: {
    questionText: string
    options: {
      text: string
      isCorrect: boolean
    }[]
  }[]
}

export type QuestionImportConfirmResponse = {
  importedCount: number
  questionIds: string[]
}
