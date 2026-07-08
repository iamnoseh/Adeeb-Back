import { httpClient } from '@/shared/api/http-client'
import type { QuestionFormValues, QuestionListQuery, QuestionListResponse, QuestionResponse } from '@/features/questions/model/question.types'

export const questionKeys = {
  list: (query: QuestionListQuery = {}) => ['questions', 'list', query] as const,
  detail: (id: string) => ['questions', 'detail', id] as const,
}

export const questionsApi = {
  async list(query: QuestionListQuery = {}) {
    const response = await httpClient.get<QuestionListResponse>(`/api/v2/admin/questions${toQueryString(query)}`)
    return response.data
  },
  async detail(id: string) {
    const response = await httpClient.get<QuestionResponse>(`/api/v2/admin/questions/${id}`)
    return response.data
  },
  async create(values: QuestionFormValues) {
    const response = await httpClient.post<QuestionResponse>('/api/v2/admin/questions', toQuestionFormData(values))
    return response.data
  },
  async update(id: string, values: QuestionFormValues) {
    const response = await httpClient.put<QuestionResponse>(`/api/v2/admin/questions/${id}`, toQuestionFormData(values))
    return response.data
  },
  async archive(id: string) {
    await httpClient.post(`/api/v2/admin/questions/${id}/archive`)
  },
  async remove(id: string) {
    await httpClient.delete(`/api/v2/admin/questions/${id}`)
  },
}

function toQueryString(query: QuestionListQuery) {
  const params = new URLSearchParams()
  if (query.subjectId) params.set('subjectId', query.subjectId)
  if (query.topicId) params.set('topicId', query.topicId)
  if (query.type !== undefined) params.set('type', String(query.type))
  if (query.difficulty !== undefined) params.set('difficulty', String(query.difficulty))
  if (query.status !== undefined) params.set('status', String(query.status))
  if (query.search) params.set('search', query.search)
  params.set('page', String(query.page ?? 1))
  params.set('pageSize', String(query.pageSize ?? 20))
  if (query.sort) params.set('sort', query.sort)
  return `?${params.toString()}`
}

function toQuestionFormData(values: QuestionFormValues) {
  const body = new FormData()
  body.append('SubjectId', values.subjectId)
  if (values.topicId) body.append('TopicId', values.topicId)
  body.append('Content', values.content)
  body.append('Explanation', values.explanation)
  body.append('Type', String(values.type))
  body.append('Difficulty', String(values.difficulty))
  body.append('Status', String(values.status))

  if (values.type === 1) {
    body.append('AnswersJson', JSON.stringify(values.answers))
  }

  if (values.type === 2) {
    body.append('AnswersJson', JSON.stringify(values.matchingPairs))
  }

  if (values.type === 3) {
    body.append('CorrectAnswer', values.correctAnswer)
  }

  const image = values.image?.item(0)
  if (image) body.append('Image', image)

  return body
}
