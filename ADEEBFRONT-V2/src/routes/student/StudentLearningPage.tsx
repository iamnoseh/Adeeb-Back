import { BookOpenCheck, Languages, Layers3, Swords } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { ComingSoonPanel, StudentPageHeader } from '@/routes/student/StudentUi'

export function StudentLearningPage() {
  const { t } = useTranslation()
  const modules = [
    { title: t('student.tests'), icon: <BookOpenCheck className="h-6 w-6" />, accent: 'orange' as const },
    { title: t('student.vocabulary'), icon: <Languages className="h-6 w-6" />, accent: 'lavender' as const },
    { title: t('student.flashcards'), icon: <Layers3 className="h-6 w-6" />, accent: 'yellow' as const },
    { title: t('student.duel'), icon: <Swords className="h-6 w-6" />, accent: 'navy' as const },
  ]
  return (
    <div className="grid gap-6">
      <StudentPageHeader title={t('student.learningTitle')} description={t('student.learningDescription')} />
      <section className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        {modules.map((module) => <ComingSoonPanel key={module.title} accent={module.accent} icon={module.icon} title={module.title} description={t('student.unavailableDescription')} />)}
      </section>
    </div>
  )
}
