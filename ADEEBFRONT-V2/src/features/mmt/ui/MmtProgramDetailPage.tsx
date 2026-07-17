import { useMutation, useQueries, useQueryClient } from "@tanstack/react-query";
import { AlertTriangle, ArrowLeft, Edit3, PenLine, Plus, Trash2 } from "lucide-react";
import { useState, type FormEvent } from "react";
import { useTranslation } from "react-i18next";
import { Link, useParams } from "react-router-dom";
import { mmtApi, mmtKeys } from "@/features/mmt/api/mmt.api";
import {
  controlLink,
  enumLabel,
  errorMessage,
  numberOrNull,
} from "@/features/mmt/lib/mmt";
import { useMmtLabels } from "@/features/mmt/lib/useMmtLabels";
import type {
  PassingScoreHistoryDto,
  PassingScoreInput,
} from "@/features/mmt/model/mmt.types";
import { useMmtToast } from "@/features/mmt/model/useMmtToast";
import { BooleanBadge, Metric, MmtToast, Modal } from "@/features/mmt/ui/MmtUi";
import { Button } from "@/shared/ui/Button";
import { FormField } from "@/shared/ui/FormField";
import { Input, Textarea } from "@/shared/ui/Input";
import { PageHeader } from "@/shared/ui/PageHeader";
import { SelectField } from "@/shared/ui/SelectField";
import { ErrorState } from "@/shared/ui/StateBlock";
import { Table, TableShell } from "@/shared/ui/Table";
import { TableActionButton } from "@/shared/ui/TableActionButton";

