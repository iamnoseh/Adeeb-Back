import { SubjectsPage, SubjectsPageActions } from '@/features/academic/ui/SubjectsPage'
import { PageHeader } from '@/shared/ui/PageHeader'
import { useTranslation } from 'react-i18next'

export function SubjectsRoute() {
  const { t } = useTranslation()
  return (
    <>
      <PageHeader title={t('subjectsTitle')} description={t('subjectsDescription')} actions={<SubjectsPageActions />} />
      <SubjectsPage />
    </>
  )
}
