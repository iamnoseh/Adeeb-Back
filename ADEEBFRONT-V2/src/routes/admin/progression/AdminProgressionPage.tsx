import { useMutation, useQuery, useQueryClient, type UseQueryResult } from '@tanstack/react-query'
import { Archive, Clock3, Edit3, Eye, History, ImagePlus, Plus, Power, RefreshCw, ShieldAlert, Trophy, X } from 'lucide-react'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { leagueKeys, progressionAdminApi } from '@/features/progression/api/league.api'
import type { LeagueDto, LeagueForm, PagedDto, SeasonDto } from '@/features/progression/model/league.types'
import { Button } from '@/shared/ui/Button'
import { FormField } from '@/shared/ui/FormField'
import { Input } from '@/shared/ui/Input'
import { OverflowMarquee } from '@/shared/ui/OverflowMarquee'

const emptyForm: LeagueForm = { nameTg: '', nameRu: '', minXp: 0, maxXp: null, displayOrder: 1, isActive: true }

export function AdminProgressionPage() {
  const { t } = useTranslation(); const queryClient = useQueryClient(); const [editing, setEditing] = useState<LeagueDto | 'new' | null>(null); const [selectedLeague, setSelectedLeague] = useState<LeagueDto | null>(null); const [historyPage, setHistoryPage] = useState(1)
  const leagues = useQuery({ queryKey: leagueKeys.leagues(), queryFn: progressionAdminApi.leagues })
  const season = useQuery({ queryKey: leagueKeys.season(), queryFn: progressionAdminApi.season, refetchInterval: 30_000 })
  const history = useQuery({ queryKey: leagueKeys.adminHistory(historyPage), queryFn: () => progressionAdminApi.history(historyPage) })
  const invalidate = async () => { await queryClient.invalidateQueries({ queryKey: leagueKeys.admin() }) }
  const start = useMutation({ mutationFn: progressionAdminApi.startSeason, onSuccess: invalidate })
  const auto = useMutation({ mutationFn: progressionAdminApi.setAutoRenewal, onSuccess: invalidate })
  const archive = useMutation({ mutationFn: progressionAdminApi.archiveLeague, onSuccess: invalidate })
  return <div className="grid gap-6">
    <header className="rounded-lg border border-white/70 bg-white/80 p-6 shadow-[0_18px_45px_rgb(24_49_45/0.08)]"><div className="flex flex-col gap-4 md:flex-row md:items-center md:justify-between"><div><h1 className="text-3xl font-black tracking-normal">{t('progression.adminTitle')}</h1><p className="mt-2 text-sm text-[var(--muted)]">{t('progression.adminDescription')}</p></div><Button onClick={() => setEditing('new')}><Plus className="h-4 w-4" />{t('progression.addLeague')}</Button></div></header>
    <section className="grid gap-4 rounded-lg border border-[var(--border)] bg-white p-5 lg:grid-cols-[minmax(0,1fr)_auto] lg:items-center">
      <div className="flex items-center gap-4"><span className="grid h-12 w-12 place-items-center rounded-lg bg-[var(--primary-soft)] text-[var(--primary-strong)]"><Clock3 className="h-5 w-5" /></span><div><h2 className="font-black">{season.data ? t('progression.seasonNumber', { number: season.data.number }) : t('progression.noActiveSeason')}</h2><p className="mt-1 text-sm text-[var(--muted)]">{season.data ? new Date(season.data.endsAtUtc).toLocaleString() : t('progression.configureBeforeStart')}</p></div></div>
      <div className="flex flex-wrap gap-2">{season.data ? <Button variant="secondary" onClick={() => auto.mutate(!season.data!.autoStartNext)} disabled={auto.isPending}><Power className="h-4 w-4" />{season.data.autoStartNext ? t('progression.pauseRenewal') : t('progression.enableRenewal')}</Button> : <Button onClick={() => start.mutate()} disabled={start.isPending || (leagues.data?.length ?? 0) < 2}><Trophy className="h-4 w-4" />{t('progression.startSeason')}</Button>}</div>
      {(start.isError || auto.isError) ? <p className="text-sm font-bold text-red-700 lg:col-span-2">{t('progression.operationFailed')}</p> : null}
    </section>
    {season.data ? <div className="flex items-start gap-3 rounded-lg border border-amber-200 bg-amber-50 p-4 text-sm text-amber-900"><ShieldAlert className="mt-0.5 h-4 w-4 shrink-0" /><p>{t('progression.structuralLockedHint')}</p></div> : null}
    <section className="overflow-hidden rounded-lg border border-[var(--border)] bg-white shadow-sm">
      <div className="grid grid-cols-[minmax(0,1.4fr)_minmax(0,1fr)_90px_132px] gap-3 bg-[var(--surface-muted)] px-4 py-3 text-xs font-black uppercase text-[var(--muted)]"><span>{t('progression.league')}</span><span>{t('progression.xpRange')}</span><span>{t('progression.order')}</span><span className="text-right">{t('actions')}</span></div>
      {leagues.isLoading ? <Empty text={t('progression.loading')} /> : leagues.isError ? <Empty text={t('progression.loadFailed')} retryLabel={t('progression.retry')} retry={() => void leagues.refetch()} /> : leagues.data?.length ? leagues.data.map((league) => <div key={league.id} className="grid min-h-16 grid-cols-[minmax(0,1.4fr)_minmax(0,1fr)_90px_132px] items-center gap-3 border-t border-[var(--border)] px-4 py-3 text-sm"><div className="flex min-w-0 items-center gap-3">{league.avatarUrl ? <img src={league.avatarUrl} alt="" className="h-10 w-10 shrink-0 rounded-lg object-cover" /> : <span className="grid h-10 w-10 shrink-0 place-items-center rounded-lg bg-[var(--primary-soft)] font-black text-[var(--primary-strong)]">{league.name[0]}</span>}<OverflowMarquee text={league.name} className="font-black" /></div><span className="font-bold tabular-nums text-[var(--muted)]">{league.minXp} – {league.maxXp ?? '∞'} XP</span><span className="font-black">#{league.displayOrder}</span><div className="flex justify-end gap-1"><IconButton label={t('progression.openLeaderboard')} onClick={() => setSelectedLeague(league)} icon={<Eye />} /><IconButton label={t('edit')} onClick={() => setEditing(league)} icon={<Edit3 />} /><IconButton label={t('progression.archive')} onClick={() => archive.mutate(league.id)} disabled={league.structuralLocked || archive.isPending} icon={<Archive />} /></div></div>) : <Empty text={t('progression.emptyLeagues')} />}
    </section>
    <SeasonHistory page={historyPage} onPage={setHistoryPage} query={history} />
    {editing ? (() => {
      const activeLeagues = leagues.data?.filter(x => x.status === 1) || [];
      return <LeagueModal league={editing === 'new' ? null : editing} nextOrder={activeLeagues.length + 1} activeLeagues={activeLeagues} onClose={() => setEditing(null)} onSaved={async () => { setEditing(null); await invalidate() }} />;
    })() : null}
    {selectedLeague ? <AdminLeaderboard league={selectedLeague} onClose={() => setSelectedLeague(null)} /> : null}
  </div>
}

function LeagueModal({ league, nextOrder, activeLeagues, onClose, onSaved }: { league: LeagueDto | null; nextOrder: number; activeLeagues: LeagueDto[]; onClose: () => void; onSaved: () => Promise<void> }) {
  const { t } = useTranslation();
  const [value, setValue] = useState<LeagueForm>(() => {
    if (league) {
      return { nameTg: league.nameTg, nameRu: league.nameRu, minXp: league.minXp, maxXp: league.maxXp, displayOrder: league.displayOrder, isActive: true };
    } else {
      const lastActive = activeLeagues[activeLeagues.length - 1];
      const defaultMinXp = lastActive ? (lastActive.maxXp ?? 0) : 0;
      return { ...emptyForm, minXp: defaultMinXp, displayOrder: nextOrder };
    }
  });
  const [preview, setPreview] = useState(league?.avatarUrl ?? '')
  const save = useMutation({ mutationFn: () => league ? progressionAdminApi.updateLeague({ id: league.id, value }) : progressionAdminApi.createLeague(value), onSuccess: onSaved })
  const locked = league?.structuralLocked ?? false
  return <div className="fixed inset-0 z-50 grid place-items-center bg-black/45 p-4" onMouseDown={(e) => { if (e.currentTarget === e.target) onClose() }}><form className="max-h-[calc(100vh-2rem)] w-full max-w-2xl overflow-y-auto rounded-lg bg-white p-5 shadow-2xl" onSubmit={(e) => { e.preventDefault(); save.mutate() }}><div className="flex items-center justify-between"><h2 className="text-xl font-black">{league ? t('progression.editLeague') : t('progression.addLeague')}</h2><button type="button" onClick={onClose} className="grid h-9 w-9 place-items-center rounded-lg hover:bg-[var(--surface-muted)]"><X className="h-4 w-4" /></button></div>
    <div className="mt-5 grid gap-4 sm:grid-cols-2"><FormField label={t('progression.nameTg')}><Input required value={value.nameTg} onChange={(e) => setValue({ ...value, nameTg: e.target.value })} /></FormField><FormField label={t('progression.nameRu')}><Input required value={value.nameRu} onChange={(e) => setValue({ ...value, nameRu: e.target.value })} /></FormField><FormField label={t('progression.minXp')}><Input required type="number" min={0} step={0.5} disabled={true} value={value.minXp} onChange={(e) => setValue({ ...value, minXp: Number(e.target.value) })} /></FormField><FormField label={t('progression.maxXp')}><Input type="number" min={0} step={0.5} disabled={locked} value={value.maxXp ?? ''} onChange={(e) => setValue({ ...value, maxXp: e.target.value ? Number(e.target.value) : null })} /></FormField><FormField label={t('progression.order')}><Input required type="number" min={1} disabled={locked} value={value.displayOrder} onChange={(e) => setValue({ ...value, displayOrder: Number(e.target.value) })} /></FormField><FormField label={t('progression.avatar')}><label className="flex min-h-11 cursor-pointer items-center gap-2 rounded-lg border border-dashed border-[var(--border)] px-3 text-sm font-bold text-[var(--muted)]"><ImagePlus className="h-4 w-4" />{t('progression.chooseImage')}<input type="file" accept="image/png,image/jpeg,image/webp" className="sr-only" onChange={(e) => { const file = e.target.files?.[0]; if (file) { setValue({ ...value, avatar: file }); setPreview(URL.createObjectURL(file)) } }} /></label></FormField></div>
    {preview ? <div className="mt-4 flex items-center gap-3 rounded-lg bg-[var(--surface-muted)] p-3"><img src={preview} alt="" className="h-14 w-14 rounded-lg object-cover" /><button type="button" className="text-sm font-black text-red-600" onClick={() => { setPreview(''); setValue({ ...value, avatar: null, removeAvatar: true }) }}>{t('progression.removeImage')}</button></div> : null}
    {save.isError ? <p className="mt-4 text-sm font-bold text-red-700">{t('progression.operationFailed')}</p> : null}<div className="mt-6 flex justify-end gap-3"><Button type="button" variant="secondary" onClick={onClose}>{t('cancel')}</Button><Button type="submit" disabled={save.isPending}>{save.isPending ? t('saving') : t('save')}</Button></div></form></div>
}
function IconButton({ label, icon, onClick, disabled }: { label: string; icon: React.ReactNode; onClick: () => void; disabled?: boolean }) { return <button type="button" aria-label={label} title={label} disabled={disabled} onClick={onClick} className="grid h-9 w-9 place-items-center rounded-lg border border-[var(--border)] text-[var(--muted)] hover:bg-[var(--surface-muted)] disabled:opacity-40 [&>svg]:h-4 [&>svg]:w-4">{icon}</button> }
function Empty({ text, retry, retryLabel }: { text: string; retry?: () => void; retryLabel?: string }) { return <div className="grid min-h-32 place-items-center border-t border-[var(--border)] p-6 text-center text-sm font-bold text-[var(--muted)]"><div><p>{text}</p>{retry ? <button type="button" onClick={retry} className="mt-2 inline-flex items-center gap-2 text-[var(--primary-strong)]"><RefreshCw className="h-4 w-4" />{retryLabel}</button> : null}</div></div> }

function AdminLeaderboard({ league, onClose }: { league: LeagueDto; onClose: () => void }) {
  const { t } = useTranslation(); const [page, setPage] = useState(1)
  const query = useQuery({ queryKey: leagueKeys.adminLeaderboard(league.id, page), queryFn: () => progressionAdminApi.leaderboard(league.id, page) })
  return <div className="fixed inset-0 z-50 grid place-items-center bg-black/45 p-4" onMouseDown={(e) => { if (e.currentTarget === e.target) onClose() }}><section className="max-h-[calc(100vh-2rem)] w-full max-w-3xl overflow-y-auto rounded-lg bg-white shadow-2xl"><header className="sticky top-0 z-10 flex items-center justify-between border-b border-[var(--border)] bg-white p-5"><div><h2 className="text-xl font-black">{t('progression.leaderboard')}</h2><p className="mt-1 text-sm text-[var(--muted)]">{league.name}</p></div><IconButton label={t('close')} onClick={onClose} icon={<X />} /></header>{query.isLoading ? <Empty text={t('progression.loading')} /> : query.isError ? <Empty text={t('progression.loadFailed')} retryLabel={t('progression.retry')} retry={() => void query.refetch()} /> : <>{query.data?.items.map((item) => <div key={item.userId} className="grid grid-cols-[48px_minmax(0,1fr)_auto_auto] items-center gap-3 border-b border-[var(--border)] px-5 py-3 text-sm"><strong>#{item.rank}</strong><OverflowMarquee text={item.displayName} className="font-bold" /><span className="text-xs font-bold text-[var(--muted)]">{t(`progression.zone.${item.zone}`)}</span><strong>{item.seasonXp} XP</strong></div>)}{query.data ? <AdminPager page={query.data.page} pageSize={query.data.pageSize} total={query.data.totalCount} onPage={setPage} /> : null}</>}</section></div>
}

function SeasonHistory({ page, onPage, query }: { page: number; onPage: (page: number) => void; query: UseQueryResult<PagedDto<SeasonDto>, Error> }) {
  const { t } = useTranslation()
  return <section className="overflow-hidden rounded-lg border border-[var(--border)] bg-white"><header className="flex items-center gap-3 p-5"><History className="h-5 w-5 text-[var(--primary-strong)]" /><h2 className="font-black">{t('progression.seasonHistory')}</h2></header>{query.isLoading ? <Empty text={t('progression.loading')} /> : query.isError ? <Empty text={t('progression.loadFailed')} retryLabel={t('progression.retry')} retry={() => void query.refetch()} /> : query.data?.items.length ? <>{query.data.items.map((item) => <div key={item.id} className="grid gap-2 border-t border-[var(--border)] px-5 py-3 text-sm sm:grid-cols-[120px_1fr_1fr]"><strong>{t('progression.seasonNumber', { number: item.number })}</strong><span>{new Date(item.startsAtUtc).toLocaleDateString()}</span><span>{new Date(item.endsAtUtc).toLocaleDateString()}</span></div>)}<AdminPager page={page} pageSize={query.data.pageSize} total={query.data.totalCount} onPage={onPage} /></> : <Empty text={t('progression.emptyHistory')} />}</section>
}

function AdminPager({ page, pageSize, total, onPage }: { page: number; pageSize: number; total: number; onPage: (page: number) => void }) { const pages = Math.max(1, Math.ceil(total / pageSize)); return <div className="flex items-center justify-end gap-3 p-4"><Button variant="secondary" disabled={page <= 1} onClick={() => onPage(page - 1)}>←</Button><strong className="text-sm tabular-nums">{page} / {pages}</strong><Button variant="secondary" disabled={page >= pages} onClick={() => onPage(page + 1)}>→</Button></div> }
