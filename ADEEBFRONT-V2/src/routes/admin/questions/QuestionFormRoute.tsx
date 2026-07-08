import { useParams } from 'react-router-dom'
import { QuestionForm } from '@/features/questions/ui/QuestionForm'
import { PageHeader } from '@/shared/ui/PageHeader'

export function QuestionFormRoute() {
  const { questionId } = useParams()

  return (
    <>
      <PageHeader title={questionId ? 'Таҳрири савол' : 'Саволи нав'} description="Image ҳамчун файл меравад; URL-ро backend месозад ва дар response бармегардонад." />
      <QuestionForm questionId={questionId ?? undefined} />
    </>
  )
}
