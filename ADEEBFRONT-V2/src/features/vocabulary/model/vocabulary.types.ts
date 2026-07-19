export type VocabularyPage<T> = {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
}

export type VocabularyListQuery = {
  search?: string
  languageId?: string
  level?: number
  topicId?: string
  type?: number
  status?: number
  page?: number
  pageSize?: number
}

export const VocabularyLevel = { A1: 0, A2: 1, B1: 2, B2: 3, C1: 4, C2: 5 } as const
export const VocabularyQuestionType = { Translation: 0, FillBlank: 1, OddWordReplacement: 2, Synonym: 3, Antonym: 4, WordOrder: 5 } as const
export const VocabularySessionMode = { DailyPractice: 0, MistakeReview: 1, FreePractice: 2, Test: 3 } as const
export const VocabularySessionStatus = { InProgress: 0, Completed: 1 } as const
export const VocabularyContentStatus = { Draft: 0, Published: 1, Archived: 2 } as const

export type LearningLanguageDto = {
  id: string
  code: string
  name: string
  nameTg: string
  nameRu: string
  displayOrder: number
  isActive: boolean
}

export type LanguageUpsertRequest = {
  code: string
  nameTg: string
  nameRu: string
  displayOrder: number
  isActive: boolean
}

export type VocabularyTopicDto = {
  id: string
  languageId: string
  level: number
  name: string
  nameTg: string
  nameRu: string
  description?: string | null
  descriptionTg?: string | null
  descriptionRu?: string | null
  status: number
}

export type TopicUpsertRequest = {
  languageId: string
  level: number
  nameTg: string
  nameRu: string
  descriptionTg?: string | null
  descriptionRu?: string | null
  status: number
}

export type VocabularyRelationDto = {
  id: string
  relatedWordId: string
  relatedTargetText: string
  type: number
}

export type VocabularyWordDto = {
  id: string
  languageId: string
  topicId: string
  level: number
  targetText: string
  translation: string
  translationTg: string
  translationRu: string
  explanation?: string | null
  explanationTg?: string | null
  explanationRu?: string | null
  exampleTarget: string
  example: string
  exampleTg: string
  exampleRu: string
  status: number
  relations: VocabularyRelationDto[]
}

export type WordUpsertRequest = {
  languageId: string
  topicId: string
  level: number
  targetText: string
  translationTg: string
  translationRu: string
  explanationTg?: string | null
  explanationRu?: string | null
  exampleTarget: string
  exampleTg: string
  exampleRu: string
  status: number
  relations?: { relatedWordId: string; type: number }[] | null
}

export type VocabularyQuestionOptionDto = {
  id: string
  wordId?: string | null
  value: string
  valueTarget: string
  valueTg: string
  valueRu: string
  displayOrder: number
  isCorrect: boolean
  correctOrder?: number | null
}

export type VocabularyQuestionDto = {
  id: string
  wordId: string
  type: number
  prompt: string
  promptTarget: string
  promptTg: string
  promptRu: string
  correctTokenIndex?: number | null
  status: number
  reviewedBy?: string | null
  reviewedAtUtc?: string | null
  options: VocabularyQuestionOptionDto[]
}

export type DraftGenerationWarning = {
  type: number
  code: string
}

export type DraftGenerationResult = {
  created: VocabularyQuestionDto[]
  warnings: DraftGenerationWarning[]
}

export type DailyWordDto = {
  languageId: string
  localDate: string
  isAutomatic: boolean
  word: VocabularyWordDto
}

export type DailyWordUpsertRequest = {
  languageId: string
  localDate: string
  wordId: string
}

export type StudentVocabularyCourseDto = {
  languageId: string
  languageName: string
  level: number
  updatedAtUtc: string
}

export type StudentVocabularyCourseRequest = {
  languageId: string
  level: number
}

export type StudentVocabularyDashboardDto = {
  course: StudentVocabularyCourseDto
  today: DailyWordDto
  masteredWords: number
  dueReviews: number
  completedSessions: number
  totalPracticedWords: number
}

export type StartVocabularySessionRequest = {
  mode: number
  topicId?: string | null
  level?: number | null
  questionCount?: number | null
}

export type StudentVocabularyOptionDto = {
  id: string
  value: string
  displayOrder: number
}

export type StudentVocabularyQuestionDto = {
  id: string
  order: number
  type: number
  prompt: string
  options: StudentVocabularyOptionDto[]
  isAnswered: boolean
}

export type VocabularySessionDto = {
  id: string
  mode: number
  status: number
  languageId: string
  level: number
  topicId?: string | null
  localDate: string
  questionCount: number
  answeredCount: number
  correctCount: number
  startedAtUtc: string
  completedAtUtc?: string | null
  questions: StudentVocabularyQuestionDto[]
}

export type SubmitVocabularyAnswerRequest = {
  questionId: string
  selectedOptionId?: string | null
  selectedTokenIndex?: number | null
  orderedOptionIds?: string[] | null
}

export type VocabularyAnswerFeedbackDto = {
  questionId: string
  isCorrect: boolean
  correctOptionId?: string | null
  correctTokenIndex?: number | null
  correctOrder?: string[] | null
}

export type VocabularyAnswerResponse = {
  session: VocabularySessionDto
  feedback?: VocabularyAnswerFeedbackDto | null
}

export type VocabularySessionResultDto = {
  sessionId: string
  mode: number
  questionCount: number
  correctCount: number
  wrongCount: number
  percentage: number
  completedAtUtc: string
  answers: VocabularyAnswerFeedbackDto[]
}

export type VocabularyHistoryItemDto = {
  sessionId: string
  mode: number
  questionCount: number
  correctCount: number
  percentage: number
  completedAtUtc: string
}

export type VocabularyMistakeDto = {
  wordId: string
  targetText: string
  translation: string
  wrongCount: number
  masteryLevel: number
  nextReviewDate?: string | null
}
