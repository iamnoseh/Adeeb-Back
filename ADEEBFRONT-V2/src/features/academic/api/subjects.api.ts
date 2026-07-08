import { httpClient } from '@/shared/api/http-client'
import { toQueryString } from '@/features/academic/lib/query'
import type { AcademicListQuery, PagedResponse, SubjectFormValues, SubjectResponse } from '@/features/academic/model/academic.types'

export const subjectKeys = {
  list: (query: AcademicListQuery = {}) => ['subjects', 'list', query] as const,
  publicList: (query: AcademicListQuery = {}) => ['subjects', 'public-list', query] as const,
  detail: (id: string) => ['subjects', 'detail', id] as const,
}

export const subjectsApi = {
  async list(query: AcademicListQuery = {}) {
    const response = await httpClient.get<PagedResponse<SubjectResponse>>(`/api/v2/admin/subjects${toQueryString(query)}`)
    return response.data
  },
  async publicList(query: AcademicListQuery = {}) {
    const response = await httpClient.get<PagedResponse<SubjectResponse>>(`/api/v2/subjects${toQueryString(query)}`)
    return response.data
  },
  async detail(id: string) {
    const response = await httpClient.get<SubjectResponse>(`/api/v2/admin/subjects/${id}`)
    return response.data
  },
  async create(values: SubjectFormValues) {
    const body = toSubjectFormData(values)
    const response = await httpClient.post<SubjectResponse>('/api/v2/admin/subjects', body)
    return response.data
  },
  async update(id: string, values: SubjectFormValues) {
    const body = toSubjectFormData(values)
    const response = await httpClient.put<SubjectResponse>(`/api/v2/admin/subjects/${id}`, body)
    return response.data
  },
  async archive(id: string) {
    await httpClient.post(`/api/v2/admin/subjects/${id}/archive`)
  },
  async remove(id: string) {
    await httpClient.delete(`/api/v2/admin/subjects/${id}`)
  },
}

function toSubjectFormData(values: SubjectFormValues) {
  const body = new FormData()
  body.append('NameTg', values.nameTg)
  body.append('NameRu', values.nameRu)
  if (values.nameEn) body.append('NameEn', values.nameEn)
  if (values.descriptionTg) body.append('DescriptionTg', values.descriptionTg)
  if (values.descriptionRu) body.append('DescriptionRu', values.descriptionRu)
  if (values.descriptionEn) body.append('DescriptionEn', values.descriptionEn)
  body.append('Status', String(values.status))
  body.append('DisplayOrder', String(values.displayOrder))

  const icon = values.icon?.item(0)
  if (icon) body.append('Icon', icon)

  return body
}
