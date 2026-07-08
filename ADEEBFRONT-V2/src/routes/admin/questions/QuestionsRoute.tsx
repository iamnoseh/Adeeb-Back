import { QuestionsPage, QuestionsPageActions } from '@/features/questions/ui/QuestionsPage'
import { PageHeader } from '@/shared/ui/PageHeader'

export function QuestionsRoute() {
  return (
    <>
      <PageHeader title="Бонки саволҳо" description="Идоракунии Single Choice, Matching ва Closed Answer." actions={<QuestionsPageActions />} />
      <QuestionsPage />
    </>
  )
}
