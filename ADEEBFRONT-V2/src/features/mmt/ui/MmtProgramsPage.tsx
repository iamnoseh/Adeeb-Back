import {
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import { Eye, PenLine, Plus, Power, Send } from "lucide-react";
import { useCallback, useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { Link, useSearchParams } from "react-router-dom";
import { mmtApi, mmtKeys } from "@/features/mmt/api/mmt.api";
import { controlLink, enumLabel, errorMessage, mmtAdmissionYear, mmtDefaultPageSize, mmtPage } from "@/features/mmt/lib/mmt";
import { useMmtLabels } from "@/features/mmt/lib/useMmtLabels";
import { useMmtToast } from "@/features/mmt/model/useMmtToast";
import { BooleanBadge, MmtToast, Pagination } from "@/features/mmt/ui/MmtUi";
import { MmtFilterToolbar } from "@/features/mmt/ui/MmtFilterToolbar";
import { useColumnVisibility, type AdminListColumn } from "@/shared/ui/useColumnVisibility";
import { MmtReferenceSelect } from "@/features/mmt/ui/MmtReferenceSelect";
import { Input } from "@/shared/ui/Input";
import { PageHeader } from "@/shared/ui/PageHeader";
import { SelectField } from "@/shared/ui/SelectField";
import { EmptyState, ErrorState } from "@/shared/ui/StateBlock";
import { Table, TableShell } from "@/shared/ui/Table";
import { TableActionButton } from "@/shared/ui/TableActionButton";

export function MmtProgramsPage() {
  const { t } = useTranslation();
  const labels = useMmtLabels();
  const [params, setParams] = useSearchParams();
  const search = params.get("search") ?? "";
  const [searchInput, setSearchInput] = useState(search);
  const page = mmtPage(params.get("page"));
  const filters = {
    search: params.get("search") || undefined,
    clusterId: params.get("clusterId") || undefined,
    universityId: params.get("universityId") || undefined,
    specialtyId: params.get("specialtyId") || undefined,
    admissionType: optionalNumber(params.get("admissionType")),
    studyForm: optionalNumber(params.get("studyForm")),
    studyLanguage: optionalNumber(params.get("studyLanguage")),
    admissionYear: mmtAdmissionYear(params.get("admissionYear")),
    isPublished: optionalBoolean(params.get("isPublished")),
    isActive: optionalBoolean(params.get("isActive")),
    page,
    pageSize: mmtDefaultPageSize,
  };
  const query = useQuery({
    queryKey: mmtKeys.programs(filters),
    queryFn: () => mmtApi.programs(filters),
  });
  const queryClient = useQueryClient();
  const toast = useMmtToast();
  const columns: AdminListColumn[] = [
    { id: "program", label: `${t("mmt.university")} / ${t("mmt.specialty")}`, locked: true },
    { id: "cluster", label: t("mmt.cluster") },
    { id: "admission", label: `${t("mmt.admissionType")} / ${t("mmt.studyForm")}` },
    { id: "language", label: t("mmt.studyLanguage") },
    { id: "year", label: t("mmt.year") },
    { id: "seats", label: t("mmt.seats") },
    { id: "score", label: t("mmt.latestScore") },
    { id: "visibility", label: t("mmt.visibility") },
    { id: "actions", label: t("mmt.actions"), locked: true },
  ];
  const columnVisibility = useColumnVisibility("adeeb.columns.mmt.programs", columns);
  const status = useMutation({
    mutationFn: ({ id, value }: { id: string; value: boolean }) =>
      mmtApi.setProgramStatus(id, value),
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ["mmt", "programs"] }),
        queryClient.invalidateQueries({ queryKey: mmtKeys.dashboard() }),
      ]);
      toast.success(t("mmt.statusSaved"));
    },
    onError: (error) =>
      toast.error(errorMessage(error, t("mmt.requestFailed"))),
  });
  const publish = useMutation({
    mutationFn: ({ id, value }: { id: string; value: boolean }) =>
      mmtApi.setProgramPublished(id, value),
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ["mmt", "programs"] }),
        queryClient.invalidateQueries({ queryKey: mmtKeys.dashboard() }),
      ]);
      toast.success(t("mmt.publicationSaved"));
    },
    onError: (error) =>
      toast.error(errorMessage(error, t("mmt.requestFailed"))),
  });

  const setFilter = useCallback(
    (key: string, value: string) => {
      const next = new URLSearchParams(params);
      next.delete("pageSize");
      if (value) next.set(key, value);
      else next.delete(key);
      if (key !== "page") next.set("page", "1");
      setParams(next);
    },
    [params, setParams],
  );

  useEffect(() => {
    if (searchInput === search) return;
    const timeout = window.setTimeout(
      () => setFilter("search", searchInput.trim()),
      350,
    );
    return () => window.clearTimeout(timeout);
  }, [search, searchInput, setFilter]);

  const filterKeys = ["clusterId", "universityId", "specialtyId", "admissionType", "studyForm", "studyLanguage", "admissionYear", "isPublished", "isActive"];
  const filterCount = filterKeys.filter((key) => params.has(key)).length;
  function clearFilters() {
    const next = new URLSearchParams();
    if (search) next.set("search", search);
    next.set("page", "1");
    setParams(next);
  }

  return (
    <>
      <PageHeader
        title={t("mmt.programsTitle")}
        description={t("mmt.programsDescription")}
        actions={
          <Link
            to="/admin/mmt/programs/new"
            className={`${controlLink} !border-transparent !bg-[var(--primary)] !text-white hover:!bg-[var(--primary)] hover:opacity-90`}
          >
            <Plus className="h-4 w-4" /> {t("mmt.newProgram")}
          </Link>
        }
      />
      <MmtFilterToolbar
        searchValue={searchInput}
        onSearchChange={setSearchInput}
        searchPlaceholder={`${t("mmt.university")}, ${t("mmt.specialty")}, ${t("mmt.code")}`}
        filterCount={filterCount}
        onClearFilters={clearFilters}
        columns={columns}
        columnVisibility={columnVisibility}
      >
        <MmtReferenceSelect kind="clusters" value={filters.clusterId ?? ""} onValueChange={(value) => setFilter("clusterId", value)} placeholder={t("mmt.allClusters")} allLabel={t("mmt.allClusters")} activeOnly={false} />
        <MmtReferenceSelect kind="universities" value={filters.universityId ?? ""} onValueChange={(value) => setFilter("universityId", value)} placeholder={t("mmt.allUniversities")} allLabel={t("mmt.allUniversities")} activeOnly={false} />
        <MmtReferenceSelect kind="specialties" value={filters.specialtyId ?? ""} onValueChange={(value) => setFilter("specialtyId", value)} placeholder={t("mmt.allSpecialties")} allLabel={t("mmt.allSpecialties")} activeOnly={false} />
        <FilterSelect
          label={t("mmt.allAdmissionTypes")}
          value={params.get("admissionType") ?? ""}
          onChange={(value) => setFilter("admissionType", value)}
          options={labels.admissionTypes.map((label, value) => ({
            value: String(value),
            label,
          }))}
        />
        <FilterSelect
          label={t("mmt.allStudyForms")}
          value={params.get("studyForm") ?? ""}
          onChange={(value) => setFilter("studyForm", value)}
          options={labels.studyForms.map((label, value) => ({
            value: String(value),
            label,
          }))}
        />
        <FilterSelect
          label={t("mmt.allLanguages")}
          value={params.get("studyLanguage") ?? ""}
          onChange={(value) => setFilter("studyLanguage", value)}
          options={labels.studyLanguages.map((label, value) => ({
            value: String(value),
            label,
          }))}
        />
        <Input
          type="number"
          min="2000"
          max="2100"
          placeholder={t("mmt.admissionYear")}
          value={params.get("admissionYear") ?? ""}
          onChange={(event) => setFilter("admissionYear", event.target.value)}
        />
        <FilterSelect
          label={t("mmt.publishedOrDraft")}
          value={params.get("isPublished") ?? ""}
          onChange={(value) => setFilter("isPublished", value)}
          options={[
            { value: "true", label: t("mmt.published") },
            { value: "false", label: t("mmt.draft") },
          ]}
        />
        <FilterSelect
          label={t("mmt.activeOrInactive")}
          value={params.get("isActive") ?? ""}
          onChange={(value) => setFilter("isActive", value)}
          options={[
            { value: "true", label: t("mmt.active") },
            { value: "false", label: t("mmt.inactive") },
          ]}
        />
      </MmtFilterToolbar>
      {query.isLoading ? (
        <p className="text-sm text-[var(--muted)]">
          {t("mmt.loadingPrograms")}
        </p>
      ) : null}
      {query.isError ? (
        <ErrorState
          title={t("mmt.loadFailed")}
          description={errorMessage(query.error, t("mmt.loadFailed"))}
        />
      ) : null}
      {query.data?.items.length === 0 ? (
        <EmptyState
          title={t("mmt.noPrograms")}
          description={t("mmt.noProgramsHint")}
        />
      ) : null}
      {query.data && query.data.items.length > 0 ? (
        <TableShell>
          <Table>
            <thead className="bg-[var(--surface-muted)] text-xs uppercase text-[var(--muted)]">
              <tr>
                <th className="px-3 py-3">
                  {t("mmt.university")} / {t("mmt.specialty")}
                </th>
                {columnVisibility.isVisible("cluster") ? <th className="px-3 py-3">{t("mmt.cluster")}</th> : null}
                {columnVisibility.isVisible("admission") ? <th className="px-3 py-3">
                  {t("mmt.admissionType")} / {t("mmt.studyForm")}
                </th> : null}
                {columnVisibility.isVisible("language") ? <th className="px-3 py-3">{t("mmt.studyLanguage")}</th> : null}
                {columnVisibility.isVisible("year") ? <th className="px-3 py-3">{t("mmt.year")}</th> : null}
                {columnVisibility.isVisible("seats") ? <th className="px-3 py-3">{t("mmt.seats")}</th> : null}
                {columnVisibility.isVisible("score") ? <th className="px-3 py-3">{t("mmt.latestScore")}</th> : null}
                {columnVisibility.isVisible("visibility") ? <th className="px-3 py-3">{t("mmt.visibility")}</th> : null}
                <th className="px-3 py-3 text-right">{t("mmt.actions")}</th>
              </tr>
            </thead>
            <tbody>
              {query.data.items.map((program) => (
                <tr
                  key={program.id}
                  className="border-t border-[var(--border)]"
                >
                  <td className="px-3 py-3">
                    <strong>{program.universityName}</strong>
                    <small className="mt-0.5 block text-[var(--muted)]">
                      {program.specialtyCode} · {program.specialtyName}
                    </small>
                  </td>
                  {columnVisibility.isVisible("cluster") ? <td className="px-3 py-3">
                    <span className="font-mono font-bold">
                      {program.clusterCode}
                    </span>
                  </td> : null}
                  {columnVisibility.isVisible("admission") ? <td className="px-3 py-3">
                    {enumLabel(
                      labels.admissionTypes,
                      program.admissionType,
                      labels.unknown,
                    )}
                    <small className="block text-[var(--muted)]">
                      {enumLabel(
                        labels.studyForms,
                        program.studyForm,
                        labels.unknown,
                      )}
                    </small>
                  </td> : null}
                  {columnVisibility.isVisible("language") ? <td className="px-3 py-3">
                    {enumLabel(
                      labels.studyLanguages,
                      program.studyLanguage,
                      labels.unknown,
                    )}
                  </td> : null}
                  {columnVisibility.isVisible("year") ? <td className="px-3 py-3">{program.admissionYear}</td> : null}
                  {columnVisibility.isVisible("seats") ? <td className="px-3 py-3">{program.seatsCount ?? "—"}</td> : null}
                  {columnVisibility.isVisible("score") ? <td className="px-3 py-3 font-semibold">
                    {program.latestPassingScore?.toFixed(2) ?? (
                      <span className="text-[var(--warning)]">
                        {t("mmt.missing")}
                      </span>
                    )}
                  </td> : null}
                  {columnVisibility.isVisible("visibility") ? <td className="px-3 py-3">
                    <div className="grid gap-1">
                      <BooleanBadge
                        value={program.isPublished}
                        positive={t("mmt.published")}
                        negative={t("mmt.draft")}
                      />
                      <BooleanBadge value={program.isActive} />
                    </div>
                  </td> : null}
                  <td className="px-3 py-3">
                    <div className="flex justify-end gap-1">
                      <TableActionButton to={`/admin/mmt/programs/${program.id}`} label={t("mmt.view")} icon={<Eye className="h-5 w-5" />} />
                      <TableActionButton to={`/admin/mmt/programs/${program.id}/edit`} label={t("mmt.edit")} icon={<PenLine className="h-5 w-5" />} />
                      <TableActionButton
                        label={program.isPublished ? t("mmt.unpublish") : t("mmt.publish")}
                        icon={<Send className="h-5 w-5" />}
                        onClick={() => {
                          if (
                            window.confirm(
                              program.isPublished
                                ? t("mmt.confirmUnpublish")
                                : t("mmt.confirmPublish"),
                            )
                          )
                            publish.mutate({
                              id: program.id,
                              value: !program.isPublished,
                            });
                        }}
                      />
                      <TableActionButton
                        label={program.isActive ? t("mmt.deactivate") : t("mmt.activate")}
                        icon={<Power className="h-5 w-5" />}
                        onClick={() => {
                          if (
                            window.confirm(
                              program.isActive
                                ? t("mmt.confirmDeactivate")
                                : t("mmt.confirmActivate"),
                            )
                          )
                            status.mutate({
                              id: program.id,
                              value: !program.isActive,
                            });
                        }}
                      />
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </Table>
        </TableShell>
      ) : null}
      {query.data && query.data.totalCount > query.data.pageSize ? (
        <Pagination
          page={query.data.page}
          pageSize={query.data.pageSize}
          total={query.data.totalCount}
          onPage={(value) => setFilter("page", String(value))}
        />
      ) : null}
      <MmtToast notice={toast.notice} onClose={toast.clear} />
    </>
  );
}

function FilterSelect({
  label,
  value,
  onChange,
  options = [],
}: {
  label: string;
  value?: string | undefined;
  onChange: (value: string) => void;
  options?: { value: string; label: string }[] | undefined;
}) {
  return (
    <SelectField
      value={value ?? ""}
      options={[{ value: "", label }, ...options]}
      onValueChange={onChange}
    />
  );
}
function optionalNumber(value: string | null) {
  if (value === null || value === "") return undefined;
  const number = Number(value);
  return Number.isFinite(number) ? number : undefined;
}
function optionalBoolean(value: string | null) {
  return value === "true" ? true : value === "false" ? false : undefined;
}
