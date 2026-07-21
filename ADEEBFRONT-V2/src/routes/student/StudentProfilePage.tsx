import { LogOut, Mail, Phone, ShieldCheck, Sparkles, Trophy, UserRound } from 'lucide-react'
import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { useAuth } from '@/features/auth/model/auth-context'
import { Button } from '@/shared/ui/Button'
import { StudentPageHeader } from '@/routes/student/StudentUi'
import { leagueKeys, progressionStudentApi } from '@/features/progression/api/league.api'

export function StudentProfilePage() {
  const { user, logout } = useAuth()
  const { i18n, t } = useTranslation()
  const language = i18n.language === 'ru-RU' ? t('languageRu') : t('languageTg')
  const initials = `${user?.firstName?.[0] ?? 'A'}${user?.lastName?.[0] ?? ''}`.toUpperCase()
  const progression = useQuery({ queryKey: leagueKeys.overview(), queryFn: progressionStudentApi.overview, retry: false })
  return (
    <div className="grid gap-6">
      <StudentPageHeader title={t('student.profileTitle')} description={t('student.profileDescription')} />
      <section className="grid gap-4 xl:grid-cols-[18rem_minmax(0,1fr)]">
        <article className="flex flex-col rounded-lg border border-[#ddd9ff] bg-[#f7f6ff] p-6 text-[#111b3d] shadow-[0_10px_28px_rgb(20_31_70/0.04)]">
          <span className="grid h-20 w-20 place-items-center rounded-full bg-[#e9e7ff] text-2xl font-black text-[#5146f0] ring-8 ring-white">{initials}</span>
          <h2 className="mt-6 text-xl font-black tracking-normal">{user?.firstName} {user?.lastName}</h2>
          <div className="mt-8 grid gap-3 border-t border-[#e1defd] pt-6 text-sm">
            <ProfileSummary icon={<Mail />} value={user?.email || t('student.noValue')} />
            <ProfileSummary icon={<Phone />} value={user?.phoneNumber || t('student.noValue')} />
          </div>
        </article>

        <article className="rounded-lg border border-[#e1e4ef] bg-white p-5 shadow-[0_10px_28px_rgb(20_31_70/0.04)] sm:p-7">
          <div className="flex items-center gap-3 border-b border-[#e7e8ee] pb-5">
            <span className="grid h-10 w-10 place-items-center rounded-lg bg-[#f0efff] text-[#5146f0]"><UserRound className="h-5 w-5" /></span>
            <h2 className="text-lg font-black tracking-normal">{t('student.accountSummary')}</h2>
          </div>
          <dl className="mt-6 grid gap-x-8 gap-y-5 sm:grid-cols-2">
            <ProfileField label={t('student.firstName')} value={user?.firstName} fallback={t('student.noValue')} />
            <ProfileField label={t('student.lastName')} value={user?.lastName} fallback={t('student.noValue')} />
            <ProfileField label={t('student.email')} value={user?.email} fallback={t('student.noValue')} />
            <ProfileField label={t('student.phone')} value={user?.phoneNumber} fallback={t('student.noValue')} />
            <ProfileField label={t('student.currentLanguage')} value={language} fallback={t('student.noValue')} />
          </dl>
          <div className="mt-7 flex flex-col items-start justify-between gap-4 rounded-lg bg-[#f6f7fb] p-4 sm:flex-row sm:items-center">
            <p className="flex items-start gap-2 text-sm leading-6 text-[#68718c]"><ShieldCheck className="mt-0.5 h-4 w-4 shrink-0 text-[#5146f0]" />{t('student.profileReadOnly')}</p>
            <Button className="shrink-0 rounded-lg border-0 !bg-[#5146f0] !bg-none text-white hover:!bg-[#4338dc]" variant="secondary" onClick={() => void logout()}><LogOut className="h-4 w-4" /> {t('logout')}</Button>
          </div>
        </article>
      </section>
      {progression.data ? <section className="grid gap-3 sm:grid-cols-3"><ProgressCard icon={<Sparkles />} label={t('progression.lifetimeXp')} value={`${progression.data.lifetimeXp} XP`} /><ProgressCard icon={<Trophy />} label={t('progression.globalRank')} value={progression.data.globalRank ? `#${progression.data.globalRank}` : '—'} /><Link to="/student/league" className="no-underline"><ProgressCard icon={<Trophy />} label={t('progression.currentLeague')} value={progression.data.league?.name ?? '—'} /></Link></section> : null}
    </div>
  )
}

function ProgressCard({ icon, label, value }: { icon: React.ReactNode; label: string; value: string }) {
  return <article className="flex min-h-24 items-center gap-3 rounded-lg border border-[#e1e4ef] bg-white p-4 shadow-sm"><span className="grid h-10 w-10 shrink-0 place-items-center rounded-lg bg-[#f0efff] text-[#5146f0] [&>svg]:h-5 [&>svg]:w-5">{icon}</span><div className="min-w-0"><p className="text-xs font-bold text-[#7b8299]">{label}</p><p className="mt-1 truncate text-lg font-black text-[#111b3d]">{value}</p></div></article>
}

function ProfileSummary({ icon, value }: { icon: React.ReactNode; value: string }) {
  return <div className="flex min-w-0 items-center gap-3"><span className="[&>svg]:h-4 [&>svg]:w-4 text-[#5146f0]">{icon}</span><span className="min-w-0 break-all text-[#68718c]">{value}</span></div>
}

function ProfileField({ label, value, fallback }: { label: string; value: string | null | undefined; fallback: string }) {
  return (
    <div className="min-w-0 border-b border-[#eceef3] pb-4">
      <dt className="text-xs font-black uppercase text-[#8b8e9b]">{label}</dt>
      <dd className="mt-1.5 truncate font-bold text-[#171a2b]" title={value ?? undefined}>{value || fallback}</dd>
    </div>
  )
}
