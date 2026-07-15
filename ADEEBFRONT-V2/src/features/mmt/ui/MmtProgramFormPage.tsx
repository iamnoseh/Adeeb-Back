import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { ArrowLeft, Save } from "lucide-react";
import { useEffect, useState, type FormEvent } from "react";
import { useTranslation } from "react-i18next";
import { Link, useNavigate, useParams, useSearchParams } from "react-router-dom";
import { mmtApi, mmtKeys } from "@/features/mmt/api/mmt.api";
import {
  controlLink,
  errorMessage,
  numberOrNull,
} from "@/features/mmt/lib/mmt";
import { useMmtLabels } from "@/features/mmt/lib/useMmtLabels";
import type { AdmissionProgramInput } from "@/features/mmt/model/mmt.types";
import { useMmtToast } from "@/features/mmt/model/useMmtToast";
import { MmtToast } from "@/features/mmt/ui/MmtUi";
import { MmtReferenceSelect } from "@/features/mmt/ui/MmtReferenceSelect";
import { Button } from "@/shared/ui/Button";
import { FormField } from "@/shared/ui/FormField";
import { Input } from "@/shared/ui/Input";
import { PageHeader } from "@/shared/ui/PageHeader";
import { SelectField } from "@/shared/ui/SelectField";
import { ErrorState } from "@/shared/ui/StateBlock";

