import type { ProblemDetails } from "@/shared/api/problem-details";

export type { ProblemDetails };

export type PagedResponse<T> = {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
};
export type MmtDashboardStatsDto = {
  activeClustersCount: number;
  activeUniversitiesCount: number;
  activeSpecialtiesCount: number;
  publishedProgramsCount: number;
  activeProgramsCount: number;
  programsMissingLatestScoreCount: number;
  programsMissingAnyScoreCount: number;
  currentAdmissionYear: number;
  evaluationsCount: number;
  studentProfilesCount: number;
};
export type ListQuery = {
  search?: string | undefined;
  isActive?: boolean | undefined;
  page?: number | undefined;
  pageSize?: number | undefined;
};

export type MmtClusterDto = {
  id: string;
  name: string;
  code: string;
  description: string | null;
  nameTg: string;
  nameRu: string;
  descriptionTg: string | null;
  descriptionRu: string | null;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc: string;
  subjects: MmtClusterSubjectDto[];
};
export type MmtClusterSubjectDto = { id: string; code: string; name: string };
export type UniversityDto = {
  id: string;
  fullName: string;
  shortName: string | null;
  city: string;
  fullNameTg: string;
  fullNameRu: string;
  shortNameTg: string | null;
  shortNameRu: string | null;
  cityTg: string;
  cityRu: string;
  type: number;
  logoUrl: string | null;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc: string;
};
export type SpecialtyDto = {
  id: string;
  code: string;
  name: string;
  description: string | null;
  nameTg: string;
  nameRu: string;
  descriptionTg: string | null;
  descriptionRu: string | null;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc: string;
};

export type AdmissionProgramListItemDto = {
  id: string;
  universityId: string;
  universityName: string;
  specialtyId: string;
  specialtyCode: string;
  specialtyName: string;
  mmtClusterId: string;
  clusterCode: string;
  clusterName: string;
  admissionType: number;
  studyForm: number;
  studyLanguage: number;
  admissionYear: number;
  seatsCount: number | null;
  isPublished: boolean;
  isActive: boolean;
  latestPassingScore: number | null;
};
export type AdmissionProgramDto = {
  id: string;
  university: UniversityDto;
  specialty: SpecialtyDto;
  cluster: MmtClusterDto;
  admissionType: number;
  studyForm: number;
  studyLanguage: number;
  admissionYear: number;
  seatsCount: number | null;
  isPublished: boolean;
  isActive: boolean;
  latestPassingScore: number | null;
  averagePassingScoreLast3Years: number | null;
  conservativeThreshold: number | null;
  createdAtUtc: string;
  updatedAtUtc: string;
};
export type AdmissionProgramInput = {
  universityId: string;
  specialtyId: string;
  mmtClusterId: string;
  admissionType: number;
  studyForm: number;
  studyLanguage: number;
  admissionYear: number;
  seatsCount: number | null;
  isPublished: boolean;
  isActive?: boolean;
};
export type AdmissionProgramQuery = ListQuery & {
  clusterId?: string | undefined;
  universityId?: string | undefined;
  specialtyId?: string | undefined;
  admissionType?: number | undefined;
  studyForm?: number | undefined;
  studyLanguage?: number | undefined;
  admissionYear?: number | undefined;
  isPublished?: boolean | undefined;
};

export type PassingScoreHistoryDto = {
  id: string;
  admissionProgramId: string;
  year: number;
  passingScore: number;
  seatsCount: number | null;
  source: string | null;
  note: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
  distributionRound: number;
};
export type PassingScoreInput = {
  year: number;
  passingScore: number;
  seatsCount: number | null;
  source: string | null;
  note: string | null;
  distributionRound: number;
};
export type PassingScoreAnalyticsDto = {
  latestPassingScore: number | null;
  averageLast3Years: number | null;
  conservativeThreshold: number | null;
};

