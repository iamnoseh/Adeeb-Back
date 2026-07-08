import { TopicsPage, TopicsPageActions } from '@/features/academic/ui/TopicsPage'
import { PageHeader } from '@/shared/ui/PageHeader'
import { useTranslation } from 'react-i18next'

export function TopicsRoute() {
  const { t } = useTranslation()
  return (
    <>
      <PageHeader title={t('topicsTitle')} description={t('topicsDescription')} actions={<TopicsPageActions />} />
      <TopicsPage />
    </>
  )
}
