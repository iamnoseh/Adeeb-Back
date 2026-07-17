import { httpClient } from "@/shared/api/http-client";
import { queryString } from "@/features/mmt/lib/mmt";
import { getStoredUiLanguage } from "@/shared/i18n/language";
import type {
  AdmissionProgramDto,
  AdmissionProgramInput,
  AdmissionProgramListItemDto,
  AdmissionProgramQuery,
  CatalogDto,
  CatalogInput,
  CatalogKind,
  EvaluationQuery,
  ImportOptions,
  ListQuery,
  MmtEvaluationDto,
  MmtEvaluationListItemDto,
  MmtImportPreviewResultDto,
  MmtImportResultDto,
  MmtDashboardStatsDto,
  PagedResponse,
  PassingScoreAnalyticsDto,
  PassingScoreHistoryDto,
  PassingScoreInput,
  StudentMmtProfileDto,
  StudentAdmissionChoiceDto,
  UpsertStudentMmtProfileInput,
  AdmissionChoiceInput,
  SimulateMmtEvaluationInput,
  StudentProfileQuery,
  MmtClusterDto,
  SpecialtyDto,
  UniversityDto,
  StudentSpecialtyLookupQuery,
  StudentUniversityLookupQuery,
  MmtCatalogImportOptions,
  MmtCatalogImportPreviewResultDto,
  MmtCatalogImportResultDto,
} from "@/features/mmt/model/mmt.types";

const root = "/api/v2/admin/mmt";
const studentRoot = "/api/v2/mmt";

export const mmtKeys = {
  all: ["mmt"] as const,
  dashboard: () => ["mmt", "dashboard"] as const,
  catalog: (kind: CatalogKind, query: ListQuery = {}) =>
    ["mmt", kind, query, getStoredUiLanguage()] as const,
  catalogDetail: (kind: CatalogKind, id: string) =>
    ["mmt", kind, id, getStoredUiLanguage()] as const,
  programs: (query: AdmissionProgramQuery = {}) =>
    ["mmt", "programs", query, getStoredUiLanguage()] as const,
  program: (id: string) =>
    ["mmt", "program", id, getStoredUiLanguage()] as const,
  scores: (id: string) => ["mmt", "scores", id, getStoredUiLanguage()] as const,
  analytics: (id: string) =>
    ["mmt", "analytics", id, getStoredUiLanguage()] as const,
  profiles: (query: StudentProfileQuery = {}) =>
    ["mmt", "profiles", query, getStoredUiLanguage()] as const,
  profile: (id: string) =>
    ["mmt", "profile", id, getStoredUiLanguage()] as const,
  profileChoices: (id: string) =>
    ["mmt", "profile", id, "choices", getStoredUiLanguage()] as const,
  evaluations: (query: EvaluationQuery = {}) =>
    ["mmt", "evaluations", query, getStoredUiLanguage()] as const,
  evaluation: (id: string) =>
    ["mmt", "evaluation", id, getStoredUiLanguage()] as const,
};

export const mmtStudentKeys = {
  all: ["mmt", "student"] as const,
  programs: (query: AdmissionProgramQuery = {}) => ["mmt", "student", "programs", query, getStoredUiLanguage()] as const,
  program: (id: string) => ["mmt", "student", "program", id, getStoredUiLanguage()] as const,
  profile: () => ["mmt", "student", "profile", getStoredUiLanguage()] as const,
  choices: () => ["mmt", "student", "choices", getStoredUiLanguage()] as const,
  clusters: (query: ListQuery = {}) => ["mmt", "student", "clusters", query, getStoredUiLanguage()] as const,
  specialties: (query: StudentSpecialtyLookupQuery) => ["mmt", "student", "specialties", query, getStoredUiLanguage()] as const,
  universities: (query: StudentUniversityLookupQuery) => ["mmt", "student", "universities", query, getStoredUiLanguage()] as const,
  evaluations: (query: EvaluationQuery = {}) => ["mmt", "student", "evaluations", query, getStoredUiLanguage()] as const,
  evaluation: (id: string) => ["mmt", "student", "evaluation", id, getStoredUiLanguage()] as const,
  scores: (id: string) => ["mmt", "student", "scores", id, getStoredUiLanguage()] as const,
};

