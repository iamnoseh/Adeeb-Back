import { useQuery } from "@tanstack/react-query";
import { ArrowLeft, Eye } from "lucide-react";
import { useTranslation } from "react-i18next";
import { Link, useParams, useSearchParams } from "react-router-dom";
import { mmtApi, mmtKeys } from "@/features/mmt/api/mmt.api";
import { compactId, controlLink, errorMessage } from "@/features/mmt/lib/mmt";
import { BooleanBadge, Pagination } from "@/features/mmt/ui/MmtUi";
import { formatDushanbeDate } from "@/shared/lib/date";
import { Input } from "@/shared/ui/Input";
import { PageHeader } from "@/shared/ui/PageHeader";
import { SelectField } from "@/shared/ui/SelectField";
import { EmptyState, ErrorState } from "@/shared/ui/StateBlock";
import { Table, TableShell } from "@/shared/ui/Table";

export function MmtProfilesPage() {
  const { t, i18n } = useTranslation();
  const [params, setParams] = useSearchParams();
  const page = Math.max(1, Number(params.get("page") ?? 1));
  const filters = {
    userId: params.get("userId") || undefined,
    admissionYear: numberValue(params.get("admissionYear")),
    isActive: boolValue(params.get("isActive")),
    page,
    pageSize: 20,
  };
  const query = useQuery({
    queryKey: mmtKeys.profiles(filters),
    queryFn: () => mmtApi.profiles(filters),
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
        title={t("mmt.profilesTitle")}
        description={t("mmt.profilesDescription")}
      />
      <div className="mb-4 grid gap-3 rounded-2xl border border-[var(--border)] bg-white p-4 md:grid-cols-[1fr_180px_220px]">
        <Input
          value={params.get("userId") ?? ""}
          onChange={(event) => setFilter("userId", event.target.value)}
          placeholder={t("mmt.userId")}
        />
        <Input
          type="number"
          min="2000"
          max="2100"
          value={params.get("admissionYear") ?? ""}
          onChange={(event) => setFilter("admissionYear", event.target.value)}
          placeholder={t("mmt.admissionYear")}
        />
        <SelectField
          value={params.get("isActive") ?? ""}
          options={[
            { value: "", label: t("mmt.allStatuses") },
            { value: "true", label: t("mmt.active") },
            { value: "false", label: t("mmt.inactive") },
          ]}
          onValueChange={(value) => setFilter("isActive", value)}
        />
      </div>
      {query.isLoading ? (
        <p className="text-sm text-[var(--muted)]">
          {t("mmt.loadingProfiles")}
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
          title={t("mmt.noProfiles")}
          description={t("mmt.profilesFilterHint")}
        />
      ) : null}
      {query.data && query.data.items.length > 0 ? (
        <TableShell>
          <Table>
            <thead className="bg-[var(--surface-muted)] text-xs uppercase text-[var(--muted)]">
              <tr>
                <th className="px-4 py-3">{t("mmt.userId")}</th>
                <th className="px-4 py-3">{t("mmt.cluster")}</th>
                <th className="px-4 py-3">{t("mmt.year")}</th>
                <th className="px-4 py-3">{t("mmt.goalProgram")}</th>
                <th className="px-4 py-3">{t("mmt.choices")}</th>
                <th className="px-4 py-3">{t("mmt.status")}</th>
                <th className="px-4 py-3">{t("mmt.created")}</th>
                <th className="px-4 py-3 text-right">{t("mmt.action")}</th>
              </tr>
            </thead>
            <tbody>
              {query.data.items.map((profile) => (
                <tr
                  key={profile.id}
                  className="border-t border-[var(--border)]"
                >
                  <td
                    className="px-4 py-3 font-mono text-xs"
                    title={profile.userId}
                  >
                    {compactId(profile.userId)}
                  </td>
                  <td className="px-4 py-3">
                    <strong>{profile.cluster.code}</strong>
                    <small className="block text-[var(--muted)]">
                      {profile.cluster.name}
                    </small>
                  </td>
                  <td className="px-4 py-3">{profile.admissionYear}</td>
                  <td
                    className="px-4 py-3 font-mono text-xs"
                    title={profile.goalAdmissionProgramId ?? ""}
                  >
                    {compactId(profile.goalAdmissionProgramId)}
                  </td>
                  <td className="px-4 py-3 font-bold">
                    {profile.choicesCount}
                  </td>
                  <td className="px-4 py-3">
                    <BooleanBadge value={profile.isActive} />
                  </td>
                  <td className="px-4 py-3 text-xs">
                    {formatDushanbeDate(profile.createdAtUtc, i18n.language)}
                  </td>
                  <td className="px-4 py-3 text-right">
                    <Link
                      to={`/admin/mmt/profiles/${profile.id}`}
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

export function MmtProfileDetailPage() {
  const { t, i18n } = useTranslation();
  const { profileId = "" } = useParams();
  const profile = useQuery({
    queryKey: mmtKeys.profile(profileId),
    queryFn: () => mmtApi.profile(profileId),
    enabled: Boolean(profileId),
  });
  const evaluations = useQuery({
    queryKey: mmtKeys.evaluations({
      studentMmtProfileId: profileId,
      pageSize: 10,
    }),
    queryFn: () =>
      mmtApi.evaluations({ studentMmtProfileId: profileId, pageSize: 10 }),
    enabled: Boolean(profileId),
  });
  if (profile.isLoading)
    return (
      <p className="text-sm text-[var(--muted)]">{t("mmt.loadingProfiles")}</p>
    );
  if (profile.isError || !profile.data)
    return (
      <ErrorState
        title={t("mmt.profileLoadFailed")}
        description={errorMessage(profile.error, t("mmt.profileLoadFailed"))}
      />
    );
  const item = profile.data;
  return (
    <>
      <PageHeader
        title={`${t("mmt.profile")} · ${item.cluster.code}`}
        description={`${t("mmt.userId")}: ${item.userId}`}
        actions={
          <Link to="/admin/mmt/profiles" className={controlLink}>
            <ArrowLeft className="h-4 w-4" /> {t("mmt.navProfiles")}
          </Link>
        }
      />
      <section className="app-surface mb-5 grid gap-4 rounded-[1.5rem] p-5 sm:grid-cols-2 lg:grid-cols-4">
        <Info
          label={t("mmt.admissionYear")}
          value={String(item.admissionYear)}
        />
        <Info
          label={t("mmt.cluster")}
          value={`${item.cluster.code} · ${item.cluster.name}`}
        />
        <Info
          label={t("mmt.goalProgram")}
          value={item.goalAdmissionProgramId ?? t("mmt.notSelected")}
        />
        <div>
          <p className="mb-2 text-xs font-bold uppercase text-[var(--muted)]">
            {t("mmt.status")}
          </p>
          <BooleanBadge value={item.isActive} />
        </div>
        <Info label={t("mmt.choiceCount")} value={String(item.choicesCount)} />
        <Info
          label={t("mmt.created")}
          value={formatDushanbeDate(item.createdAtUtc, i18n.language)}
        />
        <Info
          label={t("mmt.updated")}
          value={formatDushanbeDate(item.updatedAtUtc, i18n.language)}
        />
      </section>
      <div className="mb-5 rounded-2xl border border-[var(--border)] bg-[var(--surface-muted)] p-4 text-sm text-[var(--muted)]">
        {t("mmt.choicesContractNote")}
      </div>
      <h2 className="mb-3 text-lg font-black">{t("mmt.recentEvaluations")}</h2>
      {evaluations.isLoading ? (
        <p className="text-sm text-[var(--muted)]">
          {t("mmt.loadingEvaluations")}
        </p>
      ) : null}
      {evaluations.data?.items.length === 0 ? (
        <EmptyState title={t("mmt.noProfileEvaluations")} />
      ) : null}
      {evaluations.data && evaluations.data.items.length > 0 ? (
        <TableShell>
          <Table>
            <thead className="bg-[var(--surface-muted)] text-xs uppercase text-[var(--muted)]">
              <tr>
                <th className="px-4 py-3">{t("mmt.date")}</th>
                <th className="px-4 py-3">{t("mmt.score")}</th>
                <th className="px-4 py-3">{t("mmt.acceptedPriority")}</th>
                <th className="px-4 py-3">{t("mmt.readiness")}</th>
                <th className="px-4 py-3">{t("mmt.status")}</th>
                <th className="px-4 py-3 text-right">{t("mmt.action")}</th>
              </tr>
            </thead>
            <tbody>
              {evaluations.data.items.map((evaluation) => (
                <tr
                  key={evaluation.id}
                  className="border-t border-[var(--border)]"
                >
                  <td className="px-4 py-3">
                    {formatDushanbeDate(
                      evaluation.evaluatedAtUtc,
                      i18n.language,
                    )}
                  </td>
                  <td className="px-4 py-3 font-black">
                    {evaluation.totalScore.toFixed(2)}
                  </td>
                  <td className="px-4 py-3">
                    {evaluation.acceptedChoicePriority ?? "—"}
                  </td>
                  <td className="px-4 py-3">
                    {evaluation.readinessPercentage === null
                      ? "—"
                      : `${evaluation.readinessPercentage.toFixed(2)}%`}
                  </td>
                  <td className="px-4 py-3">
                    <Motivation value={evaluation.motivationalMessageKey} />
                  </td>
                  <td className="px-4 py-3 text-right">
                    <Link
                      to={`/admin/mmt/evaluations/${evaluation.id}`}
                      className={controlLink}
                    >
                      <Eye className="h-4 w-4" /> {t("mmt.view")}
                    </Link>
                  </td>
                </tr>
              ))}
            </tbody>
          </Table>
        </TableShell>
      ) : null}
    </>
  );
}

function Motivation({ value }: { value: string }) {
  const { t } = useTranslation();
  const labels: Record<string, string> = {
    "MMT.Accepted": t("mmt.accepted"),
    "MMT.NearMiss": t("mmt.nearMiss"),
    "MMT.ProgressNeeded": t("mmt.progressNeeded"),
    "MMT.NoThresholdData": t("mmt.noThresholdData"),
  };
  return labels[value] ?? value;
}
function Info({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <p className="text-xs font-bold uppercase text-[var(--muted)]">{label}</p>
      <p className="mt-1 break-all font-semibold">{value}</p>
    </div>
  );
}
function numberValue(value: string | null) {
  if (!value) return undefined;
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : undefined;
}
function boolValue(value: string | null) {
  return value === "true" ? true : value === "false" ? false : undefined;
}
