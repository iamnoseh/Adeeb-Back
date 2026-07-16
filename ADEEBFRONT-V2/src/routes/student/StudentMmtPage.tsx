import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { ArrowDown, ArrowUp, BookOpen, Check, ChevronLeft, ChevronRight, LoaderCircle, LockKeyhole, Plus, Search, Trash2, University } from 'lucide-react'
import { useEffect, useState } from 'react'
import type { ReactElement } from 'react'
import { useTranslation } from 'react-i18next'
import { mmtStudentApi, mmtStudentKeys } from '@/features/mmt/api/mmt.api'
import { addAdmissionChoice, buildStudentProgramQuery, maximumAdmissionChoices, moveAdmissionChoice, normalizeAdmissionChoices } from '@/features/mmt/lib/student-mmt'
import { enumLabel } from '@/features/mmt/lib/mmt'
import { useMmtLabels } from '@/features/mmt/lib/useMmtLabels'
import type { AdmissionProgramListItemDto, MmtClusterDto, SpecialtyDto, UniversityDto } from '@/features/mmt/model/mmt.types'
import { StudentMmtLookupSelect } from '@/features/mmt/ui/StudentMmtLookupSelect'
import { ApiError } from '@/shared/api/problem-details'
import { SelectField } from '@/shared/ui/SelectField'
import { StudentPageHeader } from '@/routes/student/StudentUi'

const pageSize = 10

