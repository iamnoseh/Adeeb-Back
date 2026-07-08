import { TopicsPage, TopicsPageActions } from '@/features/academic/ui/TopicsPage'
import { PageHeader } from '@/shared/ui/PageHeader'

export function TopicsRoute() {
  return (
    <>
      <PageHeader title="Мавзуъҳо" description="Мавзуъҳо ба фан пайваст мешаванд ва QuestionBank бо TopicId кор мекунад." actions={<TopicsPageActions />} />
      <TopicsPage />
    </>
  )
}
