import { useQueries } from "@tanstack/react-query";
import {
  Building2,
  ChartNoAxesCombined,
  FileSpreadsheet,
  GraduationCap,
  Layers3,
  Plus,
  School,
  Target,
} from "lucide-react";
import { Link } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { mmtApi } from "@/features/mmt/api/mmt.api";
import { controlLink } from "@/features/mmt/lib/mmt";
import type {
  MmtClusterDto,
  SpecialtyDto,
  UniversityDto,
} from "@/features/mmt/model/mmt.types";
import { Metric } from "@/features/mmt/ui/MmtUi";
import { PageHeader } from "@/shared/ui/PageHeader";
import { ErrorState } from "@/shared/ui/StateBlock";

export function MmtDashboardPage() {
  const { t } = useTranslation();
  const results = useQueries({
    queries: [
      {
        queryKey: ["mmt", "dashboard", "clusters"],
        queryFn: () =>
          mmtApi.catalogList<MmtClusterDto>("clusters", {
            isActive: true,
            pageSize: 1,
          }),
      },
      {
        queryKey: ["mmt", "dashboard", "universities"],
        queryFn: () =>
          mmtApi.catalogList<UniversityDto>("universities", {
            isActive: true,
            pageSize: 1,
          }),
      },
      {
        queryKey: ["mmt", "dashboard", "specialties"],
        queryFn: () =>
          mmtApi.catalogList<SpecialtyDto>("specialties", {
            isActive: true,
            pageSize: 1,
          }),
      },
      {
        queryKey: ["mmt", "dashboard", "programs"],
        queryFn: () =>
          mmtApi.programs({ isActive: true, isPublished: true, pageSize: 100 }),
      },
    ],
  });
  const [clusters, universities, specialties, programs] = results;
  if (results.some((query) => query.isError))
    return <ErrorState title={t("mmt.loadFailed")} />;
  const loading = results.some((query) => query.isLoading);
  const missingScores =
    programs?.data?.items.filter(
      (program) => program.latestPassingScore === null,
    ).length ?? 0;

  return (
    <>
      <PageHeader
        title={t("mmt.dashboardTitle")}
        description={t("mmt.dashboardDescription")}
      />
      <section
        className="app-surface mb-5 grid gap-y-5 rounded-[1.5rem] p-5 sm:grid-cols-2 lg:grid-cols-5"
        aria-label="MMT metrics"
      >
        <Metric
          label={t("mmt.activeClusters")}
          value={loading ? "—" : (clusters?.data?.totalCount ?? 0)}
        />
        <Metric
          label={t("mmt.activeUniversities")}
          value={loading ? "—" : (universities?.data?.totalCount ?? 0)}
        />
        <Metric
          label={t("mmt.activeSpecialties")}
          value={loading ? "—" : (specialties?.data?.totalCount ?? 0)}
        />
        <Metric
          label={t("mmt.publishedPrograms")}
          value={loading ? "—" : (programs?.data?.totalCount ?? 0)}
        />
        <Metric
          label={t("mmt.missingScores")}
          value={loading ? "—" : missingScores}
          warning={missingScores > 0}
        />
      </section>
      <div className="grid gap-5 lg:grid-cols-[1.2fr_0.8fr]">
        <section className="app-surface rounded-[1.5rem] p-5">
          <h2 className="text-base font-black">{t("mmt.quickActions")}</h2>
          <div className="mt-4 grid gap-3 sm:grid-cols-2">
            <Action
              to="/admin/mmt/import"
              icon={FileSpreadsheet}
              label={t("mmt.importExcel")}
              detail={t("mmt.importExcelHint")}
            />
            <Action
              to="/admin/mmt/programs/new"
              icon={Plus}
              label={t("mmt.addProgram")}
              detail={t("mmt.addProgramHint")}
            />
            <Action
              to="/admin/mmt/universities"
              icon={Building2}
              label={t("mmt.addUniversity")}
              detail={t("mmt.addUniversityHint")}
            />
            <Action
              to="/admin/mmt/evaluations"
              icon={ChartNoAxesCombined}
              label={t("mmt.reviewEvaluations")}
              detail={t("mmt.reviewEvaluationsHint")}
            />
          </div>
        </section>
        <section className="app-surface rounded-[1.5rem] p-5">
          <h2 className="text-base font-black">{t("mmt.dataWorkflow")}</h2>
          <ol className="mt-4 grid gap-4 text-sm">
            <Step icon={Layers3} number="1" text={t("mmt.workflow1")} />
            <Step icon={School} number="2" text={t("mmt.workflow2")} />
            <Step icon={Target} number="3" text={t("mmt.workflow3")} />
            <Step icon={GraduationCap} number="4" text={t("mmt.workflow4")} />
          </ol>
        </section>
      </div>
    </>
  );
}

function Action({
  to,
  icon: Icon,
  label,
  detail,
}: {
  to: string;
  icon: typeof Building2;
  label: string;
  detail: string;
}) {
  return (
    <Link to={to} className={`${controlLink} justify-start p-4`}>
      <Icon className="h-5 w-5 text-[var(--primary)]" />
      <span>
        <strong className="block">{label}</strong>
        <small className="mt-0.5 block font-medium text-[var(--muted)]">
          {detail}
        </small>
      </span>
    </Link>
  );
}
function Step({
  icon: Icon,
  number,
  text,
}: {
  icon: typeof Building2;
  number: string;
  text: string;
}) {
  return (
    <li className="flex items-center gap-3">
      <span className="grid h-9 w-9 place-items-center rounded-xl bg-[var(--primary-soft)] text-[var(--primary-strong)]">
        <Icon className="h-4 w-4" />
      </span>
      <span className="text-[var(--muted)]">
        <strong className="mr-2 text-[var(--text)]">{number}.</strong>
        {text}
      </span>
    </li>
  );
}
