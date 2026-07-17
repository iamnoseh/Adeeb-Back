import type { TFunction } from 'i18next'
import {
  VocabularyQuestionType,
  VocabularySessionMode,
  type StudentVocabularyOptionDto,
  type StudentVocabularyQuestionDto,
} from '@/features/vocabulary/model/vocabulary.types'

export const vocabularyLevels = [0, 1, 2, 3, 4, 5] as const

export function vocabularyLevelLabel(level: number) {
  return ['A1', 'A2', 'B1', 'B2', 'C1', 'C2'][level] ?? 'A1'
}

export function vocabularyModeLabel(mode: number, t: TFunction) {
  const key = String(mode)
  return t(`vocabulary.modes.${key}`)
}

export function vocabularyQuestionTypeLabel(type: number, t: TFunction) {
  const key = String(type)
  return t(`vocabulary.questionTypes.${key}`)
}

export function isOptionQuestion(question: StudentVocabularyQuestionDto) {
  return question.type !== VocabularyQuestionType.WordOrder && question.type !== VocabularyQuestionType.OddWordReplacement
}

export function shuffledOptions(options: StudentVocabularyOptionDto[]) {
  return [...options].sort((a, b) => a.displayOrder - b.displayOrder)
}

export function canShowImmediateFeedback(mode: number) {
  return mode !== VocabularySessionMode.Test
}

export function sessionProgress(answered: number, total: number) {
  if (total <= 0) return 0
  return Math.min(100, Math.round((answered / total) * 100))
}

export function tokensFromPrompt(prompt: string) {
  return prompt.split(/\s+/).filter(Boolean)
}
