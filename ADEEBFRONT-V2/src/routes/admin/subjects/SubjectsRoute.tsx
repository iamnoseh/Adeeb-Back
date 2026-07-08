import { SubjectsPage, SubjectsPageActions } from '@/features/academic/ui/SubjectsPage'
import { PageHeader } from '@/shared/ui/PageHeader'

export function SubjectsRoute() {
  return (
    <>
      <PageHeader title="Фанҳо" description="Фанҳо бо icon, status ва тартиби намоиш." actions={<SubjectsPageActions />} />
      <SubjectsPage />
    </>
  )
}
