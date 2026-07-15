import { useQuery } from "@tanstack/react-query";
import { ArrowLeft, Eye } from "lucide-react";
import { useTranslation } from "react-i18next";
import { Link, useParams, useSearchParams } from "react-router-dom";
import { mmtApi, mmtKeys } from "@/features/mmt/api/mmt.api";
import {
  compactId,
  controlLink,
  enumLabel,
  errorMessage,
} from "@/features/mmt/lib/mmt";
import { useMmtLabels } from "@/features/mmt/lib/useMmtLabels";
import { BooleanBadge, Metric, Pagination } from "@/features/mmt/ui/MmtUi";
import { formatDushanbeDate } from "@/shared/lib/date";
import { Input } from "@/shared/ui/Input";
import { PageHeader } from "@/shared/ui/PageHeader";
import { EmptyState, ErrorState } from "@/shared/ui/StateBlock";
import { Table, TableShell } from "@/shared/ui/Table";

export function MmtEvaluationsPage() {
  const { t, i18n } = useTranslation();
  const [params, setParams] = useSearchParams();
  const page = Math.max(1, Number(params.get("page") ?? 1));
  const filters = {
    userId: params.get("userId") || undefined,
    studentMmtProfileId: params.get("profileId") || undefined,
    admissionYear: numeric(params.get("admissionYear")),
    page,
    pageSize: 20,
  };
  const query = useQuery({
    queryKey: mmtKeys.evaluations(filters),
    queryFn: () => mmtApi.evaluations(filters),
  });
  function setFilter(key: string, value: string) {
    const next = new URLSearchParams(params);
    if (value) next.set(key, value);
    else next.delete(key);
    if (key !== "page") next.set("page", "1");
    setParams(next);
  }
  return (
    <>
      <PageHeader
        title={t("mmt.evaluationsTitle")}
        description={t("mmt.evaluationsDescription")}
      />
      <div className="mb-4 grid gap-3 rounded-2xl border border-[var(--border)] bg-white p-4 md:grid-cols-3">
        <Input
          placeholder={t("mmt.userId")}
          value={params.get("userId") ?? ""}
          onChange={(event) => setFilter("userId", event.target.value)}
        />
        <Input
          placeholder={t("mmt.profileId")}
          value={params.get("profileId") ?? ""}
          onChange={(event) => setFilter("profileId", event.target.value)}
        />
        <Input
          type="number"
          min="2000"
          max="2100"
          placeholder={t("mmt.admissionYear")}
          value={params.get("admissionYear") ?? ""}
          onChange={(event) => setFilter("admissionYear", event.target.value)}
        />
      </div>
      {query.isLoading ? (
        <p className="text-sm text-[var(--muted)]">
          {t("mmt.loadingEvaluations")}
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
          title={t("mmt.noEvaluations")}
          description={t("mmt.evaluationsFilterHint")}
        />
      ) : null}
      {query.data && query.data.items.length > 0 ? (
        <TableShell>
          <Table>
            <thead className="bg-[var(--surface-muted)] text-xs uppercase text-[var(--muted)]">
              <tr>
                <th className="px-4 py-3">{t("mmt.evaluated")}</th>
                <th className="px-4 py-3">{t("mmt.score")}</th>
                <th className="px-4 py-3">{t("mmt.year")}</th>
                <th className="px-4 py-3">{t("mmt.accepted")}</th>
                <th className="px-4 py-3">{t("mmt.goalGap")}</th>
                <th className="px-4 py-3">{t("mmt.readiness")}</th>
                <th className="px-4 py-3">{t("mmt.status")}</th>
                <th className="px-4 py-3 text-right">{t("mmt.action")}</th>
              </tr>
            </thead>
            <tbody>
              {query.data.items.map((item) => (
                <tr key={item.id} className="border-t border-[var(--border)]">
                  <td className="px-4 py-3">
                    {formatDushanbeDate(item.evaluatedAtUtc, i18n.language)}
                  </td>
                  <td className="px-4 py-3 font-black">
                    {item.totalScore.toFixed(2)}
                  </td>
                  <td className="px-4 py-3">{item.admissionYear}</td>
                  <td className="px-4 py-3">
                    {item.acceptedChoicePriority
                      ? `${t("mmt.priority")} ${item.acceptedChoicePriority}`
                      : "—"}
                  </td>
                  <td className="px-4 py-3">
                    {item.missingScoreForGoal?.toFixed(2) ?? "—"}
                  </td>
                  <td className="px-4 py-3">
                    {item.readinessPercentage === null
                      ? "—"
                      : `${item.readinessPercentage.toFixed(2)}%`}
                  </td>
                  <td className="px-4 py-3">
                    <Status keyName={item.motivationalMessageKey} />
                  </td>
                  <td className="px-4 py-3 text-right">
                    <Link
                      to={`/admin/mmt/evaluations/${item.id}`}
                      className={controlLink}
                    >
                      <Eye className="h-4 w-4" /> {t("mmt.view")}
                    </Link>
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
    </>
  );
}

export function MmtEvaluationDetailPage() {
  const { t, i18n } = useTranslation();
  const labels = useMmtLabels();
  const { evaluationId = "" } = useParams();
  const query = useQuery({
    queryKey: mmtKeys.evaluation(evaluationId),
    queryFn: () => mmtApi.evaluation(evaluationId),
    enabled: Boolean(evaluationId),
  });
  if (query.isLoading)
    return (
      <p className="text-sm text-[var(--muted)]">
        {t("mmt.loadingEvaluations")}
      </p>
    );
  if (query.isError || !query.data)
    return (
      <ErrorState
        title={t("mmt.evaluationLoadFailed")}
        description={errorMessage(query.error, t("mmt.evaluationLoadFailed"))}
      />
    );
  const item = query.data;
  return (
    <>
      <PageHeader
        title={`${t("mmt.evaluation")} · ${item.totalScore.toFixed(2)}`}
        description={`${formatDushanbeDate(item.evaluatedAtUtc, i18n.language)} · ${t("mmt.userId")} ${compactId(item.userId)}`}
        actions={
          <Link to="/admin/mmt/evaluations" className={controlLink}>
            <ArrowLeft className="h-4 w-4" /> {t("mmt.navEvaluations")}
          </Link>
        }
      />
      <section className="app-surface mb-5 grid gap-y-5 rounded-[1.5rem] p-5 sm:grid-cols-2 lg:grid-cols-5">
        <Metric
          label={t("mmt.totalScore")}
          value={item.totalScore.toFixed(2)}
        />
        <Metric
          label={t("mmt.acceptedPriority")}
          value={item.acceptedChoicePriority ?? "—"}
        />
        <Metric
          label={t("mmt.goalGap")}
          value={item.missingScoreForGoal?.toFixed(2) ?? "—"}
        />
        <Metric
          label={t("mmt.readiness")}
          value={
            item.readinessPercentage === null
              ? "—"
              : `${item.readinessPercentage.toFixed(2)}%`
          }
        />
        <div className="px-4 py-1">
          <p className="mb-2 text-xs font-bold uppercase text-[var(--muted)]">
            {t("mmt.status")}
          </p>
          <Status keyName={item.motivationalMessageKey} />
        </div>
      </section>
      <div className="mb-5 grid gap-4 rounded-2xl border border-[var(--border)] bg-white p-4 sm:grid-cols-2 lg:grid-cols-4">
        <Info label="ID" value={item.id} />
        <Info label={t("mmt.profileId")} value={item.studentMmtProfileId} />
        <Info
          label={t("mmt.acceptedProgram")}
          value={item.acceptedAdmissionProgramId ?? t("mmt.none")}
        />
        <Info
          label={t("mmt.examSession")}
          value={item.examSessionId ?? t("mmt.notLinked")}
        />
      </div>
      <h2 className="mb-3 text-lg font-black">{t("mmt.choiceSnapshots")}</h2>
      <TableShell>
        <Table>
          <thead className="bg-[var(--surface-muted)] text-xs uppercase text-[var(--muted)]">
            <tr>
              <th className="px-3 py-3">{t("mmt.priority")}</th>
              <th className="px-3 py-3">
                {t("mmt.university")} / {t("mmt.specialty")}
              </th>
              <th className="px-3 py-3">{t("mmt.cluster")}</th>
              <th className="px-3 py-3">{t("mmt.typeFormLanguage")}</th>
              <th className="px-3 py-3">{t("mmt.scoreUsed")}</th>
              <th className="px-3 py-3">{t("mmt.threshold")}</th>
              <th className="px-3 py-3">{t("mmt.goalGap")}</th>
              <th className="px-3 py-3">{t("mmt.result")}</th>
            </tr>
          </thead>
          <tbody>
            {item.choices.map((choice) => (
              <tr
                key={choice.id}
                className={
                  choice.isAccepted
                    ? "border-t border-emerald-200 bg-emerald-50/60"
                    : "border-t border-[var(--border)]"
                }
              >
                <td className="px-3 py-3 text-center font-black">
                  {choice.priorityOrder}
                </td>
                <td className="px-3 py-3">
                  <strong>{choice.universityName}</strong>
                  <small className="block text-[var(--muted)]">
                    {choice.specialtyCode} · {choice.specialtyName}
                  </small>
                </td>
                <td className="px-3 py-3 font-mono">{choice.clusterCode}</td>
                <td className="px-3 py-3 text-xs">
                  {enumLabel(
                    labels.admissionTypes,
                    choice.admissionType,
                    labels.unknown,
                  )}{" "}
                  ·{" "}
                  {enumLabel(
                    labels.studyForms,
                    choice.studyForm,
                    labels.unknown,
                  )}{" "}
                  ·{" "}
                  {enumLabel(
                    labels.studyLanguages,
                    choice.studyLanguage,
                    labels.unknown,
                  )}
                </td>
                <td className="px-3 py-3">
                  {choice.passingScoreUsed?.toFixed(2) ?? "—"}
                </td>
                <td className="px-3 py-3 font-bold">
                  {choice.conservativeThresholdUsed?.toFixed(2) ?? "—"}
                </td>
                <td className="px-3 py-3">
                  {choice.missingScore?.toFixed(2) ?? "—"}
                </td>
                <td className="px-3 py-3">
                  <BooleanBadge
                    value={choice.isAccepted}
                    positive={t("mmt.accepted")}
                    negative={t("mmt.notAccepted")}
                  />
                </td>
              </tr>
            ))}
          </tbody>
        </Table>
      </TableShell>
    </>
  );
}

function Status({ keyName }: { keyName: string }) {
  const { t } = useTranslation();
  const labels: Record<string, string> = {
    "MMT.Accepted": t("mmt.accepted"),
    "MMT.NearMiss": t("mmt.nearMiss"),
    "MMT.ProgressNeeded": t("mmt.progressNeeded"),
    "MMT.NoThresholdData": t("mmt.noThresholdData"),
  };
  const label = labels[keyName] ?? keyName;
  return (
    <BooleanBadge
      value={keyName === "MMT.Accepted"}
      positive={label}
      negative={label}
    />
  );
}
function Info({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <p className="text-xs font-bold uppercase text-[var(--muted)]">{label}</p>
      <p className="mt-1 break-all font-mono text-xs">{value}</p>
    </div>
  );
}
function numeric(value: string | null) {
  if (!value) return undefined;
  const result = Number(value);
  return Number.isFinite(result) ? result : undefined;
}
