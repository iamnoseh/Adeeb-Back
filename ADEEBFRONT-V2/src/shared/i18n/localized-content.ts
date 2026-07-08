import { languageToContentId } from '@/shared/i18n/language'

type LocalizedName = {
  language: number
  name: string
}

export function localizedName(translations: LocalizedName[], language: string, fallback: string) {
  const preferred = languageToContentId(language)
  return (
    translations.find((translation) => translation.language === preferred)?.name ||
    translations.find((translation) => translation.language === 0)?.name ||
    translations.find((translation) => translation.language === 1)?.name ||
    translations[0]?.name ||
    fallback
  )
}
