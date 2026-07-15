import { useQuery } from "@tanstack/react-query";
import { ArrowLeft, Eye, Plus } from "lucide-react";
import { useCallback, useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { Link, useParams, useSearchParams } from "react-router-dom";
import { mmtApi, mmtKeys } from "@/features/mmt/api/mmt.api";
import { controlLink, enumLabel, errorMessage, mmtDefaultPageSize, mmtPage } from "@/features/mmt/lib/mmt";
import { useMmtLabels } from "@/features/mmt/lib/useMmtLabels";
import type { SpecialtyDto, UniversityDto } from "@/features/mmt/model/mmt.types";
import { BooleanBadge, Pagination } from "@/features/mmt/ui/MmtUi";
import { MmtFilterToolbar } from "@/features/mmt/ui/MmtFilterToolbar";
import { MmtReferenceSelect } from "@/features/mmt/ui/MmtReferenceSelect";
import { Input } from "@/shared/ui/Input";
import { PageHeader } from "@/shared/ui/PageHeader";
import { SelectField } from "@/shared/ui/SelectField";
import { EmptyState, ErrorState } from "@/shared/ui/StateBlock";
import { Table, TableShell } from "@/shared/ui/Table";

type DetailKind = "universities" | "specialties";

export function MmtReferenceDetailPage({ kind }: { kind: DetailKind }) {
  const { t } = useTranslation();
  const labels = useMmtLabels();
  const routeParams = useParams();
  const id = kind === "universities" ? routeParams.universityId ?? "" : routeParams.specialtyId ?? "";
  const [params, setParams] = useSearchParams();
  const search = params.get("search") ?? "";
  const [searchInput, setSearchInput] = useState(search);
  const page = mmtPage(params.get("page"));
  const oppositeId = params.get(kind === "universities" ? "specialtyId" : "universityId") ?? "";
  const queryParams = {
    search: search || undefined,
    universityId: kind === "universities" ? id : oppositeId || undefined,
    specialtyId: kind === "specialties" ? id : oppositeId || undefined,
    clusterId: params.get("clusterId") || undefined,
    admissionYear: numberParam(params.get("admissionYear")),
    admissionType: numberParam(params.get("admissionType")),
    isPublished: booleanParam(params.get("published")),
    isActive: booleanParam(params.get("active")),
    page,
    pageSize: mmtDefaultPageSize,
  };
  const detail = useQuery({
    queryKey: mmtKeys.catalogDetail(kind, id),
    queryFn: () => mmtApi.catalogDetail<UniversityDto | SpecialtyDto>(kind, id),
    enabled: Boolean(id),
  });
  const programs = useQuery({
    queryKey: mmtKeys.programs(queryParams),
    queryFn: () => mmtApi.programs(queryParams),
    enabled: Boolean(id),
  });
  const setFilter = useCallback((key: string, value: string) => {
    const next = new URLSearchParams(params);
    if (value) next.set(key, value); else next.delete(key);
    if (key !== "page") next.set("page", "1");
    setParams(next);
  }, [params, setParams]);
  useEffect(() => {
    if (searchInput === search) return;
    const timeout = window.setTimeout(() => setFilter("search", searchInput.trim()), 350);
    return () => window.clearTimeout(timeout);
  }, [search, searchInput, setFilter]);
  const filterKeys = [kind === "universities" ? "specialtyId" : "universityId", "clusterId", "admissionYear", "admissionType", "published", "active"];
  const filterCount = filterKeys.filter((key) => params.has(key)).length;
  function clearFilters() {
    const next = new URLSearchParams();
    if (search) next.set("search", search);
    next.set("page", "1");
    setParams(next);
  }

  if (detail.isLoading) return <p className="text-sm text-[var(--muted)]">{t("mmt.loading")}</p>;
  if (detail.isError || !detail.data) return <ErrorState title={t("mmt.loadFailed")} description={errorMessage(detail.error, t("mmt.loadFailed"))} />;

  const title = kind === "universities" ? (detail.data as UniversityDto).fullName : `${(detail.data as SpecialtyDto).code} · ${(detail.data as SpecialtyDto).name}`;
  const description = kind === "universities" ? t("mmt.universityProgramsDescription") : t("mmt.specialtyProgramsDescription");
  const createParam = kind === "universities" ? `universityId=${id}` : `specialtyId=${id}`;
  return (
    <>
      <PageHeader title={title} description={description} actions={<>
        <Link className={controlLink} to={`/admin/mmt/${kind}`}><ArrowLeft className="h-4 w-4" /> {t("mmt.back")}</Link>
        <Link className={controlLink} to={`/admin/mmt/programs/new?${createParam}`}><Plus className="h-4 w-4" /> {t("mmt.addAdmissionCombination")}</Link>
      </>} />
      <MmtFilterToolbar searchValue={searchInput} onSearchChange={setSearchInput} searchPlaceholder={`${t("mmt.university")}, ${t("mmt.specialty")}, ${t("mmt.code")}`} filterCount={filterCount} onClearFilters={clearFilters}>
        <MmtReferenceSelect kind={kind === "universities" ? "specialties" : "universities"} value={oppositeId} onValueChange={(value) => setFilter(kind === "universities" ? "specialtyId" : "universityId", value)} activeOnly={false} allLabel={kind === "universities" ? t("mmt.allSpecialties") : t("mmt.allUniversities")} placeholder={kind === "universities" ? t("mmt.allSpecialties") : t("mmt.allUniversities")} />
        <MmtReferenceSelect kind="clusters" value={params.get("clusterId") ?? ""} onValueChange={(value) => setFilter("clusterId", value)} activeOnly={false} allLabel={t("mmt.allClusters")} placeholder={t("mmt.allClusters")} />
        <Input type="number" min="2000" max="2100" value={params.get("admissionYear") ?? ""} onChange={(event) => setFilter("admissionYear", event.target.value)} placeholder={t("mmt.admissionYear")} />
        <FilterSelect value={params.get("admissionType") ?? ""} all={t("mmt.allAdmissionTypes")} labels={labels.admissionTypes} onChange={(value) => setFilter("admissionType", value)} />
        <StatusSelect value={params.get("published") ?? ""} all={t("mmt.allPublishStates")} yes={t("mmt.published")} no={t("mmt.draft")} onChange={(value) => setFilter("published", value)} />
        <StatusSelect value={params.get("active") ?? ""} all={t("mmt.allStatuses")} yes={t("mmt.active")} no={t("mmt.inactive")} onChange={(value) => setFilter("active", value)} />
      </MmtFilterToolbar>
      {programs.isError ? <ErrorState title={t("mmt.programLoadFailed")} description={errorMessage(programs.error, t("mmt.programLoadFailed"))} /> : null}
      {programs.data?.items.length === 0 ? <EmptyState title={t("mmt.noAdmissionCombinations")} description={t("mmt.noAdmissionCombinationsHint")} /> : null}
      {programs.data?.items.length ? <TableShell><Table>
        <thead className="bg-[var(--surface-muted)] text-xs uppercase text-[var(--muted)]"><tr>
          <th className="px-4 py-3">{kind === "universities" ? t("mmt.specialty") : t("mmt.university")}</th><th className="px-4 py-3">{t("mmt.cluster")}</th><th className="px-4 py-3">{t("mmt.admissionType")}</th><th className="px-4 py-3">{t("mmt.studyForm")}</th><th className="px-4 py-3">{t("mmt.studyLanguage")}</th><th className="px-4 py-3">{t("mmt.year")}</th><th className="px-4 py-3">{t("mmt.latestMainScore")}</th><th className="px-4 py-3">{t("mmt.status")}</th><th className="px-4 py-3" /></tr></thead>
        <tbody>{programs.data.items.map((program) => <tr key={program.id} className="border-t border-[var(--border)]">
          <td className="px-4 py-3 font-bold">{kind === "universities" ? `${program.specialtyCode} · ${program.specialtyName}` : program.universityName}</td><td className="px-4 py-3">{program.clusterCode}</td><td className="px-4 py-3">{enumLabel(labels.admissionTypes, program.admissionType, labels.unknown)}</td><td className="px-4 py-3">{enumLabel(labels.studyForms, program.studyForm, labels.unknown)}</td><td className="px-4 py-3">{enumLabel(labels.studyLanguages, program.studyLanguage, labels.unknown)}</td><td className="px-4 py-3">{program.admissionYear}</td><td className="px-4 py-3 font-bold">{program.latestPassingScore?.toFixed(2) ?? "—"}</td><td className="px-4 py-3"><div className="flex gap-2"><BooleanBadge value={program.isPublished} positive={t("mmt.published")} negative={t("mmt.draft")} /><BooleanBadge value={program.isActive} /></div></td><td className="px-4 py-3"><Link className={controlLink} to={`/admin/mmt/programs/${program.id}`} aria-label={t("mmt.open")}><Eye className="h-4 w-4" /></Link></td>
        </tr>)}</tbody>
      </Table></TableShell> : null}
      {programs.data && programs.data.totalCount > programs.data.pageSize ? <Pagination page={programs.data.page} pageSize={programs.data.pageSize} total={programs.data.totalCount} onPage={(value) => setFilter("page", String(value))} /> : null}
    </>
  );
}

function numberParam(value: string | null) { return value && /^\d+$/.test(value) ? Number(value) : undefined; }
function booleanParam(value: string | null) { return value === "true" ? true : value === "false" ? false : undefined; }
function FilterSelect({ value, all, labels, onChange }: { value: string; all: string; labels: string[]; onChange: (value: string) => void }) { return <SelectField value={value} options={[{ value: "", label: all }, ...labels.map((label, index) => ({ value: String(index), label }))]} onValueChange={onChange} />; }
function StatusSelect({ value, all, yes, no, onChange }: { value: string; all: string; yes: string; no: string; onChange: (value: string) => void }) { return <SelectField value={value} options={[{ value: "", label: all }, { value: "true", label: yes }, { value: "false", label: no }]} onValueChange={onChange} />; }
