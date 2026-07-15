import { describe, expect, it } from 'vitest'
import { authenticatedHome, isAdminRole, isStudentRole } from '@/features/auth/lib/auth-role'

describe('auth role routing', () => {
  it.each(['Admin', 'SuperAdmin'])('routes %s to admin', (role) => {
    expect(isAdminRole(role)).toBe(true)
    expect(authenticatedHome(role)).toBe('/admin')
  })

  it.each(['User', 'Student'])('routes %s to student', (role) => {
    expect(isStudentRole(role)).toBe(true)
    expect(authenticatedHome(role)).toBe('/student')
  })

  it('uses the student shell for authenticated non-admin roles', () => {
    expect(authenticatedHome('Learner')).toBe('/student')
  })
})
