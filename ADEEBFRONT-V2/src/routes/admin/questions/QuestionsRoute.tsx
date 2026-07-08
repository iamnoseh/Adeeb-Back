import { QuestionsPage, QuestionsPageActions } from '@/features/questions/ui/QuestionsPage'
import { PageHeader } from '@/shared/ui/PageHeader'
import { useTranslation } from 'react-i18next'

export function QuestionsRoute() {
  const { t } = useTranslation()
  return (
    <>
      <PageHeader title={t('questionsTitle')} description={t('questionsDescription')} actions={<QuestionsPageActions />} />
      <QuestionsPage />
    </>
  )
}