export function StudentMmtPage() {
  const { t } = useTranslation()
  const queryClient = useQueryClient()
  const labels = useMmtLabels()
  const [notice, setNotice] = useState<{ type: 'success' | 'error'; text: string } | null>(null)
  const [clusterId, setClusterId] = useState('')
  const [clusterLabel, setClusterLabel] = useState('')
  const [clusterSearch, setClusterSearch] = useState('')
  const [clusterPage, setClusterPage] = useState(1)
  const [specialtyId, setSpecialtyId] = useState('')
  const [specialtyLabel, setSpecialtyLabel] = useState('')
  const [specialtySearch, setSpecialtySearch] = useState('')
  const [specialtyPage, setSpecialtyPage] = useState(1)
  const [universityId, setUniversityId] = useState('')
  const [universityLabel, setUniversityLabel] = useState('')
  const [universitySearch, setUniversitySearch] = useState('')
  const [universityPage, setUniversityPage] = useState(1)
  const [programSearch, setProgramSearch] = useState('')
  const [programPage, setProgramPage] = useState(1)
  const [admissionType, setAdmissionType] = useState('')
  const [studyForm, setStudyForm] = useState('')
  const [studyLanguage, setStudyLanguage] = useState('')
  const [selectedIds, setSelectedIds] = useState<string[]>([])
  const [programsById, setProgramsById] = useState<Record<string, AdmissionProgramListItemDto>>({})

  const profile = useQuery({
    queryKey: mmtStudentKeys.profile(), queryFn: mmtStudentApi.profile,
    retry: (count, error) => !(error instanceof ApiError && error.status === 404) && count < 2,
  })
  const profileMissing = profile.error instanceof ApiError && profile.error.status === 404
  const activeClusterId = profile.data?.cluster.id ?? ''

  const clustersQuery = { search: clusterSearch || undefined, page: clusterPage, pageSize }
  const clusters = useQuery({ queryKey: mmtStudentKeys.clusters(clustersQuery), queryFn: () => mmtStudentApi.clusters(clustersQuery), enabled: profileMissing })
  const specialtyQuery = { clusterId: activeClusterId, search: specialtySearch || undefined, page: specialtyPage, pageSize }
  const specialties = useQuery({ queryKey: mmtStudentKeys.specialties(specialtyQuery), queryFn: () => mmtStudentApi.specialties(specialtyQuery), enabled: Boolean(activeClusterId) })
  const universityQuery = { clusterId: activeClusterId, specialtyId, search: universitySearch || undefined, page: universityPage, pageSize }
  const universities = useQuery({ queryKey: mmtStudentKeys.universities(universityQuery), queryFn: () => mmtStudentApi.universities(universityQuery), enabled: Boolean(activeClusterId && specialtyId) })

  const programQuery = buildStudentProgramQuery({
    clusterId: activeClusterId, specialtyId, universityId,
    admissionType: admissionType === '' ? undefined : Number(admissionType),
    studyForm: studyForm === '' ? undefined : Number(studyForm),
    studyLanguage: studyLanguage === '' ? undefined : Number(studyLanguage),
    search: programSearch, page: programPage,
  })
  const programs = useQuery({ queryKey: mmtStudentKeys.programs(programQuery), queryFn: () => mmtStudentApi.programs(programQuery), enabled: Boolean(activeClusterId && specialtyId && universityId) })
  const choices = useQuery({ queryKey: mmtStudentKeys.choices(), queryFn: mmtStudentApi.choices, enabled: Boolean(profile.data) })

  useEffect(() => {
    if (!choices.data) return
    const ordered = [...choices.data].sort((a, b) => a.priorityOrder - b.priorityOrder)
    setSelectedIds(ordered.map((choice) => choice.admissionProgram.id))
    setProgramsById((current) => ({ ...current, ...Object.fromEntries(ordered.map((choice) => [choice.admissionProgram.id, choice.admissionProgram])) }))
  }, [choices.data])

  useEffect(() => {
    if (!programs.data) return
    setProgramsById((current) => ({ ...current, ...Object.fromEntries(programs.data.items.map((program) => [program.id, program])) }))
  }, [programs.data])

  const createProfile = useMutation({
    mutationFn: () => mmtStudentApi.upsertProfile({ mmtClusterId: clusterId }),
    onSuccess: async () => {
      setNotice({ type: 'success', text: t('student.clusterLocked') })
      await queryClient.invalidateQueries({ queryKey: mmtStudentKeys.all })
    },
    onError: (error) => setNotice({ type: 'error', text: friendlyError(error, t('student.loadFailed'), t('student.clusterLocked')) }),
  })
  const saveChoices = useMutation({
    mutationFn: () => mmtStudentApi.replaceChoices(normalizeAdmissionChoices(selectedIds)),
    onSuccess: async () => {
      setNotice({ type: 'success', text: t('student.choicesSaved') })
      await queryClient.invalidateQueries({ queryKey: mmtStudentKeys.choices() })
      await queryClient.invalidateQueries({ queryKey: mmtStudentKeys.profile() })
    },
    onError: (error) => setNotice({ type: 'error', text: friendlyError(error, t('student.loadFailed'), t('student.clusterLocked')) }),
  })

  function chooseSpecialty(id: string, item: SpecialtyDto) {
    setSpecialtyId(id); setSpecialtyLabel(`${item.code} - ${item.name}`)
    setUniversityId(''); setUniversityLabel(''); setUniversitySearch(''); setUniversityPage(1); setProgramPage(1)
  }

  function chooseUniversity(id: string, item: UniversityDto) {
    setUniversityId(id); setUniversityLabel(item.shortName || item.fullName); setProgramPage(1)
  }

  function addProgram(program: AdmissionProgramListItemDto) {
    const result = addAdmissionChoice(selectedIds, program.id)
    if (result.error === 'duplicate') return setNotice({ type: 'error', text: t('student.duplicateChoice') })
    if (result.error === 'limit') return setNotice({ type: 'error', text: t('student.choiceLimit') })
    setProgramsById((current) => ({ ...current, [program.id]: program }))
    setSelectedIds(result.choices)
    setNotice({ type: 'success', text: t('student.choiceAdded') })
  }

  if (profile.isLoading) return <PageLoading />
  if (profile.isError && !profileMissing) return <PageError onRetry={() => void profile.refetch()} />

  return <div className="grid gap-5">
    <StudentPageHeader title={t('student.mmtTitle')} description={t('student.mmtDescription')} />
    {notice ? <div role="status" className={`flex items-center justify-between gap-3 rounded-lg border px-4 py-3 text-sm font-bold ${notice.type === 'success' ? 'border-[#bce6c8] bg-[#effbf2] text-[#258b46]' : 'border-[#f0c7c7] bg-[#fff4f4] text-[#bc4141]'}`}><span>{notice.text}</span><button type="button" onClick={() => setNotice(null)} className="text-lg leading-none" aria-label={t('student.closeNotice')}>×</button></div> : null}

    <section className="rounded-lg border border-[var(--student-border)] bg-[var(--student-surface)] p-5 shadow-[0_10px_28px_rgb(20_31_70/0.04)] sm:p-6">
      <div className="flex items-start gap-4"><span className="grid h-11 w-11 shrink-0 place-items-center rounded-lg bg-[#f0efff] text-[#5146f0]">{profile.data ? <LockKeyhole className="h-5 w-5" /> : <BookOpen className="h-5 w-5" />}</span><div><h2 className="text-lg font-black">{t('student.mmtSetupTitle')}</h2><p className="mt-1 text-sm leading-6 text-[var(--student-muted)]">{t('student.mmtSetupDescription')}</p></div></div>
      {profile.data ? <div className="mt-5 flex flex-col gap-3 rounded-lg border border-[#d9d6ff] bg-[#f7f6ff] p-4 sm:flex-row sm:items-center sm:justify-between"><div><p className="text-xs font-black uppercase text-[#6f67c8]">{t('mmt.cluster')}</p><p className="mt-1 font-black text-[#24204f]">{profile.data.cluster.code} - {profile.data.cluster.name}</p></div><span className="inline-flex items-center gap-2 text-xs font-bold text-[#6f67c8]"><LockKeyhole className="h-4 w-4" />{t('student.clusterLocked')}</span></div> : <div className="mt-5 grid gap-3 sm:grid-cols-[minmax(0,1fr)_auto]">
        <StudentMmtLookupSelect<MmtClusterDto> value={clusterId} selectedLabel={clusterLabel} items={clusters.data?.items ?? []} getLabel={(item) => `${item.code} - ${item.name}`} onValueChange={(id, item) => { setClusterId(id); setClusterLabel(`${item.code} - ${item.name}`) }} search={clusterSearch} onSearchChange={setClusterSearch} page={clusterPage} totalCount={clusters.data?.totalCount ?? 0} onPageChange={setClusterPage} placeholder={t('student.chooseCluster')} searchPlaceholder={t('student.searchClusters')} loading={clusters.isFetching} />
        <button type="button" disabled={!clusterId || createProfile.isPending} onClick={() => createProfile.mutate()} className="inline-flex min-h-12 items-center justify-center gap-2 rounded-lg bg-[#5146f0] px-5 text-sm font-black text-white shadow-[0_8px_20px_rgb(81_70_240/0.2)] disabled:opacity-50">{createProfile.isPending ? <LoaderCircle className="h-4 w-4 animate-spin" /> : <Check className="h-4 w-4" />}{createProfile.isPending ? t('student.saving') : t('student.saveCluster')}</button>
      </div>}
    </section>

    {profile.data ? <section className="grid gap-5 xl:grid-cols-[minmax(0,1.15fr)_minmax(360px,0.85fr)]">
      <div className="grid min-w-0 gap-5">
        <article className="rounded-lg border border-[var(--student-border)] bg-[var(--student-surface)] p-5 shadow-[0_10px_28px_rgb(20_31_70/0.04)] sm:p-6">
          <h2 className="text-lg font-black">{t('student.mmtChoicesTitle')}</h2><p className="mt-1 text-sm leading-6 text-[var(--student-muted)]">{t('student.mmtChoicesDescription')}</p>
          <div className="mt-5 grid gap-3 md:grid-cols-2">
            <StudentMmtLookupSelect<SpecialtyDto> value={specialtyId} selectedLabel={specialtyLabel} items={specialties.data?.items ?? []} getLabel={(item) => `${item.code} - ${item.name}`} onValueChange={chooseSpecialty} search={specialtySearch} onSearchChange={setSpecialtySearch} page={specialtyPage} totalCount={specialties.data?.totalCount ?? 0} onPageChange={setSpecialtyPage} placeholder={t('student.chooseSpecialty')} searchPlaceholder={t('student.searchSpecialties')} loading={specialties.isFetching} />
            <StudentMmtLookupSelect<UniversityDto> value={universityId} selectedLabel={universityLabel} items={universities.data?.items ?? []} getLabel={(item) => item.shortName || item.fullName} onValueChange={chooseUniversity} search={universitySearch} onSearchChange={setUniversitySearch} page={universityPage} totalCount={universities.data?.totalCount ?? 0} onPageChange={setUniversityPage} placeholder={specialtyId ? t('student.chooseUniversity') : t('student.selectSpecialtyFirst')} searchPlaceholder={t('student.searchUniversities')} loading={universities.isFetching} disabled={!specialtyId} />
          </div>
          <div className="mt-3 grid gap-3 sm:grid-cols-3">
            <SelectField value={admissionType} onValueChange={(value) => { setAdmissionType(value); setProgramPage(1) }} placeholder={t('mmt.allAdmissionTypes')} options={[{ value: '', label: t('mmt.allAdmissionTypes') }, ...labels.admissionTypes.map((label, value) => ({ value: String(value), label }))]} />
            <SelectField value={studyForm} onValueChange={(value) => { setStudyForm(value); setProgramPage(1) }} placeholder={t('mmt.allStudyForms')} options={[{ value: '', label: t('mmt.allStudyForms') }, ...labels.studyForms.map((label, value) => ({ value: String(value), label }))]} />
            <SelectField value={studyLanguage} onValueChange={(value) => { setStudyLanguage(value); setProgramPage(1) }} placeholder={t('mmt.allLanguages')} options={[{ value: '', label: t('mmt.allLanguages') }, ...labels.studyLanguages.map((label, value) => ({ value: String(value), label }))]} />
          </div>
        </article>

        <article className="rounded-lg border border-[var(--student-border)] bg-[var(--student-surface)] p-5 shadow-[0_10px_28px_rgb(20_31_70/0.04)] sm:p-6">
          <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between"><h2 className="text-lg font-black">{t('student.availablePrograms')}</h2><label className="relative block sm:w-72"><Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-[var(--student-muted)]" /><input value={programSearch} onChange={(event) => { setProgramSearch(event.target.value); setProgramPage(1) }} placeholder={t('student.programSearch')} className="min-h-11 w-full rounded-lg border border-[var(--student-border)] bg-[var(--student-surface-soft)] pl-9 pr-3 text-sm outline-none focus:border-[#8b83f5]" /></label></div>
          {!universityId ? <p className="mt-6 rounded-lg bg-[var(--student-surface-soft)] p-5 text-center text-sm text-[var(--student-muted)]">{specialtyId ? t('student.chooseUniversity') : t('student.selectSpecialtyFirst')}</p> : null}
          {programs.isFetching ? <div className="grid min-h-32 place-items-center"><LoaderCircle className="h-6 w-6 animate-spin text-[#5146f0]" /></div> : null}
          {universityId && !programs.isFetching && programs.data?.items.length === 0 ? <p className="mt-6 rounded-lg bg-[var(--student-surface-soft)] p-5 text-center text-sm text-[var(--student-muted)]">{t('student.noPrograms')}</p> : null}
          <div className="mt-4 grid gap-3">{programs.data?.items.map((program) => <ProgramRow key={program.id} program={program} selected={selectedIds.includes(program.id)} onAdd={() => addProgram(program)} />)}</div>
          {programs.data && programs.data.totalCount > pageSize ? <Pagination page={programPage} totalCount={programs.data.totalCount} onPageChange={setProgramPage} /> : null}
        </article>
      </div>

      <article className="h-fit rounded-lg border border-[var(--student-border)] bg-[var(--student-surface)] p-5 shadow-[0_10px_28px_rgb(20_31_70/0.04)] sm:p-6 xl:sticky xl:top-24">
        <div className="flex items-center justify-between gap-3"><h2 className="text-lg font-black">{t('student.selectedChoices')}</h2><span className="rounded-md bg-[#f0efff] px-2.5 py-1 text-xs font-black text-[#5146f0]">{t('student.choicesCounter', { count: selectedIds.length })}</span></div>
        {choices.isLoading ? <div className="grid min-h-32 place-items-center"><LoaderCircle className="h-6 w-6 animate-spin text-[#5146f0]" /></div> : null}
        {!choices.isLoading && selectedIds.length === 0 ? <p className="mt-5 rounded-lg bg-[var(--student-surface-soft)] p-5 text-center text-sm text-[var(--student-muted)]">{t('student.noChoices')}</p> : null}
        <ol className="mt-4 grid gap-2">{selectedIds.map((id, index) => { const program = programsById[id]; if (!program) return null; return <li key={id} className="grid grid-cols-[32px_minmax(0,1fr)_auto] items-center gap-2 rounded-lg border border-[var(--student-border)] p-3"><span className="grid h-8 w-8 place-items-center rounded-md bg-[#f0efff] text-xs font-black text-[#5146f0]">{index + 1}</span><div className="min-w-0"><p className="truncate text-sm font-black">{program.specialtyCode} - {program.specialtyName}</p><p className="mt-1 truncate text-xs text-[var(--student-muted)]">{program.universityName}</p></div><div className="flex gap-1"><IconButton label={t('student.moveChoiceUp')} disabled={index === 0} onClick={() => setSelectedIds(moveAdmissionChoice(selectedIds, index, -1))}><ArrowUp /></IconButton><IconButton label={t('student.moveChoiceDown')} disabled={index === selectedIds.length - 1} onClick={() => setSelectedIds(moveAdmissionChoice(selectedIds, index, 1))}><ArrowDown /></IconButton><IconButton label={t('student.removeChoice')} danger onClick={() => setSelectedIds(selectedIds.filter((choiceId) => choiceId !== id))}><Trash2 /></IconButton></div></li> })}</ol>
        <button type="button" disabled={saveChoices.isPending || selectedIds.length > maximumAdmissionChoices} onClick={() => saveChoices.mutate()} className="mt-5 inline-flex min-h-11 w-full items-center justify-center gap-2 rounded-lg bg-[#5146f0] px-4 text-sm font-black text-white disabled:opacity-50">{saveChoices.isPending ? <LoaderCircle className="h-4 w-4 animate-spin" /> : <Check className="h-4 w-4" />}{saveChoices.isPending ? t('student.saving') : t('student.saveChoices')}</button>
      </article>
    </section> : null}
  </div>

  function ProgramRow({ program, selected, onAdd }: { program: AdmissionProgramListItemDto; selected: boolean; onAdd: () => void }) {
    return <div className="flex flex-col gap-3 rounded-lg border border-[var(--student-border)] p-4 sm:flex-row sm:items-center"><span className="grid h-10 w-10 shrink-0 place-items-center rounded-lg bg-[#edf2ff] text-[#2867d8]"><University className="h-5 w-5" /></span><div className="min-w-0 flex-1"><p className="font-black">{program.specialtyCode} - {program.specialtyName}</p><p className="mt-1 text-sm text-[var(--student-muted)]">{program.universityName}</p><div className="mt-2 flex flex-wrap gap-x-4 gap-y-1 text-xs font-bold text-[var(--student-muted)]"><span>{enumLabel(labels.admissionTypes, program.admissionType, labels.unknown)}</span><span>{enumLabel(labels.studyForms, program.studyForm, labels.unknown)}</span><span>{enumLabel(labels.studyLanguages, program.studyLanguage, labels.unknown)}</span><span>{t('student.latestPassingScore')}: {program.latestPassingScore ?? t('student.scoreUnavailable')}</span></div></div><button type="button" disabled={selected} onClick={onAdd} className="inline-flex min-h-10 shrink-0 items-center justify-center gap-2 rounded-lg border border-[#5146f0] px-4 text-sm font-black text-[#5146f0] disabled:border-[#b9b5e9] disabled:bg-[#f0efff] disabled:text-[#7770bd]">{selected ? <Check className="h-4 w-4" /> : <Plus className="h-4 w-4" />}{selected ? t('student.selected') : t('student.addChoice')}</button></div>
  }
}

