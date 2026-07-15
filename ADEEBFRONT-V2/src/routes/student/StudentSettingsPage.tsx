import { Languages, Moon, Sun } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { cn } from '@/shared/lib/cn'
import { LanguageSwitch } from '@/shared/ui/LanguageSwitch'
import { useStudentPreferences, type StudentTheme } from '@/routes/student/student-preferences-context'

export function StudentSettingsPage() {
  const { t } = useTranslation()
  const { theme, setTheme } = useStudentPreferences()
  return (
    <div className="grid max-w-3xl gap-5">
      <h1 className="text-2xl font-black tracking-normal text-[var(--student-text)] sm:text-3xl">{t('student.settings')}</h1>
      <section className="student-card rounded-lg border p-5 shadow-[0_10px_28px_rgb(20_31_70/0.04)] sm:p-6">
        <div className="flex items-start gap-4">
          <span className="grid h-11 w-11 shrink-0 place-items-center rounded-lg bg-[#f0efff] text-[#5146f0]"><Languages className="h-5 w-5" /></span>
          <div className="min-w-0 flex-1"><h2 className="text-base font-black tracking-normal">{t('student.languageSettings')}</h2><div className="mt-4"><LanguageSwitch tone="indigo" /></div></div>
        </div>
      </section>
      <section className="student-card rounded-lg border p-5 shadow-[0_10px_28px_rgb(20_31_70/0.04)] sm:p-6">
        <h2 className="text-base font-black tracking-normal">{t('student.appearance')}</h2>
        <p className="mt-2 text-sm leading-6 text-[var(--student-muted)]">{t('student.appearanceDescription')}</p>
        <div className="mt-5 grid gap-3 sm:grid-cols-2">
          <ThemeButton theme="light" active={theme === 'light'} icon={Sun} label={t('student.lightMode')} onSelect={setTheme} />
          <ThemeButton theme="dark" active={theme === 'dark'} icon={Moon} label={t('student.darkMode')} onSelect={setTheme} />
        </div>
      </section>
    </div>
  )
}

function ThemeButton({ theme, active, icon: Icon, label, onSelect }: { theme: StudentTheme; active: boolean; icon: typeof Sun; label: string; onSelect: (theme: StudentTheme) => void }) {
  return <button type="button" className={cn('flex min-h-20 items-center gap-4 rounded-lg border px-4 text-left transition', active ? 'border-[#5146f0] bg-[#f0efff] text-[#5146f0]' : 'border-[var(--student-border)] bg-[var(--student-surface-soft)] text-[var(--student-text)] hover:border-[#aaa4ff]')} onClick={() => onSelect(theme)} aria-pressed={active}><span className="grid h-10 w-10 place-items-center rounded-lg bg-[var(--student-surface)] shadow-sm"><Icon className="h-5 w-5" /></span><strong>{label}</strong></button>
}
