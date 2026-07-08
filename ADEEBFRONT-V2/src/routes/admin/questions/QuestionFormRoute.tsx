import { useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { QuestionForm } from '@/features/questions/ui/QuestionForm'
import { PageHeader } from '@/shared/ui/PageHeader'

export function QuestionFormRoute() {
  const { questionId } = useParams()
  const { t } = useTranslation()

  return (
    <>
      <PageHeader title={questionId ? t('editQuestion') : t('newQuestion')} />
      <QuestionForm questionId={questionId ?? undefined} />
    </>
  )
}
