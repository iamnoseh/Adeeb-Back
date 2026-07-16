import i18n from "i18next";
import { initReactI18next } from "react-i18next";
import { tgTJ } from "@/shared/i18n/locales/tg-TJ";
import { ruRU } from "@/shared/i18n/locales/ru-RU";
import { mmtRu, mmtTg } from "@/shared/i18n/locales/mmt";
import { studentRu, studentTg } from "@/shared/i18n/locales/student";
import { getStoredUiLanguage } from "@/shared/i18n/language";

void i18n.use(initReactI18next).init({
  lng: getStoredUiLanguage(),
  fallbackLng: "tg-TJ",
  interpolation: {
    escapeValue: false,
  },
  resources: {
    "tg-TJ": {
      translation: { ...tgTJ, mmt: mmtTg, student: studentTg },
    },
    "ru-RU": {
      translation: { ...ruRU, mmt: mmtRu, student: studentRu },
    },
  },
});

function syncDocumentLanguage(language: string) {
  document.documentElement.lang = language === "ru-RU" ? "ru" : "tg";
}

syncDocumentLanguage(i18n.language);
i18n.on("languageChanged", syncDocumentLanguage);

export { i18n };
