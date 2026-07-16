import { describe, expect, it } from 'vitest'
import { studentRu, studentTg } from '@/shared/i18n/locales/student'

describe('student translations', () => {
  it('keeps Tajik and Russian dictionaries aligned', () => {
    expect(Object.keys(studentRu).sort()).toEqual(Object.keys(studentTg).sort())
  })

  it('does not introduce fake economy or progress labels', () => {
    const copy = JSON.stringify({ studentRu, studentTg }).toLowerCase()
    expect(copy).not.toContain('adeebcoin')
    expect(copy).not.toContain(' xp')
    expect(copy).not.toContain('leaderboard')
  })
})
