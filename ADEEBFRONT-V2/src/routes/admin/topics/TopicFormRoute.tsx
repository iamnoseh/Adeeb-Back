import { useParams } from 'react-router-dom'
import { TopicForm } from '@/features/academic/ui/TopicForm'
import { PageHeader } from '@/shared/ui/PageHeader'

export function TopicFormRoute() {
  const { topicId } = useParams()

  return (
    <>
      <PageHeader title={topicId ? 'Таҳрири мавзуъ' : 'Мавзуи нав'} description="Мавзуъҳо бо SubjectId пайваст мешаванд; Question form дигар topic string намефиристад." />
      <TopicForm topicId={topicId ?? undefined} />
    </>
  )
}
