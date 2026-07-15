import { useQueryClient } from '@tanstack/react-query'
import { Languages } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { setStoredUiLanguage, type UiLanguage } from '@/shared/i18n/language'
import { cn } from '@/shared/lib/cn'

export function LanguageSwitch({ compact = false, tone = 'default' }: { compact?: boolean; tone?: 'default' | 'indigo' }) {
  const { i18n, t } = useTranslation()
  const queryClient = useQueryClient()

  async function changeLanguage(language: UiLanguage) {
    setStoredUiLanguage(language)
    await i18n.changeLanguage(language)
    await queryClient.invalidateQueries()
  }

  return (
    <div className="inline-flex items-center gap-1 rounded-lg border border-[var(--border)] bg-white p-1 shadow-sm" aria-label={t('student.language')}>
      {compact ? <Languages className="mx-1 h-4 w-4 text-[var(--muted)]" aria-hidden /> : null}
      {(['tg-TJ', 'ru-RU'] as const).map((language) => (
        <button
          key={language}
          type="button"
          className={cn(
            'min-h-8 rounded-md px-2.5 text-xs font-bold transition',
            i18n.language === language
              ? tone === 'indigo' ? 'bg-[#5146f0] text-white' : 'bg-[var(--text)] text-white'
              : tone === 'indigo' ? 'text-[#68718c] hover:bg-[#f0efff] hover:text-[#5146f0]' : 'text-[var(--muted)] hover:bg-[var(--surface-muted)] hover:text-[var(--text)]',
          )}
          onClick={() => void changeLanguage(language)}
          aria-pressed={i18n.language === language}
        >
          {language === 'tg-TJ' ? 'Тоҷ' : 'Рус'}
        </button>
      ))}
    </div>
  )
}
