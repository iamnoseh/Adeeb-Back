import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Edit3, Plus, Power } from "lucide-react";
import { useCallback, useEffect, useState, type FormEvent } from "react";
import { useTranslation } from "react-i18next";
import { useSearchParams } from "react-router-dom";
import { mmtApi, mmtKeys } from "@/features/mmt/api/mmt.api";
import { subjectKeys, subjectsApi } from "@/features/academic/api/subjects.api";
import { errorMessage, enumLabel } from "@/features/mmt/lib/mmt";
import { useMmtLabels } from "@/features/mmt/lib/useMmtLabels";
import type {
  CatalogDto,
  CatalogInput,
  CatalogKind,
  MmtClusterDto,
  SpecialtyDto,
  UniversityDto,
} from "@/features/mmt/model/mmt.types";
import { useMmtToast } from "@/features/mmt/model/useMmtToast";
import {
  BooleanBadge,
  MmtToast,
  Modal,
  Pagination,
} from "@/features/mmt/ui/MmtUi";
import { Button } from "@/shared/ui/Button";
import { FormField } from "@/shared/ui/FormField";
import { Input, Textarea } from "@/shared/ui/Input";
import { SelectField } from "@/shared/ui/SelectField";
import { MultiSelectField } from "@/shared/ui/MultiSelectField";
import { PageHeader } from "@/shared/ui/PageHeader";
import { EmptyState, ErrorState } from "@/shared/ui/StateBlock";
import { Table, TableShell } from "@/shared/ui/Table";

export function MmtCatalogPage({ kind }: { kind: CatalogKind }) {
  const { t } = useTranslation();
  const labels = useMmtLabels();
  const meta = {
    clusters: {
      title: t("mmt.clustersTitle"),
      description: t("mmt.clustersDescription"),
      empty: t("mmt.noClusters"),
    },
    universities: {
      title: t("mmt.universitiesTitle"),
      description: t("mmt.universitiesDescription"),
      empty: t("mmt.noUniversities"),
    },
    specialties: {
      title: t("mmt.specialtiesTitle"),
      description: t("mmt.specialtiesDescription"),
      empty: t("mmt.noSpecialties"),
    },
  } satisfies Record<
    CatalogKind,
    { title: string; description: string; empty: string }
  >;
  const [params, setParams] = useSearchParams();
  const page = Math.max(1, Number(params.get("page") ?? 1));
  const search = params.get("search") ?? "";
  const [searchInput, setSearchInput] = useState(search);
  const activeValue = params.get("active") ?? "";
  const isActive = activeValue === "" ? undefined : activeValue === "true";
  const [editing, setEditing] = useState<CatalogDto | "new" | null>(null);
  const toast = useMmtToast();
  const queryClient = useQueryClient();
  const queryParams = {
    search: search || undefined,
    isActive,
    page,
    pageSize: 20,
  };
  const query = useQuery({
    queryKey: mmtKeys.catalog(kind, queryParams),
    queryFn: () => mmtApi.catalogList(kind, queryParams),
  });
  const status = useMutation({
    mutationFn: ({ id, next }: { id: string; next: boolean }) =>
      mmtApi.setCatalogStatus(kind, id, next),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["mmt", kind] });
      toast.success(t("mmt.statusSaved"));
    },
    onError: (error) =>
      toast.error(errorMessage(error, t("mmt.requestFailed"))),
  });

  const updateParam = useCallback(
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
      () => updateParam("search", searchInput.trim()),
      350,
    );
    return () => window.clearTimeout(timeout);
  }, [search, searchInput, updateParam]);

  return (
    <>
      <PageHeader
        title={meta[kind].title}
        description={meta[kind].description}
        actions={
          <Button onClick={() => setEditing("new")}>
            <Plus className="h-4 w-4" /> {t("mmt.add")}
          </Button>
        }
      />
      <div className="mb-4 grid gap-3 rounded-2xl border border-[var(--border)] bg-white p-4 md:grid-cols-[1fr_220px]">
        <Input
          value={searchInput}
          onChange={(event) => setSearchInput(event.target.value)}
          placeholder={t("mmt.searchCatalog")}
          aria-label={t("mmt.search")}
        />
        <SelectField
          value={activeValue}
          options={[
            { value: "", label: t("mmt.allStatuses") },
            { value: "true", label: t("mmt.active") },
            { value: "false", label: t("mmt.inactive") },
          ]}
          onValueChange={(value) => updateParam("active", value)}
        />
      </div>

      {query.isLoading ? (
        <p className="text-sm text-[var(--muted)]">{t("mmt.loading")}</p>
      ) : null}
      {query.isError ? (
        <ErrorState
          title={t("mmt.loadFailed")}
          description={errorMessage(query.error, t("mmt.loadFailed"))}
        />
      ) : null}
      {query.data?.items.length === 0 ? (
        <EmptyState
          title={meta[kind].empty}
          description={t("mmt.emptyAdjust")}
        />
      ) : null}
      {query.data && query.data.items.length > 0 ? (
        <TableShell>
          <Table>
            <thead className="bg-[var(--surface-muted)] text-xs uppercase text-[var(--muted)]">
              <CatalogHead kind={kind} />
            </thead>
            <tbody>
              {query.data.items.map((item) => (
                <CatalogRow
                  key={item.id}
                  kind={kind}
                  item={item}
                  onEdit={() => setEditing(item)}
                  onStatus={() => {
                    if (
                      window.confirm(
                        item.isActive
                          ? t("mmt.confirmDeactivate")
                          : t("mmt.confirmActivate"),
                      )
                    )
                      status.mutate({ id: item.id, next: !item.isActive });
                  }}
                />
              ))}
            </tbody>
          </Table>
          <Pagination
            page={query.data.page}
            pageSize={query.data.pageSize}
            total={query.data.totalCount}
            onPage={(value) => updateParam("page", String(value))}
          />
        </TableShell>
      ) : null}
      {editing ? (
        <CatalogForm
          kind={kind}
          item={editing === "new" ? null : editing}
          universityTypes={labels.universityTypes}
          title={meta[kind].title}
          onClose={() => setEditing(null)}
          onSaved={async () => {
            setEditing(null);
            await queryClient.invalidateQueries({ queryKey: ["mmt", kind] });
            toast.success(t("mmt.recordSaved"));
          }}
          onError={toast.error}
        />
      ) : null}
      <MmtToast notice={toast.notice} onClose={toast.clear} />
    </>
  );
}

