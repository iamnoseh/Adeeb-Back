import { BookOpenCheck, Languages, Layers3, Swords } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import { ComingSoonPanel, StudentPageHeader } from '@/routes/student/StudentUi'

export function StudentLearningPage() {
  const { t } = useTranslation()
  const modules = [
    { title: t('student.tests'), icon: <BookOpenCheck className="h-6 w-6" />, accent: 'orange' as const },
    { title: t('student.flashcards'), icon: <Layers3 className="h-6 w-6" />, accent: 'yellow' as const },
    { title: t('student.duel'), icon: <Swords className="h-6 w-6" />, accent: 'navy' as const },
  ]
  return (
    <div className="grid gap-6">
      <StudentPageHeader title={t('student.learningTitle')} description={t('student.learningDescription')} />
      <Link to="/student/vocabulary" className="group grid gap-4 rounded-lg border border-[#dedbff] bg-white p-5 text-[#111b3d] no-underline shadow-[0_12px_34px_rgb(81_70_240/0.08)] transition hover:-translate-y-0.5 hover:shadow-[0_18px_42px_rgb(81_70_240/0.12)] sm:grid-cols-[auto_minmax(0,1fr)_auto] sm:items-center">
        <span className="grid h-14 w-14 place-items-center rounded-lg bg-[#f0efff] text-[#5146f0]"><Languages className="h-7 w-7" /></span>
        <span className="min-w-0">
          <strong className="block text-lg font-black">{t('vocabulary.title')}</strong>
          <span className="mt-1 block text-sm leading-6 text-[#68718c]">{t('vocabulary.description')}</span>
        </span>
        <span className="inline-flex min-h-10 items-center justify-center rounded-lg bg-[#5146f0] px-4 text-sm font-black text-white">{t('student.openLearning')}</span>
      </Link>
      <section className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        {modules.map((module) => <ComingSoonPanel key={module.title} accent={module.accent} icon={module.icon} title={module.title} description={t('student.unavailableDescription')} />)}
      </section>
    </div>
  )
}
