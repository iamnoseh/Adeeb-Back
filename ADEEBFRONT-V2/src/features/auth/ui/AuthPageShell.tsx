import { BookOpenCheck, GraduationCap, Route, ShieldCheck, type LucideIcon } from 'lucide-react'
import type { ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { AdeebBrand } from '@/shared/ui/AdeebBrand'
import { LanguageSwitch } from '@/shared/ui/LanguageSwitch'

export function AuthPageShell({ title, subtitle, panelTitle, children, mode = 'login' }: { title: string; subtitle: string; panelTitle: string; children: ReactNode; mode?: 'login' | 'register' }) {
  const { t } = useTranslation()
  const features = mode === 'register'
    ? [
        { icon: GraduationCap, label: t('student.mmtReadiness') },
        { icon: BookOpenCheck, label: t('student.tests') },
        { icon: ShieldCheck, label: t('student.account') },
      ]
    : [
        { icon: Route, label: t('student.mmtTitle') },
        { icon: BookOpenCheck, label: t('student.learning') },
        { icon: GraduationCap, label: t('student.dailyGrowth') },
      ]

  return (
    <main className="min-h-screen bg-[#f3f5fb] p-3 sm:p-5">
      <div className="mx-auto grid min-h-[calc(100vh-1.5rem)] max-w-[1500px] overflow-hidden rounded-lg border border-white bg-white shadow-[0_24px_80px_rgb(20_31_70/0.08)] sm:min-h-[calc(100vh-2.5rem)] lg:grid-cols-[minmax(320px,0.8fr)_minmax(560px,1.2fr)]">
        <section className="relative hidden overflow-hidden border-r border-[#e8eaf2] bg-[#f8f9fd] p-10 lg:flex lg:flex-col xl:p-14">
          <AdeebBrand to="/login" />
          <div className="my-auto max-w-lg py-12">
            <h1 className="text-4xl font-black leading-tight tracking-normal text-[#111b3d] xl:text-5xl">{title}</h1>
            <p className="mt-4 max-w-md text-base leading-7 text-[#68718c]">{subtitle}</p>
            <div className="mt-9 grid gap-3">
              {features.map((feature) => <AuthFeature key={feature.label} icon={feature.icon} label={feature.label} />)}
            </div>
          </div>
          <p className="text-xs font-semibold text-[#9aa1b5]">© {new Date().getFullYear()} ADEEB</p>
        </section>

        <section className="flex min-w-0 flex-col bg-white">
          <header className="flex min-h-20 items-center justify-between gap-4 border-b border-[#eceef4] px-5 sm:px-8 lg:justify-end">
            <AdeebBrand to="/login" compact className="lg:hidden" />
            <LanguageSwitch compact tone="indigo" />
          </header>
          <div className="flex flex-1 items-center justify-center p-4 sm:p-8 xl:p-12">
            <div className={mode === 'register' ? 'w-full max-w-3xl' : 'w-full max-w-xl'}>
              <div className="mb-7 text-center sm:text-left">
                <h2 className="text-2xl font-black tracking-normal text-[#111b3d] sm:text-3xl">{panelTitle}</h2>
                <p className="mt-2 text-sm leading-6 text-[#68718c]">{subtitle}</p>
              </div>
              <div className="rounded-lg border border-[#dfe3ef] bg-white p-5 shadow-[0_18px_55px_rgb(20_31_70/0.07)] sm:p-7 lg:p-8">{children}</div>
            </div>
          </div>
        </section>
      </div>
    </main>
  )
}

function AuthFeature({ icon: Icon, label }: { icon: LucideIcon; label: string }) {
  return (
    <div className="flex min-h-16 items-center gap-4 rounded-lg border border-[#e1e4ef] bg-white px-4 shadow-[0_8px_24px_rgb(20_31_70/0.04)]">
      <span className="grid h-10 w-10 shrink-0 place-items-center rounded-lg bg-[#f0efff] text-[#5146f0]"><Icon className="h-5 w-5" aria-hidden /></span>
      <span className="text-sm font-black text-[#202a4a]">{label}</span>
    </div>
  )
}
