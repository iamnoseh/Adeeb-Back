import { httpClient } from '@/shared/api/http-client'
import { getStoredUiLanguage } from '@/shared/i18n/language'
import type {
  DailyWordDto,
  LearningLanguageDto,
  StartVocabularySessionRequest,
  StudentVocabularyCourseDto,
  StudentVocabularyCourseRequest,
  StudentVocabularyDashboardDto,
  SubmitVocabularyAnswerRequest,
  VocabularyAnswerResponse,
  VocabularyHistoryItemDto,
  VocabularyListQuery,
  VocabularyMistakeDto,
  VocabularyPage,
  VocabularyQuestionDto,
  VocabularySessionDto,
  VocabularySessionResultDto,
  VocabularyTopicDto,
  VocabularyWordDto,
} from '@/features/vocabulary/model/vocabulary.types'

const studentBase = '/api/v2/students/me/vocabulary'
const adminBase = '/api/v2/admin/vocabulary'

export const vocabularyKeys = {
  student: {
    all: ['vocabulary', 'student'] as const,
    languages: () => [...vocabularyKeys.student.all, 'languages', getStoredUiLanguage()] as const,
    course: () => [...vocabularyKeys.student.all, 'course', getStoredUiLanguage()] as const,
    dashboard: () => [...vocabularyKeys.student.all, 'dashboard', getStoredUiLanguage()] as const,
    today: () => [...vocabularyKeys.student.all, 'today', getStoredUiLanguage()] as const,
    session: (id: string) => [...vocabularyKeys.student.all, 'session', id, getStoredUiLanguage()] as const,
    history: (query: VocabularyListQuery = {}) => [...vocabularyKeys.student.all, 'history', query, getStoredUiLanguage()] as const,
    mistakes: (query: VocabularyListQuery = {}) => [...vocabularyKeys.student.all, 'mistakes', query, getStoredUiLanguage()] as const,
  },
  admin: {
    all: ['vocabulary', 'admin'] as const,
    list: (resource: string, query: VocabularyListQuery = {}) => [...vocabularyKeys.admin.all, resource, query, getStoredUiLanguage()] as const,
  },
}

export const studentVocabularyApi = {
  async languages() {
    const response = await httpClient.get<LearningLanguageDto[]>(`${studentBase}/languages`)
    return response.data
  },
  async course() {
    const response = await httpClient.get<StudentVocabularyCourseDto>(`${studentBase}/course`)
    return response.data
  },
  async setCourse(input: StudentVocabularyCourseRequest) {
    const response = await httpClient.put<StudentVocabularyCourseDto>(`${studentBase}/course`, input)
    return response.data
  },
  async today() {
    const response = await httpClient.get<DailyWordDto>(`${studentBase}/today`)
    return response.data
  },
  async dashboard() {
    const response = await httpClient.get<StudentVocabularyDashboardDto>(`${studentBase}/dashboard`)
    return response.data
  },
  async startSession(input: StartVocabularySessionRequest) {
    const response = await httpClient.post<VocabularySessionDto>(`${studentBase}/sessions`, input)
    return response.data
  },
  async session(id: string) {
    const response = await httpClient.get<VocabularySessionDto>(`${studentBase}/sessions/${id}`)
    return response.data
  },
  async answer(sessionId: string, input: SubmitVocabularyAnswerRequest) {
    const response = await httpClient.post<VocabularyAnswerResponse>(`${studentBase}/sessions/${sessionId}/answers`, input)
    return response.data
  },
  async complete(sessionId: string) {
    const response = await httpClient.post<VocabularySessionResultDto>(`${studentBase}/sessions/${sessionId}/complete`)
    return response.data
  },
  async history(query: VocabularyListQuery = {}) {
    const response = await httpClient.get<VocabularyPage<VocabularyHistoryItemDto>>(`${studentBase}/history${queryString(query)}`)
    return response.data
  },
  async mistakes(query: VocabularyListQuery = {}) {
    const response = await httpClient.get<VocabularyPage<VocabularyMistakeDto>>(`${studentBase}/mistakes${queryString(query)}`)
    return response.data
  },
}

export const adminVocabularyApi = {
  async languages(query: VocabularyListQuery = {}) {
    const response = await httpClient.get<VocabularyPage<LearningLanguageDto>>(`${adminBase}/languages${queryString(query)}`)
    return response.data
  },
  async topics(query: VocabularyListQuery = {}) {
    const response = await httpClient.get<VocabularyPage<VocabularyTopicDto>>(`${adminBase}/topics${queryString(query)}`)
    return response.data
  },
  async words(query: VocabularyListQuery = {}) {
    const response = await httpClient.get<VocabularyPage<VocabularyWordDto>>(`${adminBase}/words${queryString(query)}`)
    return response.data
  },
  async questions(query: VocabularyListQuery = {}) {
    const response = await httpClient.get<VocabularyPage<VocabularyQuestionDto>>(`${adminBase}/questions${queryString(query)}`)
    return response.data
  },
  async dailyWords(query: VocabularyListQuery = {}) {
    const response = await httpClient.get<VocabularyPage<DailyWordDto>>(`${adminBase}/daily-words${queryString(query)}`)
    return response.data
  },
}

function queryString(query: VocabularyListQuery) {
  const params = new URLSearchParams()
  Object.entries(query).forEach(([key, value]) => {
    if (value === undefined || value === null || value === '') return
    params.set(key, String(value))
  })
  const value = params.toString()
  return value ? `?${value}` : ''
}