export type MmtImportNormalizedRowDto = {
  year: number;
  clusterCode: string;
  clusterName: string;
  universityFullName: string;
  universityShortName: string | null;
  universityCity: string;
  universityType: number;
  specialtyCode: string;
  specialtyName: string;
  admissionType: number;
  studyForm: number;
  studyLanguage: number;
  seatsCount: number | null;
  passingScore: number;
  source: string | null;
  note: string | null;
  distributionRound: number;
};
export type MmtImportRowPreviewDto = {
  rowNumber: number;
  values: MmtImportNormalizedRowDto | null;
  isValid: boolean;
  isDuplicate: boolean;
  validationErrors: string[];
};
export type MmtImportPreviewResultDto = {
  totalRows: number;
  validRowsCount: number;
  invalidRowsCount: number;
  duplicateRowsCount: number;
  rows: MmtImportRowPreviewDto[];
};
export type MmtImportResultDto = {
  processedRows: number;
  importedPrograms: number;
  insertedScores: number;
  updatedScores: number;
  skippedScores: number;
  invalidRows: number;
  rows: MmtImportRowPreviewDto[];
};
export type ImportOptions = {
  file: File;
  createMissingReferences: boolean;
  existingScoreMode: number;
  admissionYear?: number | undefined;
  publishAdmissionPrograms?: boolean | undefined;
};

export type StudentMmtProfileDto = {
  id: string;
  userId: string;
  cluster: MmtClusterDto;
  admissionYear: number;
  goalAdmissionProgramId: string | null;
  isActive: boolean;
  choicesCount: number;
  createdAtUtc: string;
  updatedAtUtc: string;
};
export type UpsertStudentMmtProfileInput = {
  mmtClusterId: string;
  admissionYear?: number | null;
  goalAdmissionProgramId?: string | null;
};
export type StudentProfileQuery = {
  userId?: string | undefined;
  admissionYear?: number | undefined;
  isActive?: boolean | undefined;
  page?: number | undefined;
  pageSize?: number | undefined;
};
export type StudentAdmissionChoiceDto = {
  id: string;
  priorityOrder: number;
  admissionProgram: AdmissionProgramListItemDto;
  createdAtUtc: string;
  updatedAtUtc: string;
};
export type AdmissionChoiceInput = {
  admissionProgramId: string;
  priorityOrder: number;
};
export type SimulateMmtEvaluationInput = { totalScore: number };
export type MmtAdmissionChoiceSnapshotDto = {
  id: string;
  priorityOrder: number;
  admissionProgramId: string;
  universityName: string;
  specialtyCode: string;
  specialtyName: string;
  clusterCode: string;
  admissionType: number;
  studyForm: number;
  studyLanguage: number;
  admissionYear: number;
  passingScoreUsed: number | null;
  conservativeThresholdUsed: number | null;
  studentScore: number;
  isAccepted: boolean;
  missingScore: number | null;
};
export type MmtEvaluationListItemDto = {
  id: string;
  totalScore: number;
  admissionYear: number;
  clusterId: string;
  evaluatedAtUtc: string;
  acceptedChoicePriority: number | null;
  acceptedAdmissionProgramId: string | null;
  missingScoreForGoal: number | null;
  readinessPercentage: number | null;
  motivationalMessageKey: string;
};
export type MmtEvaluationDto = MmtEvaluationListItemDto & {
  userId: string;
  studentMmtProfileId: string;
  examSessionId: string | null;
  createdAtUtc: string;
  choices: MmtAdmissionChoiceSnapshotDto[];
};
export type EvaluationQuery = {
  userId?: string | undefined;
  studentMmtProfileId?: string | undefined;
  admissionYear?: number | undefined;
  page?: number | undefined;
  pageSize?: number | undefined;
};

export type CatalogKind = "clusters" | "universities" | "specialties";
export type CatalogDto = MmtClusterDto | UniversityDto | SpecialtyDto;
export type CatalogInput = Record<
  string,
  string | number | boolean | string[] | null
>;

export const AdmissionType = { Budget: 0, Contract: 1 } as const;
export const StudyForm = {
  FullTime: 0,
  PartTime: 1,
  Distance: 2,
  Other: 3,
} as const;
export const StudyLanguage = {
  Tajik: 0,
  Russian: 1,
  English: 2,
  Other: 3,
} as const;
export const UniversityType = { Public: 0, Private: 1, Other: 2 } as const;
export const DistributionRound = {
  Main: 0,
  Repeat: 1,
  Additional: 2,
  Other: 3,
} as const;
export const ExistingScoreMode = {
  SkipExisting: 0,
  UpdateExisting: 1,
  FailOnExisting: 2,
} as const;
