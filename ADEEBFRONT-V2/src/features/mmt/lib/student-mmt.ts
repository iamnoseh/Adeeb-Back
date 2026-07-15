import type { AdmissionChoiceInput, AdmissionProgramQuery } from '@/features/mmt/model/mmt.types'

export const maximumAdmissionChoices = 12

export type AddAdmissionChoiceResult = {
  choices: string[]
  error: 'duplicate' | 'limit' | null
}

export function addAdmissionChoice(choices: string[], admissionProgramId: string): AddAdmissionChoiceResult {
  if (choices.includes(admissionProgramId)) return { choices, error: 'duplicate' }
  if (choices.length >= maximumAdmissionChoices) return { choices, error: 'limit' }
  return { choices: [...choices, admissionProgramId], error: null }
}

export function moveAdmissionChoice(choices: string[], index: number, direction: -1 | 1) {
  const target = index + direction
  if (index < 0 || index >= choices.length || target < 0 || target >= choices.length) return choices
  const next = [...choices]
  ;[next[index], next[target]] = [next[target]!, next[index]!]
  return next
}

export function normalizeAdmissionChoices(choices: string[]): AdmissionChoiceInput[] {
  return choices.map((admissionProgramId, index) => ({ admissionProgramId, priorityOrder: index + 1 }))
}

export function buildStudentProgramQuery(input: {
  clusterId: string
  specialtyId: string
  universityId: string
  admissionType?: number | undefined
  studyForm?: number | undefined
  studyLanguage?: number | undefined
  search?: string | undefined
  page?: number | undefined
}): AdmissionProgramQuery {
  return {
    clusterId: input.clusterId,
    specialtyId: input.specialtyId,
    universityId: input.universityId,
    admissionType: input.admissionType,
    studyForm: input.studyForm,
    studyLanguage: input.studyLanguage,
    search: input.search?.trim() || undefined,
    page: input.page ?? 1,
    pageSize: 10,
  }
}