export const mmtStudentApi = {
  async clusters(query: ListQuery = {}) {
    const response = await httpClient.get<PagedResponse<MmtClusterDto>>(
      `${studentRoot}/clusters${queryString(query)}`,
    );
    return response.data;
  },
  async specialties(query: StudentSpecialtyLookupQuery) {
    const response = await httpClient.get<PagedResponse<SpecialtyDto>>(
      `${studentRoot}/specialties${queryString(query)}`,
    );
    return response.data;
  },
  async universities(query: StudentUniversityLookupQuery) {
    const response = await httpClient.get<PagedResponse<UniversityDto>>(
      `${studentRoot}/universities${queryString(query)}`,
    );
    return response.data;
  },
  async programs(query: AdmissionProgramQuery = {}) {
    const response = await httpClient.get<PagedResponse<AdmissionProgramListItemDto>>(
      `${studentRoot}/admission-programs${queryString(query)}`,
    );
    return response.data;
  },
  async program(id: string) {
    const response = await httpClient.get<AdmissionProgramDto>(
      `${studentRoot}/admission-programs/${id}`,
    );
    return response.data;
  },
  async scores(id: string) {
    const response = await httpClient.get<PassingScoreHistoryDto[]>(`${studentRoot}/admission-programs/${id}/passing-scores`);
    return response.data;
  },
  async profile() {
    const response = await httpClient.get<StudentMmtProfileDto>(`${studentRoot}/profile`);
    return response.data;
  },
  async upsertProfile(input: UpsertStudentMmtProfileInput) {
    const response = await httpClient.put<StudentMmtProfileDto>(`${studentRoot}/profile`, input);
    return response.data;
  },
  async choices() {
    const response = await httpClient.get<StudentAdmissionChoiceDto[]>(`${studentRoot}/profile/choices`);
    return response.data;
  },
  async replaceChoices(choices: AdmissionChoiceInput[]) {
    const response = await httpClient.put<StudentAdmissionChoiceDto[]>(`${studentRoot}/profile/choices`, { choices });
    return response.data;
  },
  async simulate(input: SimulateMmtEvaluationInput) {
    const response = await httpClient.post<MmtEvaluationDto>(`${studentRoot}/evaluations/simulate`, input);
    return response.data;
  },
  async evaluations(query: EvaluationQuery = {}) {
    const response = await httpClient.get<PagedResponse<MmtEvaluationListItemDto>>(
      `${studentRoot}/evaluations${queryString(query)}`,
    );
    return response.data;
  },
  async evaluation(id: string) {
    const response = await httpClient.get<MmtEvaluationDto>(`${studentRoot}/evaluations/${id}`);
    return response.data;
  },
};

