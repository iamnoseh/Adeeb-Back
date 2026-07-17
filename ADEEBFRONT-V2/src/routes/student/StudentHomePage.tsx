import { ArrowRight, CalendarCheck2, CheckCircle2, Flame, Mail, Route, ShieldCheck, Sparkles, Swords, Trophy, UserRound } from 'lucide-react'
import type { ReactNode } from 'react'
import { useTranslation } from 'react-i18next'
import { Link } from 'react-router-dom'
import { useAuth } from '@/features/auth/model/auth-context'
import { StudentCalendar } from '@/routes/student/StudentCalendar'
import { useCurrentStudentActivity } from '@/features/student-activity/model/useStudentActivity'

export function StudentHomePage() {
  const { user } = useAuth()
  const { t } = useTranslation()
  const name = user?.firstName || t('student.profile')
  const initials = `${user?.firstName?.[0] ?? 'A'}${user?.lastName?.[0] ?? ''}`.toUpperCase()
  const activity = useCurrentStudentActivity()
  const modules = [
    { title: t('student.tests'), to: '/student/tests', icon: <ShieldCheck />, tone: 'purple' as const },
    { title: t('student.duel'), to: '/student/duels', icon: <Swords />, tone: 'blue' as const },
    { title: t('student.mmt'), to: '/student/mmt', icon: <Route />, tone: 'orange' as const },
  ]

  return (
    <div className="grid gap-5 xl:grid-cols-[minmax(0,1fr)_300px]">
      <div className="grid min-w-0 gap-5">
        <section className="flex flex-col gap-5 rounded-lg border border-[#e1e4ef] bg-white p-5 shadow-[0_10px_28px_rgb(20_31_70/0.04)] sm:flex-row sm:items-center sm:p-6">
          <span className="grid h-24 w-24 shrink-0 place-items-center rounded-full bg-[#f0efff] text-3xl font-black text-[#5146f0] ring-8 ring-[#faf9ff]">{initials}</span>
          <div className="min-w-0 flex-1">
            <h1 className="text-xl font-black tracking-normal text-[#111b3d] sm:text-2xl">{t('student.greeting', { name })}</h1>
            <p className="mt-2 max-w-2xl text-sm leading-6 text-[#68718c]">{t('student.heroSubtitle')}</p>
            <div className="mt-4 flex flex-wrap gap-x-6 gap-y-2 text-sm text-[#68718c]">
              <span className="inline-flex min-w-0 items-center gap-2"><Mail className="h-4 w-4 text-[#5146f0]" /><span className="truncate">{user?.email || t('student.noValue')}</span></span>
            </div>
          </div>
          <span className="inline-flex shrink-0 items-center gap-2 rounded-lg bg-[#effaf1] px-3 py-2 text-xs font-black text-[#2c9b4b]"><CheckCircle2 className="h-4 w-4" />{t('student.signedInAs')}</span>
        </section>

        <section className="grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
          <OverviewCard icon={<Flame />} label={t('student.currentStreak')} value={activity.data ? t('student.daysCount', { count: activity.data.currentStreak }) : t('student.loading')} tone="orange" />
          <OverviewCard icon={<Route />} label={t('student.mmtReadiness')} value={t('student.notActiveYet')} tone="blue" />
          <OverviewCard icon={<UserRound />} label={t('student.account')} value={t('student.openProfile')} tone="orange" />
          <OverviewCard icon={<Sparkles />} label={t('student.achievements')} value={t('student.noValue')} tone="green" />
        </section>

        <section className="grid gap-4 lg:grid-cols-[0.85fr_1.15fr]">
          <article className="rounded-lg border border-[#e1e4ef] bg-white p-5 shadow-[0_10px_28px_rgb(20_31_70/0.04)]">
            <div className="flex items-center justify-between gap-3"><h2 className="text-base font-black tracking-normal">{t('student.mmtReadiness')}</h2><Route className="h-5 w-5 text-[#5146f0]" /></div>
            <div className="mt-8 rounded-lg bg-[#f7f6ff] p-5">
              <p className="text-sm font-black text-[#5146f0]">{t('student.startAdmissionPath')}</p>
              <p className="mt-2 text-sm leading-6 text-[#68718c]">{t('student.mmtPreparing')}</p>
              <Link to="/student/mmt" className="mt-5 inline-flex min-h-10 w-full items-center justify-center gap-2 rounded-lg bg-[#5146f0] px-4 text-sm font-black text-white no-underline hover:bg-[#4338dc]">{t('student.startAdmissionPath')}<ArrowRight className="h-4 w-4" /></Link>
            </div>
          </article>

          <article className="rounded-lg border border-[#e1e4ef] bg-white shadow-[0_10px_28px_rgb(20_31_70/0.04)]">
            <div className="border-b border-[#eceef4] px-5 py-4"><h2 className="text-base font-black tracking-normal">{t('student.learning')}</h2><p className="mt-1 text-xs text-[#68718c]">{t('student.learningPreparing')}</p></div>
            <div className="grid gap-px bg-[#eceef4] sm:grid-cols-3">
              {modules.map((module) => <ModuleCard key={module.to} {...module} status={t('student.comingSoon')} />)}
            </div>
          </article>
        </section>

        <section className="rounded-lg border border-[#e1e4ef] bg-white p-5 shadow-[0_10px_28px_rgb(20_31_70/0.04)]">
          <div className="flex items-center justify-between gap-4"><div><h2 className="text-base font-black tracking-normal">{t('student.openLearning')}</h2><p className="mt-1 text-xs text-[#68718c]">{t('student.learningDescription')}</p></div><Link to="/student/learning" className="text-sm font-black text-[#5146f0] no-underline">{t('student.openLearning')}</Link></div>
          <div className="mt-5 grid gap-3 sm:grid-cols-3">{modules.map((module) => <ContinueCard key={module.to} {...module} status={t('student.notActiveYet')} />)}</div>
        </section>
      </div>

      <aside className="grid content-start gap-4">
        <StudentCalendar />
        <section className="overflow-hidden rounded-lg border border-[var(--student-border)] bg-[var(--student-surface)] shadow-[0_14px_36px_rgb(20_31_70/0.06)]">
          <div className="border-b border-[var(--student-border)] bg-gradient-to-br from-[#fbfcff] via-white to-[#f7f8ff] p-5">
            <span className="grid h-10 w-10 place-items-center rounded-lg bg-[#f0efff] text-[#5146f0] shadow-[0_8px_20px_rgb(81_70_240/0.12)]">
              <Sparkles className="h-5 w-5" />
            </span>
            <h2 className="mt-3 text-base font-black tracking-normal text-[var(--student-text)]">{t('student.recentActivity')}</h2>
          </div>
          {activity.data ? (
            <div className="grid gap-2 p-5">
              <ActivityRow icon={<CalendarCheck2 />} label={t('student.activeThisMonth')} value={activity.data.activeDaysInMonth} tone="blue" />
              <ActivityRow icon={<Trophy />} label={t('student.longestStreak')} value={activity.data.longestStreak} tone="orange" />
              <ActivityRow icon={<Sparkles />} label={t('student.totalActiveDays')} value={activity.data.totalActiveDays} tone="purple" />
            </div>
          ) : <div className="m-5 h-40 animate-pulse rounded-lg bg-[var(--student-surface-soft)]" />}
        </section>
      </aside>
    </div>
  )
}