export function MmtProgramDetailPage() {
  const { t } = useTranslation();
  const labels = useMmtLabels();
  const { programId = "" } = useParams();
  const queryClient = useQueryClient();
  const toast = useMmtToast();
  const [scoreForm, setScoreForm] = useState<
    PassingScoreHistoryDto | "new" | null
  >(null);
  const [program, scores, analytics, dashboard] = useQueries({
    queries: [
      {
        queryKey: mmtKeys.program(programId),
        queryFn: () => mmtApi.program(programId),
        enabled: Boolean(programId),
      },
      {
        queryKey: mmtKeys.scores(programId),
        queryFn: () => mmtApi.scores(programId),
        enabled: Boolean(programId),
      },
      {
        queryKey: mmtKeys.analytics(programId),
        queryFn: () => mmtApi.analytics(programId),
        enabled: Boolean(programId),
      },
      {
        queryKey: mmtKeys.dashboard(),
        queryFn: mmtApi.dashboard,
      },
    ],
  });
  const remove = useMutation({
    mutationFn: mmtApi.deleteScore,
    onSuccess: async () => {
      await invalidate();
      toast.success(t("mmt.scoreDeleted"));
    },
    onError: (error) =>
      toast.error(errorMessage(error, t("mmt.requestFailed"))),
  });
  async function invalidate() {
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: mmtKeys.program(programId) }),
      queryClient.invalidateQueries({ queryKey: mmtKeys.scores(programId) }),
      queryClient.invalidateQueries({ queryKey: mmtKeys.analytics(programId) }),
      queryClient.invalidateQueries({ queryKey: ["mmt", "programs"] }),
      queryClient.invalidateQueries({ queryKey: mmtKeys.dashboard() }),
    ]);
  }
  if (program.isLoading || scores.isLoading || analytics.isLoading)
    return (
      <p className="text-sm text-[var(--muted)]">{t("mmt.loadingProgram")}</p>
    );
  if (program.isError || !program.data)
    return (
      <ErrorState
        title={t("mmt.programLoadFailed")}
        description={errorMessage(program.error, t("mmt.programLoadFailed"))}
      />
    );
  const item = program.data;
  return (
    <>
      <PageHeader
        title={`${item.specialty.code} · ${item.specialty.name}`}
        description={`${item.university.fullName} · ${item.cluster.code}`}
        actions={
          <>
            <Link to="/admin/mmt/programs" className={controlLink}>
              <ArrowLeft className="h-4 w-4" /> {t("mmt.detailPrograms")}
            </Link>
            <Link
              to={`/admin/mmt/programs/${item.id}/edit`}
              className={controlLink}
            >
              <Edit3 className="h-4 w-4" /> {t("mmt.edit")}
            </Link>
          </>
        }
      />
      <section className="app-surface mb-5 grid gap-y-5 rounded-[1.5rem] p-5 sm:grid-cols-2 lg:grid-cols-5">
        <Metric
          label={t("mmt.latestPassingScore")}
          value={item.latestPassingScore?.toFixed(2) ?? "—"}
          warning={item.latestPassingScore === null}
        />
        <Metric
          label={t("mmt.average3")}
          value={item.averagePassingScoreLast3Years?.toFixed(2) ?? "—"}
        />
        <Metric
          label={t("mmt.conservativeThreshold")}
          value={item.conservativeThreshold?.toFixed(2) ?? "—"}
        />
        <Metric label={t("mmt.admissionYear")} value={item.admissionYear} />
        <Metric label={t("mmt.seats")} value={item.seatsCount ?? "—"} />
      </section>
      <div className="mb-5 grid gap-4 rounded-2xl border border-[var(--border)] bg-white p-5 sm:grid-cols-2 lg:grid-cols-4">
        <Info label={t("mmt.university")} value={item.university.fullName} />
        <Info label={t("mmt.specialty")} value={`${item.specialty.code} · ${item.specialty.name}`} />
        <Info label={t("mmt.cluster")} value={`${item.cluster.code} · ${item.cluster.name}`} />
        <Info
          label={t("mmt.admissionType")}
          value={enumLabel(
            labels.admissionTypes,
            item.admissionType,
            labels.unknown,
          )}
        />
        <Info
          label={t("mmt.studyForm")}
          value={enumLabel(labels.studyForms, item.studyForm, labels.unknown)}
        />
        <Info
          label={t("mmt.studyLanguage")}
          value={enumLabel(
            labels.studyLanguages,
            item.studyLanguage,
            labels.unknown,
          )}
        />
        <Info label={t("mmt.studyLocation")} value={item.studyLocation || "-"} />
        <Info label={t("mmt.tuitionFee")} value={item.tuitionFeeTjs == null ? "-" : `${item.tuitionFeeTjs} TJS`} />
        {item.needsTranslation ? <strong className="text-sm text-[var(--warning)]">{t("mmt.needsTranslation")}</strong> : null}
        <div className="flex items-center gap-2">
          <BooleanBadge
            value={item.isPublished}
            positive={t("mmt.published")}
            negative={t("mmt.draft")}
          />
          <BooleanBadge value={item.isActive} />
        </div>
      </div>
      {scores.data?.length === 0 ? (
        <div className="mb-5 flex items-start gap-3 rounded-2xl border border-amber-200 bg-amber-50 p-4 text-[var(--warning)]">
          <AlertTriangle className="mt-0.5 h-5 w-5" />
          <div>
            <strong>{t("mmt.noPassingScore")}</strong>
            <p className="mt-1 text-sm">{t("mmt.noPassingScoreHint")}</p>
          </div>
        </div>
      ) : null}
      <section>
        <div className="mb-3 flex items-center justify-between">
          <h2 className="text-lg font-black">{t("mmt.scoreHistory")}</h2>
          <Button onClick={() => setScoreForm("new")}>
            <Plus className="h-4 w-4" /> {t("mmt.addScore")}
          </Button>
        </div>
        <TableShell>
          <Table>
            <thead className="bg-[var(--surface-muted)] text-xs uppercase text-[var(--muted)]">
              <tr>
                <th className="px-4 py-3">{t("mmt.year")}</th>
                <th className="px-4 py-3">{t("mmt.distributionRound")}</th>
                <th className="px-4 py-3">{t("mmt.score")}</th>
                <th className="px-4 py-3">{t("mmt.seats")}</th>
                <th className="px-4 py-3">{t("mmt.source")}</th>
                <th className="px-4 py-3">{t("mmt.note")}</th>
                <th className="px-4 py-3 text-right">{t("mmt.actions")}</th>
              </tr>
            </thead>
            <tbody>
              {scores.data?.map((score) => (
                <tr key={score.id} className="border-t border-[var(--border)]">
                  <td className="px-4 py-3 font-bold">{score.year}</td>
                  <td className="px-4 py-3">
                    {enumLabel(labels.distributionRounds, score.distributionRound, labels.unknown)}
                  </td>
                  <td className="px-4 py-3 font-black">
                    {score.passingScore.toFixed(2)}
                  </td>
                  <td className="px-4 py-3">{score.seatsCount ?? "—"}</td>
                  <td className="px-4 py-3">{score.source ?? "—"}</td>
                  <td className="max-w-xs px-4 py-3 text-[var(--muted)]">
                    {score.note ?? "—"}
                  </td>
                  <td className="px-4 py-3">
                    <div className="flex justify-end gap-2">
                      <TableActionButton label={t("mmt.edit")} icon={<PenLine className="h-5 w-5" />} onClick={() => setScoreForm(score)} />
                      <TableActionButton
                        label={t("delete")}
                        icon={<Trash2 className="h-5 w-5" />}
                        tone="danger"
                        onClick={() => {
                          if (window.confirm(t("mmt.deleteScoreConfirm")))
                            remove.mutate(score.id);
                        }}
                      />
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </Table>
        </TableShell>
      </section>
      {scoreForm ? (
        <ScoreForm
          programId={programId}
          score={scoreForm === "new" ? null : scoreForm}
          currentAdmissionYear={dashboard.data?.currentAdmissionYear}
          onClose={() => setScoreForm(null)}
          onSaved={async () => {
            setScoreForm(null);
            await invalidate();
            toast.success(t("mmt.scoreSaved"));
          }}
          onError={toast.error}
        />
      ) : null}
      <MmtToast notice={toast.notice} onClose={toast.clear} />
    </>
  );
}

function Info({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <p className="text-xs font-bold uppercase text-[var(--muted)]">{label}</p>
      <p className="mt-1 font-bold">{value}</p>
    </div>
  );
}

function ScoreForm({
  programId,
  score,
  currentAdmissionYear,
  onClose,
  onSaved,
  onError,
}: {
  programId: string;
  score: PassingScoreHistoryDto | null;
  currentAdmissionYear: number | undefined;
  onClose: () => void;
  onSaved: () => void;
  onError: (message: string) => void;
}) {
  const { t } = useTranslation();
  const labels = useMmtLabels();
  const [distributionRound, setDistributionRound] = useState(
    String(score?.distributionRound ?? 0),
  );
  const mutation = useMutation({
    mutationFn: (input: PassingScoreInput) =>
      score
        ? mmtApi.updateScore(score.id, input)
        : mmtApi.createScore(programId, input),
    onSuccess: onSaved,
    onError: (error) => onError(errorMessage(error, t("mmt.requestFailed"))),
  });
  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    mutation.mutate({
      year: Number(data.get("year")),
      passingScore: Number(data.get("passingScore")),
      seatsCount: numberOrNull(data.get("seatsCount")),
      source: String(data.get("source") || "") || null,
      note: String(data.get("note") || "") || null,
      distributionRound: Number(distributionRound),
    });
  }
  return (
    <Modal
      title={score ? t("mmt.editScore") : t("mmt.addScore")}
      onClose={onClose}
    >
      <form className="grid gap-4" onSubmit={submit}>
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          <FormField label={t("mmt.year")}>
            <Input
              name="year"
              type="number"
              min="2000"
              max="2100"
              defaultValue={score?.year ?? currentAdmissionYear ?? ""}
              required
            />
          </FormField>
          <FormField label={t("mmt.distributionRound")}>
            <SelectField
              name="distributionRound"
              value={distributionRound}
              onValueChange={setDistributionRound}
              options={labels.distributionRounds.map((label, value) => ({ value: String(value), label }))}
            />
          </FormField>
          <FormField label={t("mmt.passingScore")}>
            <Input
              name="passingScore"
              type="number"
              min="0.01"
              max="1000"
              step="0.01"
              defaultValue={score?.passingScore}
              required
            />
          </FormField>
          <FormField label={t("mmt.seats")}>
            <Input
              name="seatsCount"
              type="number"
              min="0"
              defaultValue={score?.seatsCount ?? ""}
            />
          </FormField>
        </div>
        <FormField label={t("mmt.source")}>
          <Input
            name="source"
            maxLength={500}
            defaultValue={score?.source ?? ""}
          />
        </FormField>
        <FormField label={t("mmt.note")}>
          <Textarea
            name="note"
            maxLength={2000}
            defaultValue={score?.note ?? ""}
          />
        </FormField>
        <div className="flex justify-end gap-2 border-t border-[var(--border)] pt-4">
          <Button type="button" variant="secondary" onClick={onClose}>
            {t("mmt.cancel")}
          </Button>
          <Button disabled={mutation.isPending}>
            {mutation.isPending ? t("mmt.saving") : t("mmt.saveScore")}
          </Button>
        </div>
      </form>
    </Modal>
  );
}
