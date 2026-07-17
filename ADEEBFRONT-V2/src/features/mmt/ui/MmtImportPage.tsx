import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Download, FileCheck2, FileSpreadsheet, Upload } from "lucide-react";
import { useEffect, useState, type FormEvent } from "react";
import { useTranslation } from "react-i18next";
import { mmtApi, mmtKeys } from "@/features/mmt/api/mmt.api";
import { enumLabel, errorMessage } from "@/features/mmt/lib/mmt";
import { useMmtLabels } from "@/features/mmt/lib/useMmtLabels";
import type {
  ImportOptions,
  MmtCatalogImportOptions,
  MmtCatalogImportPreviewResultDto,
  MmtCatalogImportResultDto,
  MmtImportPreviewResultDto,
  MmtImportResultDto,
} from "@/features/mmt/model/mmt.types";
import { ExistingScoreMode } from "@/features/mmt/model/mmt.types";
import { useMmtToast } from "@/features/mmt/model/useMmtToast";
import { Metric, MmtToast } from "@/features/mmt/ui/MmtUi";
import { Button } from "@/shared/ui/Button";
import { FormField } from "@/shared/ui/FormField";
import { Input } from "@/shared/ui/Input";
import { PageHeader } from "@/shared/ui/PageHeader";
import { SelectField } from "@/shared/ui/SelectField";
import { Table, TableShell } from "@/shared/ui/Table";
import { MmtReferenceSelect } from "@/features/mmt/ui/MmtReferenceSelect";

export function MmtImportPage() {
  const { t } = useTranslation();
  const [tab, setTab] = useState<"catalog" | "scores">("catalog");
  return <div className="grid gap-5">
    <div className="inline-flex w-fit rounded-lg border border-[var(--border)] bg-[var(--surface)] p-1">
      <button type="button" onClick={() => setTab("catalog")} className={`rounded-md px-4 py-2 text-sm font-bold ${tab === "catalog" ? "bg-[var(--primary)] text-white" : "text-[var(--muted)]"}`}>{t("mmt.catalogImportTab")}</button>
      <button type="button" onClick={() => setTab("scores")} className={`rounded-md px-4 py-2 text-sm font-bold ${tab === "scores" ? "bg-[var(--primary)] text-white" : "text-[var(--muted)]"}`}>{t("mmt.scoreImportTab")}</button>
    </div>
    {tab === "catalog" ? <CatalogImportPanel /> : <PassingScoreImportPanel />}
  </div>;
}

