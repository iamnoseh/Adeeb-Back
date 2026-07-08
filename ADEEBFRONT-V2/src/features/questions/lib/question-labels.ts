export function questionTypeLabel(type: number, t: (key: string) => string) {
  if (type === 1) return t('typeSingleChoice')
  if (type === 2) return t('typeMatching')
  if (type === 3) return t('typeClosedAnswer')
  return 'Unknown'
}

export function difficultyLabel(difficulty: number, t: (key: string) => string) {
  if (difficulty === 1) return t('difficultyEasy')
  if (difficulty === 2) return t('difficultyMedium')
  if (difficulty === 3) return t('difficultyHard')
  return 'Unknown'
}
