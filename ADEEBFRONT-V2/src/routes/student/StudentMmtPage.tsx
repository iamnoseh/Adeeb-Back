import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { ArrowDown, ArrowUp, BookOpen, Check, ChevronLeft, ChevronRight, Filter, LoaderCircle, LockKeyhole, Pencil, Plus, Search, Trash2, University, X } from "lucide-react";
import { useEffect, useState, type ReactElement } from "react";
import { useTranslation } from "react-i18next";
import { useSearchParams } from "react-router-dom";
import { mmtStudentApi, mmtStudentKeys } from "@/features/mmt/api/mmt.api";
import { addAdmissionChoice, buildStudentProgramQuery, maximumAdmissionChoices, moveAdmissionChoice, normalizeAdmissionChoices } from "@/features/mmt/lib/student-mmt";
import { enumLabel } from "@/features/mmt/lib/mmt";
import { useMmtLabels } from "@/features/mmt/lib/useMmtLabels";
import type { AdmissionProgramListItemDto, MmtClusterDto, PassingScoreHistoryDto, SpecialtyDto, StudentAdmissionChoiceDto, UniversityDto } from "@/features/mmt/model/mmt.types";
import { StudentMmtLookupSelect } from "@/features/mmt/ui/StudentMmtLookupSelect";
import { ApiError } from "@/shared/api/problem-details";
import { OverflowMarquee } from "@/shared/ui/OverflowMarquee";
import { SelectField } from "@/shared/ui/SelectField";
import { StudentPageHeader } from "@/routes/student/StudentUi";

const pageSize = 10;