function CatalogHead({ kind }: { kind: CatalogKind }) {
  const { t } = useTranslation();
  if (kind === "clusters")
    return (
      <tr>
        <th className="px-4 py-3">{t("mmt.code")}</th>
        <th className="px-4 py-3">{t("mmt.name")}</th>
        <th className="px-4 py-3">{t("mmt.subjects")}</th>
        <th className="px-4 py-3">{t("mmt.status")}</th>
        <th className="px-4 py-3 text-right">{t("mmt.actions")}</th>
      </tr>
    );
  if (kind === "universities")
    return (
      <tr>
        <th className="px-4 py-3">{t("mmt.university")}</th>
        <th className="px-4 py-3">{t("mmt.city")}</th>
        <th className="px-4 py-3">{t("mmt.universityType")}</th>
        <th className="px-4 py-3">{t("mmt.status")}</th>
        <th className="px-4 py-3 text-right">{t("mmt.actions")}</th>
      </tr>
    );
  return (
    <tr>
      <th className="px-4 py-3">{t("mmt.code")}</th>
      <th className="px-4 py-3">{t("mmt.specialty")}</th>
      <th className="px-4 py-3">{t("mmt.status")}</th>
      <th className="px-4 py-3 text-right">{t("mmt.actions")}</th>
    </tr>
  );
}

function CatalogRow({
  kind,
  item,
  onEdit,
  onStatus,
}: {
  kind: CatalogKind;
  item: CatalogDto;
  onEdit: () => void;
  onStatus: () => void;
}) {
  const { t } = useTranslation();
  const { universityTypes } = useMmtLabels();
  const cells =
    kind === "clusters"
      ? [
          <span className="font-mono font-bold" key="code">
            {(item as MmtClusterDto).code}
          </span>,
          <strong key="name">{(item as MmtClusterDto).name}</strong>,
          <span className="text-sm" key="subjects">
            {(item as MmtClusterDto).subjects
              .map((subject) => subject.name)
              .join(", ") || t("mmt.none")}
          </span>,
        ]
      : kind === "universities"
        ? [
            <span key="name">
              <strong>{(item as UniversityDto).fullName}</strong>
              <small className="mt-0.5 block text-[var(--muted)]">
                {(item as UniversityDto).shortName}
              </small>
            </span>,
            (item as UniversityDto).city,
            enumLabel(
              universityTypes,
              (item as UniversityDto).type,
              t("mmt.enum.unknown"),
            ),
          ]
        : [
            <span className="font-mono font-bold" key="code">
              {(item as SpecialtyDto).code}
            </span>,
            <strong key="name">{(item as SpecialtyDto).name}</strong>,
          ];
  return (
    <tr className="border-t border-[var(--border)]">
      {cells.map((cell, index) => (
        <td className="px-4 py-3" key={index}>
          {cell}
        </td>
      ))}
      <td className="px-4 py-3">
        <BooleanBadge value={item.isActive} />
      </td>
      <td className="px-4 py-3">
        <div className="flex justify-end gap-2">
          <Button
            variant="secondary"
            className="h-10 min-h-10 px-3"
            onClick={onEdit}
          >
            <Edit3 className="h-4 w-4" /> {t("mmt.edit")}
          </Button>
          <Button
            variant="ghost"
            className="h-10 min-h-10 px-3"
            onClick={onStatus}
          >
            <Power className="h-4 w-4" />{" "}
            {item.isActive ? t("mmt.deactivate") : t("mmt.activate")}
          </Button>
        </div>
      </td>
    </tr>
  );
}

