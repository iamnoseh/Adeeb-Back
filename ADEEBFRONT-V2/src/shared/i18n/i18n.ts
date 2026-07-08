import i18n from 'i18next'
import { initReactI18next } from 'react-i18next'
import { tgTJ } from '@/shared/i18n/locales/tg-TJ'
import { ruRU } from '@/shared/i18n/locales/ru-RU'
import { getStoredUiLanguage } from '@/shared/i18n/language'

void i18n.use(initReactI18next).init({
  lng: getStoredUiLanguage(),
  fallbackLng: 'tg-TJ',
  interpolation: {
    escapeValue: false,
  },
  resources: {
    'tg-TJ': {
      translation: tgTJ,
    },
    'ru-RU': {
      translation: ruRU,
    },
  },
})

export { i18n }
