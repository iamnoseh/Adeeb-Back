import { useTranslation } from 'react-i18next'
import { QuestionImportPage } from '@/features/questions/ui/QuestionImportPage'
import { PageHeader } from '@/shared/ui/PageHeader'

export function QuestionImportRoute() {
  const { i18n } = useTranslation()
  const isRu = i18n.language === 'ru-RU'

  return (
    <>
      <PageHeader
        title={isRu ? 'Импорт вопросов' : 'Ворид кардани саволҳо'}
        description={isRu ? 'Загрузите DOCX или текстовый PDF, проверьте вопросы и подтвердите импорт.' : 'DOCX ё PDF-и матниро бор кунед, саволҳоро санҷед ва воридкуниро тасдиқ намоед.'}
      />
      <QuestionImportPage />
    </>
  )
}
