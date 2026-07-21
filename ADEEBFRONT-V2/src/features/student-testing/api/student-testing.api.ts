import { httpClient } from '@/shared/api/http-client'
import { getStoredUiLanguage } from '@/shared/i18n/language'
import type {
  RedListItemDto,
  CheckTestAnswerRequest,
  CheckedTestAnswerDto,
  RedListQuery,
  RedListSummaryDto,
  StartMmtPracticeRequest,
  StartRedListPracticeRequest,
  StartSubjectTestRequest,
  StudentTestingConfigDto,
  SubmitAttemptRequest,
  TestAttemptDto,
  TestHistoryItemDto,
  TestingHistoryQuery,
  TestingPageDto,
  TestResultDto,
} from '@/features/student-testing/model/student-testing.types'

const testsBase = '/api/v2/student/tests'
const redListBase = '/api/v2/student/red-list'

function queryString(query: Record<string, string | number | undefined>) {
  const params = new URLSearchParams()
  Object.entries(query).forEach(([key, value]) => {
    if (value !== undefined && value !== '') params.set(key, String(value))
  })
  const value = params.toString()
  return value ? `?${value}` : ''
}

export const studentTestingKeys = {
  all: ['student-testing'] as const,
  config: () => [...studentTestingKeys.all, 'config'] as const,
  attempt: (id: string) => [...studentTestingKeys.all, 'attempt', id, getStoredUiLanguage()] as const,
  result: (id: string) => [...studentTestingKeys.all, 'result', id, getStoredUiLanguage()] as const,
  history: (query: TestingHistoryQuery) => [...studentTestingKeys.all, 'history', query] as const,
  redList: (query: RedListQuery) => [...studentTestingKeys.all, 'red-list', query, getStoredUiLanguage()] as const,
  redListSummary: () => [...studentTestingKeys.all, 'red-list-summary'] as const,
}

export const studentTestingApi = {
  async getTestingConfig() {
    return (await httpClient.get<StudentTestingConfigDto>(`${testsBase}/config`)).data
  },
  async startSubjectTest(input: StartSubjectTestRequest) {
    return (await httpClient.post<TestAttemptDto>(`${testsBase}/subject/start`, input)).data
  },
  async startMmtPractice(input: StartMmtPracticeRequest) {
    return (await httpClient.post<TestAttemptDto>(`${testsBase}/mmt-practice/start`, input)).data
  },
  async startMonthlyExam() {
    return (await httpClient.post<TestAttemptDto>(`${testsBase}/monthly-exam/start`)).data
  },
  async startRedListPractice(input: StartRedListPracticeRequest) {
    return (await httpClient.post<TestAttemptDto>(`${testsBase}/red-list/start`, input)).data
  },
  async getAttempt(attemptId: string) {
    return (await httpClient.get<TestAttemptDto>(`${testsBase}/attempts/${attemptId}`)).data
  },
  async submitAttempt(attemptId: string, input: SubmitAttemptRequest) {
    return (await httpClient.post<TestResultDto>(`${testsBase}/attempts/${attemptId}/submit`, input)).data
  },
  async checkAnswer(attemptId: string, questionId: string, input: CheckTestAnswerRequest) {
    return (await httpClient.post<CheckedTestAnswerDto>(`${testsBase}/attempts/${attemptId}/questions/${questionId}/check`, input)).data
  },
  async getAttemptResult(attemptId: string) {
    return (await httpClient.get<TestResultDto>(`${testsBase}/attempts/${attemptId}/result`)).data
  },
  async getTestingHistory(query: TestingHistoryQuery = {}) {
    return (await httpClient.get<TestingPageDto<TestHistoryItemDto>>(`${testsBase}/history${queryString(query)}`)).data
  },
  async getRedList(query: RedListQuery = {}) {
    return (await httpClient.get<TestingPageDto<RedListItemDto>>(`${redListBase}${queryString(query)}`)).data
  },
  async getRedListSummary() {
    return (await httpClient.get<RedListSummaryDto>(`${redListBase}/summary`)).data
  },
  async archiveRedListQuestion(questionId: string) {
    await httpClient.post(`${redListBase}/${questionId}/archive`)
  },
  async restoreRedListQuestion(questionId: string) {
    await httpClient.post(`${redListBase}/${questionId}/restore`)
  },
}