export const mmtApi = {
  async dashboard() {
    const response = await httpClient.get<MmtDashboardStatsDto>(`${root}/dashboard`);
    return response.data;
  },
  async catalogList<T extends CatalogDto>(
    kind: CatalogKind,
    query: ListQuery = {},
  ) {
    const response = await httpClient.get<PagedResponse<T>>(
      `${root}/${kind}${queryString(query)}`,
    );
    return response.data;
  },
  async catalogDetail<T extends CatalogDto>(kind: CatalogKind, id: string) {
    const response = await httpClient.get<T>(`${root}/${kind}/${id}`);
    return response.data;
  },
  async createCatalog<T extends CatalogDto>(
    kind: CatalogKind,
    input: CatalogInput,
  ) {
    const response = await httpClient.post<T>(`${root}/${kind}`, input);
    return response.data;
  },
  async updateCatalog<T extends CatalogDto>(
    kind: CatalogKind,
    id: string,
    input: CatalogInput,
  ) {
    const response = await httpClient.put<T>(`${root}/${kind}/${id}`, input);
    return response.data;
  },
  async setCatalogStatus(kind: CatalogKind, id: string, isActive: boolean) {
    await httpClient.patch(`${root}/${kind}/${id}/status`, { isActive });
  },
  async programs(query: AdmissionProgramQuery = {}) {
    const response = await httpClient.get<
      PagedResponse<AdmissionProgramListItemDto>
    >(`${root}/admission-programs${queryString(query)}`);
    return response.data;
  },
  async program(id: string) {
    const response = await httpClient.get<AdmissionProgramDto>(
      `${root}/admission-programs/${id}`,
    );
    return response.data;
  },
  async createProgram(input: AdmissionProgramInput) {
    const response = await httpClient.post<AdmissionProgramDto>(
      `${root}/admission-programs`,
      input,
    );
    return response.data;
  },
  async updateProgram(
    id: string,
    input: AdmissionProgramInput & { isActive: boolean },
  ) {
    const response = await httpClient.put<AdmissionProgramDto>(
      `${root}/admission-programs/${id}`,
      input,
    );
    return response.data;
  },
  async setProgramStatus(id: string, isActive: boolean) {
    await httpClient.patch(`${root}/admission-programs/${id}/status`, {
      isActive,
    });
  },
  async setProgramPublished(id: string, isPublished: boolean) {
    await httpClient.patch(`${root}/admission-programs/${id}/publish`, {
      isPublished,
    });
  },
  async scores(programId: string) {
    const response = await httpClient.get<PassingScoreHistoryDto[]>(
      `${root}/admission-programs/${programId}/passing-scores`,
    );
    return response.data;
  },
  async analytics(programId: string) {
    const response = await httpClient.get<PassingScoreAnalyticsDto>(
      `${root}/admission-programs/${programId}/passing-scores/analytics`,
    );
    return response.data;
  },
  async createScore(programId: string, input: PassingScoreInput) {
    const response = await httpClient.post<PassingScoreHistoryDto>(
      `${root}/admission-programs/${programId}/passing-scores`,
      input,
    );
    return response.data;
  },
  async updateScore(id: string, input: PassingScoreInput) {
    const response = await httpClient.put<PassingScoreHistoryDto>(
      `${root}/passing-scores/${id}`,
      input,
    );
    return response.data;
  },
  async deleteScore(id: string) {
    await httpClient.delete(`${root}/passing-scores/${id}`);
  },
  async downloadTemplate() {
    const response = await httpClient.get<Blob>(`${root}/import/template`, {
      responseType: "blob",
    });
    return response.data;
  },
  async previewImport(options: ImportOptions) {
    const response = await httpClient.post<MmtImportPreviewResultDto>(
      `${root}/import/preview`,
      importForm(options),
    );
    return response.data;
  },
  async confirmImport(options: ImportOptions) {
    const response = await httpClient.post<MmtImportResultDto>(
      `${root}/import/confirm`,
      importForm(options),
    );
    return response.data;
  },
  async downloadCatalogTemplate() {
    const response = await httpClient.get<Blob>(`${root}/import/catalog/template`, { responseType: "blob" });
    return response.data;
  },
  async previewCatalogImport(options: MmtCatalogImportOptions) {
    const response = await httpClient.post<MmtCatalogImportPreviewResultDto>(`${root}/import/catalog/preview`, catalogImportForm(options));
    return response.data;
  },
  async confirmCatalogImport(options: MmtCatalogImportOptions) {
    const response = await httpClient.post<MmtCatalogImportResultDto>(`${root}/import/catalog/confirm`, catalogImportForm(options));
    return response.data;
  },
  async profiles(query: StudentProfileQuery = {}) {
    const response = await httpClient.get<PagedResponse<StudentMmtProfileDto>>(
      `${root}/student-profiles${queryString(query)}`,
    );
    return response.data;
  },
  async profile(id: string) {
    const response = await httpClient.get<StudentMmtProfileDto>(
      `${root}/student-profiles/${id}`,
    );
    return response.data;
  },
  async profileChoices(id: string) {
    const response = await httpClient.get<StudentAdmissionChoiceDto[]>(
      `${root}/student-profiles/${id}/choices`,
    );
    return response.data;
  },
  async evaluations(query: EvaluationQuery = {}) {
    const response = await httpClient.get<
      PagedResponse<MmtEvaluationListItemDto>
    >(`${root}/evaluations${queryString(query)}`);
    return response.data;
  },
  async evaluation(id: string) {
    const response = await httpClient.get<MmtEvaluationDto>(
      `${root}/evaluations/${id}`,
    );
    return response.data;
  },
};

function importForm(options: ImportOptions) {
  const form = new FormData();
  form.append("File", options.file);
  form.append(
    "CreateMissingReferences",
    String(options.createMissingReferences),
  );
  form.append("ExistingScoreMode", String(options.existingScoreMode));
  if (options.admissionYear !== undefined)
    form.append("AdmissionYear", String(options.admissionYear));
  if (options.publishAdmissionPrograms !== undefined)
    form.append(
      "PublishAdmissionPrograms",
      String(options.publishAdmissionPrograms),
    );
  return form;
}

function catalogImportForm(options: MmtCatalogImportOptions) {
  const form = new FormData();
  form.append("File", options.file);
  form.append("MmtClusterId", options.mmtClusterId);
  form.append("AdmissionYear", String(options.admissionYear));
  form.append("DefaultUniversityType", String(options.defaultUniversityType));
  form.append("UniversityTypeOverridesJson", JSON.stringify(options.universityTypeOverrides));
  return form;
}
