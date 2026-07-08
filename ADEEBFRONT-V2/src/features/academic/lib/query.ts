import type { AcademicListQuery } from '@/features/academic/model/academic.types'

export function toQueryString(query: AcademicListQuery = {}) {
  const params = new URLSearchParams()
  if (query.search) params.set('search', query.search)
  if (query.status !== undefined) params.set('status', String(query.status))
  if (query.page !== undefined) params.set('page', String(query.page))
  if (query.pageSize !== undefined) params.set('pageSize', String(query.pageSize))
  if (query.sort) params.set('sort', query.sort)
  const value = params.toString()
  return value ? `?${value}` : ''
}
