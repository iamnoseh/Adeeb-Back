export function questionTypeLabel(type: number) {
  if (type === 1) return 'Single Choice'
  if (type === 2) return 'Matching'
  if (type === 3) return 'Closed Answer'
  return 'Unknown'
}

export function difficultyLabel(difficulty: number) {
  if (difficulty === 1) return 'Easy'
  if (difficulty === 2) return 'Medium'
  if (difficulty === 3) return 'Hard'
  return 'Unknown'
}
