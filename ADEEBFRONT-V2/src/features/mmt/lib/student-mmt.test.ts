import { describe, expect, it } from 'vitest'
import { addAdmissionChoice, buildStudentProgramQuery, maximumAdmissionChoices, moveAdmissionChoice, normalizeAdmissionChoices } from '@/features/mmt/lib/student-mmt'

describe('student MMT choices', () => {
  it('does not add the same admission program twice', () => {
    expect(addAdmissionChoice(['program-1'], 'program-1')).toEqual({ choices: ['program-1'], error: 'duplicate' })
  })

  it('enforces the maximum of twelve choices', () => {
    const choices = Array.from({ length: maximumAdmissionChoices }, (_, index) => `program-${index + 1}`)
    expect(addAdmissionChoice(choices, 'program-13')).toEqual({ choices, error: 'limit' })
  })

  it('normalizes priority after reordering', () => {
    const reordered = moveAdmissionChoice(['one', 'two', 'three'], 2, -1)
    expect(normalizeAdmissionChoices(reordered)).toEqual([
      { admissionProgramId: 'one', priorityOrder: 1 },
      { admissionProgramId: 'three', priorityOrder: 2 },
      { admissionProgramId: 'two', priorityOrder: 3 },
    ])
  })

  it('builds a dependent server-side program query', () => {
    expect(buildStudentProgramQuery({ clusterId: 'cluster', specialtyId: 'specialty', universityId: 'university', studyLanguage: 1, search: '  medical  ' })).toEqual({
      clusterId: 'cluster', specialtyId: 'specialty', universityId: 'university', studyLanguage: 1,
      admissionType: undefined, studyForm: undefined, search: 'medical', page: 1, pageSize: 10,
    })
  })
})