export function StudentMmtPage() {
  const { t } = useTranslation();
  const labels = useMmtLabels();
  const queryClient = useQueryClient();
  const [params, setParams] = useSearchParams();
  const [notice, setNotice] = useState<{ type: "success" | "error"; text: string } | null>(null);
  const [clusterId, setClusterId] = useState("");
  const [clusterLabel, setClusterLabel] = useState("");
  const [clusterSearch, setClusterSearch] = useState("");
  const [clusterPage, setClusterPage] = useState(1);
  const [search, setSearch] = useState(params.get("search") ?? "");
  const [page, setPage] = useState(Math.max(1, Number(params.get("page")) || 1));
  const [specialtyId, setSpecialtyId] = useState(params.get("specialtyId") ?? "");
  const [specialtyLabel, setSpecialtyLabel] = useState("");
  const [specialtySearch, setSpecialtySearch] = useState("");
  const [specialtyPage, setSpecialtyPage] = useState(1);
  const [universityId, setUniversityId] = useState(params.get("universityId") ?? "");
  const [universityLabel, setUniversityLabel] = useState("");
  const [universitySearch, setUniversitySearch] = useState("");
  const [universityPage, setUniversityPage] = useState(1);
  const [admissionType, setAdmissionType] = useState(params.get("admissionType") ?? "");
  const [studyForm, setStudyForm] = useState(params.get("studyForm") ?? "");
  const [studyLanguage, setStudyLanguage] = useState(params.get("studyLanguage") ?? "");
  const [filtersOpen, setFiltersOpen] = useState(false);
  const [selectedIds, setSelectedIds] = useState<string[]>([]);
  const [serverSnapshot, setServerSnapshot] = useState<string[]>([]);
  const [programsById, setProgramsById] = useState<Record<string, AdmissionProgramListItemDto>>({});
  const [savedChoices, setSavedChoices] = useState<StudentAdmissionChoiceDto[]>([]);
  const [editing, setEditing] = useState(false);

  const profile = useQuery({
    queryKey: mmtStudentKeys.profile(), queryFn: mmtStudentApi.profile,
    retry: (count, error) => !(error instanceof ApiError && error.status === 404) && count < 2,
  });
  const profileMissing = profile.error instanceof ApiError && profile.error.status === 404;
  const activeClusterId = profile.data?.cluster.id ?? "";
  const clusterQuery = { search: clusterSearch || undefined, page: clusterPage, pageSize };
  const clusters = useQuery({ queryKey: mmtStudentKeys.clusters(clusterQuery), queryFn: () => mmtStudentApi.clusters(clusterQuery), enabled: profileMissing });
  const specialtyQuery = { clusterId: activeClusterId, search: specialtySearch || undefined, page: specialtyPage, pageSize };
  const specialties = useQuery({ queryKey: mmtStudentKeys.specialties(specialtyQuery), queryFn: () => mmtStudentApi.specialties(specialtyQuery), enabled: Boolean(activeClusterId) });
  const universityQuery = { clusterId: activeClusterId, ...(specialtyId ? { specialtyId } : {}), search: universitySearch || undefined, page: universityPage, pageSize };
  const universities = useQuery({ queryKey: mmtStudentKeys.universities(universityQuery), queryFn: () => mmtStudentApi.universities(universityQuery), enabled: Boolean(activeClusterId) });
  const programQuery = buildStudentProgramQuery({ clusterId: activeClusterId, specialtyId, universityId,
    admissionType: admissionType ? Number(admissionType) : undefined, studyForm: studyForm ? Number(studyForm) : undefined,
    studyLanguage: studyLanguage ? Number(studyLanguage) : undefined, search, page });
  const programs = useQuery({ queryKey: mmtStudentKeys.programs(programQuery), queryFn: () => mmtStudentApi.programs(programQuery), enabled: Boolean(activeClusterId) });
  const choices = useQuery({ queryKey: mmtStudentKeys.choices(), queryFn: mmtStudentApi.choices, enabled: Boolean(profile.data) });

  useEffect(() => {
    if (!choices.data) return;
    const ordered = [...choices.data].sort((a, b) => a.priorityOrder - b.priorityOrder);
    const ids = ordered.map((choice) => choice.admissionProgram.id);
    setSelectedIds(ids); setServerSnapshot(ids); setSavedChoices(ordered);
    setProgramsById((current) => ({ ...current, ...Object.fromEntries(ordered.map((choice) => [choice.admissionProgram.id, choice.admissionProgram])) }));
  }, [choices.data]);
  useEffect(() => {
    if (!programs.data) return;
    setProgramsById((current) => ({ ...current, ...Object.fromEntries(programs.data.items.map((program) => [program.id, program])) }));
  }, [programs.data]);
  useEffect(() => {
    if (!activeClusterId) return;
    const next = new URLSearchParams();
    if (page > 1) next.set("page", String(page));
    if (search.trim()) next.set("search", search.trim());
    if (specialtyId) next.set("specialtyId", specialtyId);
    if (universityId) next.set("universityId", universityId);
    if (admissionType) next.set("admissionType", admissionType);
    if (studyForm) next.set("studyForm", studyForm);
    if (studyLanguage) next.set("studyLanguage", studyLanguage);
    setParams(next, { replace: true });
  }, [activeClusterId, admissionType, page, search, setParams, specialtyId, studyForm, studyLanguage, universityId]);

  const createProfile = useMutation({
    mutationFn: () => mmtStudentApi.upsertProfile({ mmtClusterId: clusterId }),
    onSuccess: async () => { setNotice({ type: "success", text: t("student.clusterLocked") }); await queryClient.invalidateQueries({ queryKey: mmtStudentKeys.all }); },
    onError: (error) => setNotice({ type: "error", text: friendlyError(error, t("student.loadFailed"), t("student.clusterLocked")) }),
  });
  const saveChoices = useMutation({
    mutationFn: () => mmtStudentApi.replaceChoices(normalizeAdmissionChoices(selectedIds)),
    onSuccess: async (data) => {
      const ordered = [...data].sort((a, b) => a.priorityOrder - b.priorityOrder);
      const ids = ordered.map((choice) => choice.admissionProgram.id);
      setSavedChoices(ordered); setServerSnapshot(ids); setSelectedIds(ids); setEditing(false);
      setNotice({ type: "success", text: t("student.choicesSaved") });
      await Promise.all([queryClient.invalidateQueries({ queryKey: mmtStudentKeys.choices() }), queryClient.invalidateQueries({ queryKey: mmtStudentKeys.profile() })]);
    },
    onError: (error) => setNotice({ type: "error", text: friendlyError(error, t("student.loadFailed"), t("student.clusterLocked")) }),
  });
  const dirty = selectedIds.join("|") !== serverSnapshot.join("|");
  const summaryMode = savedChoices.length === maximumAdmissionChoices && !editing;

  if (profile.isLoading) return <PageLoading />;
  if (profile.isError && !profileMissing) return <PageError onRetry={() => void profile.refetch()} />;

  return <div className="grid gap-5">
    <StudentPageHeader title={t("student.mmtTitle")} description={t("student.mmtDescription")} />
    {notice ? <Notice notice={notice} onClose={() => setNotice(null)} /> : null}
    <ClusterSetup profile={profile.data} missing={profileMissing} clusterId={clusterId} clusterLabel={clusterLabel} clusterSearch={clusterSearch} clusterPage={clusterPage} clusters={clusters.data?.items ?? []} total={clusters.data?.totalCount ?? 0} loading={clusters.isFetching} pending={createProfile.isPending} onCluster={(id, item) => { setClusterId(id); setClusterLabel(`${item.code} - ${item.name}`); }} onSearch={setClusterSearch} onPage={setClusterPage} onSave={() => createProfile.mutate()} />
    {profile.data && summaryMode ? <ChoiceSummary choices={savedChoices} onEdit={() => setEditing(true)} /> : null}
    {profile.data && !summaryMode ? <section className="grid gap-5 xl:grid-cols-[minmax(0,1.3fr)_minmax(340px,0.7fr)]">
      <div className="grid min-w-0 gap-5">
        <article className="rounded-lg border border-[var(--student-border)] bg-[var(--student-surface)] p-5 shadow-[0_10px_28px_rgb(20_31_70/0.04)] sm:p-6">
          <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between"><div><h2 className="text-lg font-black">{t("student.availablePrograms")}</h2><p className="mt-1 text-sm text-[var(--student-muted)]">{t("student.mmtChoicesDescription")}</p></div><button type="button" onClick={() => setFiltersOpen((value) => !value)} className="inline-flex min-h-10 items-center justify-center gap-2 rounded-lg border border-[var(--student-border)] px-4 text-sm font-bold"><Filter className="h-4 w-4" />{t("student.filters")}</button></div>
          <label className="relative mt-4 block"><Search className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-[var(--student-muted)]" /><input value={search} onChange={(event) => { setSearch(event.target.value); setPage(1); }} placeholder={t("student.programSearch")} className="min-h-11 w-full rounded-lg border border-[var(--student-border)] bg-[var(--student-surface-soft)] pl-9 pr-3 text-sm outline-none focus:border-[#8b83f5]" /></label>
          {filtersOpen ? <div className="mt-4 grid gap-3 rounded-lg border border-[var(--student-border)] bg-[var(--student-surface-soft)] p-4 md:grid-cols-2">
            <StudentMmtLookupSelect<SpecialtyDto> value={specialtyId} selectedLabel={specialtyLabel} items={specialties.data?.items ?? []} getLabel={(item) => `${item.code} - ${item.name}`} onValueChange={(id, item) => { setSpecialtyId(id); setSpecialtyLabel(`${item.code} - ${item.name}`); setUniversityId(""); setUniversityLabel(""); setPage(1); }} search={specialtySearch} onSearchChange={setSpecialtySearch} page={specialtyPage} totalCount={specialties.data?.totalCount ?? 0} onPageChange={setSpecialtyPage} placeholder={t("student.chooseSpecialty")} searchPlaceholder={t("student.searchSpecialties")} loading={specialties.isFetching} />
            <StudentMmtLookupSelect<UniversityDto> value={universityId} selectedLabel={universityLabel} items={universities.data?.items ?? []} getLabel={(item) => item.shortName || item.fullName} onValueChange={(id, item) => { setUniversityId(id); setUniversityLabel(item.shortName || item.fullName); setPage(1); }} search={universitySearch} onSearchChange={setUniversitySearch} page={universityPage} totalCount={universities.data?.totalCount ?? 0} onPageChange={setUniversityPage} placeholder={t("student.chooseUniversity")} searchPlaceholder={t("student.searchUniversities")} loading={universities.isFetching} />
            <FilterSelect value={admissionType} onChange={setAdmissionType} placeholder={t("mmt.allAdmissionTypes")} labels={labels.admissionTypes} />
            <FilterSelect value={studyForm} onChange={setStudyForm} placeholder={t("mmt.allStudyForms")} labels={labels.studyForms} />
            <FilterSelect value={studyLanguage} onChange={setStudyLanguage} placeholder={t("mmt.allLanguages")} labels={labels.studyLanguages} />
            <button type="button" onClick={() => { setSpecialtyId(""); setSpecialtyLabel(""); setUniversityId(""); setUniversityLabel(""); setAdmissionType(""); setStudyForm(""); setStudyLanguage(""); setPage(1); }} className="inline-flex min-h-11 items-center justify-center gap-2 rounded-lg border border-[var(--student-border)] text-sm font-bold"><X className="h-4 w-4" />{t("student.clearFilters")}</button>
          </div> : null}
          {programs.isFetching ? <div className="grid min-h-32 place-items-center"><LoaderCircle className="h-6 w-6 animate-spin text-[#5146f0]" /></div> : null}
          {!programs.isFetching && programs.data?.items.length === 0 ? <p className="mt-6 rounded-lg bg-[var(--student-surface-soft)] p-5 text-center text-sm text-[var(--student-muted)]">{t("student.noPrograms")}</p> : null}
          <div className="mt-4 grid gap-3">{programs.data?.items.map((program) => <ProgramRow key={program.id} program={program} selected={selectedIds.includes(program.id)} onAdd={() => { const result = addAdmissionChoice(selectedIds, program.id); if (result.error) { setNotice({ type: "error", text: result.error === "limit" ? t("student.choiceLimit") : t("student.duplicateChoice") }); return; } setProgramsById((current) => ({ ...current, [program.id]: program })); setSelectedIds(result.choices); }} />)}</div>
          {programs.data && programs.data.totalCount > pageSize ? <Pagination page={page} totalCount={programs.data.totalCount} onPageChange={setPage} /> : null}
        </article>
      </div>
      <SelectedChoices ids={selectedIds} programs={programsById} dirty={dirty} pending={saveChoices.isPending} canSave={selectedIds.length === maximumAdmissionChoices && dirty} onChange={setSelectedIds} onSave={() => saveChoices.mutate()} onCancel={() => { setSelectedIds(serverSnapshot); setEditing(false); }} />
    </section> : null}
  </div>;

  function ProgramRow({ program, selected, onAdd }: { program: AdmissionProgramListItemDto; selected: boolean; onAdd: () => void }) {
    return <div className="grid gap-3 rounded-lg border border-[var(--student-border)] p-4 sm:grid-cols-[40px_minmax(0,1fr)_auto] sm:items-center"><span className="grid h-10 w-10 place-items-center rounded-lg bg-[#edf2ff] text-[#2867d8]"><University className="h-5 w-5" /></span><div className="min-w-0"><OverflowMarquee text={`${program.specialtyCode} - ${program.specialtyName}`} className="font-black" /><OverflowMarquee text={program.universityName} className="mt-1 text-sm text-[var(--student-muted)]" /><div className="mt-2 grid gap-1 text-xs font-bold text-[var(--student-muted)] sm:grid-cols-2"><span>{t("mmt.studyLocation")}: {program.studyLocation}</span><span>{enumLabel(labels.admissionTypes, program.admissionType, labels.unknown)}{program.tuitionFeeTjs != null ? ` · ${program.tuitionFeeTjs} TJS` : ""}</span><span>{enumLabel(labels.studyForms, program.studyForm, labels.unknown)} · {enumLabel(labels.studyLanguages, program.studyLanguage, labels.unknown)}</span><span>{t("mmt.seats")}: {program.seatsCount ?? "-"} · {t("student.latestPassingScore")}: {program.latestPassingScore ?? "-"}</span></div></div><button type="button" disabled={selected} onClick={onAdd} className="inline-flex min-h-10 items-center justify-center gap-2 rounded-lg border border-[#5146f0] px-4 text-sm font-black text-[#5146f0] disabled:bg-[#f0efff] disabled:opacity-60">{selected ? <Check className="h-4 w-4" /> : <Plus className="h-4 w-4" />}{selected ? t("student.selected") : t("student.addChoice")}</button></div>;
  }
}

function ClusterSetup({ profile, missing, clusterId, clusterLabel, clusterSearch, clusterPage, clusters, total, loading, pending, onCluster, onSearch, onPage, onSave }: { profile: { cluster: MmtClusterDto } | undefined; missing: boolean; clusterId: string; clusterLabel: string; clusterSearch: string; clusterPage: number; clusters: MmtClusterDto[]; total: number; loading: boolean; pending: boolean; onCluster: (id: string, item: MmtClusterDto) => void; onSearch: (value: string) => void; onPage: (page: number) => void; onSave: () => void }) {
  const { t } = useTranslation();
  return <section className="rounded-lg border border-[var(--student-border)] bg-[var(--student-surface)] p-5 sm:p-6"><div className="flex items-start gap-4"><span className="grid h-11 w-11 place-items-center rounded-lg bg-[#f0efff] text-[#5146f0]">{profile ? <LockKeyhole className="h-5 w-5" /> : <BookOpen className="h-5 w-5" />}</span><div><h2 className="text-lg font-black">{t("student.mmtSetupTitle")}</h2><p className="mt-1 text-sm text-[var(--student-muted)]">{t("student.mmtSetupDescription")}</p></div></div>{profile ? <div className="mt-5 rounded-lg border border-[#d9d6ff] bg-[#f7f6ff] p-4 font-black">{profile.cluster.code} - {profile.cluster.name}</div> : missing ? <div className="mt-5 grid gap-3 sm:grid-cols-[minmax(0,1fr)_auto]"><StudentMmtLookupSelect value={clusterId} selectedLabel={clusterLabel} items={clusters} getLabel={(item: MmtClusterDto) => `${item.code} - ${item.name}`} onValueChange={onCluster} search={clusterSearch} onSearchChange={onSearch} page={clusterPage} totalCount={total} onPageChange={onPage} placeholder={t("student.chooseCluster")} searchPlaceholder={t("student.searchClusters")} loading={loading} /><button type="button" disabled={!clusterId || pending} onClick={onSave} className="inline-flex min-h-12 items-center justify-center gap-2 rounded-lg bg-[#5146f0] px-5 text-sm font-black text-white disabled:opacity-50">{pending ? <LoaderCircle className="h-4 w-4 animate-spin" /> : <Check className="h-4 w-4" />}{t("student.saveCluster")}</button></div> : null}</section>;
}

function SelectedChoices({ ids, programs, dirty, pending, canSave, onChange, onSave, onCancel }: { ids: string[]; programs: Record<string, AdmissionProgramListItemDto>; dirty: boolean; pending: boolean; canSave: boolean; onChange: (ids: string[]) => void; onSave: () => void; onCancel: () => void }) {
  const { t } = useTranslation();
  return <article className="h-fit rounded-lg border border-[var(--student-border)] bg-[var(--student-surface)] p-5 sm:p-6 xl:sticky xl:top-24"><div className="flex items-center justify-between"><h2 className="text-lg font-black">{t("student.selectedChoices")}</h2><span className="rounded-md bg-[#f0efff] px-2.5 py-1 text-xs font-black text-[#5146f0]">{ids.length}/12</span></div>{ids.length === 0 ? <p className="mt-5 rounded-lg bg-[var(--student-surface-soft)] p-5 text-center text-sm text-[var(--student-muted)]">{t("student.noChoices")}</p> : null}<ol className="mt-4 grid gap-2">{ids.map((id, index) => { const program = programs[id]; if (!program) return null; return <li key={id} className="grid grid-cols-[32px_minmax(0,1fr)_auto] items-center gap-2 rounded-lg border border-[var(--student-border)] p-3"><span className="grid h-8 w-8 place-items-center rounded-md bg-[#f0efff] text-xs font-black text-[#5146f0]">{index + 1}</span><div className="min-w-0"><OverflowMarquee text={`${program.specialtyCode} - ${program.specialtyName}`} className="text-sm font-black" /><OverflowMarquee text={program.universityName} className="mt-1 text-xs text-[var(--student-muted)]" /></div><div className="flex gap-1"><IconButton label={t("student.moveChoiceUp")} disabled={index === 0} onClick={() => onChange(moveAdmissionChoice(ids, index, -1))}><ArrowUp /></IconButton><IconButton label={t("student.moveChoiceDown")} disabled={index === ids.length - 1} onClick={() => onChange(moveAdmissionChoice(ids, index, 1))}><ArrowDown /></IconButton><IconButton label={t("student.removeChoice")} danger onClick={() => onChange(ids.filter((value) => value !== id))}><Trash2 /></IconButton></div></li>; })}</ol><p className="mt-4 text-xs font-bold text-[var(--student-muted)]">{dirty ? t("student.unsavedChanges") : t("student.noUnsavedChanges")}</p><div className="mt-3 grid grid-cols-2 gap-2"><button type="button" disabled={!dirty || pending} onClick={onCancel} className="min-h-11 rounded-lg border border-[var(--student-border)] text-sm font-black disabled:opacity-40">{t("student.cancel")}</button><button type="button" disabled={!canSave || pending} onClick={onSave} className="inline-flex min-h-11 items-center justify-center gap-2 rounded-lg bg-[#5146f0] text-sm font-black text-white disabled:opacity-40">{pending ? <LoaderCircle className="h-4 w-4 animate-spin" /> : <Check className="h-4 w-4" />}{pending ? t("student.saving") : t("student.saveChoices")}</button></div>{ids.length !== 12 ? <p className="mt-2 text-center text-xs text-[var(--student-muted)]">{t("student.chooseExactlyTwelve")}</p> : null}</article>;
}

function ChoiceSummary({ choices, onEdit }: { choices: StudentAdmissionChoiceDto[]; onEdit: () => void }) {
  const { t } = useTranslation(); const labels = useMmtLabels();
  return <section className="rounded-lg border border-[var(--student-border)] bg-[var(--student-surface)] p-5 sm:p-6"><div className="flex items-center justify-between gap-3"><div><h2 className="text-lg font-black">{t("student.selectedChoices")}</h2><p className="mt-1 text-sm text-[var(--student-muted)]">{t("student.savedChoiceSummary")}</p></div><button type="button" onClick={onEdit} className="inline-flex min-h-10 items-center gap-2 rounded-lg border border-[#5146f0] px-4 text-sm font-black text-[#5146f0]"><Pencil className="h-4 w-4" />{t("student.editChoices")}</button></div><div className="mt-5 grid gap-3">{choices.map((choice) => { const program = choice.admissionProgram; return <article key={choice.id} className="grid gap-3 rounded-lg border border-[var(--student-border)] p-4 md:grid-cols-[44px_minmax(0,1fr)_minmax(220px,0.45fr)]"><span className="grid h-11 w-11 place-items-center rounded-lg bg-[#f0efff] font-black text-[#5146f0]">{choice.priorityOrder}</span><div className="min-w-0"><OverflowMarquee text={`${program.specialtyCode} - ${program.specialtyName}`} className="font-black" /><OverflowMarquee text={program.universityName} className="mt-1 text-sm text-[var(--student-muted)]" /><div className="mt-2 flex flex-wrap gap-x-4 gap-y-1 text-xs font-bold text-[var(--student-muted)]"><span>{program.studyLocation}</span><span>{enumLabel(labels.admissionTypes, program.admissionType, labels.unknown)}{program.tuitionFeeTjs != null ? ` · ${program.tuitionFeeTjs} TJS` : ""}</span><span>{enumLabel(labels.studyForms, program.studyForm, labels.unknown)}</span><span>{enumLabel(labels.studyLanguages, program.studyLanguage, labels.unknown)}</span><span>{t("mmt.seats")}: {program.seatsCount ?? "-"}</span></div></div><ScoreHistory programId={program.id} recent={choice.recentPassingScores ?? []} /></article>; })}</div></section>;
}

function ScoreHistory({ programId, recent }: { programId: string; recent: PassingScoreHistoryDto[] }) {
  const { t } = useTranslation(); const [expanded, setExpanded] = useState(false);
  const all = useQuery({ queryKey: mmtStudentKeys.scores(programId), queryFn: () => mmtStudentApi.scores(programId), enabled: expanded });
  const rows = expanded ? all.data ?? recent : recent;
  return <div className="rounded-lg bg-[var(--student-surface-soft)] p-3"><p className="text-xs font-black uppercase text-[var(--student-muted)]">{t("student.passingScoreHistory")}</p>{rows.length ? <ul className="mt-2 grid gap-1 text-sm">{rows.map((score) => <li key={score.id} className="flex justify-between gap-3"><span>{score.year}{score.distributionRound ? ` · ${score.distributionRound}` : ""}</span><strong>{score.passingScore}</strong></li>)}</ul> : <p className="mt-2 text-xs text-[var(--student-muted)]">{t("student.scoreUnavailable")}</p>}<button type="button" disabled={all.isFetching} onClick={() => setExpanded((value) => !value)} className="mt-2 text-xs font-black text-[#5146f0]">{all.isFetching ? t("student.loading") : expanded ? t("student.showRecent") : t("student.allYears")}</button></div>;
}

function FilterSelect({ value, onChange, placeholder, labels }: { value: string; onChange: (value: string) => void; placeholder: string; labels: string[] }) { return <SelectField value={value} onValueChange={onChange} placeholder={placeholder} options={[{ value: "", label: placeholder }, ...labels.map((label, index) => ({ value: String(index), label }))]} />; }
function Pagination({ page, totalCount, onPageChange }: { page: number; totalCount: number; onPageChange: (page: number) => void }) { const { t } = useTranslation(); const pages = Math.max(1, Math.ceil(totalCount / pageSize)); return <div className="mt-4 flex items-center justify-end gap-2 text-xs font-black text-[var(--student-muted)]"><span>{page} / {pages}</span><IconButton label={t("student.previousPage")} disabled={page <= 1} onClick={() => onPageChange(page - 1)}><ChevronLeft /></IconButton><IconButton label={t("student.nextPage")} disabled={page >= pages} onClick={() => onPageChange(page + 1)}><ChevronRight /></IconButton></div>; }
function IconButton({ label, onClick, disabled = false, danger = false, children }: { label: string; onClick: () => void; disabled?: boolean; danger?: boolean; children: ReactElement<{ className?: string }> }) { return <button type="button" title={label} aria-label={label} disabled={disabled} onClick={onClick} className={`grid h-8 w-8 place-items-center rounded-md border border-[var(--student-border)] disabled:opacity-35 ${danger ? "text-[#d94848]" : "text-[var(--student-muted)]"}`}>{children}</button>; }
function Notice({ notice, onClose }: { notice: { type: "success" | "error"; text: string }; onClose: () => void }) { const { t } = useTranslation(); return <div role="status" className={`flex items-center justify-between gap-3 rounded-lg border px-4 py-3 text-sm font-bold ${notice.type === "success" ? "border-[#bce6c8] bg-[#effbf2] text-[#258b46]" : "border-[#f0c7c7] bg-[#fff4f4] text-[#bc4141]"}`}><span>{notice.text}</span><button type="button" onClick={onClose} aria-label={t("student.closeNotice")}><X className="h-4 w-4" /></button></div>; }
function PageLoading() { return <div className="grid min-h-80 place-items-center"><LoaderCircle className="h-7 w-7 animate-spin text-[#5146f0]" /></div>; }
function PageError({ onRetry }: { onRetry: () => void }) { const { t } = useTranslation(); return <div className="grid min-h-80 place-items-center rounded-lg border border-[var(--student-border)] bg-[var(--student-surface)] p-6 text-center"><div><p className="font-bold text-[var(--student-muted)]">{t("student.loadFailed")}</p><button type="button" onClick={onRetry} className="mt-4 rounded-lg bg-[#5146f0] px-4 py-2 text-sm font-black text-white">{t("student.retry")}</button></div></div>; }
function friendlyError(error: unknown, fallback: string, clusterLocked: string) { if (error instanceof ApiError) { if (error.problem?.code === "mmt.cluster_locked") return clusterLocked; return error.problem?.title || fallback; } return fallback; }
