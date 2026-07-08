import type { TranslationResponse } from '@/features/academic/model/academic.types'
import { Badge } from '@/shared/ui/Badge'

export function TranslationBadges({ translations }: { translations: TranslationResponse[] }) {
  const languages = new Set(translations.map((item) => item.language))
  return (
    <div className="flex flex-wrap gap-1">
      <Badge tone={languages.has(0) ? 'success' : 'warning'}>TJ {languages.has(0) ? '✓' : '!'}</Badge>
      <Badge tone={languages.has(1) ? 'success' : 'warning'}>RU {languages.has(1) ? '✓' : '!'}</Badge>
      <Badge tone={languages.has(2) ? 'success' : 'neutral'}>EN {languages.has(2) ? '✓' : '-'}</Badge>
    </div>
  )
}
