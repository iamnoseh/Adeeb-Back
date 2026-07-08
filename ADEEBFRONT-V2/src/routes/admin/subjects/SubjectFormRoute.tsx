import { useParams } from 'react-router-dom'
import { SubjectForm } from '@/features/academic/ui/SubjectForm'
import { PageHeader } from '@/shared/ui/PageHeader'

export function SubjectFormRoute() {
  const { subjectId } = useParams()

  return (
    <>
      <PageHeader title={subjectId ? 'Таҳрири фан' : 'Фани нав'} description="Дар V2 ҳоло subject admin form бо Name/Icon кор мекунад; backend translations-ро худ map мекунад." />
      <SubjectForm subjectId={subjectId ?? undefined} />
    </>
  )
}