function CatalogForm({
  kind,
  item,
  universityTypes,
  title,
  onClose,
  onSaved,
  onError,
}: {
  kind: CatalogKind;
  item: CatalogDto | null;
  universityTypes: string[];
  title: string;
  onClose: () => void;
  onSaved: () => void;
  onError: (message: string) => void;
}) {
  const { t } = useTranslation();
  const [universityType, setUniversityType] = useState(
    String((item as UniversityDto | null)?.type ?? 0),
  );
  const cluster = item as MmtClusterDto | null;
  const [subjectIds, setSubjectIds] = useState(
    cluster?.subjects.map((subject) => subject.id) ?? [],
  );
  const subjectsQuery = useQuery({
    queryKey: subjectKeys.list({ pageSize: 100 }),
    queryFn: () => subjectsApi.list({ pageSize: 100 }),
    enabled: kind === "clusters",
  });
  const mutation = useMutation({
    mutationFn: (input: CatalogInput) =>
      item
        ? mmtApi.updateCatalog(kind, item.id, input)
        : mmtApi.createCatalog(kind, input),
    onSuccess: onSaved,
    onError: (error) => onError(errorMessage(error, t("mmt.requestFailed"))),
  });
  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    const active = item?.isActive ?? true;
    let input: CatalogInput;
    if (kind === "clusters")
      input = {
        name: String(form.get("nameTg")),
        code: String(form.get("code")),
        description: optionalText(form.get("descriptionTg")),
        nameTg: String(form.get("nameTg")),
        nameRu: String(form.get("nameRu")),
        descriptionTg: optionalText(form.get("descriptionTg")),
        descriptionRu: optionalText(form.get("descriptionRu")),
        subjectIds,
        ...(item ? { isActive: active } : {}),
      };
    else if (kind === "universities")
      input = {
        fullName: String(form.get("fullNameTg")),
        shortName: optionalText(form.get("shortNameTg")),
        city: String(form.get("cityTg")),
        fullNameTg: String(form.get("fullNameTg")),
        fullNameRu: String(form.get("fullNameRu")),
        shortNameTg: optionalText(form.get("shortNameTg")),
        shortNameRu: optionalText(form.get("shortNameRu")),
        cityTg: String(form.get("cityTg")),
        cityRu: String(form.get("cityRu")),
        type: Number(form.get("type")),
        logoUrl: optionalText(form.get("logoUrl")),
        ...(item ? { isActive: active } : {}),
      };
    else
      input = {
        code: String(form.get("code")),
        name: String(form.get("nameTg")),
        description: optionalText(form.get("descriptionTg")),
        nameTg: String(form.get("nameTg")),
        nameRu: String(form.get("nameRu")),
        descriptionTg: optionalText(form.get("descriptionTg")),
        descriptionRu: optionalText(form.get("descriptionRu")),
        ...(item ? { isActive: active } : {}),
      };
    mutation.mutate(input);
  }
  const university = item as UniversityDto | null;
  const specialty = item as SpecialtyDto | null;
  return (
    <Modal
      title={`${item ? t("mmt.edit") : t("mmt.add")}: ${title}`}
      onClose={onClose}
    >
      <form className="grid gap-4" onSubmit={submit}>
        {kind === "universities" ? (
          <>
            <div className="grid gap-4 sm:grid-cols-2">
              <FormField label={t("mmt.fullNameTg")}>
                <Input
                  name="fullNameTg"
                  defaultValue={university?.fullNameTg ?? university?.fullName}
                  required
                  maxLength={300}
                />
              </FormField>
              <FormField label={t("mmt.fullNameRu")}>
                <Input
                  name="fullNameRu"
                  defaultValue={university?.fullNameRu ?? university?.fullName}
                  required
                  maxLength={300}
                />
              </FormField>
              <FormField label={t("mmt.shortNameTg")}>
                <Input
                  name="shortNameTg"
                  defaultValue={
                    university?.shortNameTg ?? university?.shortName ?? ""
                  }
                  maxLength={120}
                />
              </FormField>
              <FormField label={t("mmt.shortNameRu")}>
                <Input
                  name="shortNameRu"
                  defaultValue={
                    university?.shortNameRu ?? university?.shortName ?? ""
                  }
                  maxLength={120}
                />
              </FormField>
              <FormField label={t("mmt.cityTg")}>
                <Input
                  name="cityTg"
                  defaultValue={university?.cityTg ?? university?.city}
                  required
                  maxLength={120}
                />
              </FormField>
              <FormField label={t("mmt.cityRu")}>
                <Input
                  name="cityRu"
                  defaultValue={university?.cityRu ?? university?.city}
                  required
                  maxLength={120}
                />
              </FormField>
            </div>
            <div className="grid gap-4 sm:grid-cols-2">
              <FormField label={t("mmt.universityType")}>
                <SelectField
                  name="type"
                  value={universityType}
                  options={universityTypes.map((label, value) => ({
                    value: String(value),
                    label,
                  }))}
                  onValueChange={setUniversityType}
                />
              </FormField>
              <FormField label={t("mmt.logoUrl")}>
                <Input
                  name="logoUrl"
                  defaultValue={university?.logoUrl ?? ""}
                  maxLength={512}
                />
              </FormField>
            </div>
          </>
        ) : (
          <>
            <FormField label={t("mmt.code")}>
              <Input
                name="code"
                defaultValue={
                  kind === "clusters" ? cluster?.code : specialty?.code
                }
                required
              />
            </FormField>
            <div className="grid gap-4 sm:grid-cols-2">
              <FormField label={t("mmt.nameTg")}>
                <Input
                  name="nameTg"
                  defaultValue={
                    kind === "clusters"
                      ? (cluster?.nameTg ?? cluster?.name)
                      : (specialty?.nameTg ?? specialty?.name)
                  }
                  required
                />
              </FormField>
              <FormField label={t("mmt.nameRu")}>
                <Input
                  name="nameRu"
                  defaultValue={
                    kind === "clusters"
                      ? (cluster?.nameRu ?? cluster?.name)
                      : (specialty?.nameRu ?? specialty?.name)
                  }
                  required
                />
              </FormField>
            </div>
            <div className="grid gap-4 sm:grid-cols-2">
              <FormField label={t("mmt.descriptionTg")}>
                <Textarea
                  name="descriptionTg"
                  defaultValue={
                    (kind === "clusters"
                      ? (cluster?.descriptionTg ?? cluster?.description)
                      : (specialty?.descriptionTg ?? specialty?.description)) ??
                    ""
                  }
                />
              </FormField>
              <FormField label={t("mmt.descriptionRu")}>
                <Textarea
                  name="descriptionRu"
                  defaultValue={
                    (kind === "clusters"
                      ? (cluster?.descriptionRu ?? cluster?.description)
                      : (specialty?.descriptionRu ?? specialty?.description)) ??
                    ""
                  }
                />
              </FormField>
            </div>
            {kind === "clusters" ? (
              <FormField label={t("mmt.subjects")}>
                <MultiSelectField
                  values={subjectIds}
                  options={(subjectsQuery.data?.items ?? [])
                    .filter((subject) => subject.status === 1)
                    .map((subject) => ({
                      value: subject.id,
                      label: `${subject.code} - ${subject.name}`,
                    }))}
                  onValuesChange={setSubjectIds}
                  placeholder={
                    subjectsQuery.isLoading
                      ? t("mmt.loadingSubjects")
                      : t("mmt.selectSubjects")
                  }
                  disabled={subjectsQuery.isLoading}
                />
              </FormField>
            ) : null}
          </>
        )}
        <div className="flex justify-end gap-2 border-t border-[var(--border)] pt-4">
          <Button type="button" variant="secondary" onClick={onClose}>
            {t("mmt.cancel")}
          </Button>
          <Button disabled={mutation.isPending}>
            {mutation.isPending ? t("mmt.saving") : t("mmt.save")}
          </Button>
        </div>
      </form>
    </Modal>
  );
}

function optionalText(value: FormDataEntryValue | null) {
  return typeof value === "string" && value.trim() ? value : null;
}
