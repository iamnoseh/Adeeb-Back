export const TestMode = {
  subject: 1,
  mmtPractice: 2,
  monthlyExam: 3,
  redListPractice: 4,
} as const

export const TestAttemptStatus = {
  created: 0,
  inProgress: 1,
  submitted: 2,
  autoSubmitted: 3,
  expired: 4,
  cancelled: 5,
} as const

export const TestQuestionType = {
  singleChoice: 1,
  matching: 2,
  closedAnswer: 3,
} as const

export const RedListStatus = {
  active: 1,
  mastered: 2,
  archived: 3,
} as const

export type StudentTestingConfigDto = {
  subjectQuestionCounts: number[]
  redListMinimumQuestions: number
  redListDefaultQuestions: number
  mmtPracticeDefaultQuestions: number
  monthlyExamQuestionCount: number
  mmtDurationMinutes: number
  monthlyExamAvailable: boolean
  monthlyExamClosesAtUtc: string | null
}

export type StartSubjectTestRequest = { subjectId: string; questionCount: number; includeRedList: boolean }
export type StartMmtPracticeRequest = { strictSimulation: boolean; questionCount?: number }
export type StartRedListPracticeRequest = { questionCount?: number }

export type TestAnswerOptionDto = { id: string; text: string }
export type TestQuestionDto = {
  id: string
  order: number
  subjectId: string
  topicId: string | null
  type: number
  difficulty: number
  content: string
  imageUrl: string | null
  options: TestAnswerOptionDto[]
  matchingOptions: string[]
}

export type TestAttemptDto = {
  id: string
  mode: number
  status: number
  subjectId: string | null
  clusterId: string | null
  startedAtUtc: string
  expiresAtUtc: string
  submittedAtUtc: string | null
  questionCount: number
  questions: TestQuestionDto[]
}

export type SubmitAnswerDto = {
  questionId: string
  selectedOptionId?: string
  textResponse?: string
  matchingPairs?: Record<string, string>
}
export type SubmitAttemptRequest = { answers: SubmitAnswerDto[] }

export type TopicBreakdownDto = { topicId: string | null; total: number; correct: number; wrong: number }
export type SubjectBreakdownDto = { subjectId: string; total: number; correct: number; wrong: number; percentage: number }
export type WeakTopicDto = { subjectId: string; topicId: string | null; total: number; correct: number; percentage: number }
export type TestAnswerResultDto = {
  questionId: string
  subjectId: string
  isAnswered: boolean
  isCorrect: boolean
  content: string
  userAnswer: string | null
  correctAnswer: string | null
  explanation: string | null
  topicId: string | null
  difficulty: number
  correctPairsCount: number | null
  totalPairsCount: number | null
}
export type TestResultDto = {
  attemptId: string
  mode: number
  status: number
  questionCount: number
  correctCount: number
  wrongCount: number
  score: number
  percentage: number
  submittedAtUtc: string
  topicBreakdown: TopicBreakdownDto[]
  subjectBreakdown: SubjectBreakdownDto[]
  weakTopics: WeakTopicDto[]
  answers: TestAnswerResultDto[]
  easyCorrect: number
  mediumCorrect: number
  hardCorrect: number
  answerXp: number
  completionBonusXp: number
  totalXp: number
  xpAwarded: boolean
}

export type TestHistoryItemDto = {
  attemptId: string
  mode: number
  status: number
  startedAtUtc: string
  submittedAtUtc: string | null
  questionCount: number
  correctCount: number
  percentage: number
  totalXp: number
  xpAwarded: boolean
}
export type TestingPageDto<T> = { items: T[]; page: number; pageSize: number; totalCount: number }
export type TestingHistoryQuery = { page?: number; pageSize?: number }

export type RedListItemDto = {
  id: string
  questionId: string
  subjectId: string
  topicId: string | null
  questionType: number
  wrongCount: number
  correctStreak: number
  lastWrongAtUtc: string
  lastPracticedAtUtc: string
  status: number
  questionContent: string
}
export type RedListSubjectSummaryDto = { subjectId: string; activeCount: number }
export type RedListSummaryDto = {
  activeCount: number
  masteredCount: number
  archivedCount: number
  subjects: RedListSubjectSummaryDto[]
}
export type RedListQuery = { subjectId?: string; status?: number; page?: number; pageSize?: number }

export type DraftAnswer = {
  selectedOptionId?: string
  textResponse?: string
  matchingPairs?: Record<string, string>
}
export type AttemptAnswers = Record<string, DraftAnswer>
