import { describe, expect, it } from 'vitest'
import { studentRu, studentTg } from '@/shared/i18n/locales/student'

describe('student translations', () => {
  it('keeps Tajik and Russian dictionaries aligned', () => {
    expect(keyPaths(studentRu)).toEqual(keyPaths(studentTg))
  })

  it('does not introduce fake economy or progress labels', () => {
    const copy = JSON.stringify({ studentRu, studentTg }).toLowerCase()
    expect(copy).not.toContain('adeebcoin')
    expect(copy).not.toContain(' xp')
    expect(copy).not.toContain('leaderboard')
  })
})

function keyPaths(value: object, prefix = ''): string[] {
  return Object.entries(value).flatMap(([key, child]) => {
    const path = prefix ? `${prefix}.${key}` : key
    return child && typeof child === 'object' ? keyPaths(child as object, path) : [path]
  }).sort()
}