function PassingScoreImportPanel() {
  const { t } = useTranslation();
  const labels = useMmtLabels();
  const queryClient = useQueryClient();
  const [scoreMode, setScoreMode] = useState(
    String(ExistingScoreMode.SkipExisting),
  );
  const [options, setOptions] = useState<ImportOptions | null>(null);
  const [preview, setPreview] = useState<MmtImportPreviewResultDto | null>(
    null,
  );
  const [result, setResult] = useState<MmtImportResultDto | null>(null);
  const toast = useMmtToast();
  const download = useMutation({
    mutationFn: mmtApi.downloadTemplate,
    onSuccess: (blob) => {
      const url = URL.createObjectURL(blob);
      const anchor = document.createElement("a");
      anchor.href = url;
      anchor.download = "mmt-import-template.xlsx";
      anchor.click();
      URL.revokeObjectURL(url);
    },
    onError: (error) =>
      toast.error(errorMessage(error, t("mmt.requestFailed"))),
  });
  const previewMutation = useMutation({
    mutationFn: mmtApi.previewImport,
    onSuccess: (data) => {
      setPreview(data);
      setResult(null);
      toast.success(t("mmt.previewReady"));
    },
    onError: (error) =>
      toast.error(errorMessage(error, t("mmt.workbookFailed"))),
  });
  const confirm = useMutation({
    mutationFn: mmtApi.confirmImport,
    onSuccess: async (data) => {
      setResult(data);
      await queryClient.invalidateQueries({ queryKey: mmtKeys.all });
      toast.success(t("mmt.importCompleted"));
    },
    onError: (error) => toast.error(errorMessage(error, t("mmt.importFailed"))),
  });
  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    const file = data.get("file") as File | null;
    if (!file || file.size === 0) {
      toast.error(t("mmt.chooseWorkbook"));
      return;
    }
    const yearValue = String(data.get("admissionYear") ?? "").trim();
    const next: ImportOptions = {
      file,
      createMissingReferences: data.get("createMissingReferences") === "on",
      existingScoreMode: Number(scoreMode),
      publishAdmissionPrograms: data.get("publishAdmissionPrograms") === "on",
      ...(yearValue ? { admissionYear: Number(yearValue) } : {}),
    };
    setOptions(next);
    previewMutation.mutate(next);
  }
  return (
    <>
      <PageHeader
        title={t("mmt.importTitle")}
        description={t("mmt.importDescription")}
        actions={
          <Button
            variant="secondary"
            onClick={() => download.mutate()}
            disabled={download.isPending}
          >
            <Download className="h-4 w-4" /> {t("mmt.downloadTemplate")}
          </Button>
        }
      />
      <form
        className="app-surface mb-5 grid gap-4 rounded-[1.5rem] p-5"
        onSubmit={submit}
      >
        <FormField label={t("mmt.workbook")}>
          <Input
            name="file"
            type="file"
            accept=".xlsx,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            required
          />
        </FormField>
        <div className="grid gap-4 md:grid-cols-2">
          <FormField label={t("mmt.existingScoreMode")}>
            <SelectField
              value={scoreMode}
              options={labels.scoreModes.map((label, value) => ({
                value: String(value),
                label,
              }))}
              onValueChange={setScoreMode}
            />
          </FormField>
          <FormField label={t("mmt.defaultAdmissionYear")}>
            <Input
              name="admissionYear"
              type="number"
              min="2000"
              max="2100"
              placeholder={t("mmt.defaultYearHint")}
            />
          </FormField>
        </div>
        <div className="flex flex-wrap gap-4 rounded-2xl bg-[var(--surface-muted)] p-4">
          <Toggle
            name="createMissingReferences"
            label={t("mmt.createReferences")}
          />
          <Toggle
            name="publishAdmissionPrograms"
            label={t("mmt.publishImported")}
          />
        </div>
        <div className="flex justify-end">
          <Button disabled={previewMutation.isPending}>
            <Upload className="h-4 w-4" />{" "}
            {previewMutation.isPending
              ? t("mmt.readingWorkbook")
              : t("mmt.previewImport")}
          </Button>
        </div>
      </form>
      {preview ? (
        <section className="grid gap-4">
          <div className="app-surface grid gap-y-4 rounded-[1.5rem] p-5 sm:grid-cols-4">
            <Metric label={t("mmt.totalRows")} value={preview.totalRows} />
            <Metric label={t("mmt.valid")} value={preview.validRowsCount} />
            <Metric
              label={t("mmt.invalid")}
              value={preview.invalidRowsCount}
              warning={preview.invalidRowsCount > 0}
            />
            <Metric
              label={t("mmt.duplicate")}
              value={preview.duplicateRowsCount}
              warning={preview.duplicateRowsCount > 0}
            />
          </div>
          <TableShell>
            <Table>
              <thead className="bg-[var(--surface-muted)] text-xs uppercase text-[var(--muted)]">
                <tr>
                  <th className="px-3 py-3">{t("mmt.row")}</th>
                  <th className="px-3 py-3">{t("mmt.program")}</th>
                  <th className="px-3 py-3">{t("mmt.cluster")}</th>
                  <th className="px-3 py-3">{t("mmt.year")}</th>
                  <th className="px-3 py-3">{t("mmt.score")}</th>
                  <th className="px-3 py-3">{t("mmt.distributionRound")}</th>
                  <th className="px-3 py-3">{t("mmt.statusErrors")}</th>
                </tr>
              </thead>
              <tbody>
                {preview.rows.map((row) => (
                  <tr
                    key={row.rowNumber}
                    className="border-t border-[var(--border)]"
                  >
                    <td className="px-3 py-3 font-mono">{row.rowNumber}</td>
                    <td className="px-3 py-3">
                      {row.values ? (
                        <>
                          <strong>{row.values.universityFullName}</strong>
                          <small className="block text-[var(--muted)]">
                            {row.values.specialtyCode} ·{" "}
                            {row.values.specialtyName} ·{" "}
                            {enumLabel(
                              labels.admissionTypes,
                              row.values.admissionType,
                              labels.unknown,
                            )}{" "}
                            /{" "}
                            {enumLabel(
                              labels.studyForms,
                              row.values.studyForm,
                              labels.unknown,
                            )}
                          </small>
                        </>
                      ) : (
                        "—"
                      )}
                    </td>
                    <td className="px-3 py-3">
                      {row.values?.clusterCode ?? "—"}
                    </td>
                    <td className="px-3 py-3">{row.values?.year ?? "—"}</td>
                    <td className="px-3 py-3 font-semibold">
                      {row.values?.passingScore?.toFixed(2) ?? "—"}
                    </td>
                    <td className="px-3 py-3">
                      {row.values
                        ? enumLabel(labels.distributionRounds, row.values.distributionRound, labels.unknown)
                        : "—"}
                    </td>
                    <td className="px-3 py-3">
                      {row.isValid && !row.isDuplicate ? (
                        <span className="font-bold text-[var(--success)]">
                          {t("mmt.valid")}
                        </span>
                      ) : (
                        <>
                          <span className="font-bold text-[var(--danger)]">
                            {row.isDuplicate
                              ? t("mmt.duplicate")
                              : t("mmt.invalid")}
                          </span>
                          {row.validationErrors.map((error) => (
                            <small
                              className="block text-[var(--danger)]"
                              key={error}
                            >
                              {error}
                            </small>
                          ))}
                        </>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </Table>
          </TableShell>
          <div className="flex justify-end">
            <Button
              disabled={
                !options || preview.validRowsCount === 0 || confirm.isPending
              }
              onClick={() => {
                if (options && window.confirm(t("mmt.confirmImportQuestion")))
                  confirm.mutate(options);
              }}
            >
              <FileCheck2 className="h-4 w-4" />{" "}
              {confirm.isPending ? t("mmt.importing") : t("mmt.confirmImport")}
            </Button>
          </div>
        </section>
      ) : (
        <div className="rounded-2xl border border-dashed border-[var(--border)] p-8 text-center text-[var(--muted)]">
          <FileSpreadsheet className="mx-auto mb-3 h-8 w-8" />
          <p className="font-semibold">{t("mmt.uploadHint")}</p>
        </div>
      )}
      {result ? (
        <section className="app-surface mt-5 rounded-[1.5rem] p-5">
          <h2 className="mb-4 text-lg font-black">{t("mmt.importResult")}</h2>
          <div className="grid gap-y-4 sm:grid-cols-3 lg:grid-cols-6">
            <Metric label={t("mmt.processed")} value={result.processedRows} />
            <Metric label={t("mmt.programs")} value={result.importedPrograms} />
            <Metric
              label={t("mmt.insertedScores")}
              value={result.insertedScores}
            />
            <Metric
              label={t("mmt.updatedScores")}
              value={result.updatedScores}
            />
            <Metric
              label={t("mmt.skippedScores")}
              value={result.skippedScores}
            />
            <Metric
              label={t("mmt.invalid")}
              value={result.invalidRows}
              warning={result.invalidRows > 0}
            />
          </div>
        </section>
      ) : null}
      <MmtToast notice={toast.notice} onClose={toast.clear} />
    </>
  );
}

function CatalogImportPanel() {
  const { t } = useTranslation();
  const labels = useMmtLabels();
  const queryClient = useQueryClient();
  const toast = useMmtToast();
  const [clusterId, setClusterId] = useState("");
  const [year, setYear] = useState("");
  const [universityType, setUniversityType] = useState("0");
  const [options, setOptions] = useState<MmtCatalogImportOptions | null>(null);
  const [preview, setPreview] = useState<MmtCatalogImportPreviewResultDto | null>(null);
  const [result, setResult] = useState<MmtCatalogImportResultDto | null>(null);
  const dashboard = useQuery({ queryKey: mmtKeys.dashboard(), queryFn: mmtApi.dashboard });
  useEffect(() => { if (!year && dashboard.data) setYear(String(dashboard.data.currentAdmissionYear)); }, [dashboard.data, year]);
  const download = useMutation({
    mutationFn: mmtApi.downloadCatalogTemplate,
    onSuccess: (blob) => downloadBlob(blob, "mmt-catalog-template.xlsx"),
    onError: (error) => toast.error(errorMessage(error, t("mmt.requestFailed"))),
  });
  const previewMutation = useMutation({
    mutationFn: mmtApi.previewCatalogImport,
    onSuccess: (data, submitted) => {
      const next = { ...submitted, universityTypeOverrides: data.universities.map((item) => ({ universityNameRu: item.universityNameRu, universityType: item.universityType })) };
      setOptions(next); setPreview(data); setResult(null); toast.success(t("mmt.previewReady"));
    },
    onError: (error) => toast.error(errorMessage(error, t("mmt.workbookFailed"))),
  });
  const confirm = useMutation({
    mutationFn: mmtApi.confirmCatalogImport,
    onSuccess: async (data) => {
      setResult(data);
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ["mmt", "programs"] }),
        queryClient.invalidateQueries({ queryKey: ["mmt", "universities"] }),
        queryClient.invalidateQueries({ queryKey: ["mmt", "specialties"] }),
        queryClient.invalidateQueries({ queryKey: mmtKeys.dashboard() }),
      ]);
      toast.success(t("mmt.importCompleted"));
    },
    onError: (error) => toast.error(errorMessage(error, t("mmt.importFailed"))),
  });
  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = new FormData(event.currentTarget);
    const file = form.get("catalogFile") as File | null;
    if (!file?.size || !clusterId) { toast.error(t("mmt.catalogFieldsRequired")); return; }
    const next: MmtCatalogImportOptions = { file, mmtClusterId: clusterId, admissionYear: Number(year), defaultUniversityType: Number(universityType), universityTypeOverrides: [] };
    setOptions(next); previewMutation.mutate(next);
  }
  function changeUniversityType(name: string, value: string) {
    setOptions((current) => current ? { ...current, universityTypeOverrides: current.universityTypeOverrides.map((item) => item.universityNameRu === name ? { ...item, universityType: Number(value) } : item) } : current);
  }
  return <>
    <PageHeader title={t("mmt.catalogImportTitle")} description={t("mmt.catalogImportDescription")} actions={<Button variant="secondary" onClick={() => download.mutate()} disabled={download.isPending}><Download className="h-4 w-4" />{t("mmt.downloadTemplate")}</Button>} />
    <form className="app-surface grid gap-4 rounded-[1.5rem] p-5" onSubmit={submit}>
      <div className="grid gap-4 md:grid-cols-2">
        <FormField label={t("mmt.workbook")}><Input name="catalogFile" type="file" accept=".xlsx,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" required /></FormField>
        <FormField label={t("mmt.cluster")}><MmtReferenceSelect kind="clusters" value={clusterId} onValueChange={setClusterId} placeholder={t("mmt.selectCluster")} /></FormField>
        <FormField label={t("mmt.admissionYear")}><Input type="number" min="2000" max="2100" value={year} onChange={(event) => setYear(event.target.value)} required /></FormField>
        <FormField label={t("mmt.defaultUniversityType")}><SelectField value={universityType} onValueChange={setUniversityType} options={labels.universityTypes.map((label, value) => ({ value: String(value), label }))} /></FormField>
      </div>
      <div className="flex justify-end"><Button disabled={previewMutation.isPending || !clusterId}><Upload className="h-4 w-4" />{previewMutation.isPending ? t("mmt.readingWorkbook") : t("mmt.previewImport")}</Button></div>
    </form>
    {preview && options ? <section className="mt-5 grid gap-4">
      <div className="app-surface grid gap-y-4 rounded-[1.5rem] p-5 sm:grid-cols-3 lg:grid-cols-6">
        <Metric label={t("mmt.totalRows")} value={preview.totalRows} /><Metric label={t("mmt.valid")} value={preview.validRowsCount} /><Metric label={t("mmt.invalid")} value={preview.invalidRowsCount} warning={preview.invalidRowsCount > 0} /><Metric label={t("mmt.newPrograms")} value={preview.newRowsCount} /><Metric label={t("mmt.skippedPrograms")} value={preview.skippedRowsCount} /><Metric label={t("mmt.needsTranslation")} value={preview.needsTranslationCount} warning={preview.needsTranslationCount > 0} />
      </div>
      <div className="app-surface rounded-[1.5rem] p-5"><h2 className="mb-4 text-lg font-black">{t("mmt.universityTypeOverrides")}</h2><div className="grid gap-3 md:grid-cols-2">{preview.universities.map((university) => <div key={university.universityNameRu} className="grid gap-2 rounded-lg border border-[var(--border)] p-3 sm:grid-cols-[minmax(0,1fr)_180px] sm:items-center"><span className="min-w-0 text-sm font-bold">{university.universityNameRu}</span><SelectField value={String(options.universityTypeOverrides.find((item) => item.universityNameRu === university.universityNameRu)?.universityType ?? university.universityType)} onValueChange={(value) => changeUniversityType(university.universityNameRu, value)} options={labels.universityTypes.map((label, value) => ({ value: String(value), label }))} /></div>)}</div></div>
      <TableShell><Table><thead className="bg-[var(--surface-muted)] text-xs uppercase text-[var(--muted)]"><tr><th className="px-3 py-3">{t("mmt.row")}</th><th className="px-3 py-3">{t("mmt.program")}</th><th className="px-3 py-3">{t("mmt.studyLocation")}</th><th className="px-3 py-3">{t("mmt.conditions")}</th><th className="px-3 py-3">{t("mmt.statusErrors")}</th></tr></thead><tbody>{preview.rows.map((row) => <tr key={row.rowNumber} className="border-t border-[var(--border)]"><td className="px-3 py-3 font-mono">{row.rowNumber}</td><td className="px-3 py-3"><strong>{row.values ? `${row.values.specialtyCode} - ${row.values.specialtyNameRu}` : "-"}</strong><small className="block text-[var(--muted)]">{row.values?.universityNameRu}</small></td><td className="px-3 py-3">{row.values?.studyLocationRu ?? "-"}</td><td className="px-3 py-3 text-xs">{row.values ? `${enumLabel(labels.admissionTypes, row.values.admissionType, labels.unknown)} · ${row.values.tuitionFeeTjs ?? "-"} · ${enumLabel(labels.studyForms, row.values.studyForm, labels.unknown)} · ${enumLabel(labels.studyLanguages, row.values.studyLanguage, labels.unknown)} · ${row.values.seatsCount}` : "-"}</td><td className="px-3 py-3"><span className={`font-bold ${row.isValid ? "text-[var(--success)]" : "text-[var(--danger)]"}`}>{row.isExisting ? t("mmt.skipped") : row.isValid ? t("mmt.new") : t("mmt.invalid")}</span>{row.needsTranslation ? <small className="block text-[var(--warning)]">{t("mmt.needsTranslation")}</small> : null}{[...row.validationErrors, ...row.warnings].map((message) => <small key={message} className="block text-[var(--muted)]">{message}</small>)}</td></tr>)}</tbody></Table></TableShell>
      <div className="flex justify-end"><Button disabled={confirm.isPending || preview.invalidRowsCount > 0 || preview.newRowsCount === 0} onClick={() => confirm.mutate(options)}><FileCheck2 className="h-4 w-4" />{confirm.isPending ? t("mmt.importing") : t("mmt.confirmImport")}</Button></div>
    </section> : null}
    {result ? <section className="app-surface mt-5 rounded-[1.5rem] p-5"><h2 className="mb-4 text-lg font-black">{t("mmt.importResult")}</h2><div className="grid gap-y-4 sm:grid-cols-3 lg:grid-cols-6"><Metric label={t("mmt.processed")} value={result.processedRows} /><Metric label={t("mmt.newPrograms")} value={result.importedPrograms} /><Metric label={t("mmt.skippedPrograms")} value={result.skippedPrograms} /><Metric label={t("mmt.createdUniversities")} value={result.createdUniversities} /><Metric label={t("mmt.createdSpecialties")} value={result.createdSpecialties} /><Metric label={t("mmt.invalid")} value={result.invalidRows} warning={result.invalidRows > 0} /></div></section> : null}
    <MmtToast notice={toast.notice} onClose={toast.clear} />
  </>;
}

function downloadBlob(blob: Blob, fileName: string) {
  const url = URL.createObjectURL(blob); const anchor = document.createElement("a"); anchor.href = url; anchor.download = fileName; anchor.click(); URL.revokeObjectURL(url);
}

function Toggle({ name, label }: { name: string; label: string }) {
  return (
    <label className="flex items-center gap-3 text-sm font-bold">
      <input
        type="checkbox"
        name={name}
        className="h-4 w-4 accent-[var(--primary)]"
      />{" "}
      {label}
    </label>
  );
}
