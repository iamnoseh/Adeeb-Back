import { useParams } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { SubjectForm } from '@/features/academic/ui/SubjectForm'
import { PageHeader } from '@/shared/ui/PageHeader'

export function SubjectFormRoute() {
  const { subjectId } = useParams()
  const { t } = useTranslation()

  return (
    <>
      <PageHeader title={subjectId ? t('editSubject') : t('newSubject')} description={t('subjectFormIntro')} />
      <SubjectForm subjectId={subjectId ?? undefined} />
    </>
  )
}
