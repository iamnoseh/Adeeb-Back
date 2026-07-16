import { ArrowUpRight, Clock3 } from 'lucide-react'
import type { ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { cn } from '@/shared/lib/cn'

export function StudentPageHeader({ title, description }: { title: string; description: string }) {
  return (
    <header className="flex flex-col gap-2 sm:flex-row sm:items-end sm:justify-between">
      <div className="min-w-0">
        <h1 className="text-2xl font-black tracking-normal text-[#111b3d] sm:text-3xl">{title}</h1>
        <p className="mt-1 max-w-3xl text-sm leading-6 text-[#68718c] sm:text-base">{description}</p>
      </div>
    </header>
  )
}

export function ComingSoonPanel({ title, description, icon, accent = 'lavender' }: { title: string; description: string; icon: ReactNode; accent?: 'lavender' | 'orange' | 'yellow' | 'navy' }) {
  const { t } = useTranslation()
  const accents = {
    lavender: 'bg-[#f0efff] text-[#5146f0]',
    orange: 'bg-[#fff2e9] text-[#f07829]',
    yellow: 'bg-[#fff7d8] text-[#ae7b00]',
    navy: 'bg-[#edf2ff] text-[#2867d8]',
  }

  return (
    <article className="flex min-h-52 flex-col rounded-lg border border-[#e1e4ef] bg-white p-5 shadow-[0_10px_28px_rgb(20_31_70/0.04)]">
      <div className="flex items-start justify-between gap-4">
        <span className={cn('grid h-11 w-11 place-items-center rounded-lg', accents[accent])}>{icon}</span>
        <ArrowUpRight className="h-4 w-4 text-[#a0a6b8]" aria-hidden />
      </div>
      <div className="mt-auto pt-8">
        <span className="inline-flex items-center gap-1.5 text-xs font-black uppercase text-[#68718c]"><Clock3 className="h-3.5 w-3.5" /> {t('student.comingSoon')}</span>
        <h2 className="mt-2 text-lg font-black tracking-normal text-[#111b3d]">{title}</h2>
        <p className="mt-2 text-sm leading-6 text-[#68718c]">{description}</p>
      </div>
    </article>
  )
}
