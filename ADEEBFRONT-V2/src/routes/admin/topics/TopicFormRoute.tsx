import { useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { TopicForm } from '@/features/academic/ui/TopicForm'
import { PageHeader } from '@/shared/ui/PageHeader'

export function TopicFormRoute() {
  const { topicId } = useParams()
  const { t } = useTranslation()

  return (
    <>
      <PageHeader title={topicId ? t('editTopic') : t('newTopic')} description={t('topicFormIntro')} />
      <TopicForm topicId={topicId ?? undefined} />
    </>
  )
}
