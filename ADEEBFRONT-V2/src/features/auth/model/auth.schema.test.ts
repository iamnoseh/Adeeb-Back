import { describe, expect, it } from 'vitest'
import { createRegisterSchema } from '@/features/auth/model/auth.schema'

const schema = createRegisterSchema((key) => key)
const valid = {
  firstName: 'Фирӯз',
  lastName: 'Раҳмон',
  email: 'student@adeeb.tj',
  phoneNumber: '+992 900 00 00 00',
  password: 'Strong123',
  confirmPassword: 'Strong123',
}

describe('student registration schema', () => {
  it('accepts a backend-compatible registration request', () => {
    expect(schema.safeParse(valid).success).toBe(true)
  })

  it('allows registration without a phone number', () => {
    expect(schema.safeParse({ ...valid, phoneNumber: '' }).success).toBe(true)
  })

  it('enforces password policy and confirmation', () => {
    expect(schema.safeParse({ ...valid, password: 'weak', confirmPassword: 'different' }).success).toBe(false)
  })

  it('rejects phone numbers outside the backend length limits', () => {
    expect(schema.safeParse({ ...valid, phoneNumber: '123' }).success).toBe(false)
  })
})
