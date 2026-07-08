export type UiLanguage = 'tg-TJ' | 'ru-RU'

const storageKey = 'adeeb.uiLanguage'

export function getStoredUiLanguage(): UiLanguage {
  if (typeof window === 'undefined') return 'tg-TJ'
  const value = window.localStorage.getItem(storageKey)
  return value === 'ru-RU' ? 'ru-RU' : 'tg-TJ'
}

export function setStoredUiLanguage(language: UiLanguage) {
  window.localStorage.setItem(storageKey, language)
}

export function languageToContentId(language: string) {
  return language === 'ru-RU' ? 1 : 0
}
