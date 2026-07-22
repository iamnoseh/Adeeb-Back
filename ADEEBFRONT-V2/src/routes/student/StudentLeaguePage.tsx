import { useQuery } from '@tanstack/react-query'
import { ArrowDown, ArrowUp, Clock3, Medal, RefreshCw, Sparkles, Trophy, UsersRound } from 'lucide-react'
import { useEffect, useMemo, useRef, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { leagueKeys, progressionStudentApi } from '@/features/progression/api/league.api'
import type { LeaderboardItemDto } from '@/features/progression/model/league.types'
import { leagueCountdown, serverClockOffset } from '@/features/progression/lib/league-time'
import { ListPagination } from '@/shared/ui/ListPagination'
import { StudentPageHeader } from '@/routes/student/StudentUi'
import { cn } from '@/shared/lib/cn'

export function StudentLeaguePage() {
  const { t, i18n } = useTranslation(); const [page, setPage] = useState(1)
  const overview = useQuery({ queryKey: leagueKeys.overview(), queryFn: progressionStudentApi.overview, refetchInterval: 30_000 })
  const leagues = useQuery({ queryKey: leagueKeys.leaguesList(), queryFn: progressionStudentApi.leagues })
  const [selectedLeagueId, setSelectedLeagueId] = useState<string | undefined>(undefined)
  const data = overview.data

  useEffect(() => {
    if (data?.league?.id && !selectedLeagueId) {
      setSelectedLeagueId(data.league.id)
    }
  }, [data?.league?.id, selectedLeagueId])

  const leaderboard = useQuery({
    queryKey: leagueKeys.leaderboard(page, selectedLeagueId),
    queryFn: () => progressionStudentApi.leaderboard(page, selectedLeagueId),
    enabled: Boolean(selectedLeagueId)
  })

  const countdown = useCountdown(overview.data?.serverNowUtc, overview.data?.endsAtUtc, () => { void overview.refetch(); void leaderboard.refetch() })
  const number = useMemo(() => new Intl.NumberFormat(i18n.language, { maximumFractionDigits: 1 }), [i18n.language])

  if (overview.isLoading) return <LeagueState text={t('progression.loading')} />
  if (overview.isError) return <LeagueState text={t('progression.loadFailed')} retryLabel={t('progression.retry')} retry={() => void overview.refetch()} />
  if (!data?.league) return <div className="grid gap-6"><StudentPageHeader description={t('progression.studentDescription')} /><LeagueState text={data?.lifetimeXp ? t('progression.noSeason') : t('progression.unranked')} /></div>

  return <div className="grid gap-5">
    <StudentPageHeader description={t('progression.studentDescription')} />
    <section className="overflow-hidden rounded-lg border border-[#ddd9ff] bg-white shadow-[0_14px_36px_rgb(20_31_70/0.06)]">
      <div className="grid gap-5 bg-[linear-gradient(110deg,#f4f2ff,#fff_55%,#fff8e8)] p-5 md:grid-cols-[auto_minmax(0,1fr)_auto] md:items-center md:p-7">
        <LeagueAvatar name={data.league.name} url={data.league.avatarUrl} large />
        <div className="min-w-0"><p className="text-xs font-black uppercase text-[#7368ee]">{t('progression.currentLeague')}</p><h2 className="mt-1 truncate text-2xl font-black text-[#111b3d]" title={data.league.name}>{data.league.name}</h2><div className="mt-3 flex flex-wrap gap-2 text-sm font-bold text-[#68718c]"><span>{number.format(data.seasonXp)} XP</span><span>•</span><span>#{data.seasonRank ?? '—'}</span><span>•</span><span>{data.participantCount} {t('progression.participants')}</span></div></div>
        <div className="rounded-lg border border-[#e2dffd] bg-white/90 px-4 py-3 text-right shadow-sm"><p className="flex items-center justify-end gap-2 text-xs font-black text-[#68718c]"><Clock3 className="h-4 w-4 text-[#5146f0]" />{t('progression.timeLeft')}</p><p className="mt-1 text-xl font-black tabular-nums text-[#111b3d]">{countdown.days} {t('progression.daysShort')} {countdown.time}</p></div>
      </div>
      <div className="grid border-t border-[#eceaf9] sm:grid-cols-2"><Metric icon={<Sparkles />} label={t('progression.lifetimeXp')} value={`${number.format(data.lifetimeXp)} XP`} /><Metric icon={<UsersRound />} label={t('progression.rankedStudents')} value={number.format(data.rankedStudentsCount)} /></div>
    </section>

    <section className="overflow-hidden rounded-lg border border-[#e1e4ef] bg-white shadow-[0_10px_28px_rgb(20_31_70/0.04)]">
      <div className="flex flex-col border-b border-[#eceef3] bg-white">
        <div className="flex items-center justify-between px-5 py-4"><div><h2 className="font-black text-[#111b3d]">{t('progression.leaderboard')}</h2><p className="mt-1 text-xs text-[#68718c]">{t('progression.movementHint', { count: leaderboard.data?.movementCount ?? 0 })}</p></div><Trophy className="h-5 w-5 text-[#5146f0]" /></div>
        {leagues.data?.length ? (
          <div className="flex flex-wrap gap-2 border-t border-[#eceef3] px-5 py-3 bg-[#f8f9fc]">
            {leagues.data.map((lg) => {
              const isActive = lg.id === selectedLeagueId;
              const isUserLeague = lg.id === data.league?.id;
              return (
                <button
                  key={lg.id}
                  type="button"
                  onClick={() => { setSelectedLeagueId(lg.id); setPage(1); }}
                  className={cn(
                    "flex items-center gap-2 rounded-lg px-3 py-1.5 text-xs font-black transition-all",
                    isActive 
                      ? "bg-[#5146f0] text-white shadow-[0_4px_12px_rgba(81,70,240,0.25)]" 
                      : "bg-white border border-[#e1e4ef] text-[#505978] hover:bg-[#eceaf9] hover:text-[#5146f0]"
                  )}
                >
                  <LeagueAvatar name={lg.name} url={lg.avatarUrl} mini />
                  <span>{lg.name}</span>
                  {isUserLeague && (
                    <span className={cn(
                      "rounded px-1 text-[9px] uppercase font-black tracking-wider",
                      isActive ? "bg-white/20 text-white" : "bg-[#f0efff] text-[#5146f0]"
                    )}>
                      {t('progression.you')}
                    </span>
                  )}
                </button>
              );
            })}
          </div>
        ) : null}
      </div>
      {leaderboard.isLoading ? <LeagueState text={t('progression.loading')} compact /> : leaderboard.isError ? <LeagueState text={t('progression.loadFailed')} retryLabel={t('progression.retry')} retry={() => void leaderboard.refetch()} compact /> : <div>{leaderboard.data?.items.map((item) => <LeaderRow key={item.userId} item={item} />)}</div>}
      {leaderboard.data ? <div className="border-t border-[#eceef3] px-4 pb-3"><ListPagination page={leaderboard.data.page} pageSize={leaderboard.data.pageSize} total={leaderboard.data.totalCount} onPage={setPage} /></div> : null}
    </section>
    {leaderboard.data?.currentUser && !leaderboard.data.items.some((x) => x.isCurrentUser) ? <section className="rounded-lg border border-[#bcb7ff] bg-[#f2f0ff] p-3"><p className="mb-2 text-xs font-black uppercase text-[#5146f0]">{t('progression.aroundMe')}</p><LeaderRow item={leaderboard.data.currentUser} /></section> : null}
    {data.previousSeason ? <section className="flex flex-wrap items-center justify-between gap-3 rounded-lg border border-[#e1e4ef] bg-white p-4"><div><p className="text-xs font-bold text-[#7b8299]">{t('progression.previousSeason')}</p><p className="mt-1 font-black text-[#111b3d]">{t('progression.seasonNumber', { number: data.previousSeason.seasonNumber })}</p></div><div className="text-right"><strong className="text-[#5146f0]">#{data.previousSeason.finalRank}</strong><p className="mt-1 text-xs font-bold text-[#68718c]">{t(`progression.outcome.${data.previousSeason.outcome.toLowerCase()}`)}</p></div></section> : null}
  </div>
}

function LeagueAvatar({ name, url, large = false, mini = false }: { name: string; url: string | null; large?: boolean; mini?: boolean }) {
  const sizeClass = large ? 'h-20 w-20' : mini ? 'h-5 w-5 text-[10px]' : 'h-10 w-10';
  return url 
    ? <img src={url} alt="" className={cn('rounded-lg object-cover shadow-sm', sizeClass)} /> 
    : <span className={cn('grid rounded-lg bg-[#e9e7ff] font-black text-[#5146f0] place-items-center', sizeClass)}>{name[0] ?? 'A'}</span>;
}

function Metric({ icon, label, value }: { icon: React.ReactNode; label: string; value: string }) { return <div className="flex items-center gap-3 border-b border-[#eceaf9] p-4 last:border-b-0 sm:border-b-0 sm:border-r sm:last:border-r-0"><span className="grid h-10 w-10 place-items-center rounded-lg bg-[#f0efff] text-[#5146f0] [&>svg]:h-5 [&>svg]:w-5">{icon}</span><div><p className="text-xs font-bold text-[#7b8299]">{label}</p><p className="mt-0.5 text-lg font-black text-[#111b3d]">{value}</p></div></div> }

function LeaderRow({ item }: { item: LeaderboardItemDto }) {
  const { t } = useTranslation();
  const isPromoting = item.zone === 'promotion' || item.zone === 'top';
  const isRelegating = item.zone === 'relegation' || item.zone === 'bottom';
  const medalColor = item.rank === 1 ? 'text-[#eab308]' : item.rank === 2 ? 'text-[#94a3b8]' : 'text-[#b45309]';
  const rankContent = item.rank <= 3 
    ? <Medal className={cn("mx-auto h-5 w-5", medalColor)} /> 
    : `#${item.rank}`;

  return (
    <div className={cn(
      'grid min-h-[4.5rem] grid-cols-[42px_42px_minmax(0,1fr)_auto] items-center gap-3 border-b border-[#eef0f5] px-4 py-2 last:border-b-0 transition-colors',
      item.isCurrentUser 
        ? 'bg-[#f3f1ff] border-l-4 border-[#5146f0] pl-3' 
        : isPromoting 
          ? 'bg-[#f0fdf4]/50 border-l-4 border-emerald-500 pl-3 hover:bg-[#f0fdf4]/80' 
          : isRelegating 
            ? 'bg-[#fff1f2]/50 border-l-4 border-rose-500 pl-3 hover:bg-[#fff1f2]/80' 
            : 'hover:bg-[#f8f9fa]'
    )}>
      <span className="text-center font-black tabular-nums text-[#505978]">
        {rankContent}
      </span>
      <StudentAvatar name={item.displayName} url={item.avatarUrl} />
      <div className="min-w-0">
        <p className="truncate text-sm font-black text-[#111b3d] flex items-center">
          {item.displayName}
          {item.isCurrentUser && (
            <span className="ml-2 inline-flex items-center rounded bg-[#5146f0] px-1.5 py-0.5 text-[10px] font-black text-white uppercase tracking-wider">
              {t('progression.you')}
            </span>
          )}
        </p>
        <Zone zone={item.zone} />
      </div>
      <strong className="text-sm tabular-nums text-[#111b3d]">{item.seasonXp} XP</strong>
    </div>
  );
}

function StudentAvatar({ name, url }: { name: string; url: string | null }) {
  if (url) return <img src={url} alt="" className="h-10 w-10 rounded-full border border-white object-cover shadow-sm" />
  const initials = name.split(/\s+/).filter(Boolean).slice(0, 2).map((part) => part[0]).join('').toUpperCase()
  return <span className="grid h-10 w-10 place-items-center rounded-full bg-[#e9e7ff] text-xs font-black text-[#5146f0]">{initials || 'A'}</span>
}

function Zone({ zone }: { zone: string }) { const { t } = useTranslation(); if (zone === 'promotion') return <span className="mt-1 inline-flex items-center gap-1 text-xs font-bold text-emerald-600"><ArrowUp className="h-3 w-3" />{t('progression.promotion')}</span>; if (zone === 'relegation') return <span className="mt-1 inline-flex items-center gap-1 text-xs font-bold text-red-600"><ArrowDown className="h-3 w-3" />{t('progression.relegation')}</span>; return <span className="mt-1 block text-xs text-[#8a90a5]">{t(`progression.zone.${zone}`, { defaultValue: t('progression.stable') })}</span> }

function LeagueState({ text, retry, retryLabel, compact = false }: { text: string; retry?: () => void; retryLabel?: string; compact?: boolean }) { return <div className={cn('grid place-items-center text-center text-sm font-bold text-[#68718c]', compact ? 'min-h-32 p-5' : 'min-h-72 rounded-lg border border-[#e1e4ef] bg-white p-8')}><div><Trophy className="mx-auto mb-3 h-8 w-8 text-[#8d84f4]" /><p>{text}</p>{retry ? <button type="button" onClick={retry} className="mt-3 inline-flex items-center gap-2 text-[#5146f0]"><RefreshCw className="h-4 w-4" />{retryLabel}</button> : null}</div></div> }

function useCountdown(serverNow?: string, endsAt?: string | null, onEnd?: () => void) { const offset = useMemo(() => serverClockOffset(serverNow, Date.now()), [serverNow]); const [value, setValue] = useState(() => leagueCountdown(endsAt, Date.now(), offset)); const onEndRef = useRef(onEnd); const firedRef = useRef(false); useEffect(() => { onEndRef.current = onEnd }, [onEnd]); useEffect(() => { firedRef.current = false; if (!endsAt) { setValue(leagueCountdown(null, Date.now(), offset)); return }; const tick = () => { const next = leagueCountdown(endsAt, Date.now(), offset); setValue(next); if (next.days === 0 && next.time === '00:00:00' && !firedRef.current) { firedRef.current = true; onEndRef.current?.() } }; tick(); const id = window.setInterval(tick, 1000); return () => window.clearInterval(id) }, [endsAt, offset]); return value }