function Pagination({ page, totalCount, onPageChange }: { page: number; totalCount: number; onPageChange: (page: number) => void }) {
  const { t } = useTranslation(); const pages = Math.max(1, Math.ceil(totalCount / pageSize))
  return <div className="mt-4 flex items-center justify-end gap-2 text-xs font-black text-[var(--student-muted)]"><span>{page} / {pages}</span><IconButton label={t('student.previousPage')} disabled={page <= 1} onClick={() => onPageChange(page - 1)}><ChevronLeft /></IconButton><IconButton label={t('student.nextPage')} disabled={page >= pages} onClick={() => onPageChange(page + 1)}><ChevronRight /></IconButton></div>
}

function IconButton({ label, onClick, disabled = false, danger = false, children }: { label: string; onClick: () => void; disabled?: boolean; danger?: boolean; children: ReactElement<{ className?: string }> }) {
  return <button type="button" title={label} aria-label={label} disabled={disabled} onClick={onClick} className={`grid h-8 w-8 place-items-center rounded-md border border-[var(--student-border)] disabled:opacity-35 ${danger ? 'text-[#d94848]' : 'text-[var(--student-muted)]'}`}>{children}</button>
}

function PageLoading() { return <div className="grid min-h-80 place-items-center"><LoaderCircle className="h-7 w-7 animate-spin text-[#5146f0]" /></div> }
function PageError({ onRetry }: { onRetry: () => void }) { const { t } = useTranslation(); return <div className="grid min-h-80 place-items-center rounded-lg border border-[var(--student-border)] bg-[var(--student-surface)] p-6 text-center"><div><p className="font-bold text-[var(--student-muted)]">{t('student.loadFailed')}</p><button type="button" onClick={onRetry} className="mt-4 rounded-lg bg-[#5146f0] px-4 py-2 text-sm font-black text-white">{t('student.retry')}</button></div></div> }
function friendlyError(error: unknown, fallback: string, clusterLocked: string) { if (error instanceof ApiError) { if (error.problem?.code === 'mmt.cluster_locked') return clusterLocked; return error.problem?.title || fallback } return fallback }
