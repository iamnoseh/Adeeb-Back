import {
  useMutation,
  useQueries,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import { Edit3, Eye, Plus, Power, Send } from "lucide-react";
import { useCallback, useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { Link, useSearchParams } from "react-router-dom";
import { mmtApi, mmtKeys } from "@/features/mmt/api/mmt.api";
import { controlLink, enumLabel, errorMessage } from "@/features/mmt/lib/mmt";
import { useMmtLabels } from "@/features/mmt/lib/useMmtLabels";
import type {
  MmtClusterDto,
  SpecialtyDto,
  UniversityDto,
} from "@/features/mmt/model/mmt.types";
import { useMmtToast } from "@/features/mmt/model/useMmtToast";
import { BooleanBadge, MmtToast, Pagination } from "@/features/mmt/ui/MmtUi";
import { Button } from "@/shared/ui/Button";
import { Input } from "@/shared/ui/Input";
import { PageHeader } from "@/shared/ui/PageHeader";
import { SelectField } from "@/shared/ui/SelectField";
import { EmptyState, ErrorState } from "@/shared/ui/StateBlock";
import { Table, TableShell } from "@/shared/ui/Table";

export function MmtProgramsPage() {
  const { t } = useTranslation();
  const labels = useMmtLabels();
  const [params, setParams] = useSearchParams();
  const search = params.get("search") ?? "";
  const [searchInput, setSearchInput] = useState(search);
  const page = Math.max(1, Number(params.get("page") ?? 1));
  const filters = {
    search: params.get("search") || undefined,
    clusterId: params.get("clusterId") || undefined,
    universityId: params.get("universityId") || undefined,
    specialtyId: params.get("specialtyId") || undefined,
    admissionType: optionalNumber(params.get("admissionType")),
    studyForm: optionalNumber(params.get("studyForm")),
    studyLanguage: optionalNumber(params.get("studyLanguage")),
    admissionYear: optionalNumber(params.get("admissionYear")),
    isPublished: optionalBoolean(params.get("isPublished")),
    isActive: optionalBoolean(params.get("isActive")),
    page,
    pageSize: 20,
  };
  const query = useQuery({
    queryKey: mmtKeys.programs(filters),
    queryFn: () => mmtApi.programs(filters),
  });
  const refs = useQueries({
    queries: [
      {
        queryKey: mmtKeys.catalog("clusters", { pageSize: 100 }),
        queryFn: () =>
          mmtApi.catalogList<MmtClusterDto>("clusters", { pageSize: 100 }),
      },
      {
        queryKey: mmtKeys.catalog("universities", { pageSize: 100 }),
        queryFn: () =>
          mmtApi.catalogList<UniversityDto>("universities", { pageSize: 100 }),
      },
      {
        queryKey: mmtKeys.catalog("specialties", { pageSize: 100 }),
        queryFn: () =>
          mmtApi.catalogList<SpecialtyDto>("specialties", { pageSize: 100 }),
      },
    ],
  });
  const queryClient = useQueryClient();
  const toast = useMmtToast();
  const status = useMutation({
    mutationFn: ({ id, value }: { id: string; value: boolean }) =>
      mmtApi.setProgramStatus(id, value),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["mmt", "programs"] });
      toast.success(t("mmt.statusSaved"));
    },
    onError: (error) =>
      toast.error(errorMessage(error, t("mmt.requestFailed"))),
  });
  const publish = useMutation({
    mutationFn: ({ id, value }: { id: string; value: boolean }) =>
      mmtApi.setProgramPublished(id, value),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["mmt", "programs"] });
      toast.success(t("mmt.publicationSaved"));
    },
    onError: (error) =>
      toast.error(errorMessage(error, t("mmt.requestFailed"))),
  });

  const setFilter = useCallback(
    (key: string, value: string) => {
      const next = new URLSearchParams(params);
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

  return (
    <>
      <PageHeader
        title={t("mmt.programsTitle")}
        description={t("mmt.programsDescription")}
        actions={
          <Link
            to="/admin/mmt/programs/new"
            className={`${controlLink} border-transparent bg-[var(--primary)] text-white`}
          >
            <Plus className="h-4 w-4" /> {t("mmt.newProgram")}
          </Link>
        }
      />
      <div className="mb-4 grid gap-3 rounded-2xl border border-[var(--border)] bg-white p-4 md:grid-cols-4">
        <div className="md:col-span-2">
          <Input
            value={searchInput}
            onChange={(event) => setSearchInput(event.target.value)}
            placeholder={`${t("mmt.university")}, ${t("mmt.specialty")}, ${t("mmt.code")}`}
            aria-label={t("mmt.search")}
          />
        </div>
        <FilterSelect
          label={t("mmt.allClusters")}
          value={filters.clusterId}
          onChange={(value) => setFilter("clusterId", value)}
          options={refs[0].data?.items.map((item) => ({
            value: item.id,
            label: `${item.code} · ${item.name}`,
          }))}
        />
        <FilterSelect
          label={t("mmt.allUniversities")}
          value={filters.universityId}
          onChange={(value) => setFilter("universityId", value)}
          options={refs[1].data?.items.map((item) => ({
            value: item.id,
            label: item.fullName,
          }))}
        />
        <FilterSelect
          label={t("mmt.allSpecialties")}
          value={filters.specialtyId}
          onChange={(value) => setFilter("specialtyId", value)}
          options={refs[2].data?.items.map((item) => ({
            value: item.id,
            label: `${item.code} · ${item.name}`,
          }))}
        />
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
      </div>
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
                <th className="px-3 py-3">{t("mmt.cluster")}</th>
                <th className="px-3 py-3">
                  {t("mmt.admissionType")} / {t("mmt.studyForm")}
                </th>
                <th className="px-3 py-3">{t("mmt.studyLanguage")}</th>
                <th className="px-3 py-3">{t("mmt.year")}</th>
                <th className="px-3 py-3">{t("mmt.seats")}</th>
                <th className="px-3 py-3">{t("mmt.latestScore")}</th>
                <th className="px-3 py-3">{t("mmt.visibility")}</th>
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
                  <td className="px-3 py-3">
                    <span className="font-mono font-bold">
                      {program.clusterCode}
                    </span>
                  </td>
                  <td className="px-3 py-3">
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
                  </td>
                  <td className="px-3 py-3">
                    {enumLabel(
                      labels.studyLanguages,
                      program.studyLanguage,
                      labels.unknown,
                    )}
                  </td>
                  <td className="px-3 py-3">{program.admissionYear}</td>
                  <td className="px-3 py-3">{program.seatsCount ?? "—"}</td>
                  <td className="px-3 py-3 font-semibold">
                    {program.latestPassingScore?.toFixed(2) ?? (
                      <span className="text-[var(--warning)]">
                        {t("mmt.missing")}
                      </span>
                    )}
                  </td>
                  <td className="px-3 py-3">
                    <div className="grid gap-1">
                      <BooleanBadge
                        value={program.isPublished}
                        positive={t("mmt.published")}
                        negative={t("mmt.draft")}
                      />
                      <BooleanBadge value={program.isActive} />
                    </div>
                  </td>
                  <td className="px-3 py-3">
                    <div className="flex justify-end gap-1">
                      <Link
                        to={`/admin/mmt/programs/${program.id}`}
                        className={controlLink}
                        aria-label={t("mmt.view")}
                      >
                        <Eye className="h-4 w-4" />
                      </Link>
                      <Link
                        to={`/admin/mmt/programs/${program.id}/edit`}
                        className={controlLink}
                        aria-label={t("mmt.edit")}
                      >
                        <Edit3 className="h-4 w-4" />
                      </Link>
                      <Button
                        variant="ghost"
                        className="h-10 min-h-10 px-3"
                        aria-label={
                          program.isPublished
                            ? t("mmt.unpublish")
                            : t("mmt.publish")
                        }
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
                      >
                        <Send className="h-4 w-4" />
                      </Button>
                      <Button
                        variant="ghost"
                        className="h-10 min-h-10 px-3"
                        aria-label={
                          program.isActive
                            ? t("mmt.deactivate")
                            : t("mmt.activate")
                        }
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
                      >
                        <Power className="h-4 w-4" />
                      </Button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </Table>
          <Pagination
            page={query.data.page}
            pageSize={query.data.pageSize}
            total={query.data.totalCount}
            onPage={(value) => setFilter("page", String(value))}
          />
        </TableShell>
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
