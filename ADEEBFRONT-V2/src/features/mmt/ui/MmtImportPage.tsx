import { useMutation } from "@tanstack/react-query";
import { Download, FileCheck2, FileSpreadsheet, Upload } from "lucide-react";
import { useState, type FormEvent } from "react";
import { useTranslation } from "react-i18next";
import { mmtApi } from "@/features/mmt/api/mmt.api";
import { enumLabel, errorMessage } from "@/features/mmt/lib/mmt";
import { useMmtLabels } from "@/features/mmt/lib/useMmtLabels";
import type {
  ImportOptions,
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

export function MmtImportPage() {
  const { t } = useTranslation();
  const labels = useMmtLabels();
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
    onSuccess: (data) => {
      setResult(data);
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
