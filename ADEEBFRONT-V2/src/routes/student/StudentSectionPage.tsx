import type { LucideIcon } from 'lucide-react'
import { Clock3 } from 'lucide-react'
import { useTranslation } from 'react-i18next'

export function StudentSectionPage({ titleKey, icon: Icon }: { titleKey: string; icon: LucideIcon }) {
  const { t } = useTranslation()
  return (
    <div className="grid gap-5">
      <h1 className="text-2xl font-black tracking-normal text-[#111b3d] sm:text-3xl">{t(titleKey)}</h1>
      <section className="min-h-[28rem] rounded-lg border border-[#e1e4ef] bg-white p-6 shadow-[0_10px_28px_rgb(20_31_70/0.04)]">
        <span className="grid h-12 w-12 place-items-center rounded-lg bg-[#f0efff] text-[#5146f0]"><Icon className="h-6 w-6" /></span>
        <div className="mt-24 max-w-lg">
          <span className="inline-flex items-center gap-2 text-xs font-black uppercase text-[#5146f0]"><Clock3 className="h-4 w-4" />{t('student.comingSoon')}</span>
          <h2 className="mt-3 text-2xl font-black tracking-normal text-[#111b3d]">{t(titleKey)}</h2>
          <p className="mt-3 leading-7 text-[#68718c]">{t('student.sectionUnavailable')}</p>
        </div>
      </section>
    </div>
  )
}