const tones = {
  purple: 'bg-[#f0efff] text-[#5146f0]',
  blue: 'bg-[#edf5ff] text-[#2b78db]',
  orange: 'bg-[#fff3e7] text-[#e98a16]',
  green: 'bg-[#eaf8ee] text-[#2fa451]',
}

function OverviewCard({ icon, label, value, tone }: { icon: ReactNode; label: string; value: string; tone: keyof typeof tones }) {
  return <article className="flex min-h-24 items-center gap-3 rounded-lg border border-[#e1e4ef] bg-white p-4 shadow-[0_8px_24px_rgb(20_31_70/0.035)]"><span className={`grid h-11 w-11 shrink-0 place-items-center rounded-lg [&>svg]:h-5 [&>svg]:w-5 ${tones[tone]}`}>{icon}</span><span className="min-w-0"><small className="block truncate font-bold text-[#68718c]">{label}</small><strong className="mt-1 block truncate text-sm text-[#111b3d]">{value}</strong></span></article>
}

function ModuleCard({ title, to, icon, tone, status }: { title: string; to: string; icon: ReactNode; tone: keyof typeof tones; status: string }) {
  return <Link to={to} className="min-h-40 bg-white p-4 text-[#111b3d] no-underline hover:bg-[#fafaff]"><span className={`grid h-10 w-10 place-items-center rounded-lg [&>svg]:h-5 [&>svg]:w-5 ${tones[tone]}`}>{icon}</span><strong className="mt-7 block text-sm">{title}</strong><small className="mt-2 block text-[#858da5]">{status}</small></Link>
}

function ContinueCard({ title, to, icon, tone, status }: { title: string; to: string; icon: ReactNode; tone: keyof typeof tones; status: string }) {
  return <Link to={to} className="flex min-h-20 items-center gap-3 rounded-lg border border-[#e5e8f1] p-3 text-[#111b3d] no-underline hover:border-[#cbc7ff]"><span className={`grid h-10 w-10 shrink-0 place-items-center rounded-lg [&>svg]:h-5 [&>svg]:w-5 ${tones[tone]}`}>{icon}</span><span className="min-w-0"><strong className="block truncate text-sm">{title}</strong><small className="mt-1 block truncate text-[#858da5]">{status}</small></span></Link>
}

function ActivityRow({ icon, label, value, tone }: { icon: ReactNode; label: string; value: number; tone: 'blue' | 'orange' | 'purple' }) {
  const toneClass = {
    blue: 'bg-[#edf5ff] text-[#2b78db]',
    orange: 'bg-[#fff3e7] text-[#d97706]',
    purple: 'bg-[#f0efff] text-[#5146f0]',
  }[tone]

  return (
    <div className="flex min-h-16 items-center gap-3 rounded-lg border border-[var(--student-border)] bg-[var(--student-surface-soft)] p-3 transition hover:-translate-y-0.5 hover:bg-[var(--student-surface)] hover:shadow-[0_10px_22px_rgb(20_31_70/0.06)]">
      <span className={`grid h-10 w-10 shrink-0 place-items-center rounded-lg shadow-sm [&>svg]:h-4 [&>svg]:w-4 ${toneClass}`}>{icon}</span>
      <span className="min-w-0 flex-1 text-sm font-black leading-5 text-[var(--student-muted)]">{label}</span>
      <strong className="text-xl font-black text-[var(--student-text)]">{value}</strong>
    </div>
  )
}
