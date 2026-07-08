import { httpClient } from '@/shared/api/http-client'
import { toQueryString } from '@/features/academic/lib/query'
import type { AcademicListQuery, PagedResponse, TopicFormValues, TopicResponse, TranslationRequest } from '@/features/academic/model/academic.types'

export type TopicListQuery = AcademicListQuery & {
  subjectId?: string
}

export const topicKeys = {
  list: (query: TopicListQuery = {}) => ['topics', 'list', query] as const,
  bySubject: (subjectId: string) => ['topics', 'bySubject', subjectId] as const,
}

export const topicsApi = {
  async list(query: TopicListQuery = {}) {
    const base = toQueryString(query)
    const joiner = base ? '&' : '?'
    const subject = query.subjectId ? `${joiner}subjectId=${encodeURIComponent(query.subjectId)}` : ''
    const response = await httpClient.get<PagedResponse<TopicResponse>>(`/api/v2/admin/topics${base}${subject}`)
    return response.data
  },
  async publicBySubject(subjectId: string) {
    const response = await httpClient.get<PagedResponse<TopicResponse>>(`/api/v2/subjects/${subjectId}/topics?pageSize=100`)
    return response.data
  },
  async create(values: TopicFormValues) {
    const response = await httpClient.post<TopicResponse>('/api/v2/admin/topics', toTopicRequest(values))
    return response.data
  },
  async update(id: string, values: TopicFormValues) {
    const response = await httpClient.put<TopicResponse>(`/api/v2/admin/topics/${id}`, toTopicRequest(values))
    return response.data
  },
  async archive(id: string) {
    await httpClient.post(`/api/v2/admin/topics/${id}/archive`)
  },
  async remove(id: string) {
    await httpClient.delete(`/api/v2/admin/topics/${id}`)
  },
}

function toTopicRequest(values: TopicFormValues) {
  const translations: TranslationRequest[] = [
    { language: 0, name: values.nameTg, description: values.descriptionTg || null },
    { language: 1, name: values.nameRu, description: values.descriptionRu || null },
  ]

  if (values.nameEn) {
    translations.push({ language: 2, name: values.nameEn, description: values.descriptionEn || null })
  }

    return {
    subjectId: values.subjectId,
    code: toCode(values.nameTg || values.nameRu),
    displayOrder: values.displayOrder,
    status: values.status,
    translations,
  }
}

function toCode(value: string) {
  const normalized = value
    .trim()
    .toUpperCase()
    .replace(/[^\p{L}\p{N}]+/gu, '_')
    .replace(/^_+|_+$/g, '')

  return normalized || 'TOPIC'
}