export function MmtProgramFormPage() {
  const { t } = useTranslation();
  const labels = useMmtLabels();
  const { programId } = useParams();
  const [searchParams] = useSearchParams();
  const editing = Boolean(programId);
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const toast = useMmtToast();
  const presetUniversityId = editing ? "" : searchParams.get("universityId") ?? "";
  const presetSpecialtyId = editing ? "" : searchParams.get("specialtyId") ?? "";
  const [universityId, setUniversityId] = useState(presetUniversityId);
  const [specialtyId, setSpecialtyId] = useState(presetSpecialtyId);
  const [clusterId, setClusterId] = useState("");
  const [admissionType, setAdmissionType] = useState("0");
  const [studyForm, setStudyForm] = useState("0");
  const [studyLanguage, setStudyLanguage] = useState("0");
  const [admissionYear, setAdmissionYear] = useState("");
  const detail = useQuery({
    queryKey: mmtKeys.program(programId ?? ""),
    queryFn: () => mmtApi.program(programId!),
    enabled: editing,
  });
  const dashboard = useQuery({
    queryKey: mmtKeys.dashboard(),
    queryFn: mmtApi.dashboard,
  });
  useEffect(() => {
    if (!detail.data) return;
    setUniversityId(detail.data.university.id);
    setSpecialtyId(detail.data.specialty.id);
    setClusterId(detail.data.cluster.id);
    setAdmissionType(String(detail.data.admissionType));
    setStudyForm(String(detail.data.studyForm));
    setStudyLanguage(String(detail.data.studyLanguage));
    setAdmissionYear(String(detail.data.admissionYear));
  }, [detail.data]);
  useEffect(() => {
    if (!editing && !admissionYear && dashboard.data)
      setAdmissionYear(String(dashboard.data.currentAdmissionYear));
  }, [admissionYear, dashboard.data, editing]);
  const mutation = useMutation({
    mutationFn: (input: AdmissionProgramInput) =>
      editing
        ? mmtApi.updateProgram(programId!, {
            ...input,
            isActive: detail.data?.isActive ?? true,
          })
        : mmtApi.createProgram(input),
    onSuccess: async (program) => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ["mmt", "programs"] }),
        queryClient.invalidateQueries({ queryKey: mmtKeys.dashboard() }),
      ]);
      navigate(`/admin/mmt/programs/${program.id}`);
    },
    onError: (error) =>
      toast.error(errorMessage(error, t("mmt.requestFailed"))),
  });
  if (editing && detail.isLoading)
    return (
      <p className="text-sm text-[var(--muted)]">{t("mmt.loadingProgram")}</p>
    );
  if (editing && detail.isError)
    return (
      <ErrorState
        title={t("mmt.programLoadFailed")}
        description={errorMessage(detail.error, t("mmt.programLoadFailed"))}
      />
    );
  const program = detail.data;
  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    mutation.mutate({
      universityId,
      specialtyId,
      mmtClusterId: clusterId,
      admissionType: Number(admissionType),
      studyForm: Number(studyForm),
      studyLanguage: Number(studyLanguage),
      admissionYear: Number(admissionYear),
      seatsCount: numberOrNull(data.get("seatsCount")),
      isPublished: data.get("isPublished") === "on",
    });
  }
  return (
    <>
      <PageHeader
        title={editing ? t("mmt.editProgram") : t("mmt.newProgram")}
        description={t("mmt.programFormDescription")}
        actions={
          <Link to="/admin/mmt/programs" className={controlLink}>
            <ArrowLeft className="h-4 w-4" /> {t("mmt.back")}
          </Link>
        }
      />
      <form
        className="app-surface grid gap-5 rounded-[1.5rem] p-5"
        onSubmit={submit}
      >
        <div className="grid gap-4 md:grid-cols-3">
          <FormField label={t("mmt.university")}>
            <MmtReferenceSelect
              kind="universities"
              value={universityId}
              placeholder={t("mmt.selectUniversity")}
              onValueChange={setUniversityId}
              disabled={Boolean(presetUniversityId)}
            />
          </FormField>
          <FormField label={t("mmt.specialty")}>
            <MmtReferenceSelect
              kind="specialties"
              value={specialtyId}
              placeholder={t("mmt.selectSpecialty")}
              onValueChange={setSpecialtyId}
              disabled={Boolean(presetSpecialtyId)}
            />
          </FormField>
          <FormField label={t("mmt.cluster")}>
            <MmtReferenceSelect
              kind="clusters"
              value={clusterId}
              placeholder={t("mmt.selectCluster")}
              onValueChange={setClusterId}
            />
          </FormField>
        </div>
        <div className="grid gap-4 md:grid-cols-3">
          <FormField label={t("mmt.admissionType")}>
            <SelectField
              value={admissionType}
              options={labels.admissionTypes.map((label, value) => ({
                value: String(value),
                label,
              }))}
              onValueChange={setAdmissionType}
            />
          </FormField>
          <FormField label={t("mmt.studyForm")}>
            <SelectField
              value={studyForm}
              options={labels.studyForms.map((label, value) => ({
                value: String(value),
                label,
              }))}
              onValueChange={setStudyForm}
            />
          </FormField>
          <FormField label={t("mmt.studyLanguage")}>
            <SelectField
              value={studyLanguage}
              options={labels.studyLanguages.map((label, value) => ({
                value: String(value),
                label,
              }))}
              onValueChange={setStudyLanguage}
            />
          </FormField>
        </div>
        <div className="grid gap-4 md:grid-cols-3">
          <FormField label={t("mmt.admissionYear")}>
            <Input
              name="admissionYear"
              type="number"
              min="2000"
              max="2100"
              value={admissionYear}
              onChange={(event) => setAdmissionYear(event.target.value)}
              required
            />
          </FormField>
          <FormField label={t("mmt.seats")}>
            <Input
              name="seatsCount"
              type="number"
              min="0"
              defaultValue={program?.seatsCount ?? ""}
            />
          </FormField>
          <label className="flex min-h-11 items-center gap-3 self-end rounded-2xl bg-[var(--surface-muted)] px-4 text-sm font-bold">
            <input
              name="isPublished"
              type="checkbox"
              defaultChecked={program?.isPublished ?? false}
              className="h-4 w-4 accent-[var(--primary)]"
            />{" "}
            {t("mmt.publishForStudents")}
          </label>
        </div>
        <div className="flex justify-end border-t border-[var(--border)] pt-4">
          <Button
            disabled={
              mutation.isPending || !universityId || !specialtyId || !clusterId
            }
          >
            <Save className="h-4 w-4" />{" "}
            {mutation.isPending ? t("mmt.saving") : t("mmt.saveProgram")}
          </Button>
        </div>
      </form>
      <MmtToast notice={toast.notice} onClose={toast.clear} />
    </>
  );
}
