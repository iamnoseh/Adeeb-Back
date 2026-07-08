import { QuestionImportPage } from '@/features/questions/ui/QuestionImportPage'
import { PageHeader } from '@/shared/ui/PageHeader'

export function QuestionImportRoute() {
  return (
    <>
      <PageHeader title="Import questions" description="Upload DOCX or text-based PDF files, review the parsed questions, edit them, and confirm the import." />
      <QuestionImportPage />
    </>
  )
}
