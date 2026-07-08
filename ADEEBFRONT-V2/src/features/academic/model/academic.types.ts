export type PagedResponse<T> = {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
}

export type TranslationResponse = {
  language: number
  name: string
  description?: string | null
}

export type TranslationRequest = {
  language: number
  name: string
  description?: string | null
}

export type AcademicListQuery = {
  search?: string
  status?: number
  page?: number
  pageSize?: number
  sort?: string
}

export type SubjectResponse = {
  id: string
  code: string
  name: string
  iconUrl?: string | null
  displayOrder: number
  status: number
  translations: TranslationResponse[]
}

export type SubjectFormValues = {
  nameTg: string
  nameRu: string
  nameEn: string
  descriptionTg: string
  descriptionRu: string
  descriptionEn: string
  status: number
  displayOrder: number
  icon?: FileList
}

export type TopicResponse = {
  id: string
  subjectId: string
  code: string
  name: string
  displayOrder: number
  status: number
  translations: TranslationResponse[]
}

export type TopicFormValues = {
  subjectId: string
  displayOrder: number
  status: number
  nameTg: string
  nameRu: string
  nameEn: string
  descriptionTg: string
  descriptionRu: string
  descriptionEn: string
}
